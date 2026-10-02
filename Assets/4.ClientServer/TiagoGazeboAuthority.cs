using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes Gazebo the reference of the digital twin, on the display side.
///
/// The prefab holds two TiagoArmsModel instances at the same place:
///   • Digital_Twin   — driven by TiagoDualArmIK (VR targets); TiagoDualArmRosSync sends its
///                      pose to Gazebo as commands. Opaque in the prefab.
///   • Digital Shadow — driven by TiagoShadowFromRos from /joint_states, i.e. where Gazebo
///                      really is. Translucent (GhostMaterials) in the prefab.
///
/// With gazeboIsAuthority (twin_server.json, default true) their materials are exchanged at
/// runtime: the opaque robot is the one Gazebo moves, and the commanded pose becomes the
/// translucent setpoint. Nothing in the control chain changes — the IK still drives
/// Digital_Twin and its pose still goes to Gazebo — so the opaque robot only moves when
/// Gazebo does. The prefab and the twin scripts are left untouched.
///
/// Installed automatically after the scene loads; no setup needed.
/// </summary>
public class TiagoGazeboAuthority : MonoBehaviour
{
    [Tooltip("Hide the commanded (IK) robot instead of drawing it translucent.")]
    public bool hideCommandedRobot = false;

    [Header("Status (read-only)")]
    public bool swapped;
    public int swappedRenderers;

    private TiagoArmsModel commandedModel, gazeboModel;
    private float giveUpAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!TwinServerConfig.Current.gazeboIsAuthority) return;

        var shadow = FindAnyObjectByType<TiagoShadowFromRos>();
        var sync = FindAnyObjectByType<TiagoDualArmRosSync>();
        if (shadow == null || sync == null)
        {
            Debug.LogWarning("[TiagoGazeboAuthority] no TiagoShadowFromRos / TiagoDualArmRosSync " +
                             "in the scene; Gazebo-as-reference display not installed.");
            return;
        }
        if (FindAnyObjectByType<TiagoGazeboAuthority>() != null) return;

        var authority = shadow.gameObject.AddComponent<TiagoGazeboAuthority>();
        authority.gazeboModel = shadow.shadowModel != null
            ? shadow.shadowModel : shadow.GetComponent<TiagoArmsModel>();
        authority.commandedModel = sync.model != null
            ? sync.model : sync.GetComponent<TiagoArmsModel>();
    }

    private void Start() { giveUpAt = Time.time + 30f; }

    // The joint hierarchy (and its renderers) is only built once TiagoArmsModel has run, so
    // retry each frame until both robots exist.
    private void Update()
    {
        if (swapped) { enabled = false; return; }
        if (gazeboModel == null || commandedModel == null || gazeboModel == commandedModel)
        {
            Debug.LogError("[TiagoGazeboAuthority] the commanded and Gazebo robots must be two " +
                           "different TiagoArmsModel instances.");
            enabled = false;
            return;
        }
        if (gazeboModel.robot == null || commandedModel.robot == null) return;

        if (TrySwap()) return;
        if (Time.time > giveUpAt)
        {
            Debug.LogError("[TiagoGazeboAuthority] robot renderers never matched; display unchanged.");
            enabled = false;
        }
    }

    /// <summary>
    /// Exchange sharedMaterials between renderers at the same path under each model. Both
    /// models come from the same generated code, so their hierarchies are identical.
    /// </summary>
    private bool TrySwap()
    {
        Dictionary<string, Renderer> gazeboByPath = RenderersByPath(gazeboModel.transform);
        Dictionary<string, Renderer> commandedByPath = RenderersByPath(commandedModel.transform);
        if (gazeboByPath.Count == 0 || gazeboByPath.Count != commandedByPath.Count) return false;

        int count = 0;
        foreach (KeyValuePair<string, Renderer> pair in gazeboByPath)
        {
            Renderer commanded;
            if (!commandedByPath.TryGetValue(pair.Key, out commanded)) return false;

            Material[] opaque = commanded.sharedMaterials;
            commanded.sharedMaterials = pair.Value.sharedMaterials;
            pair.Value.sharedMaterials = opaque;
            if (hideCommandedRobot) commanded.enabled = false;
            count++;
        }

        swappedRenderers = count;
        swapped = true;
        Debug.Log($"[TiagoGazeboAuthority] Gazebo is the reference: the /joint_states robot " +
                  $"is now opaque, the IK robot the " +
                  $"{(hideCommandedRobot ? "hidden" : "translucent")} setpoint ({count} renderers).");
        return true;
    }

    private static Dictionary<string, Renderer> RenderersByPath(Transform root)
    {
        var result = new Dictionary<string, Renderer>();
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            string path = RelativePath(root, r.transform);
            // Same-named siblings would collide; suffix keeps them distinct in a stable order.
            string key = path;
            for (int i = 1; result.ContainsKey(key); i++) key = path + "#" + i;
            result[key] = r;
        }
        return result;
    }

    private static string RelativePath(Transform root, Transform t)
    {
        var parts = new List<string>();
        for (Transform c = t; c != null && c != root; c = c.parent) parts.Add(c.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}
