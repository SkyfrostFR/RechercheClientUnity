using RosSharp.RosBridgeClient;
using UnityEngine;
using Msg = Tiago.Ros;

/// <summary>
/// Direct pan/tilt control of TIAGo's head.
///
/// Publishes trajectory_msgs/JointTrajectory to /head_controller/command with explicit
/// head_1_joint (pan) and head_2_joint (tilt) angles. No controller switch is needed:
/// head_controller is a position JointTrajectoryController and is already running.
///
/// Limits below are the robot's own, read from its URDF:
///   head_1_joint  -75 deg .. +75 deg   (velocity 3.0 rad/s)
///   head_2_joint  -60 deg .. +45 deg   (velocity 3.0 rad/s)
/// The tilt range is asymmetric — the head looks further down than up. Commands are
/// clamped here as well as on the robot, so a slider can never ask for the impossible.
///
/// PAL'S HEAD MANAGER WILL FIGHT YOU. pal_head_manager runs idle head behaviours and will
/// steal the head back. It is disabled through its own action, whose goal carries a
/// duration in seconds, so the disable lapses unless refreshed. This component refreshes
/// it at half that duration — which also means the robot's normal head behaviour returns
/// by itself if Unity stops. Nothing is left permanently disabled on a shared robot.
///
/// Drive it from the Inspector sliders, or from your own code via SetAngles().
/// </summary>
public class TiagoHeadController : MonoBehaviour
{
    // From the robot's URDF. Kept as constants so the Range attributes and the runtime
    // clamp can never drift apart.
    public const float PanMinDeg = -75f, PanMaxDeg = 75f;
    public const float TiltMinDeg = -60f, TiltMaxDeg = 45f;

    [Header("Connection")]
    public RosConnector rosConnector;

    [Header("Head angles (degrees)")]
    [Tooltip("head_1_joint. Positive turns the head to the robot's left.")]
    [Range(PanMinDeg, PanMaxDeg)] public float pan = 0f;

    [Tooltip("head_2_joint. Negative looks DOWN, which is the useful direction for " +
             "watching the grippers.")]
    [Range(TiltMinDeg, TiltMaxDeg)] public float tilt = 0f;

    [Header("Motion")]
    [Tooltip("time_from_start of each command. Short values make the head snappy but jerky " +
             "when you drag a slider; the robot caps speed at 3 rad/s regardless.")]
    public float moveDurationSeconds = 0.5f;

    [Tooltip("Resend even when nothing changed. Off by default: a JointTrajectoryController " +
             "holds its last goal, so repeating it only preempts a move in progress.")]
    public bool continuousPublish = false;

    [Tooltip("Smallest change, in degrees, worth sending.")]
    public float changeThresholdDeg = 0.25f;

    [Tooltip("Upper bound on send rate, however fast the sliders move.")]
    public float maxPublishRateHz = 10f;

    [Header("PAL head manager")]
    [Tooltip("Disable pal_head_manager so it stops stealing the head. Without this the head " +
             "will visibly fight you.")]
    public bool disableHeadManager = true;

    [Tooltip("Seconds requested per disable goal. Refreshed at half this, and it lapses on " +
             "its own once Unity stops.")]
    public float disableSeconds = 4f;

    [Header("Status (read-only)")]
    public float sentPan, sentTilt;

    private static readonly string[] HeadJoints = { "head_1_joint", "head_2_joint" };

    private string headCmdId, disableGoalId;
    private bool advertised, everSent;
    private float lastPan, lastTilt, nextPublishAllowed, nextDisable;
    private int sequence;

    /// <summary>Command an absolute head pose in degrees. Clamped to the robot's limits.</summary>
    public void SetAngles(float panDegrees, float tiltDegrees)
    {
        pan = Mathf.Clamp(panDegrees, PanMinDeg, PanMaxDeg);
        tilt = Mathf.Clamp(tiltDegrees, TiltMinDeg, TiltMaxDeg);
    }

    private void Update()
    {
        if (!EnsureAdvertised()) return;

        if (disableHeadManager && Time.time >= nextDisable)
        {
            nextDisable = Time.time + Mathf.Max(0.5f, disableSeconds * 0.5f);
            SendDisableHeadManager();
        }

        if (Time.time < nextPublishAllowed) return;

        // Clamp here too: the sliders are bounded, but SetAngles and direct field writes
        // from other scripts are not.
        float p = Mathf.Clamp(pan, PanMinDeg, PanMaxDeg);
        float t = Mathf.Clamp(tilt, TiltMinDeg, TiltMaxDeg);

        bool moved = !everSent
                  || Mathf.Abs(p - lastPan) >= changeThresholdDeg
                  || Mathf.Abs(t - lastTilt) >= changeThresholdDeg;
        if (!moved && !continuousPublish) return;

        nextPublishAllowed = Time.time + 1f / Mathf.Max(1f, maxPublishRateHz);
        SendHead(p, t);
        lastPan = p;
        lastTilt = t;
        everSent = true;
    }

    private bool EnsureAdvertised()
    {
        if (advertised) return true;
        if (rosConnector == null || rosConnector.RosSocket == null) return false;
        if (!rosConnector.IsConnected.WaitOne(0)) return false;

        headCmdId = rosConnector.RosSocket.Advertise<Msg.JointTrajectory>(
            "/head_controller/command");
        disableGoalId = rosConnector.RosSocket.Advertise<Msg.DisableActionGoal>(
            "/pal_head_manager/disable/goal");
        advertised = true;
        Debug.Log("[TiagoHeadController] head publishers advertised.");
        return true;
    }

    private void SendHead(float panDeg, float tiltDeg)
    {
        rosConnector.RosSocket.Publish(headCmdId, new Msg.JointTrajectory
        {
            joint_names = HeadJoints,
            points = new[]
            {
                new Msg.JointTrajectoryPoint
                {
                    positions = new double[] { panDeg * Mathf.Deg2Rad, tiltDeg * Mathf.Deg2Rad },
                    velocities = new double[] { 0.0, 0.0 },
                    time_from_start = Msg.Duration.FromSeconds(moveDurationSeconds)
                }
            }
        });
        sentPan = panDeg;
        sentTilt = tiltDeg;
    }

    private void SendDisableHeadManager()
    {
        rosConnector.RosSocket.Publish(disableGoalId, new Msg.DisableActionGoal
        {
            goal_id = new Msg.GoalID { id = $"unity-headmgr-{++sequence}" },
            goal = new Msg.DisableGoal { duration = disableSeconds }
        });
    }
}
