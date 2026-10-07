using UnityEngine;

/// <summary>
/// Materials for objects created at runtime. GameObject.CreatePrimitive gives the built-in
/// Default-Material, which URP draws magenta in a player, so an opaque URP/Lit material of
/// the robot is copied instead, with its textures removed.
/// </summary>
public static class TwinMaterials
{
    private static Material template;

    public static Material Lit(Transform robotRoot, Color colour)
    {
        if (template == null) template = FindRobotMaterial(robotRoot);
        if (template == null) return null;
        var m = new Material(template);
        foreach (string tex in m.GetTexturePropertyNames()) m.SetTexture(tex, null);
        m.color = colour;
        return m;
    }

    private static Material FindRobotMaterial(Transform robotRoot)
    {
        if (robotRoot != null)
            foreach (Renderer r in robotRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material m = r.sharedMaterial;
                // Opaque only: the robot also carries translucent ghost materials.
                if (m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Lit" &&
                    (!m.HasProperty("_Surface") || m.GetFloat("_Surface") == 0f)) return m;
            }
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        return lit != null ? new Material(lit) : null;
    }
}
