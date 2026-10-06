using System.Collections.Generic;
using Newtonsoft.Json;
using RosSharp.RosBridgeClient;
using UnityEngine;
using Twin.Ros;

/// <summary>
/// Brings the Gazebo world into the client, Gazebo being the reference:
///   • the test objects (table, cubes...) listed under /stageir/objects on the server
///     (ros1/stageir_sim/config/objects.yaml), read once through rosapi/get_param and drawn
///     as plain primitives, then placed every frame from /gazebo/model_states — they move
///     when the Gazebo robot pushes or carries them;
///   • the mobile base: the whole twin follows the Gazebo pose of the robot model, so
///     driving the base (TwinBaseTeleop) moves it in Unity too. See TwinRobotMover.
///
/// ANCHOR. The first robot pose received ties Gazebo's world to Unity's: the twin stays
/// where the scene put it, and the Gazebo world is placed around it.
///
/// The object visuals have no collider on purpose: the IK twin is a physical articulation
/// and would otherwise push them or be pushed by them, while only Gazebo decides where
/// they are.
///
/// Installed automatically after the scene loads; no setup needed.
/// </summary>
public class TwinGazeboWorld : MonoBehaviour
{
    [System.Serializable]
    public class ObjectSpec
    {
        public string name;
        public string shape;
        public float[] size;
        public float radius;
        public float length;
        public float[] colour;
        [JsonProperty("static")] public bool isStatic;
    }

    [Header("ROS")]
    public string modelStatesTopic = "/gazebo/model_states";
    public string objectsParam = "/stageir/objects";
    public string robotModelName = "tiago_dual";
    public int throttleMilliseconds = 33;

    [Header("Behaviour")]
    [Tooltip("Move the whole twin with the Gazebo base pose.")]
    public bool followBase = true;

    [Tooltip("Smoothing time constant for poses received at ~30 Hz, seconds. 0 = snap.")]
    public float smoothing = 0.05f;

    [Header("Status (read-only)")]
    public bool anchored;
    public int objectCount;
    public float framesPerSecond;

    public RosConnector rosConnector;
    public TwinRobotMover mover;
    public Transform World { get; private set; }

    /// <summary>Where Gazebo says the robot is, in Unity world space (valid once anchored).</summary>
    public Vector3 RobotTargetPosition { get { return World.TransformPoint(robotLocal); } }
    public float RobotTargetYaw { get { return World.eulerAngles.y + robotYaw; } }

    private readonly Dictionary<string, Transform> visuals = new Dictionary<string, Transform>();
    private bool subscribed, paramPending, paramLoaded;
    private float nextParamTry, paramRequestedAt;

