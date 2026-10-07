using System.Collections.Generic;
using System.Text;
using RosSharp.RosBridgeClient;
using UnityEngine;
using Msg = Tiago.Ros;

/// <summary>
/// Automated check of the Gazebo world features, without a headset. Only installed when
/// started with <c>-twinWorldProbe</c> (add <c>-twinProbeQuit</c> to exit at the end).
///   1. objects drawn and the twin anchored on the Gazebo base;
///   2. BASE  — scripted stick input turns the base left and back (away from the table, so the
///              swinging grippers hit nothing), then drives it forward; the twin must
///              follow the Gazebo base and the arms must not move relative to the robot;
///   3. GRASP — the left IK handle brings the gripper around cylinder_red from the front,
///              the gripper closes, the arm lifts: the object must rise in Gazebo, and in
///              Unity with it.
/// The grasp point is computed from the twin's finger joints: the Unity copy of
/// gripper_left_grasping_frame is not driven by the articulation.
/// Lines start with "[TwinWorldProbe]".
/// </summary>
public class TwinWorldProbe : MonoBehaviour
{
    public string graspObject = "cylinder_red";
    public float driveDistance = 0.15f;
    public float turnDeg = 15f;
    public float tolerancePos = 0.03f;
    public float toleranceYawDeg = 3f;
    public float toleranceArmDeg = 3f;

    private enum Phase { Waiting, TurnLeft, TurnBack, Drive, Settle, Clear, Reach, Advance, Close, Lift, Hold, Done }

    private TwinGazeboWorld world;
    private TwinBaseTeleop teleop;
    private TwinGripperControl gripper;
    private TiagoDualArmIK ik;
    private TiagoDualArmRosSync sync;
    private RosConnector connector;
    private bool quitWhenDone;
    private string shotDir;            // -twinProbeShots <dir>: side views at each phase

    private Phase phase = Phase.Waiting;
    private float phaseStart, nextReport;
    private readonly List<string> failures = new List<string>();
    private readonly StringBuilder summary = new StringBuilder();

    // Twin joints vs /joint_states.
    private readonly object gate = new object();
    private readonly Dictionary<string, double> gazeboRad = new Dictionary<string, double>();
    private readonly Dictionary<string, ArticulationBody> cmdBodies = new Dictionary<string, ArticulationBody>();
    private bool subscribed;
    private float worstArmGapWhileDriving, maxTurn;

    // Base check.
    private Vector3 startRootPos;
    private float startRootYaw;
    private Vector3 handleInRootAtStart;

