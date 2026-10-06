using RosSharp.RosBridgeClient;
using UnityEngine;
using UnityEngine.InputSystem;
using Twin.Ros;

/// <summary>
/// Drives the TIAGo base from the joysticks, by publishing geometry_msgs/Twist to the
/// twist_mux "joystick" input (/joy_vel) — the same entry point the real robot's gamepad
/// uses, so twist_mux arbitration and its 0.5 s timeout (the base stops if Unity goes
/// silent) apply as on the robot.
///
///   forward / back : left thumbstick Y (Quest), left stick Y (gamepad), Up / Down arrows
///   turn           : right (or left) thumbstick X, right/left stick X (gamepad), Left / Right arrows
///
/// The XR Origin's own locomotion (move, turn, teleport...) used the same thumbsticks; it is
/// switched off, and the operator rides with the robot instead: the XR Origin is inside the
/// twin, which TwinGazeboWorld moves with the Gazebo base.
///
/// The twin itself is not moved here: Gazebo moves, and the twin follows it.
/// Installed automatically after the scene loads; no setup needed.
/// </summary>
public class TwinBaseTeleop : MonoBehaviour
{
    public string topic = "/joy_vel";

    [Tooltip("m/s at full stick.")]
    public float maxLinear = 0.4f;

    [Tooltip("rad/s at full stick.")]
    public float maxAngular = 0.8f;

    [Range(0f, 0.5f)] public float deadzone = 0.15f;
    public float publishRateHz = 20f;

    [Tooltip("Turn off the XR Origin's locomotion providers, which share the thumbsticks.")]
    public bool disableXrLocomotion = true;

    [Header("Scripted input (tests): used instead of the sticks while non-zero")]
    public Vector2 scriptedInput;

    [Header("Status (read-only)")]
    public float linear;
    public float angular;

    public RosConnector rosConnector;

    private static readonly string[] LocomotionObjects =
        { "Move", "Turn", "Teleportation", "Grab Move", "Jump", "Climb", "Climb Teleport" };

    private InputAction move, turn;
    private string publisherId;
    private float nextPublish;
    private int zerosToSend;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!TwinServerConfig.Current.baseTeleop) return;
        if (FindAnyObjectByType<TwinBaseTeleop>() != null) return;
        var connector = FindAnyObjectByType<RosConnector>();
        if (connector == null) return;

        var teleop = new GameObject("TwinBaseTeleop").AddComponent<TwinBaseTeleop>();
        teleop.rosConnector = connector;
    }

    private void Awake()
    {
        move = new InputAction("BaseMove", InputActionType.Value);
        move.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");
        move.AddBinding("<Gamepad>/leftStick");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        turn = new InputAction("BaseTurn", InputActionType.Value);
        turn.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");
        turn.AddBinding("<Gamepad>/rightStick");
    }

    private void OnEnable()
    {
        move.Enable();
        turn.Enable();
        if (disableXrLocomotion) DisableXrLocomotion();
    }

    private void OnDisable()
    {
        move.Disable();
        turn.Disable();
        // On quit the socket may already be closed; twist_mux stops the base by timeout anyway.
        if (rosConnector != null && rosConnector.IsConnected.WaitOne(0)) Publish(0f, 0f);
    }

    private void Update()
    {
        if (rosConnector == null || rosConnector.RosSocket == null || !rosConnector.IsConnected.WaitOne(0)) return;
        if (publisherId == null) publisherId = rosConnector.RosSocket.Advertise<Twist>(topic);

        Vector2 m = move.ReadValue<Vector2>();
        Vector2 t = turn.ReadValue<Vector2>();
        // Keyboard: Left/Right arrows land on the move composite's X; they turn the robot.
        float forward = Dead(m.y);
        float yaw = Dead(Mathf.Abs(t.x) > Mathf.Abs(m.x) ? t.x : m.x);
        if (scriptedInput != Vector2.zero)
        {
            forward = scriptedInput.y;
            yaw = scriptedInput.x;
        }

        // Stick right turns right: negative angular.z in ROS.
        linear = forward * maxLinear;
        angular = -yaw * maxAngular;

        if (Time.time < nextPublish) return;
        nextPublish = Time.time + 1f / Mathf.Max(1f, publishRateHz);

        if (linear != 0f || angular != 0f)
        {
            Publish(linear, angular);
            zerosToSend = 3;            // explicit stop on release, before twist_mux times out
        }
        else if (zerosToSend > 0)
        {
            Publish(0f, 0f);
            zerosToSend--;
        }
    }

    private float Dead(float v)
    {
        float a = Mathf.Abs(v);
        return a < deadzone ? 0f : Mathf.Sign(v) * (a - deadzone) / (1f - deadzone);
    }

    private void Publish(float lin, float ang)
    {
        if (publisherId == null || rosConnector == null || rosConnector.RosSocket == null) return;
        var msg = new Twist();
        msg.linear.x = lin;
        msg.angular.z = ang;
        rosConnector.RosSocket.Publish(publisherId, msg);
    }

    private static void DisableXrLocomotion()
    {
        int n = 0;
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.parent == null || t.parent.name != "Locomotion") continue;
            if (System.Array.IndexOf(LocomotionObjects, t.name) < 0) continue;
            t.gameObject.SetActive(false);
            n++;
        }
        if (n > 0)
            Debug.Log($"[TwinBaseTeleop] {n} XR locomotion provider(s) disabled: the thumbsticks drive the robot base.");
    }
}
