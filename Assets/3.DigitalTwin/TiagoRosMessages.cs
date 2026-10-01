using Newtonsoft.Json;
using RosSharp.RosBridgeClient;

/// <summary>
/// The two ROS message types this integration sends, declared locally.
///
/// ROS# ships equivalents under RosSharp.RosBridgeClient.MessageTypes.*, but their
/// namespaces and the exact C# type used for Duration have moved between ROS#
/// versions. Declaring them here means the integration depends only on the stable
/// part of the ROS# API — Message, RosSocket.Advertise and RosSocket.Publish — so
/// it compiles against whichever version you install.
///
/// Field names are snake_case on purpose: rosbridge matches them against the ROS
/// message definition verbatim, so they must not be renamed to C# conventions.
/// </summary>
namespace Tiago.Ros
{
    public class Time
    {
        public uint secs;
        public uint nsecs;

        public static Time Now() { return new Time();  }   // zeros = "stamp on arrival"
    }

    public class Duration
    {
        public int secs;
        public int nsecs;

        public static Duration FromSeconds(double s)
        {
            int whole = (int)s;
            return new Duration { secs = whole, nsecs = (int)((s - whole) * 1e9) };
        }
    }

    public class Header
    {
        public uint seq = 0;
        public Time stamp = new Time();
        public string frame_id = "";
    }

    public class GoalID
    {
        public Time stamp = new Time();
        public string id = "";
    }

    public class Point
    {
        public double x, y, z;
    }

    // Named Vec3 rather than Vector3 so it never reads as UnityEngine.Vector3 at a call
    // site. Only the JSON field names matter to rosbridge.
    public class Vec3
    {
        public double x, y, z;
    }

    public class PointStamped
    {
        public Header header = new Header();
        public Point point = new Point();
    }

    /// <summary>
    /// control_msgs/PointHeadGoal. The head action server does the kinematics: give it a
    /// target expressed in a frame that moves with the gripper and it keeps the head on
    /// it, so Unity never has to solve pan/tilt.
    /// </summary>
    public class PointHeadGoal
    {
        public PointStamped target = new PointStamped();
        public Vec3 pointing_axis = new Vec3();
        public string pointing_frame = "";
        public Duration min_duration = new Duration();
        public double max_velocity;
    }

    /// <summary>
    /// An actionlib goal is an ordinary topic, so it goes out as a normal publish —
    /// no action client needed on the Unity side.
    /// </summary>
    public class PointHeadActionGoal : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "control_msgs/PointHeadActionGoal";

        public Header header = new Header();
        public GoalID goal_id = new GoalID();
        public PointHeadGoal goal = new PointHeadGoal();
    }

    /// <summary>
    /// pal_common_msgs/DisableGoal. `duration` is in seconds and the disable LAPSES on its
    /// own, so it has to be refreshed to stay in effect — which also means the robot's own
    /// head behaviour comes back by itself if Unity stops.
    /// </summary>
    public class DisableGoal
    {
        public float duration;
    }

    public class DisableActionGoal : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "pal_common_msgs/DisableActionGoal";

        public Header header = new Header();
        public GoalID goal_id = new GoalID();
        public DisableGoal goal = new DisableGoal();
    }

    public class MultiArrayLayout
    {
        // JointGroupVelocityController reads `data` only, so an empty dim is correct
        // for a plain N-vector — but the field must still be present and non-null.
        public object[] dim = new object[0];
        public uint data_offset = 0;
    }

    /// <summary>
    /// sensor_msgs/CompressedImage. `data` is a JPEG payload; rosbridge sends it as a
    /// base64 string and Json.NET turns that straight into byte[], which Unity's
    /// ImageConversion.LoadImage decodes as-is.
    /// </summary>
    public class CompressedImage : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "sensor_msgs/CompressedImage";

        public Header header = new Header();

        // e.g. "rgb8; jpeg compressed bgr8" — the part after the semicolon describes the
        // cv::Mat that was fed to the encoder, not the byte order inside the JPEG, so a
        // standard decode gives correct colours.
        public string format;

        public byte[] data;
    }

    /// <summary>
    /// sensor_msgs/JointState. The robot publishes all 33 joints at ~50 Hz; `name` is the
    /// only reliable key, since the order is not part of the contract.
    ///
    /// NaN ARRIVES AS null. The wheels have no effort sensor and report NaN; JSON has no
    /// NaN, so rosbridge encodes it as null. ROS# deserialises with System.Text.Json, whose
    /// AllowNamedFloatingPointLiterals accepts the STRING "NaN" but not a null, so a single
    /// null inside a double[] throws and drops the whole message — every frame, silently
    /// from the subscriber's point of view.
    ///
    /// Hence `effort` is not declared at all (nothing here uses it, and System.Text.Json
    /// skips JSON properties with no matching member), and `position` / `velocity` are
    /// nullable so one NaN joint can never take the other 32 down with it.
    /// </summary>
    public class JointState : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "sensor_msgs/JointState";

        public Header header = new Header();
        public string[] name;
        public double?[] position;     // radians for revolute, metres for prismatic
        public double?[] velocity;
    }

    /// <summary>std_msgs/Float64MultiArray — raw joint velocity vector (rad/s).</summary>
    public class Float64MultiArray : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "std_msgs/Float64MultiArray";

        public MultiArrayLayout layout = new MultiArrayLayout();
        public double[] data;
    }

    public class JointTrajectoryPoint
    {
        public double[] positions;
        public double[] velocities;
        public double[] accelerations = new double[0];
        public double[] effort = new double[0];
        public Duration time_from_start;
    }

    /// <summary>trajectory_msgs/JointTrajectory — what a JointTrajectoryController takes.</summary>
    public class JointTrajectory : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "trajectory_msgs/JointTrajectory";

        public Header header = new Header();
        public string[] joint_names;
        public JointTrajectoryPoint[] points;
    }

    /// <summary>
    /// controller_manager_msgs/SwitchController. Only one controller may claim a joint
    /// set, so changing between position and velocity following means stopping one and
    /// starting the other in a single atomic call.
    /// </summary>
    public class SwitchControllerRequest : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "controller_manager_msgs/SwitchController";

        public const int BEST_EFFORT = 1;
        public const int STRICT = 2;

        public string[] start_controllers;
        public string[] stop_controllers;

        // BEST_EFFORT, not STRICT: we list every controller that could be holding the
        // arm without knowing which one actually is, and STRICT fails the whole call if
        // asked to stop one that is not running.
        public int strictness = BEST_EFFORT;
    }

    public class SwitchControllerResponse : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "controller_manager_msgs/SwitchController";

        public bool ok;
    }
}
