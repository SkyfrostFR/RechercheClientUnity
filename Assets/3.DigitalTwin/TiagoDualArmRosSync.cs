using System.Collections.Generic;
using DT.Tools;
using RosSharp.RosBridgeClient;
using UnityEngine;
using Msg = Tiago.Ros;

/// <summary>
/// Mirrors BOTH arms of the digital twin onto the real TIAGo over rosbridge (ROS#).
///
/// Reads the twin's MEASURED state — BaseRobot.jointsActualPosition and
/// jointsActualVelocity — never the command arrays, so what leaves Unity is where the
/// simulated arms actually are, not where they were told to go.
///
/// Both arms are handled together on purpose. Each arm needs its own controller, but
/// controller_manager/switch_controller takes lists, so one call starts both and stops
/// everything else atomically. Two independent components would issue two concurrent
/// switches and could interleave badly.
///
/// Startup:
///   1. wait for the ROS# connection and for TiagoArmsModel to have built its robot
///   2. IMMEDIATELY send BaseRobot.homePosition to the real arms, so they home at the
///      same time as the twin rather than after it
///   3. hand over to the IK and start following once the move has had time to finish
///
/// FOLLOW MODES, applied to both arms at once:
///   Position → arm_&lt;side&gt;_controller                  (trajectory_msgs/JointTrajectory)
///   Velocity → arm_&lt;side&gt;_forward_velocity_controller  (std_msgs/Float64MultiArray)
///   Off      → publishes nothing; leaves controllers as they are
///
/// `mode` is live — change it in the Inspector during Play and the switch happens.
///
/// UNITS: the twin stores S_act in degrees and dS_act in °/s (see Axe.McInfo), while
/// ros_control is radians and rad/s throughout. Everything published goes through
/// Mathf.Deg2Rad; sending the raw arrays would be wrong by 57x.
///
/// DEADMAN: JointGroupVelocityController has no command timeout — it re-applies its
/// last velocity every cycle, forever. Zeros are published for every arm on each exit
/// from Velocity mode, on disable and on quit. Do not remove.
/// </summary>
public class TiagoDualArmRosSync : MonoBehaviour
{
    public enum FollowMode { Off, Position, Velocity }

    private enum Phase { WaitingForRobot, Homing, Following }

    /// <summary>
    /// One arm's slice and its runtime state. jointStartIndex defaults match
    /// TiagoDualArmIK: InitRobot drops the Fixed joints, so the right arm starts at 9,
    /// not 12.
    /// </summary>
    [System.Serializable]
    public class Arm
    {
        [Tooltip("Used to build controller and joint names: arm_<name>_controller, " +
                 "arm_<name>_1_joint, ...")]
        public string name = "right";

        [Tooltip("Uncheck to leave this arm alone entirely.")]
        public bool enabled = true;

        [Tooltip("First index of this arm inside jointsActualPosition / jointsActualVelocity.")]
        public int jointStartIndex = 9;

        public int dof = 7;

        [HideInInspector] public string[] jointNames;
        [HideInInspector] public double[] limits;        // per-joint |dS_max|, rad/s
        [HideInInspector] public string posPublisherId, velPublisherId;
        [HideInInspector] public bool velocityWasLive;

        public string PosController { get { return "arm_" + name + "_controller"; } }
        public string VelController { get { return "arm_" + name + "_forward_velocity_controller"; } }

        /// <summary>Every controller that could be holding this arm.</summary>
        public string[] AllControllers
        {
            get
            {
                return new[]
                {
                    PosController, VelController,
                    "arm_" + name + "_velocity_controller",
                    "arm_" + name + "_impedance_controller"
                };
            }
        }
    }

    [Header("Connections")]
    [Tooltip("ROS# connector. May live on any GameObject.")]
    public RosConnector rosConnector;

    [Tooltip("The model that owns the BaseRobot. GetComponent is tried if empty.")]
    public TiagoArmsModel model;

    [Header("Arms")]
    public Arm leftArm = new Arm { name = "left", jointStartIndex = 0, dof = 7 };
    public Arm rightArm = new Arm { name = "right", jointStartIndex = 9, dof = 7 };

    [Header("Following")]
    [Tooltip("What both arms follow. Changeable during Play.")]
    public FollowMode mode = FollowMode.Off;

    [Tooltip("Call controller_manager/switch_controller when the mode needs different " +
             "controllers. Turn off to manage controllers yourself.")]
    public bool autoSwitchController = true;

    public float publishRateHz = 20f;

    [Tooltip("Position mode: time_from_start per point. Must exceed the publish period.")]
    public float positionLookaheadSeconds = 0.15f;

    [Tooltip("Velocity mode: safety cap in rad/s, on top of each joint's own dS_max.")]
    public float maxSpeed = 0.35f;

