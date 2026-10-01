using System.Collections.Generic;
using DT.Simulation;
using RosSharp.RosBridgeClient;
using UnityEngine;
using Joint = DT.Model.Joint;
using Msg = Tiago.Ros;

/// <summary>
/// Drives a SECOND TiagoArmsModel from the real robot's /joint_states, so it stands in the
/// scene as a shadow of where the hardware actually is.
///
/// This is the reverse direction from TiagoDualArmRosSync: that one sends the twin's pose to
/// the robot, this one brings the robot's pose back. Put them on different model instances —
/// the same model doing both would drive itself.
///
/// NAMES DO NOT MATCH, for the grippers. The twin's joint names come from the URDF the code
/// generator was fed and read left_hand_gripper_left_finger_joint, while the robot publishes
/// gripper_left_left_finger_joint. The arms agree (arm_left_1_joint ...), the grippers do
/// not, so the aliases below bridge them. They are Inspector-editable, and any ROS name that
/// matched nothing is logged once — if a gripper does not follow, look there first.
///
/// UNITS, per joint and not per model. BaseRobot.jointsPosition is read back into
/// Command.S_Cmd, and JointController.setPosition converts SI to degrees only for Revolute
/// joints. So the array wants DEGREES for the arm joints and METRES for the prismatic
/// gripper fingers. ROS gives radians and metres, so only the revolute ones are converted.
/// Doing this per model rather than per joint is how you get grippers 57x out of range.
///
/// THREADING. The rosbridge handler runs on the socket thread; it only copies the arrays.
/// The write into jointsPosition happens in Update, on the main thread, because
/// TiagoArmsModel.Update reads it in the same frame.
///
/// INSTANT MODE (default). The normal path is jointsPosition → S_Cmd → Ruckig → drive
/// target → PhysX. Ruckig adds jerk/acceleration-limited smoothing, and every body also
/// carries maxAngularVelocity = dS_max, so PhysX itself caps joint speed. A shadow must show
/// where the robot IS, not chase it, so in instant mode:
///   • this model's ComputeModel is unhooked from the Scheduler, so neither Ruckig nor
///     JointController touches its drives any more;
///   • each ArticulationBody is teleported: jointPosition set, jointVelocity zeroed, and the
///     drive target set to the same value so the drive holds it rather than pulling back.
/// Only THIS model is affected — JointController and TiagoArmsModel are untouched, and the
/// main twin keeps its Ruckig pipeline.
///
/// Setup:
///   • shadowModel  → the second TiagoArmsModel. Do NOT put TiagoDualArmIK on it.
///   • rosConnector → a ROS# RosConnector (ws://&lt;robot-ip&gt;:9090)
/// Give its meshes a translucent material so it reads as a ghost rather than a second robot.
/// </summary>
public class TiagoShadowFromRos : MonoBehaviour
{
    [Header("Connections")]
    public RosConnector rosConnector;

    [Tooltip("The model to drive. GetComponent is tried if empty. Must NOT also carry " +
             "TiagoDualArmIK or TiagoDualArmRosSync.")]
    public TiagoArmsModel shadowModel;

    public string topic = "/joint_states";

    [Tooltip("Minimum gap between frames, enforced by rosbridge. /joint_states runs at " +
             "~50 Hz; 50 ms is plenty for a visual shadow.")]
    public int throttleMilliseconds = 50;

    [Header("Options")]
    [Tooltip("Bypass Ruckig and PhysX speed limits: teleport each joint to the measured " +
             "value. Toggling this off mid-run gives one visible catch-up, because Ruckig " +
             "resumes from wherever its internal state was left when it was unhooked.")]
    public bool instant = true;

    [Tooltip("Ruckig mode only (instant off). Ruckig then aims to ARRIVE at each target " +
             "with the measured velocity, which tracks a moving robot more smoothly but " +
             "amplifies noise in /joint_states.")]
    public bool applyVelocities = false;

    [Tooltip("ROS names the twin does not have (head, torso, wheels) are expected to go " +
             "unmatched. Logged once so genuine mismatches are still visible.")]
    public bool logUnmatchedNames = true;

    [System.Serializable]
    public class NameAlias
    {
        [Tooltip("Name as published by the robot in /joint_states.")]
        public string rosName;

        [Tooltip("Name as generated in TiagoArmsModel.")]
        public string twinName;
    }

    [Header("Name aliases (robot -> twin)")]
    public List<NameAlias> aliases = new List<NameAlias>
    {
        new NameAlias { rosName = "gripper_left_left_finger_joint",
                        twinName = "left_hand_gripper_left_finger_joint" },
        new NameAlias { rosName = "gripper_left_right_finger_joint",
                        twinName = "left_hand_gripper_right_finger_joint" },
        new NameAlias { rosName = "gripper_right_left_finger_joint",
                        twinName = "right_hand_gripper_left_finger_joint" },
        new NameAlias { rosName = "gripper_right_right_finger_joint",
                        twinName = "right_hand_gripper_right_finger_joint" },
    };

