using RosSharp.RosBridgeClient;
using UnityEngine;
using Msg = Tiago.Ros;

/// <summary>
/// Receives TIAGo's depth image over rosbridge and turns it into millimetres, a texture,
/// and an unprojected point cloud — the input a Gaussian splat needs.
///
/// Topic: /xtion/depth/image_rect_raw/compressedDepth, format "16UC1; compressedDepth png".
/// Measured on this robot: ~38 kB payload per frame, against ~614 kB for the raw
/// sensor_msgs/Image — a 16x saving, which matters because the robot's control PC is the
/// scarce resource here, not the LAN.
///
/// WIRE FORMAT. compressedDepth is NOT a plain PNG. It is a 12-byte ConfigHeader
/// (int32 format, float depthParam[2]) followed by the PNG. For 16UC1 the format enum is
/// 0 = UNDEFINED and depthParam is unused, so the 16-bit samples are millimetres
/// directly — verified against a live frame. The 32FC1 variant on
/// /xtion/depth_registered/... instead uses inverse-depth quantisation and would need
/// depth = depthParam[0] / (raw - depthParam[1]); this component does not handle that.
///
/// Zero means "no return" (no stereo match, too close, too far, absorbing surface) and
/// must be discarded, not treated as distance 0. Roughly a quarter of the frame on a
/// typical scene.
///
/// THREADING. The rosbridge handler runs on the socket thread. PNG decoding is pure byte
/// work with no Unity API, so it happens there — keeping ~300k pixels of unfiltering off
/// the main thread. Only the texture upload and the cloud build run in Update.
/// </summary>
public class TiagoDepthSubscriber : MonoBehaviour
{
    [Header("Connection")]
    public RosConnector rosConnector;

    public string topic = "/xtion/depth/image_rect_raw/compressedDepth";

    [Tooltip("Minimum gap between frames, enforced by rosbridge. The robot only encodes " +
             "while something is subscribed, so this is CPU you are asking it to spend.")]
    public int throttleMilliseconds = 200;

    [Header("Intrinsics (from /xtion/depth/camera_info)")]
    [Tooltip("These are the values this robot reports. They match the RGB camera exactly, " +
             "and camera_info gives frame_id xtion_rgb_optical_frame for both, so depth and " +
             "colour share pixel coordinates.")]
    public float fx = 524.43f;
    public float fy = 523.44f;
    public float cx = 320.63f;
    public float cy = 221.70f;

    [Header("Point cloud")]
    [Tooltip("Take every Nth pixel in each axis. 1 = all 307k points, 2 = ~77k, 4 = ~19k.")]
    [Range(1, 8)] public int subsample = 2;

    [Tooltip("Discard points outside this range, in metres. The Xtion is unreliable below " +
             "~0.5 m and beyond ~4 m.")]
    public float minDepthMetres = 0.4f;
    public float maxDepthMetres = 4.0f;

    [Tooltip("Optional: sample colour per point from this camera feed. Depth and RGB are " +
             "the same 640x480 frame here, so the lookup is a direct pixel index.")]
    public TiagoCameraSubscriber colorSource;

    [Header("Output")]
    [Tooltip("Optional: R16 texture of the raw millimetre values, for shader use.")]
    public bool buildDepthTexture = false;

    [Header("Status (read-only)")]
    public float framesPerSecond;
    public int lastFrameKilobytes;
    public int validPoints;
    public int invalidPercent;

    /// <summary>Latest depth frame in millimetres, row-major from the top. 0 = no return.</summary>
    public ushort[] DepthMillimetres { get { return depth; } }
    public int Width { get { return width; } }
    public int Height { get { return height; } }

    /// <summary>Unprojected cloud, Unity camera space. Only the first `validPoints` entries are live.</summary>
    public Vector3[] Points { get { return points; } }
    public Color32[] PointColors { get { return pointColors; } }

    /// <summary>Raised on the main thread after each new cloud is built.</summary>
    public event System.Action<Vector3[], Color32[], int> CloudUpdated;

    private ushort[] depth;
    private int width, height;
    private Vector3[] points;
    private Color32[] pointColors;
    private Texture2D depthTexture;

    private bool subscribed;
    private string subscriptionId;

    // Written on the socket thread, read on the main thread; newest frame wins.
    private volatile bool pending;
    private ushort[] incoming;
    private int incomingW, incomingH, incomingBytes;
    private readonly object gate = new object();

    private int framesThisSecond;
    private float fpsWindowStart;

    private void Update()
    {
        if (!subscribed) { TrySubscribe(); return; }

        if (pending) ApplyLatestFrame();

        if (Time.time - fpsWindowStart >= 1f)
        {
            framesPerSecond = framesThisSecond / (Time.time - fpsWindowStart);
            framesThisSecond = 0;
            fpsWindowStart = Time.time;
        }
    }

