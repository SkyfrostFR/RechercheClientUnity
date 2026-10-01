using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the depth cloud from TiagoDepthSubscriber as a point mesh.
///
/// This is a preview, not a Gaussian splat. It shows you that the depth, the intrinsics
/// and the unprojection are right, which is what you need working before any splatting
/// is worth attempting.
///
/// Points are in the depth camera's optical frame, so this GameObject's transform places
/// the cloud in the scene: park it on the twin's xtion frame and the cloud lands where
/// the real camera sees it.
///
/// Setup:
///   • put this on a GameObject with MeshFilter + MeshRenderer (added automatically)
///   • source   → the TiagoDepthSubscriber
///   • material → a material using Tiago/PointCloudUnlit
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TiagoPointCloudRenderer : MonoBehaviour
{
    public TiagoDepthSubscriber source;

    [Tooltip("Material using the Tiago/PointCloudUnlit shader. Created automatically if empty.")]
    public Material material;

    [Tooltip("Bounds half-extent in metres. Set explicitly because recomputing bounds over " +
             "~77k vertices every frame is pure waste, and wrong bounds get the mesh culled.")]
    public float boundsExtent = 6f;

    private Mesh mesh;
    private int[] indices;

    private void OnEnable()
    {
        var filter = GetComponent<MeshFilter>();
        var renderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh { name = "TiagoDepthCloud" };
            // 16-bit indices top out at 65535 vertices; a 640x480 frame at subsample 1 is
            // 307k, so 32-bit is mandatory here.
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.MarkDynamic();
        }
        filter.sharedMesh = mesh;

        if (material == null)
        {
            Shader s = Shader.Find("Tiago/PointCloudUnlit");
            if (s == null)
                Debug.LogError("[TiagoPointCloudRenderer] shader Tiago/PointCloudUnlit not found.");
            else
                material = new Material(s);
        }
        renderer.sharedMaterial = material;

        if (source != null) source.CloudUpdated += OnCloudUpdated;
    }

    private void OnDisable()
    {
        if (source != null) source.CloudUpdated -= OnCloudUpdated;
    }

    /// <summary>
    /// Raised from TiagoDepthSubscriber.Update, so this already runs on the main thread
    /// and may touch the mesh directly.
    /// </summary>
    private void OnCloudUpdated(Vector3[] points, Color32[] colors, int count)
    {
        if (mesh == null || count <= 0) return;

        // The index buffer is just 0..count-1 and only has to grow.
        if (indices == null || indices.Length < count)
        {
            indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;
        }

        // Clear first: leaving a longer index buffer against a shorter vertex buffer for
        // even one assignment throws.
        mesh.Clear(false);
        mesh.SetVertices(points, 0, count);
        mesh.SetColors(colors, 0, count);
        mesh.SetIndices(indices, 0, count, MeshTopology.Points, 0, false);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * boundsExtent * 2f);
    }
}