    // Newest model_states frame, written on the socket thread.
    private readonly object gate = new object();
    private ModelStates latest;
    private int framesThisSecond;
    private float fpsWindowStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!TwinServerConfig.Current.gazeboWorld) return;
        if (FindAnyObjectByType<TwinGazeboWorld>() != null) return;

        TiagoDualArmRosSync sync = null;
        foreach (var s in FindObjectsByType<TiagoDualArmRosSync>(FindObjectsSortMode.InstanceID))
            if (s.enabled) { sync = s; break; }
        if (sync == null || sync.rosConnector == null)
        {
            Debug.LogWarning("[TwinGazeboWorld] no TiagoDualArmRosSync/RosConnector in the scene; " +
                             "Gazebo objects and base following not installed.");
            return;
        }

        var go = new GameObject("TwinGazeboWorld");
        var world = go.AddComponent<TwinGazeboWorld>();
        world.rosConnector = sync.rosConnector;
        world.mover = new TwinRobotMover(sync.transform.root,
                                         FindAnyObjectByType<TiagoDualArmIK>(),
                                         FindAnyObjectByType<TiagoVrTargetController>());
    }

    private void Awake()
    {
        World = new GameObject("Gazebo World").transform;
    }

    private void Update()
    {
        if (!IsConnected()) return;
        if (!subscribed) Subscribe();
        if (paramPending && Time.time - paramRequestedAt > 10f) paramPending = false;   // no answer: retry
        if (!paramLoaded && !paramPending && Time.time >= nextParamTry) RequestObjects();

        ModelStates frame;
        lock (gate) { frame = latest; latest = null; }
        if (frame != null && frame.name != null && frame.pose != null)
        {
            Store(frame);
            framesThisSecond++;
        }
        Tick();

        if (Time.time - fpsWindowStart >= 1f)
        {
            framesPerSecond = framesThisSecond / (Time.time - fpsWindowStart);
            framesThisSecond = 0;
            fpsWindowStart = Time.time;
        }
    }

    private bool IsConnected()
    {
        return rosConnector != null && rosConnector.RosSocket != null && rosConnector.IsConnected.WaitOne(0);
    }

    private void Subscribe()
    {
        rosConnector.RosSocket.Subscribe<ModelStates>(modelStatesTopic, OnModelStates, throttleMilliseconds, 1);
        subscribed = true;
        fpsWindowStart = Time.time;
    }

    // Socket thread: keep the newest frame only.
    private void OnModelStates(ModelStates message)
    {
        lock (gate) { latest = message; }
    }

    // ------------------------------------------------------------------ objects

    private void RequestObjects()
    {
        paramPending = true;
        paramRequestedAt = Time.time;
        rosConnector.RosSocket.CallService<GetParamRequest, GetParamResponse>(
            "/rosapi/get_param", OnObjectsParam, new GetParamRequest { name = objectsParam });
    }

    // Socket thread: parse only, the GameObjects are built on the main thread.
    private List<ObjectSpec> pendingSpecs;

    private void OnObjectsParam(GetParamResponse response)
    {
        List<ObjectSpec> specs = null;
        try
        {
            if (response != null && !string.IsNullOrEmpty(response.value))
                specs = JsonConvert.DeserializeObject<List<ObjectSpec>>(response.value);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TwinGazeboWorld] cannot parse {objectsParam}: {e.Message}");
        }
        // paramPending stays set until LateUpdate has consumed the answer, so Update does
        // not ask a second time in between.
        lock (gate) { pendingSpecs = specs ?? new List<ObjectSpec>(); }
    }

    private void LateUpdate()
    {
        List<ObjectSpec> specs;
        lock (gate) { specs = pendingSpecs; pendingSpecs = null; }
        if (specs == null) return;
        paramPending = false;

        if (specs.Count == 0)
        {
            // Not set (yet): the server may still be starting. Ask again later.
            nextParamTry = Time.time + 5f;
            return;
        }

        foreach (ObjectSpec spec in specs)
            if (!string.IsNullOrEmpty(spec.name) && !visuals.ContainsKey(spec.name))
                visuals[spec.name] = BuildVisual(spec);
        paramLoaded = true;
        objectCount = visuals.Count;
        Debug.Log($"[TwinGazeboWorld] {objectCount} Gazebo object(s): {string.Join(", ", visuals.Keys)}.");
    }

    private Material template;

    private Material FindRobotMaterial()
    {
        foreach (Renderer r in mover.Root.GetComponentsInChildren<Renderer>(true))
        {
            Material m = r.sharedMaterial;
            // Opaque only: the robot also carries translucent ghost materials.
            if (m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Lit" &&
                (!m.HasProperty("_Surface") || m.GetFloat("_Surface") == 0f)) return m;
        }
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        return lit != null ? new Material(lit) : null;
    }

    private Transform BuildVisual(ObjectSpec spec)
    {
        PrimitiveType type;
        Vector3 scale;
        switch (spec.shape)
        {
            case "cylinder":
                // Unity cylinder: radius 0.5, height 2 along y; ROS cylinder along z (= Unity y).
                type = PrimitiveType.Cylinder;
                scale = new Vector3(2f * spec.radius, spec.length / 2f, 2f * spec.radius);
                break;
            case "sphere":
                type = PrimitiveType.Sphere;
                scale = Vector3.one * 2f * spec.radius;
                break;
            default:
                type = PrimitiveType.Cube;
                float[] s = spec.size != null && spec.size.Length == 3 ? spec.size : new[] { 0.05f, 0.05f, 0.05f };
                scale = new Vector3(s[1], s[2], s[0]);          // ROS (x, y, z) -> Unity (y, z, x)
                break;
        }

        GameObject go = GameObject.CreatePrimitive(type);
        go.name = spec.name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(World, false);
        go.transform.localScale = scale;
        go.SetActive(false);                                     // until its first pose

        // CreatePrimitive gives the built-in Default-Material, which URP draws magenta in a
        // player: copy a material of the robot (URP/Lit) instead.
        var r = go.GetComponent<Renderer>();
        if (template == null) template = FindRobotMaterial();
        if (template != null)
        {
            var m = new Material(template);
            foreach (string tex in m.GetTexturePropertyNames()) m.SetTexture(tex, null);
            r.sharedMaterial = m;
        }
        if (spec.colour != null && spec.colour.Length >= 3)
            r.material.color = new Color(spec.colour[0], spec.colour[1], spec.colour[2]);
        return go.transform;
    }

    // --------------------------------------------------------------------- poses

    // Latest Gazebo poses, already in World-local Unity coordinates.
    private bool haveRobot;
    private Vector3 robotLocal;
    private float robotYaw;
    private readonly Dictionary<string, Twin.Ros.Pose> objectPoses = new Dictionary<string, Twin.Ros.Pose>();

    private void Store(ModelStates frame)
    {
        int n = Mathf.Min(frame.name.Length, frame.pose.Length);
        for (int i = 0; i < n; i++)
        {
            if (frame.pose[i] == null) continue;
            if (frame.name[i] == robotModelName)
            {
                robotLocal = RosFrame.ToUnity(frame.pose[i].position);
                robotYaw = RosFrame.YawDeg(frame.pose[i].orientation);
                haveRobot = true;
            }
            else if (visuals.ContainsKey(frame.name[i]))
            {
                objectPoses[frame.name[i]] = frame.pose[i];
            }
        }
    }

    /// <summary>Every frame: ease every visual towards its latest Gazebo pose.</summary>
    private void Tick()
    {
        if (!haveRobot) return;
        float k = smoothing > 0f ? 1f - Mathf.Exp(-Time.deltaTime / smoothing) : 1f;

        if (!anchored) Anchor();
        else if (followBase) FollowRobot(k);

        foreach (var kv in objectPoses)
        {
            Transform t = visuals[kv.Key];
            Vector3 p = RosFrame.ToUnity(kv.Value.position);
            Quaternion q = RosFrame.ToUnity(kv.Value.orientation);
            if (!t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
                t.SetLocalPositionAndRotation(p, q);
            }
            else
            {
                t.SetLocalPositionAndRotation(Vector3.Lerp(t.localPosition, p, k),
                                              Quaternion.Slerp(t.localRotation, q, k));
            }
        }
    }

    /// <summary>Place Gazebo's world so that the robot's Gazebo pose lands on the twin.</summary>
    private void Anchor()
    {
        Transform root = mover.Root;
        float worldYaw = root.eulerAngles.y - robotYaw;
        Quaternion r = Quaternion.Euler(0f, worldYaw, 0f);
        World.SetPositionAndRotation(root.position - r * robotLocal, r);
        anchored = true;
        Debug.Log($"[TwinGazeboWorld] anchored: Gazebo world origin at {World.position}, yaw {worldYaw:0.0}deg.");
    }

    private void FollowRobot(float k)
    {
        Transform root = mover.Root;
        Vector3 targetPos = World.TransformPoint(robotLocal);
        float targetYaw = World.eulerAngles.y + robotYaw;
        // A reset or a teleport in Gazebo is a jump, not a motion: do not smooth it.
        bool jump = (targetPos - root.position).sqrMagnitude > 1f;
        Vector3 pos = jump ? targetPos : Vector3.Lerp(root.position, targetPos, k);
        float yaw = jump ? targetYaw : Mathf.LerpAngle(root.eulerAngles.y, targetYaw, k);
        mover.MoveTo(pos, yaw);
    }
}
