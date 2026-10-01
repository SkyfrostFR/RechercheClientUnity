using RosSharp.RosBridgeClient;
using UnityEngine;
using UnityEngine.UI;
using Msg = Tiago.Ros;

/// <summary>
/// Receives TIAGo's head camera as JPEG over rosbridge and puts it on a Texture2D.
///
/// The robot has no web_video_server, so the only route is the compressed image topic.
/// Raw sensor_msgs/Image is not an option here: 640x480x3 would be ~1.2 MB per frame
/// before base64, against ~53 kB for the JPEG.
///
/// Measured on this robot: /xtion/rgb/image_raw/compressed is ~53 kB per frame,
/// /xtion/rgb/image_rect_color/compressed ~66 kB. Base64 adds a third on the wire.
///
/// THROTTLING MATTERS. image_transport only compresses while something is subscribed,
/// so this subscription costs CPU on the robot's control PC — which has been running a
/// high load average and previously hit its file-descriptor ceiling. throttleMilliseconds
/// caps the rate at the rosbridge end, so the robot never produces frames Unity is not
/// going to show. Leave it at 100 ms (10 Hz) unless you actually need more.
///
/// THREADING. The rosbridge handler runs on the socket thread, where no Unity API may be
/// touched. It only stashes the JPEG bytes; the decode and the texture upload happen in
/// Update on the main thread, and only the newest frame is kept.
///
/// Setup (Inspector):
///   • rosConnector → a ROS# RosConnector (ws://&lt;robot-ip&gt;:9090)
///   • targetRawImage and/or targetRenderer → where to show it. Both optional; the
///     texture is also exposed as `Texture` so you can wire it anywhere yourself.
/// </summary>
public class TiagoCameraSubscriber : MonoBehaviour
{
    [Header("Connection")]
    public RosConnector rosConnector;

    [Tooltip("Compressed image topic. image_rect_color is undistorted but slightly larger.")]
    public string topic = "/xtion/rgb/image_raw/compressed";

    [Tooltip("Minimum gap between frames, enforced by rosbridge. 100 ms = 10 Hz. " +
             "Lower values cost CPU on the robot, which is already loaded.")]
    public int throttleMilliseconds = 100;

    [Header("Output")]
    public RawImage targetRawImage;

    [Tooltip("Optional: the texture is also assigned to this renderer's material.")]
    public Renderer targetRenderer;

    [Tooltip("Fallback if colours come out inverted. Costs a full pixel pass per frame, " +
             "so leave it off unless you actually see red and blue swapped.")]
    public bool swapRedBlue = false;

    [Header("Status (read-only)")]
    public float framesPerSecond;
    public int lastFrameKilobytes;

    /// <summary>The live camera texture. Null until the first frame arrives.</summary>
    public Texture2D Texture { get { return texture; } }

    private Texture2D texture;
    private bool subscribed;
    private string subscriptionId;

    // Written on the socket thread, read on the main thread. `pending` is the handoff
    // flag; only the newest frame survives, older ones are simply overwritten.
    private volatile bool pending;
    private byte[] latestJpeg;
    private readonly object jpegLock = new object();

    private int framesThisSecond;
    private float fpsWindowStart;

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribe();
            return;
        }

        if (pending) ApplyLatestFrame();

        // Rolling 1 s window, so the Inspector shows the rate actually arriving.
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
        // Unsubscribe explicitly: while anything is subscribed the robot keeps JPEG-encoding
        // every frame, so a leaked subscription is a standing CPU cost over there.
        rosConnector.RosSocket.Unsubscribe(subscriptionId);
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (rosConnector == null || rosConnector.RosSocket == null) return;
        if (!rosConnector.IsConnected.WaitOne(0)) return;

        subscriptionId = rosConnector.RosSocket.Subscribe<Msg.CompressedImage>(
            topic,
            OnImage,
            throttleMilliseconds,
            1);          // queue_length 1: a stale frame is worthless, drop it
        subscribed = true;
        fpsWindowStart = Time.time;
        Debug.Log($"[TiagoCameraSubscriber] subscribed to {topic} " +
                  $"({throttleMilliseconds} ms throttle).");
    }

    // Socket thread. No Unity API here.
    private void OnImage(Msg.CompressedImage message)
    {
        if (message == null || message.data == null || message.data.Length == 0) return;
        lock (jpegLock) { latestJpeg = message.data; }
        pending = true;
    }

    private void ApplyLatestFrame()
    {
        byte[] jpeg;
        lock (jpegLock)
        {
            jpeg = latestJpeg;
            latestJpeg = null;
        }
        pending = false;
        if (jpeg == null) return;

        if (texture == null)
        {
            // Size and format are replaced wholesale by LoadImage on the first frame.
            texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (targetRawImage != null) targetRawImage.texture = texture;
            if (targetRenderer != null) targetRenderer.material.mainTexture = texture;
        }

        if (!texture.LoadImage(jpeg, markNonReadable: false))
        {
            Debug.LogWarning("[TiagoCameraSubscriber] JPEG decode failed.");
            return;
        }

        if (swapRedBlue)
        {
            Color32[] px = texture.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                byte r = px[i].r;
                px[i].r = px[i].b;
                px[i].b = r;
            }
            texture.SetPixels32(px);
            texture.Apply(false);
        }

        lastFrameKilobytes = jpeg.Length / 1024;
        framesThisSecond++;
    }
}