    [Header("Homing at startup")]
    [Tooltip("Send BaseRobot.homePosition to the real arms as soon as the connection is up, " +
             "so they home in parallel with the twin. Uncheck to start straight in Following.")]
    public bool homeOnStart = true;

    [Tooltip("time_from_start for the real arms' move to home. This runs CONCURRENTLY with " +
             "the twin's own homing, so it is not added on top of it.")]
    public float homeMoveSeconds = 5f;

    [Tooltip("Also wait for the twin to actually reach home before handing over to the IK. " +
             "Costs nothing when the twin gets there first.")]
    public bool waitForTwinAtHome = true;

    [Tooltip("Per-joint tolerance, in degrees, for calling the twin 'at home'.")]
    public float homeToleranceDeg = 1.0f;

    [Tooltip("Twin is settled when all its joints are slower than this, in °/s.")]
    public float settleSpeedDegPerSec = 0.5f;

    [Header("Once home is reached")]
    [Tooltip("TiagoDualArmIK whose followTarget flags are switched on. Optional.")]
    public TiagoDualArmIK armIK;

    [Tooltip("Tick followTarget on the IK for every enabled arm. The IK spawns its handle at " +
             "the current end-effector pose, so the twin starts at zero error and will not jump.")]
    public bool enableFollowTargetAfterHome = true;

    [Tooltip("Mode to fall into once home is reached. Off leaves the mode untouched.")]
    public FollowMode modeAfterHome = FollowMode.Velocity;

    private BaseRobot robot;
    private readonly List<Arm> arms = new List<Arm>();
    private Phase phase = Phase.WaitingForRobot;
    private FollowMode appliedMode = FollowMode.Off;
    private float nextPublish, homeMoveDoneAt;
    private bool homeSent;

    // Written by the rosbridge socket thread in the service callback, read on the main
    // thread. No Unity API is touched in the callback.
    private volatile bool switchPending, switchDone, switchOk;
    private FollowMode switchTargetMode = FollowMode.Off;

    // ------------------------------------------------------------------ lifecycle

    private void Update()
    {
        if (!EnsureReady()) return;

        if (switchPending)
        {
            HandleSwitchResult();
            return;                      // never publish while ownership is in flux
        }

        switch (phase)
        {
            case Phase.Homing:
                TickHoming();
                break;

            case Phase.Following:
                TickFollowing();
                break;
        }
    }

    private void OnDisable() { StopAllVelocities(); }

    private void OnApplicationQuit() { StopAllVelocities(); }

    // ----------------------------------------------------------------------- init

    private bool EnsureReady()
    {
        if (phase != Phase.WaitingForRobot) return true;

        if (model == null) model = GetComponent<TiagoArmsModel>();
        if (model == null || model.robot == null || model.robot.joints == null) return false;
        if (rosConnector == null || rosConnector.RosSocket == null) return false;
        if (!rosConnector.IsConnected.WaitOne(0)) return false;

        robot = model.robot;

        arms.Clear();
        foreach (Arm a in new[] { leftArm, rightArm })
        {
            if (!a.enabled) continue;
            if (!InitArm(a)) { enabled = false; return false; }
            arms.Add(a);
        }

        if (arms.Count == 0)
        {
            Debug.LogWarning("[TiagoDualArmRosSync] no arm enabled; nothing to do.");
            enabled = false;
            return false;
        }

        phase = homeOnStart ? Phase.Homing : Phase.Following;
        return true;
    }

    private bool InitArm(Arm a)
    {
        int need = a.jointStartIndex + a.dof;
        if (robot.jointsActualPosition == null || robot.jointsActualPosition.Length < need)
        {
            Debug.LogError($"[TiagoDualArmRosSync] jointsActualPosition has " +
                           $"{robot.jointsActualPosition?.Length ?? 0} entries; {a.name} arm " +
                           $"needs {a.jointStartIndex}..{need - 1}.");
            return false;
        }

        a.jointNames = new string[a.dof];
        a.limits = new double[a.dof];
        for (int i = 0; i < a.dof; i++)
        {
            a.jointNames[i] = $"arm_{a.name}_{i + 1}_joint";
            // dS_max is stored in rad/s for the revolute arm joints, so this stays rad/s.
            a.limits[i] = System.Math.Abs(
                robot.joints[a.jointStartIndex + i].Param.dS_max.ConvertToSI());
        }

        a.posPublisherId = rosConnector.RosSocket.Advertise<Msg.JointTrajectory>(
            $"/{a.PosController}/command");
        a.velPublisherId = rosConnector.RosSocket.Advertise<Msg.Float64MultiArray>(
            $"/{a.VelController}/command");
        return true;
    }

