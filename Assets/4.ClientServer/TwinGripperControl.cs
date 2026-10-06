using RosSharp.RosBridgeClient;
using UnityEngine;
using UnityEngine.InputSystem;
using Twin.Ros;
using Msg = Tiago.Ros;

/// <summary>
/// Opens and closes both grippers:
///   Quest   : hold the trigger of a hand to close that hand's gripper (grip still moves the arm)
///   gamepad : hold LT / RT
///   keyboard: G toggles the left gripper, H the right one
///
/// Closing calls the PAL grasp service (/parallel_gripper_&lt;side&gt;_controller/grasp), which
/// closes until the fingers meet the object and then holds it without crushing it.
/// Opening calls release, then sends both fingers fully open through
/// /gripper_&lt;side&gt;_controller/command. (The parallel controller's own command topic is
/// not acted upon in the simulation.) Gazebo is the reference: the fingers seen in Unity
/// are those of /joint_states (TiagoShadowFromRos), so they stop on the object as in Gazebo.
/// Installed automatically after the scene loads; no setup needed.
/// </summary>
public class TwinGripperControl : MonoBehaviour
{
    [System.Serializable]
    public class Side
    {
        public string name = "left";

        [Header("Status (read-only)")]
        public bool closed;

        [HideInInspector] public bool toggled, sent, lastSent;
        [HideInInspector] public InputAction hold;
        [HideInInspector] public InputAction toggle;
        [HideInInspector] public string publisherId;

        public string Topic { get { return $"/gripper_{name}_controller/command"; } }
        public string[] Joints
        {
            get { return new[] { $"gripper_{name}_right_finger_joint", $"gripper_{name}_left_finger_joint" }; }
        }
        public string GraspService { get { return $"/parallel_gripper_{name}_controller/grasp"; } }
        public string ReleaseService { get { return $"/parallel_gripper_{name}_controller/release"; } }
    }

    [Tooltip("Each finger's opening when open, metres (PAL gripper: 0.045).")]
    public float openFinger = 0.045f;

    public float moveSeconds = 0.8f;

    public Side left = new Side { name = "left" };
    public Side right = new Side { name = "right" };

    [Header("Scripted command (tests): -1 = inputs, 0 = open, 1 = closed")]
    public int scriptedLeft = -1;
    public int scriptedRight = -1;

    public RosConnector rosConnector;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!TwinServerConfig.Current.gripperControl) return;
        if (FindAnyObjectByType<TwinGripperControl>() != null) return;
        var connector = FindAnyObjectByType<RosConnector>();
        if (connector == null) return;

        var control = new GameObject("TwinGripperControl").AddComponent<TwinGripperControl>();
        control.rosConnector = connector;
    }

    private void Awake()
    {
        Bind(left, "LeftHand", "leftTrigger", "g");
        Bind(right, "RightHand", "rightTrigger", "h");
    }

    private static void Bind(Side s, string hand, string padTrigger, string key)
    {
        s.hold = new InputAction(s.name + "GripperHold", InputActionType.Value);
        s.hold.AddBinding($"<XRController>{{{hand}}}/trigger");
        s.hold.AddBinding($"<Gamepad>/{padTrigger}");
        s.toggle = new InputAction(s.name + "GripperToggle", InputActionType.Button, $"<Keyboard>/{key}");
    }

    private void OnEnable()
    {
        foreach (Side s in new[] { left, right }) { s.hold.Enable(); s.toggle.Enable(); }
    }

    private void OnDisable()
    {
        foreach (Side s in new[] { left, right }) { s.hold.Disable(); s.toggle.Disable(); }
    }

    private void Update()
    {
        if (rosConnector == null || rosConnector.RosSocket == null || !rosConnector.IsConnected.WaitOne(0)) return;
        Tick(left, scriptedLeft);
        Tick(right, scriptedRight);
    }

    private void Tick(Side s, int scripted)
    {
        if (s.publisherId == null)
            s.publisherId = rosConnector.RosSocket.Advertise<Msg.JointTrajectory>(s.Topic);

        if (s.toggle.WasPressedThisFrame()) s.toggled = !s.toggled;

        // Hysteresis on the analog trigger, so a half-pressed trigger does not chatter.
        float v = s.hold.ReadValue<float>();
        bool held = s.closed && !s.toggled ? v > 0.3f : v > 0.6f;

        s.closed = scripted >= 0 ? scripted == 1 : held || s.toggled;
        // Nothing is sent until the first change: the gripper starts as Gazebo has it.
        if (!s.sent) { s.sent = true; s.lastSent = s.closed; return; }
        if (s.closed == s.lastSent) return;
        s.lastSent = s.closed;

        if (s.closed)
        {
            rosConnector.RosSocket.CallService<EmptyRequest, EmptyResponse>(s.GraspService, _ => { }, new EmptyRequest());
            return;
        }
        rosConnector.RosSocket.CallService<EmptyRequest, EmptyResponse>(s.ReleaseService, _ => { }, new EmptyRequest());
        rosConnector.RosSocket.Publish(s.publisherId, new Msg.JointTrajectory
        {
            joint_names = s.Joints,
            points = new[]
            {
                new Msg.JointTrajectoryPoint
                {
                    positions = new double[] { openFinger, openFinger },
                    velocities = new double[] { 0.0, 0.0 },
                    time_from_start = Msg.Duration.FromSeconds(moveSeconds)
                }
            }
        });
    }
}
