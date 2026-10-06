using Newtonsoft.Json;
using RosSharp.RosBridgeClient;
using UnityEngine;

/// <summary>
/// ROS messages used by the client/server additions (Gazebo world, base teleop), declared
/// locally like Tiago.Ros in TiagoRosMessages.cs. Field names are snake_case on purpose:
/// rosbridge matches them against the ROS definition verbatim.
/// </summary>
namespace Twin.Ros
{
    public class Vector3Msg
    {
        public double x, y, z;
    }

    public class QuaternionMsg
    {
        public double x, y, z, w = 1.0;
    }

    public class Pose
    {
        public Vector3Msg position = new Vector3Msg();
        public QuaternionMsg orientation = new QuaternionMsg();
    }

    /// <summary>geometry_msgs/Twist — base velocity (m/s, rad/s), sent to twist_mux.</summary>
    public class Twist : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "geometry_msgs/Twist";

        public Vector3Msg linear = new Vector3Msg();
        public Vector3Msg angular = new Vector3Msg();
    }

    /// <summary>gazebo_msgs/ModelStates — pose of every Gazebo model (twist not needed).</summary>
    public class ModelStates : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "gazebo_msgs/ModelStates";

        public string[] name;
        public Pose[] pose;
    }

    /// <summary>rosapi/GetParam — value comes back JSON-encoded.</summary>
    public class GetParamRequest : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "rosapi/GetParam";

        public string name;
        public string @default = "";
    }

    public class GetParamResponse : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "rosapi/GetParam";

        public string value;
    }

    /// <summary>std_srvs/Empty — the PAL gripper grasp / release services.</summary>
    public class EmptyRequest : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "std_srvs/Empty";
    }

    public class EmptyResponse : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "std_srvs/Empty";
    }

    /// <summary>
    /// ROS (x forward, y left, z up, right-handed) to Unity (z forward, x right, y up,
    /// left-handed) — the ROS# convention, which is also how the twin prefab was built
    /// (base_laser_link at ROS +x is at Unity +z, the left antenna at Unity -x).
    /// </summary>
    public static class RosFrame
    {
        public static Vector3 ToUnity(Vector3Msg p)
        {
            return new Vector3(-(float)p.y, (float)p.z, (float)p.x);
        }

        public static Quaternion ToUnity(QuaternionMsg q)
        {
            return new Quaternion((float)q.y, -(float)q.z, -(float)q.x, (float)q.w);
        }

        /// <summary>Heading in Unity degrees (rotation about +y) of a ROS orientation.</summary>
        public static float YawDeg(QuaternionMsg q)
        {
            double yaw = System.Math.Atan2(2.0 * (q.w * q.z + q.x * q.y),
                                           1.0 - 2.0 * (q.y * q.y + q.z * q.z));
            return -(float)(yaw * Mathf.Rad2Deg);
        }
    }
}