    private void OnDisable()
    {
        if (!subscribed || rosConnector == null || rosConnector.RosSocket == null) return;
        // While anything is subscribed the robot keeps PNG-encoding every frame, so a
        // leaked subscription is a standing CPU cost over there.
        rosConnector.RosSocket.Unsubscribe(subscriptionId);
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (rosConnector == null || rosConnector.RosSocket == null) return;
        if (!rosConnector.IsConnected.WaitOne(0)) return;

        subscriptionId = rosConnector.RosSocket.Subscribe<Msg.CompressedImage>(
            topic, OnDepth, throttleMilliseconds, 1);
        subscribed = true;
        fpsWindowStart = Time.time;
        Debug.Log($"[TiagoDepthSubscriber] subscribed to {topic} " +
                  $"({throttleMilliseconds} ms throttle).");
    }

    // Socket thread: strip the ConfigHeader and decode. No Unity API here.
    private void OnDepth(Msg.CompressedImage message)
    {
        if (message == null || message.data == null || message.data.Length < 13) return;

        string err;
        int w, h;
        ushort[] mm = Png16Decoder.Decode(message.data, 12, out w, out h, out err);
        if (mm == null)
        {
            Debug.LogWarning("[TiagoDepthSubscriber] decode failed: " + err);
            return;
        }

        lock (gate)
        {
            incoming = mm;
            incomingW = w;
            incomingH = h;
            incomingBytes = message.data.Length;
        }
        pending = true;
    }

    private void ApplyLatestFrame()
    {
        lock (gate)
        {
            depth = incoming;
            width = incomingW;
            height = incomingH;
            lastFrameKilobytes = incomingBytes / 1024;
            incoming = null;
        }
        pending = false;
        if (depth == null) return;

        framesThisSecond++;
        if (buildDepthTexture) UploadDepthTexture();
        BuildPointCloud();
    }

    private void UploadDepthTexture()
    {
        if (depthTexture == null || depthTexture.width != width || depthTexture.height != height)
            depthTexture = new Texture2D(width, height, TextureFormat.R16, false);

        depthTexture.SetPixelData(depth, 0);
        depthTexture.Apply(false);
    }

    /// <summary>The R16 texture of raw millimetres, or null when buildDepthTexture is off.</summary>
    public Texture2D DepthTexture { get { return depthTexture; } }

    /// <summary>
    /// Unproject the depth frame into Unity camera space.
    ///
    /// ROS optical frames are X right, Y DOWN, Z forward; Unity is X right, Y UP, Z
    /// forward. Only Y flips — no handedness change is needed, because both are already
    /// Z-forward.
    /// </summary>
    private void BuildPointCloud()
    {
        int step = Mathf.Max(1, subsample);
        int capacity = (width / step + 1) * (height / step + 1);
        if (points == null || points.Length < capacity)
        {
            points = new Vector3[capacity];
            pointColors = new Color32[capacity];
        }

        Texture2D rgb = colorSource != null ? colorSource.Texture : null;
        bool sampleColor = rgb != null && rgb.width == width && rgb.height == height;
        Color32[] rgbPixels = sampleColor ? rgb.GetPixels32() : null;

        ushort minMm = (ushort)Mathf.Max(0f, minDepthMetres * 1000f);
        ushort maxMm = (ushort)Mathf.Max(0f, maxDepthMetres * 1000f);

        int n = 0, invalid = 0;
        for (int v = 0; v < height; v += step)
        {
            int row = v * width;
            for (int u = 0; u < width; u += step)
            {
                ushort mm = depth[row + u];
                // 0 is "no return", not distance zero — discarding it is mandatory.
                if (mm == 0 || mm < minMm || mm > maxMm) { invalid++; continue; }

                float z = mm * 0.001f;
                points[n] = new Vector3((u - cx) * z / fx,
                                        -(v - cy) * z / fy,
                                        z);

                if (sampleColor)
                {
                    // Texture2D rows run bottom-up; the depth image runs top-down.
                    pointColors[n] = rgbPixels[(height - 1 - v) * width + u];
                }
                else
                {
                    byte g = (byte)Mathf.Clamp(255f - (z / maxDepthMetres) * 255f, 0f, 255f);
                    pointColors[n] = new Color32(g, g, g, 255);
                }
                n++;
            }
        }

        validPoints = n;
        int total = n + invalid;
        invalidPercent = total > 0 ? (invalid * 100) / total : 0;

        if (CloudUpdated != null) CloudUpdated(points, pointColors, n);
    }
}
