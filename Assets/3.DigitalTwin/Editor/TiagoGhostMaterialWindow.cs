using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Makes every renderer under the selected GameObject(s) translucent — for turning the
/// shadow robot into a ghost.
///
/// IT DUPLICATES MATERIALS, and that is the whole point. The shadow model is a duplicate of
/// the twin, so its renderers reference the SAME material assets. Editing those in place
/// would turn the real twin translucent too. One ghost copy is made per distinct source
/// material, written to an asset folder, and shared by every renderer that used the
/// original — so the change is persistent, reversible by deleting the folder, and does not
/// break batching.
///
/// Undo works: renderer assignments go through Undo.RecordObject, so Ctrl+Z puts the
/// original materials back. The generated assets stay behind; delete the output folder.
///
/// Tools > Tiago > Ghost Materials
/// </summary>
public class TiagoGhostMaterialWindow : EditorWindow
{
    private const string DefaultFolder = "Assets/3.DigitalTwin/GhostMaterials";

    [SerializeField] private float alpha = 0.3f;
    [SerializeField] private string outputFolder = DefaultFolder;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool disableShadowCasting = true;
    [SerializeField] private bool reuseExistingGhosts = true;

    [MenuItem("Tools/Tiago/Ghost Materials")]
    public static void Open()
    {
        GetWindow<TiagoGhostMaterialWindow>("Ghost Materials").minSize = new Vector2(340, 260);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Select the root GameObject (e.g. the shadow TiagoArmsModel). Every Renderer " +
            "underneath gets a translucent copy of its material.\n\n" +
            "Materials are duplicated, never edited in place — the shadow shares material " +
            "assets with the real twin.",
            MessageType.Info);

        alpha = EditorGUILayout.Slider("Alpha", alpha, 0f, 1f);
        outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);
        includeInactive = EditorGUILayout.Toggle("Include inactive", includeInactive);
        disableShadowCasting = EditorGUILayout.Toggle("Disable shadow casting", disableShadowCasting);
        reuseExistingGhosts = EditorGUILayout.Toggle("Reuse existing ghosts", reuseExistingGhosts);

        EditorGUILayout.Space();

        GameObject[] roots = Selection.gameObjects;
        EditorGUILayout.LabelField("Selected", roots.Length == 0
            ? "nothing" : string.Format("{0} object(s)", roots.Length));

        using (new EditorGUI.DisabledScope(roots.Length == 0))
        {
            if (GUILayout.Button("Apply transparency to children", GUILayout.Height(30)))
                Apply(roots);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Ctrl+Z restores the original material assignments. The generated ghost assets " +
            "remain; delete the output folder to clean up.",
            MessageType.None);
    }

    private void Apply(GameObject[] roots)
    {
        EnsureFolder(outputFolder);

        // One ghost per distinct source material, shared across renderers.
        var ghostBySource = new Dictionary<Material, Material>();
        int renderersTouched = 0, materialsCreated = 0;

        Undo.SetCurrentGroupName("Ghost materials");
        int group = Undo.GetCurrentGroup();

        try
        {
            foreach (GameObject root in roots)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
                foreach (Renderer r in renderers)
                {
                    Material[] source = r.sharedMaterials;
                    if (source == null || source.Length == 0) continue;

                    var replaced = new Material[source.Length];
                    bool changed = false;

                    for (int i = 0; i < source.Length; i++)
                    {
                        Material src = source[i];
                        if (src == null) { replaced[i] = null; continue; }

                        // Already a ghost: retune its alpha rather than making a ghost of a
                        // ghost, which would stack _Ghost suffixes on every run.
                        if (src.name.EndsWith("_Ghost"))
                        {
                            MakeTransparent(src, alpha);
                            EditorUtility.SetDirty(src);
                            replaced[i] = src;
                            continue;
                        }

                        Material ghost;
                        if (!ghostBySource.TryGetValue(src, out ghost))
                        {
                            ghost = GetOrCreateGhost(src, ref materialsCreated);
                            ghostBySource[src] = ghost;
                        }
                        replaced[i] = ghost;
                        changed = true;
                    }

                    if (!changed && !disableShadowCasting) continue;

                    Undo.RecordObject(r, "Ghost materials");
                    r.sharedMaterials = replaced;
                    if (disableShadowCasting)
                        r.shadowCastingMode = ShadowCastingMode.Off;
                    EditorUtility.SetDirty(r);
                    renderersTouched++;
                }
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            Undo.CollapseUndoOperations(group);
        }

        Debug.Log(string.Format(
            "[GhostMaterials] {0} renderer(s) updated, {1} ghost material(s) created, " +
            "{2} distinct source material(s), alpha {3:0.00}.",
            renderersTouched, materialsCreated, ghostBySource.Count, alpha));
    }

    private Material GetOrCreateGhost(Material source, ref int created)
    {
        string path = string.Format("{0}/{1}_Ghost.mat", outputFolder, source.name);

        if (reuseExistingGhosts)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                MakeTransparent(existing, alpha);
                EditorUtility.SetDirty(existing);
                return existing;
            }
        }
        else
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);
        }

        var ghost = new Material(source) { name = source.name + "_Ghost" };
        MakeTransparent(ghost, alpha);
        AssetDatabase.CreateAsset(ghost, path);
        created++;
        return ghost;
    }

    /// <summary>
    /// Switch a material to alpha blending.
    ///
    /// URP does not infer this from the colour's alpha: the surface type, the blend factors,
    /// ZWrite, the shader keyword and the render queue all have to be set, otherwise the
    /// material still renders fully opaque. Every property is probed with HasProperty so the
    /// same code covers URP Lit / Simple Lit / Unlit and the built-in Standard shader.
    /// </summary>
    private static void MakeTransparent(Material m, float alpha)
    {
        // --- URP ---
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);     // 0 opaque, 1 transparent
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);         // 0 alpha
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);

        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        // --- built-in Standard, harmless on URP materials that lack _Mode ---
        if (m.HasProperty("_Mode"))
        {
            m.SetFloat("_Mode", 3f);                                   // Transparent
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // A translucent ghost casting solid shadows gives the illusion away immediately.
        m.SetShaderPassEnabled("ShadowCaster", false);

        foreach (string prop in new[] { "_BaseColor", "_Color" })
        {
            if (!m.HasProperty(prop)) continue;
            Color c = m.GetColor(prop);
            c.a = alpha;
            m.SetColor(prop, c);
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0];                                     // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
        AssetDatabase.Refresh();
    }
}