    // --------------------------------------------------------------------- homing

    /// <summary>
    /// The real arms are sent to homePosition — the same target the twin is driving to —
    /// as soon as the link is up, so both home concurrently. Nothing is streamed while
    /// that trajectory runs; the controller is executing it on the robot.
    /// </summary>
    private void TickHoming()
    {
        if (!homeSent)
        {
            // A position command needs the position controllers to own the arms first.
            // RequestMode returns false while the switch is in flight.
            if (!RequestMode(FollowMode.Position)) return;

            foreach (Arm a in arms) SendPose(a, robot.homePosition, homeMoveSeconds, false);
            homeSent = true;
            appliedMode = FollowMode.Position;
            homeMoveDoneAt = Time.time + homeMoveSeconds;
            Debug.Log($"[TiagoDualArmRosSync] homing {arms.Count} real arm(s) alongside the " +
                      $"twin ({homeMoveSeconds:0.#}s).");
            return;
        }

        // Following with a 0.15 s lookahead while an arm is still far from the twin would
        // command a step it could only chase at maximum speed, so wait out the move.
        if (Time.time < homeMoveDoneAt) return;

        // Handing over to the IK before the twin itself is home would let the solver fight
        // the twin's own homing motion.
        if (waitForTwinAtHome)
            foreach (Arm a in arms)
                if (!ArmIsAtHome(a)) return;

        OnHomeReached();
        phase = Phase.Following;
    }

