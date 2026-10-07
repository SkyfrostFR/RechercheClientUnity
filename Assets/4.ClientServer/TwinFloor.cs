using UnityEngine;

/// <summary>
/// A floor under the twin. The scene has none (the brown "ground" is only the skybox), so
/// the XR Origin's gravity provider let the operator fall through the world.
///
/// A 40 m x 40 m slab with a BoxCollider, its top at the robot's base_footprint (the prefab
/// root), i.e. Gazebo's ground plane. Default layer: the XR gravity provider's ground check
/// includes it. Nothing is added if a collider already lies just below the robot, so a
/// floor added to the scene later takes over.
/// Installed automatically after the scene loads; "floor": false in twin_server.json
/// turns it off.
/// </summary>
public static class TwinFloor
{
    public const float Size = 40f;
    public const float Thickness = 0.1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!TwinServerConfig.Current.floor) return;
        var sync = Object.FindAnyObjectByType<TiagoDualArmRosSync>();
        if (sync == null) return;
        Transform root = sync.transform.root;

        // Something solid already 0..0.5 m under the base: keep it.
        RaycastHit hit;
        if (Physics.Raycast(root.position + Vector3.up * 0.05f, Vector3.down, out hit, 0.55f) &&
            !hit.collider.transform.IsChildOf(root))
            return;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Twin Floor";
        floor.transform.SetPositionAndRotation(
            new Vector3(root.position.x, root.position.y - Thickness / 2f, root.position.z), Quaternion.identity);
        floor.transform.localScale = new Vector3(Size, Thickness, Size);
        floor.isStatic = true;

        Material m = TwinMaterials.Lit(root, new Color(0.62f, 0.62f, 0.6f));
        if (m != null) floor.GetComponent<Renderer>().sharedMaterial = m;
        Debug.Log($"[TwinFloor] floor added at y={root.position.y:0.000}.");
    }
}