    [Header("Status (read-only)")]
    public int mappedJoints;
    public int unmatchedRosNames;
    public float framesPerSecond;
    public bool ruckigBypassed;
    [Tooltip("Mapped joints whose ArticulationBody could not be found. Should be 0.")]
    public int missingBodies;

    private BaseRobot robot;
    private Dictionary<string, int> twinIndexByRosName;
    private bool[] isRotational;
    private ArticulationBody[] bodies;       // by twin index k

    private bool subscribed, mapReady, loggedUnmatched;
    private string subscriptionId;

    // Written on the socket thread, read on the main thread; newest frame wins.
    private volatile bool pending;
    private string[] inName;
    private double?[] inPos, inVel;
    private readonly object gate = new object();

    private int framesThisSecond;
    private float fpsWindowStart;

    private void Update()
    {
        if (!subscribed) { TrySubscribe(); return; }
        if (!mapReady && !BuildMap()) return;

        // Follows the Inspector toggle live, in both directions.
        if (instant != ruckigBypassed) SetRuckigBypassed(instant);

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
        // Hand the model back to its own pipeline so disabling this component never leaves
        // a robot whose joints nobody drives.
        if (ruckigBypassed) SetRuckigBypassed(false);

        if (!subscribed || rosConnector == null || rosConnector.RosSocket == null) return;
        rosConnector.RosSocket.Unsubscribe(subscriptionId);
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (rosConnector == null || rosConnector.RosSocket == null) return;
        if (!rosConnector.IsConnected.WaitOne(0)) return;

        subscriptionId = rosConnector.RosSocket.Subscribe<Msg.JointState>(
            topic, OnJointState, throttleMilliseconds, 1);
        subscribed = true;
        fpsWindowStart = Time.time;
        Debug.Log($"[TiagoShadowFromRos] subscribed to {topic}.");
    }

    /// <summary>
    /// Index the twin's movable joints by the ROS name that feeds them.
    ///
    /// The ordering has to be derived exactly as BaseRobot.InitRobot does it — walk Joints,
    /// skip Constraint and Fixed — because that is what makes index k of jointsPosition
    /// correspond to joints[k].
    /// </summary>
    private bool BuildMap()
    {
        if (shadowModel == null) shadowModel = GetComponent<TiagoArmsModel>();
        if (shadowModel == null || shadowModel.robot == null ||
            shadowModel.robot.joints == null || shadowModel.robot.Joints == null) return false;

        robot = shadowModel.robot;
        twinIndexByRosName = new Dictionary<string, int>();
        isRotational = new bool[robot.joints.Length];
        bodies = new ArticulationBody[robot.joints.Length];
        missingBodies = 0;

        // twin name -> alias source, inverted so the lookup is by ROS name.
        var rosNameForTwinName = new Dictionary<string, string>();
        foreach (NameAlias a in aliases)
        {
            if (!string.IsNullOrEmpty(a.twinName) && !string.IsNullOrEmpty(a.rosName))
                rosNameForTwinName[a.twinName] = a.rosName;
        }

        int k = 0;
        foreach (Joint j in robot.Joints)
        {
            if (j.Constraint || j.Type == Joint.JointTypes.Fixed) continue;
            if (k >= robot.joints.Length) break;

            string rosName;
            if (!rosNameForTwinName.TryGetValue(j.Name, out rosName)) rosName = j.Name;
            twinIndexByRosName[rosName] = k;

            var t = robot.joints[k].Type;
            isRotational[k] = t == DT.Model.Axe.Axe_enum.RotX ||
                              t == DT.Model.Axe.Axe_enum.RotY ||
                              t == DT.Model.Axe.Axe_enum.RotZ;

            // ComponentController names each joint GameObject after the Joint and puts the
            // ArticulationBody on it (Revolute/PrismaticJointToUnity.Create). Searched under
            // THIS model only, so the main twin's identically-named joints are never hit.
            Transform jt = FindDeep(shadowModel.transform, j.Name);
            bodies[k] = jt != null ? jt.GetComponent<ArticulationBody>() : null;
            if (bodies[k] == null) missingBodies++;
            k++;
        }

        mappedJoints = twinIndexByRosName.Count;
        mapReady = true;
        Debug.Log($"[TiagoShadowFromRos] mapped {mappedJoints} twin joints" +
                  (missingBodies > 0 ? $", {missingBodies} without an ArticulationBody." : "."));
        return true;
    }