    // Grasp.
    private Transform tip, fingerA, fingerB, graspTarget;
    // The IK twin's fingers are never commanded (both sit at 0); the shadow's follow
    // /joint_states, so their separation gives the real finger direction.
    private Transform shadowFingerA, shadowFingerB;
    private Pose tipFromGrasp;            // grasp frame -> tip
    private Quaternion graspRot;
    private float objectStartY;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-twinWorldProbe") < 0) return;
        var probe = new GameObject("TwinWorldProbe").AddComponent<TwinWorldProbe>();
        probe.quitWhenDone = System.Array.IndexOf(args, "-twinProbeQuit") >= 0;
        int i = System.Array.IndexOf(args, "-twinProbeShots");
        if (i >= 0 && i + 1 < args.Length) probe.shotDir = args[i + 1];
    }

    private void Start()
    {
        world = FindAnyObjectByType<TwinGazeboWorld>();
        teleop = FindAnyObjectByType<TwinBaseTeleop>();
        gripper = FindAnyObjectByType<TwinGripperControl>();
        ik = FindAnyObjectByType<TiagoDualArmIK>();
        foreach (var s in FindObjectsByType<TiagoDualArmRosSync>(FindObjectsSortMode.InstanceID))
            if (s.enabled) { sync = s; break; }
        connector = sync != null ? sync.rosConnector : null;
        if (world == null || teleop == null || gripper == null || ik == null || sync == null)
            Finish("FAIL missing components");
        else
            Log("installed.");
    }

    private void Update()
    {
        if (phase == Phase.Done) return;
        if (!subscribed && connector != null && connector.RosSocket != null && connector.IsConnected.WaitOne(0))
        {
            connector.RosSocket.Subscribe<Msg.JointState>("/joint_states", OnJointState, 50, 1);
            subscribed = true;
        }
        float t = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Waiting:
                if (Ready()) { Discover(); if (phase != Phase.Done) Enter(Phase.TurnLeft); }
                else if (Time.time > 150f) Finish($"FAIL not ready (anchored={world.anchored}, objects={world.objectCount})");
                break;

            case Phase.Drive:      // closed loop on the twin, which follows Gazebo
                teleop.scriptedInput = new Vector2(0f, 0.6f);
                TrackArms();
                // The twin lags Gazebo a little and the base coasts: stop early.
                if (Moved() >= driveDistance - 0.04f || t > 8f) { teleop.scriptedInput = Vector2.zero; Enter(Phase.Settle); }
                break;

            case Phase.TurnLeft:   // stick left = turn left; Unity yaw: left is negative
                TrackArms();
                maxTurn = Mathf.Max(maxTurn, Mathf.Abs(Turned()));
                if (TurnTowards(-turnDeg) || t > 8f) Enter(Phase.TurnBack);
                break;

            case Phase.TurnBack:
                TrackArms();
                maxTurn = Mathf.Max(maxTurn, Mathf.Abs(Turned()));
                if (TurnTowards(0f) || t > 10f) { teleop.scriptedInput = Vector2.zero; Enter(Phase.Drive); }
                break;

            case Phase.Settle:
                TrackArms();
                if (t > 3f) { CheckBase(); Enter(Phase.Clear); }
                break;

            case Phase.Clear:      // back off and up, away from the object, levelling the fingers
                gripper.scriptedLeft = 0;
                if (!phaseGoalSet) levelRoll = LevelFingersRotation();
                AimGrasp(graspTarget.position - world.mover.Root.forward * 0.22f + Vector3.up * 0.10f, t, 4f, true);
                if (t > 5f) Enter(Phase.Reach);
                break;

            case Phase.Reach:      // pre-grasp: 15 cm in front of the object, at its height
                AimGrasp(graspTarget.position - world.mover.Root.forward * 0.15f, t, 4f);
                if (t > 6f) Enter(Phase.Advance);
                break;

            case Phase.Advance:
                AimGrasp(graspTarget.position, t, 4f);
                if (t > 6f) Enter(Phase.Close);
                break;

            case Phase.Close:
                gripper.scriptedLeft = 1;
                if (t > 3f) { objectStartY = graspTarget.position.y; Enter(Phase.Lift); }
                break;

            case Phase.Lift:
                AimGrasp(graspTarget.position + Vector3.up * 0.12f, t, 4f, true);
                if (t > 6f) Enter(Phase.Hold);
                break;

            case Phase.Hold:
                if (t > 2f) { CheckGrasp(); CheckXrOrigin(); Report(true); }
                break;
        }

        if (phase != Phase.Waiting && phase != Phase.Done && Time.time >= nextReport)
        {
            nextReport = Time.time + 1f;
            Report(false);
        }
    }

    private bool Ready()
    {
        if (!world.anchored || world.objectCount == 0) return false;
        if (ik.leftArm.target == null || !ik.leftArm.followTarget) return false;
        if (sync.mode == TiagoDualArmRosSync.FollowMode.Off) return false;
        lock (gate) { if (gazeboRad.Count == 0) return false; }
        graspTarget = GameObject.Find(graspObject) != null ? GameObject.Find(graspObject).transform : null;
        return graspTarget != null && graspTarget.gameObject.activeInHierarchy;
    }

    // -------------------------------------------------------------- discovery

    private void Discover()
    {
        Transform root = world.mover.Root;
        TiagoArmsModel model = sync.model != null ? sync.model : sync.GetComponent<TiagoArmsModel>();
        foreach (string side in new[] { "left", "right" })
            for (int i = 1; i <= 7; i++)
            {
                string n = $"arm_{side}_{i}_joint";
                cmdBodies[n] = FindBody(model.transform, n);
            }

        tip = ik.leftArm.tip != null ? ik.leftArm.tip.transform : null;
        fingerA = FindDeep(model.transform, "left_hand_gripper_left_finger_joint");
        fingerB = FindDeep(model.transform, "left_hand_gripper_right_finger_joint");
        TiagoShadowFromRos shadow = FindAnyObjectByType<TiagoShadowFromRos>();
        Transform shadowRoot = shadow != null && shadow.shadowModel != null ? shadow.shadowModel.transform : null;
        if (shadowRoot != null)
        {
            shadowFingerA = FindDeep(shadowRoot, "left_hand_gripper_left_finger_joint");
            shadowFingerB = FindDeep(shadowRoot, "left_hand_gripper_right_finger_joint");
        }
        if (tip == null || fingerA == null || fingerB == null || shadowFingerA == null || shadowFingerB == null)
        {
            Finish("FAIL left tip/finger joints not found");
            return;
        }
        Log($"grasp point {GraspPoint() - tip.position} from the tip (URDF: 0.242 m along the gripper)");
        foreach (Transform f in new[] { tip, fingerA, shadowFingerA, shadowFingerB, graspTarget })
        {
            if (f == null) continue;
            Vector3 p = root.InverseTransformPoint(f.position);
            Log($"{f.name}: robot-frame ROS xyz=({p.z:0.000}, {-p.x:0.000}, {p.y:0.000}) " +
                $"fwd={root.InverseTransformDirection(f.forward)} up={root.InverseTransformDirection(f.up)} " +
                $"right={root.InverseTransformDirection(f.right)}");
        }

        GameObject xr = GameObject.Find("XR Origin (XR Rig)");
        if (xr != null)
        {
            xrOrigin = xr.transform;
            xrStartY = root.InverseTransformPoint(xrOrigin.position).y;
        }

        startRootPos = root.position;
        startRootYaw = root.eulerAngles.y;
        handleInRootAtStart = root.InverseTransformPoint(ik.leftArm.target.transform.position);
    }

    // ------------------------------------------------------------------- base

    private void TrackArms()
    {
        worstArmGapWhileDriving = Mathf.Max(worstArmGapWhileDriving, ArmGap());
    }

    /// <summary>
    /// Proportional stick on the yaw error (the base keeps turning a little after the
    /// stick is released). True once within 1.5 deg and nearly still.
    /// </summary>
    private bool TurnTowards(float targetDeg)
    {
        float err = targetDeg - Turned();
        float stick = Mathf.Clamp(err / 15f, -0.6f, 0.6f);
        if (Mathf.Abs(stick) < 0.25f) stick = 0.25f * Mathf.Sign(err);
        bool done = Mathf.Abs(err) < 1.5f;
        teleop.scriptedInput = done ? Vector2.zero : new Vector2(stick, 0f);
        return done;
    }

    private float Moved() { return Vector3.Distance(world.mover.Root.position, startRootPos); }

    private float Turned() { return Mathf.DeltaAngle(startRootYaw, world.mover.Root.eulerAngles.y); }

    private void CheckBase()
    {
        Transform root = world.mover.Root;
        float moved = Moved();
        float turned = maxTurn;
        Vector3 gz = world.RobotTargetPosition;
        float posErr = Vector3.Distance(new Vector3(gz.x, 0, gz.z), new Vector3(root.position.x, 0, root.position.z));
        float yawErr = Mathf.Abs(Mathf.DeltaAngle(world.RobotTargetYaw, root.eulerAngles.y));
        float handleDrift = Vector3.Distance(handleInRootAtStart,
                                             root.InverseTransformPoint(ik.leftArm.target.transform.position));

        summary.Append($"base: moved {moved:0.00}m turned {turned:0}deg, unity-vs-gazebo {posErr * 100:0.0}cm " +
                       $"{yawErr:0.0}deg, arm gap while driving {worstArmGapWhileDriving:0.0}deg, " +
                       $"IK handle drift in robot frame {handleDrift * 100:0.0}cm | ");
        if (moved < driveDistance * 0.8f) failures.Add("base did not move");
        if (turned < turnDeg * 0.8f) failures.Add("base did not turn");
        if (posErr > tolerancePos || yawErr > toleranceYawDeg) failures.Add("twin not on Gazebo base");
        if (worstArmGapWhileDriving > toleranceArmDeg) failures.Add("arms moved while driving");
        if (handleDrift > 0.01f) failures.Add("IK handle left behind");
    }

    // ------------------------------------------------------------------ grasp

    /// <summary>
    /// Move the left IK handle so that the gripper's grasping frame reaches <paramref name="goal"/>
    /// with the orientation it had at the start of the phase, interpolated over `duration`.
    /// </summary>
    private void AimGrasp(Vector3 goal, float t, float duration, bool keepGoalFixed = false)
    {
        Transform handle = ik.leftArm.target.transform;
        if (!phaseGoalSet)
        {
            phaseGoalSet = true;
            // Keep the handle's orientation (Reach also levels the fingers); the grasp point
            // is a fixed offset from it.
            graspRot = levelRoll * handle.rotation;
            levelRoll = Quaternion.identity;
            tipFromGrasp = new Pose(Quaternion.Inverse(handle.rotation) * (tip.position - GraspPoint()), Quaternion.identity);
            phaseStartGrasp = GraspPoint();
            phaseStartRot = handle.rotation;
            phaseGoal = goal;
        }
        // The lift goal is relative to the graspTarget, which rises with the gripper: freeze it.
        if (!keepGoalFixed) phaseGoal = goal;
        float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
        Vector3 g = Vector3.Lerp(phaseStartGrasp, phaseGoal, a);
        Quaternion r = Quaternion.Slerp(phaseStartRot, graspRot, a);
        handle.SetPositionAndRotation(g + r * tipFromGrasp.position, r);
    }

    /// <summary>
    /// Centre of the finger pads. URDF: the finger links sit 0.077 m from arm_7_link along
    /// the gripper axis and their collision boxes span 0.11 m centred 0.165 m further, i.e.
    /// 0.242 m from arm_7_link (the grasping frame, at 0.197 m, is at their inner end).
    /// </summary>
    private Vector3 GraspPoint()
    {
        Vector3 mid = (fingerA.position + fingerB.position) * 0.5f;
        return tip.position + (mid - tip.position) * (0.242f / 0.077f);
    }

    private bool phaseGoalSet;
    private Vector3 phaseStartGrasp, phaseGoal;
    private Quaternion phaseStartRot, levelRoll = Quaternion.identity;

    /// <summary>
    /// The home pose holds the gripper rolled 45 deg, one finger low. Roll it about its own
    /// axis (smallest way) so the fingers open horizontally around an upright object.
    /// </summary>
    private Quaternion LevelFingersRotation()
    {
        Vector3 axis = (GraspPoint() - tip.position).normalized;
        Vector3 sep = Vector3.ProjectOnPlane(shadowFingerA.position - shadowFingerB.position, axis);
        Vector3 level = Vector3.Cross(axis, Vector3.up).normalized;
        if (sep.sqrMagnitude < 1e-8f || level.sqrMagnitude < 1e-8f) return Quaternion.identity;
        float angle = Vector3.SignedAngle(sep, level, axis);
        if (angle > 90f) angle -= 180f;
        if (angle < -90f) angle += 180f;
        Log($"levelling the gripper: roll {angle:0.0}deg");
        return Quaternion.AngleAxis(angle, axis);
    }

    // The XR Origin's gravity provider makes it fall without a floor (TwinFloor).
    private Transform xrOrigin;
    private float xrStartY;

    private void CheckXrOrigin()
    {
        if (xrOrigin == null) return;
        float drop = xrStartY - world.mover.Root.InverseTransformPoint(xrOrigin.position).y;
        summary.Append($" | XR Origin drop {drop * 100:0.0}cm");
        if (drop > 0.05f) failures.Add("XR Origin fell");
    }

    private void CheckGrasp()
    {
        float rise = graspTarget.position.y - objectStartY;
        summary.Append($"grasp {graspObject}: rose {rise * 100:0.0}cm in Unity (from Gazebo)");
        if (rise < 0.05f) failures.Add($"{graspObject} not lifted");
    }

    // ---------------------------------------------------------------- helpers

    private void Enter(Phase next)
    {
        Shot(phase.ToString() + "_end");
        phase = next;
        phaseStart = Time.time;
        phaseGoalSet = false;
        Log($"phase {next}");
    }

    private float ArmGap()
    {
        float worst = 0f;
        foreach (var kv in cmdBodies)
        {
            double gz;
            lock (gate) { if (!gazeboRad.TryGetValue(kv.Key, out gz)) continue; }
            if (kv.Value == null || kv.Value.jointPosition.dofCount == 0) continue;
            worst = Mathf.Max(worst, Mathf.Abs(Mathf.DeltaAngle(kv.Value.jointPosition[0] * Mathf.Rad2Deg,
                                                                  (float)(gz * Mathf.Rad2Deg))));
        }
        return worst;
    }

    private void Report(bool final)
    {
        Transform root = world.mover.Root;
        string g = fingerA != null && graspTarget != null
                 ? $" grasp->obj {Vector3.Distance(GraspPoint(), graspTarget.position) * 100:0.0}cm objY {graspTarget.position.y:0.000}"
                 : "";
        if (!final && phase >= Phase.Reach)
        {
            var sb = new StringBuilder("left joints twin/gz deg:");
            for (int i = 1; i <= 7; i++)
            {
                string n = $"arm_left_{i}_joint";
                double gz;
                lock (gate) { gazeboRad.TryGetValue(n, out gz); }
                ArticulationBody b;
                if (cmdBodies.TryGetValue(n, out b) && b != null && b.jointPosition.dofCount > 0)
                    sb.Append($" {i}:{b.jointPosition[0] * Mathf.Rad2Deg:0}/{gz * Mathf.Rad2Deg:0}");
            }
            Log(sb.ToString());
        }
        if (!final)
        {
            Log($"{phase} t={Time.time - phaseStart:0.0}s root=({root.position.x:0.00},{root.position.z:0.00}) " +
                $"yaw={root.eulerAngles.y:0} lin={teleop.linear:0.00} ang={teleop.angular:0.00} armGap={ArmGap():0.0}deg{g}");
            return;
        }
        Finish((failures.Count == 0 ? "PASS " : "FAIL " + string.Join("; ", failures) + " | ") + summary);
    }

    /// <summary>Side and top views of the left arm and the grasped object, as PNG.</summary>
    private void Shot(string label)
    {
        if (string.IsNullOrEmpty(shotDir) || graspTarget == null || world == null) return;
        Transform root = world.mover.Root;
        var go = new GameObject("ProbeCam");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 0.35f;
        cam.nearClipPlane = 0.02f;
        var rt = new RenderTexture(960, 720, 24);
        cam.targetTexture = rt;
        var views = new[]
        {
            ("side", graspTarget.position - root.right * 1.5f),     // from the robot's left: forward = right
            ("top", graspTarget.position + Vector3.up * 1.5f),      // from above
        };
        foreach (var (name, pos) in views)
        {
            go.transform.position = pos;
            go.transform.LookAt(graspTarget.position, name == "top" ? root.forward : Vector3.up);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            System.IO.Directory.CreateDirectory(shotDir);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(shotDir, $"{Time.frameCount:D6}_{label}_{name}.png"),
                                         tex.EncodeToPNG());
            Destroy(tex);
        }
        RenderTexture.active = null;
        cam.targetTexture = null;
        Destroy(rt);
        Destroy(go);
    }

    private void OnJointState(Msg.JointState m)
    {
        if (m == null || m.name == null || m.position == null) return;
        lock (gate)
        {
            for (int i = 0; i < m.name.Length && i < m.position.Length; i++)
                if (m.position[i].HasValue) gazeboRad[m.name[i]] = m.position[i].Value;
        }
    }

    private void Finish(string result)
    {
        phase = Phase.Done;
        if (teleop != null) teleop.scriptedInput = Vector2.zero;
        Log("RESULT " + result);
        if (quitWhenDone) Application.Quit(result.StartsWith("PASS") ? 0 : 1);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform f = FindDeep(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    private static ArticulationBody FindBody(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<ArticulationBody>() : null;
    }

    private static void Log(string message) { Debug.Log("[TwinWorldProbe] " + message); }
}