    /// <summary>
    /// True when every joint of this arm is within tolerance of its home value AND barely
    /// moving. Both checks matter: position alone would pass while the twin is still
    /// swinging through the home pose on its way past it.
    /// </summary>
    private bool ArmIsAtHome(Arm a)
    {
        if (robot.homePosition == null ||
            robot.homePosition.Length < a.jointStartIndex + a.dof) return false;

        for (int i = 0; i < a.dof; i++)
        {
            int j = a.jointStartIndex + i;
            // homePosition and jointsActualPosition are both in the joint's own unit
            // (degrees for these revolute joints), so they compare directly.
            if (Mathf.Abs(robot.jointsActualPosition[j] - robot.homePosition[j]) > homeToleranceDeg)
                return false;
            if (Mathf.Abs(robot.jointsActualVelocity[j]) > settleSpeedDegPerSec)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Runs once, when homing is done: hand the twin over to the IK and drop into the
    /// follow mode we actually want to run in.
    ///
    /// Order matters. The IK is enabled first: TiagoDualArmIK spawns its target handle at
    /// the current end-effector pose, so the solver starts at zero Cartesian error and the
    /// twin does not jump. Only then do we change mode, so the first velocities leaving
    /// Unity are the ones the IK produces, not a transient.
    /// </summary>
    private void OnHomeReached()
    {
        if (armIK != null && enableFollowTargetAfterHome)
        {
            if (leftArm.enabled && armIK.leftArm != null) armIK.leftArm.followTarget = true;
            if (rightArm.enabled && armIK.rightArm != null) armIK.rightArm.followTarget = true;
            Debug.Log("[TiagoDualArmRosSync] home reached; IK followTarget enabled.");
        }

        if (modeAfterHome != FollowMode.Off)
        {
            mode = modeAfterHome;
            Debug.Log($"[TiagoDualArmRosSync] home reached; switching to {modeAfterHome} mode.");
        }
    }

    // ---------------------------------------------------------------- following

    private void TickFollowing()
    {
        if (mode != appliedMode && !ApplyModeChange()) return;

        if (Time.time < nextPublish) return;
        nextPublish = Time.time + 1f / Mathf.Max(1f, publishRateHz);

        foreach (Arm a in arms)
        {
            if (appliedMode == FollowMode.Position)
                SendPose(a, robot.jointsActualPosition, positionLookaheadSeconds, true);
            else if (appliedMode == FollowMode.Velocity)
                SendVelocities(a);
        }
    }

    /// <summary>Apply a mode change to both arms. Returns true when the new mode is live.</summary>
    private bool ApplyModeChange()
    {
        // Leaving Velocity must cancel the standing commands before ownership moves.
        if (appliedMode == FollowMode.Velocity) StopAllVelocities();

        if (mode == FollowMode.Off)
        {
            appliedMode = FollowMode.Off;
            return false;
        }

        if (!RequestMode(mode)) return false;

        appliedMode = mode;
        Debug.Log($"[TiagoDualArmRosSync] {arms.Count} arm(s) now following in {mode} mode.");
        return true;
    }

    /// <summary>
    /// Ensure the controllers for `want` own every enabled arm. Returns true if they
    /// already do; otherwise starts ONE async switch covering both arms and returns
    /// false, so nothing is published this frame.
    /// </summary>
    private bool RequestMode(FollowMode want)
    {
        if (!autoSwitchController) return true;          // caller manages controllers
        if (switchTargetMode == want && switchDone && switchOk) return true;
        if (switchPending) return false;

        var start = new List<string>();
        var stop = new List<string>();
        foreach (Arm a in arms)
        {
            string target = want == FollowMode.Position ? a.PosController : a.VelController;
            start.Add(target);
            foreach (string c in a.AllControllers)
                if (c != target) stop.Add(c);
        }

        switchTargetMode = want;
        switchDone = false;
        switchOk = false;
        switchPending = true;

        rosConnector.RosSocket.CallService<Msg.SwitchControllerRequest, Msg.SwitchControllerResponse>(
            "/controller_manager/switch_controller",
            OnSwitchResponse,
            new Msg.SwitchControllerRequest
            {
                start_controllers = start.ToArray(),
                stop_controllers = stop.ToArray(),
                // BEST_EFFORT: we list every controller that could be holding an arm
                // without knowing which one is, and STRICT fails the whole call if asked
                // to stop one that is not running.
                strictness = Msg.SwitchControllerRequest.BEST_EFFORT
            });
        return false;
    }

    // Socket thread. Flags only — no Unity API here.
    private void OnSwitchResponse(Msg.SwitchControllerResponse response)
    {
        switchOk = response != null && response.ok;
        switchDone = true;
    }

    private void HandleSwitchResult()
    {
        if (!switchDone) return;
        switchPending = false;

        if (switchOk)
        {
            Debug.Log($"[TiagoDualArmRosSync] controller switch ok for {switchTargetMode} mode.");
        }
        else
        {
            Debug.LogError($"[TiagoDualArmRosSync] switch for {switchTargetMode} returned " +
                           "ok=false; dropping to Off. Are all four controllers loaded?");
            mode = FollowMode.Off;
            appliedMode = FollowMode.Off;
            switchTargetMode = FollowMode.Off;
        }
    }

    // ---------------------------------------------------------------- publishing

    /// <summary>
    /// Send one arm's slice of <paramref name="sourceDeg"/> as a single-point
    /// JointTrajectory, in radians. Used both for the home pose (homePosition) and for
    /// live following (jointsActualPosition).
    /// </summary>
    private void SendPose(Arm a, float[] sourceDeg, float seconds, bool includeVelocities)
    {
        if (sourceDeg == null || sourceDeg.Length < a.jointStartIndex + a.dof) return;

        double[] q = new double[a.dof];
        double[] qd = new double[a.dof];
        for (int i = 0; i < a.dof; i++)
        {
            q[i] = sourceDeg[a.jointStartIndex + i] * Mathf.Deg2Rad;
            // Feed-forward velocity lets the controller blend consecutive points instead
            // of decelerating to a stop at each one.
            qd[i] = includeVelocities
                  ? robot.jointsActualVelocity[a.jointStartIndex + i] * Mathf.Deg2Rad
                  : 0.0;
        }

        rosConnector.RosSocket.Publish(a.posPublisherId, new Msg.JointTrajectory
        {
            joint_names = a.jointNames,
            points = new[]
            {
                new Msg.JointTrajectoryPoint
                {
                    positions = q,
                    velocities = qd,
                    time_from_start = Msg.Duration.FromSeconds(seconds)
                }
            }
        });
    }

    private void SendVelocities(Arm a)
    {
        double[] data = new double[a.dof];
        for (int i = 0; i < a.dof; i++)
        {
            // °/s (twin) -> rad/s (ros_control), clamped by the joint's own dS_max and
            // the Inspector cap, whichever is tighter.
            double v = robot.jointsActualVelocity[a.jointStartIndex + i] * Mathf.Deg2Rad;
            double cap = System.Math.Min(a.limits[i], System.Math.Abs(maxSpeed));
            data[i] = System.Math.Max(-cap, System.Math.Min(cap, v));
        }
        PublishVelocities(a, data);
        a.velocityWasLive = true;
    }

    private void StopAllVelocities()
    {
        foreach (Arm a in arms)
        {
            if (!a.velocityWasLive) continue;
            // Repeated because a single dropped message would leave the arm driving.
            double[] zeros = new double[a.dof];
            for (int n = 0; n < 3; n++) PublishVelocities(a, zeros);
            a.velocityWasLive = false;
        }
    }

    private void PublishVelocities(Arm a, double[] values)
    {
        if (rosConnector == null || rosConnector.RosSocket == null ||
            a.velPublisherId == null) return;
        rosConnector.RosSocket.Publish(a.velPublisherId,
            new Msg.Float64MultiArray { data = (double[])values.Clone() });
    }
}