    // Socket thread. Copy only — no Unity API, no model writes.
    private void OnJointState(Msg.JointState message)
    {
        if (message == null || message.name == null || message.position == null) return;
        lock (gate)
        {
            inName = message.name;
            inPos = message.position;
            inVel = message.velocity;
        }
        pending = true;
    }

    private void ApplyLatestFrame()
    {
        string[] names;
        double?[] pos, vel;
        lock (gate)
        {
            names = inName; pos = inPos; vel = inVel;
            inName = null;
        }
        pending = false;
        if (names == null || pos == null) return;
        if (robot.jointsPosition == null) return;

        // goHome would overwrite everything we write this frame.
        robot.goHome = false;

        int unmatched = 0;
        int n = Mathf.Min(names.Length, pos.Length);
        for (int i = 0; i < n; i++)
        {
            int k;
            if (!twinIndexByRosName.TryGetValue(names[i], out k)) { unmatched++; continue; }
            if (k >= robot.jointsPosition.Length) continue;

            // A NaN from the robot arrives as null. Keep the last good value rather than
            // writing a NaN into the drive target, which would poison the articulation.
            if (!pos[i].HasValue) continue;

            // Revolute joints want degrees; prismatic gripper fingers are already metres.
            float si = (float)pos[i].Value;
            float native = isRotational[k] ? si * Mathf.Rad2Deg : si;

            // Written in both modes: it keeps the model's command state coherent, so
            // switching instant off resumes from the right target.
            robot.jointsPosition[k] = native;

            if (instant)
            {
                Teleport(bodies[k], si, native);
                continue;
            }

            if (applyVelocities && vel != null && i < vel.Length && vel[i].HasValue &&
                robot.jointsVelocity != null && k < robot.jointsVelocity.Length)
            {
                robot.jointsVelocity[k] = isRotational[k]
                                        ? (float)vel[i].Value * Mathf.Rad2Deg
                                        : (float)vel[i].Value;
            }
        }

        unmatchedRosNames = unmatched;
        framesThisSecond++;

        if (logUnmatchedNames && !loggedUnmatched && unmatched > 0)
        {
            loggedUnmatched = true;
            var list = new List<string>();
            for (int i = 0; i < n; i++)
                if (!twinIndexByRosName.ContainsKey(names[i])) list.Add(names[i]);
            Debug.Log("[TiagoShadowFromRos] ROS joints with no twin counterpart (head, torso " +
                      "and wheels are expected): " + string.Join(", ", list));
        }
    }

    // ------------------------------------------------------------------ instant mode

    /// <summary>
    /// Put one joint exactly at <paramref name="si"/> this frame.
    ///
    /// jointPosition is in SI (radians / metres) while the drive target is in the joint's
    /// native unit (degrees for revolute, metres for prismatic) — the same split
    /// JointController.setPosition and GetPosition use. Setting the target as well matters:
    /// jointPosition alone moves the joint, but the drive would then pull it straight back
    /// to its old target.
    /// </summary>
    private static void Teleport(ArticulationBody body, float si, float native)
    {
        // dofCount is only valid once the articulation has been simulated at least once.
        if (body == null || body.dofCount != 1) return;

        body.jointPosition = new ArticulationReducedSpace(si);
        body.jointVelocity = new ArticulationReducedSpace(0f);

        ArticulationDrive drive = body.xDrive;
        drive.target = native;
        drive.targetVelocity = 0f;
        body.xDrive = drive;
    }

    /// <summary>
    /// Unhook (or re-hook) this model's ComputeModel from the Scheduler.
    ///
    /// It is removed from every period bucket rather than just the one CyclicTime selects:
    /// removing a delegate that is not registered is a no-op, and this way nothing depends on
    /// knowing which bucket RegisterMethod picked. A new delegate built from the same method
    /// on the same controller compares equal to the registered one, which is what lets -=
    /// find it from out here.
    /// </summary>
    private void SetRuckigBypassed(bool bypass)
    {
        Scheduler s = Scheduler.Instance;
        if (s == null || shadowModel == null || shadowModel.controller == null) return;

        Scheduler.TaskHandler compute = shadowModel.controller.ComputeModel;
        s.On1ms -= compute;
        s.On2ms -= compute;
        s.On4ms -= compute;
        s.On6ms -= compute;
        s.On8ms -= compute;
        s.On10ms -= compute;

        if (!bypass) s.RegisterMethod(compute, robot.CyclicTime);

        ruckigBypassed = bypass;
        Debug.Log($"[TiagoShadowFromRos] shadow {(bypass ? "INSTANT — Ruckig bypassed" : "back on Ruckig")}.");
    }

    private static Transform FindDeep(Transform root, string targetName)
    {
        if (root.name == targetName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }
}
