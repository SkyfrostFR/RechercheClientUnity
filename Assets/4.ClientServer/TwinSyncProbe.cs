using System.Collections.Generic;
using System.Text;
using RosSharp.RosBridgeClient;
using UnityEngine;
using Msg = Tiago.Ros;

/// <summary>
/// Automated check that both arms of the twin and of Gazebo stay in sync, without a headset.
///
/// Only installed when the player (or editor) is started with <c>-twinProbe</c>, so normal
/// and VR runs never see it. Once TiagoDualArmRosSync has homed and handed over to the IK,
/// it scripts the IK target handles instead of the VR controllers:
///   1. MOVE  — both handles follow a slow Lissajous loop (±moveAmplitude) for moveSeconds;
///   2. HOLD  — handles stop; after settleSeconds the steady-state error is measured;
///   3. EXTERNAL — TiagoDualArmRosSync is set to Off and "EXTERNAL waiting" is logged: a test
///             script moves Gazebo's arms from ROS during externalSeconds, and the shadow must
///             follow (Gazebo → Unity). Following is restored afterwards;
///   4. DONE  — a PASS/FAIL line is logged and, with -twinProbeQuit, the app exits.
/// The run also fails if Gazebo's arms travelled less than minTravelDeg in MOVE or EXTERNAL,
/// so a twin that never moved cannot pass.
///
/// Every second it logs, per arm, the largest joint gap in degrees between:
///   • cmd→gz : the commanded twin (Digital_Twin, IK-driven) and Gazebo's /joint_states
///             — Unity → Gazebo direction;
///   • gz→sh  : Gazebo's /joint_states and the shadow robot Unity draws from it
///             — Gazebo → Unity direction.
/// Lines start with "[TwinSyncProbe]" so a log can be grepped.
/// </summary>
public class TwinSyncProbe : MonoBehaviour
{
    public float moveAmplitude = 0.08f;
    public float movePeriod = 8f;
    public float moveSeconds = 24f;
    public float settleSeconds = 4f;
    public float holdSeconds = 4f;
    public float passToleranceDeg = 3f;
    public float startTimeoutSeconds = 120f;
    public float externalSeconds = 20f;
    public float minTravelDeg = 10f;

    private enum Phase { Waiting, Move, Hold, External, Done }

    private static readonly string[] Sides = { "left", "right" };

    private TiagoDualArmIK ik;
    private TiagoDualArmRosSync sync;
    // Every enabled TiagoDualArmRosSync has to stop for Gazebo to be free (see TwinSingleArmSync).
    private TiagoDualArmRosSync[] syncs;
    private TiagoShadowFromRos shadow;
    private RosConnector connector;
    private bool quitWhenDone;

    private Phase phase = Phase.Waiting;
    private float phaseStart, nextReport;
    private readonly Dictionary<string, Vector3> handleHome = new Dictionary<string, Vector3>();
    private readonly Dictionary<string, ArticulationBody> cmdBodies = new Dictionary<string, ArticulationBody>();
    private readonly Dictionary<string, ArticulationBody> shadowBodies = new Dictionary<string, ArticulationBody>();
    private float worstMoveCmd, worstHoldCmd, worstHoldShadow, worstExternalShadow;
    private float moveTravel, externalTravel;
    private TiagoDualArmRosSync.FollowMode modeBeforeExternal;
    private readonly Dictionary<string, Vector2> range = new Dictionary<string, Vector2>();

    // /joint_states, copied on the socket thread.
    private readonly object gate = new object();
    private readonly Dictionary<string, double> gazeboRad = new Dictionary<string, double>();
    private bool subscribed;
    private int jointStateFrames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-twinProbe") < 0) return;
        var go = new GameObject("TwinSyncProbe");
        var probe = go.AddComponent<TwinSyncProbe>();
        probe.quitWhenDone = System.Array.IndexOf(args, "-twinProbeQuit") >= 0;
        Log("installed.");
    }

    private void Start()
    {
        ik = FindAnyObjectByType<TiagoDualArmIK>();
        syncs = System.Array.FindAll(FindObjectsByType<TiagoDualArmRosSync>(FindObjectsSortMode.None),
                                     sy => sy.enabled);
        sync = syncs.Length > 0 ? syncs[0] : null;
        shadow = FindAnyObjectByType<TiagoShadowFromRos>();
        connector = sync != null ? sync.rosConnector : FindAnyObjectByType<RosConnector>();
        if (ik == null || sync == null || shadow == null || connector == null)
        {
            Finish($"FAIL missing components (ik={ik != null} sync={sync != null} " +
                   $"shadow={shadow != null} connector={connector != null})");
        }
    }

    private void Update()
    {
        if (phase == Phase.Done) return;
        if (!subscribed) TrySubscribe();

        switch (phase)
        {
            case Phase.Waiting:
                if (ReadyToMove()) Enter(Phase.Move);
                else if (Time.time > startTimeoutSeconds)
                    Finish($"FAIL never reached Following (connected={IsConnected()}, " +
                           $"jointStates={jointStateFrames}, " +
                           $"leftTarget={ik.leftArm.target != null}, rightTarget={ik.rightArm.target != null})");
                break;

            case Phase.Move:
                DriveHandles(Time.time - phaseStart);
                if (Time.time - phaseStart > moveSeconds) Enter(Phase.Hold);
                break;

            case Phase.Hold:
                if (Time.time - phaseStart > settleSeconds + holdSeconds) Enter(Phase.External);
                break;

            case Phase.External:
                if (Time.time - phaseStart > externalSeconds)
                {
                    externalTravel = Travel();
                    foreach (var sy in syncs) sy.mode = modeBeforeExternal;
                    bool pass = worstHoldCmd <= passToleranceDeg && worstHoldShadow <= passToleranceDeg &&
                                worstExternalShadow <= passToleranceDeg &&
                                moveTravel >= minTravelDeg && externalTravel >= minTravelDeg;
                    Finish($"{(pass ? "PASS" : "FAIL")} unity->gazebo: hold cmd->gz={worstHoldCmd:0.00}deg, " +
                           $"worst while moving {worstMoveCmd:0.00}deg, travel {moveTravel:0.0}deg | " +
                           $"gazebo->unity: worst gz->shadow={Mathf.Max(worstHoldShadow, worstExternalShadow):0.00}deg, " +
                           $"external travel {externalTravel:0.0}deg | tolerance {passToleranceDeg}deg, " +
                           $"min travel {minTravelDeg}deg, jointStates={jointStateFrames}");
                }
                break;
        }

        if (phase != Phase.Waiting && phase != Phase.Done && Time.time >= nextReport)
        {
            nextReport = Time.time + 1f;
            Report();
        }
    }

    private bool IsConnected()
    {
        return connector != null && connector.RosSocket != null && connector.IsConnected.WaitOne(0);
    }

    private bool ReadyToMove()
    {
        if (ik.leftArm.target == null || ik.rightArm.target == null) return false;
        if (!ik.leftArm.followTarget || !ik.rightArm.followTarget) return false;
        if (sync.mode == TiagoDualArmRosSync.FollowMode.Off) return false;
        if (jointStateFrames == 0) return false;
        return BindBodies();
    }

    private bool BindBodies()
    {
        TiagoArmsModel cmdModel = sync.model != null ? sync.model : sync.GetComponent<TiagoArmsModel>();
        TiagoArmsModel shModel = shadow.shadowModel != null ? shadow.shadowModel : shadow.GetComponent<TiagoArmsModel>();
        if (cmdModel == null || shModel == null) return false;

        foreach (string side in Sides)
            for (int i = 1; i <= 7; i++)
            {
                string n = $"arm_{side}_{i}_joint";
                ArticulationBody c = Find(cmdModel.transform, n), s = Find(shModel.transform, n);
                if (c == null || s == null) return false;
                cmdBodies[n] = c;
                shadowBodies[n] = s;
            }
        return true;
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
        if (next == Phase.Move)
        {
            handleHome["left"] = ik.leftArm.target.transform.position;
            handleHome["right"] = ik.rightArm.target.transform.position;
            Log($"following in {sync.mode} mode; moving both IK handles for {moveSeconds}s.");
        }
        else if (next == Phase.Hold)
        {
            moveTravel = Travel();
            Log($"handles stopped (arms travelled {moveTravel:0.0}deg); measuring steady state.");
        }
        else if (next == Phase.External)
        {
            modeBeforeExternal = sync.mode;
            foreach (var sy in syncs) sy.mode = TiagoDualArmRosSync.FollowMode.Off;
            Log($"EXTERNAL waiting: Unity stopped commanding for {externalSeconds}s; move Gazebo's arms now.");
        }
        range.Clear();
    }

    private void DriveHandles(float t)
    {
        float w = 2f * Mathf.PI / movePeriod;
        // Ramp in over one second so the first step is not a jump.
        float a = moveAmplitude * Mathf.Clamp01(t);
        var offset = new Vector3(Mathf.Sin(w * t), 0.6f * Mathf.Sin(2f * w * t), 0.5f * (1f - Mathf.Cos(w * t))) * a;
        ik.leftArm.target.transform.position = handleHome["left"] + offset;
        ik.rightArm.target.transform.position = handleHome["right"] + new Vector3(-offset.x, offset.y, offset.z);
    }

    private void Report()
    {
        var sb = new StringBuilder();
        float worstCmd = 0f, worstShadow = 0f;
        foreach (string side in Sides)
        {
            float cmdGap = 0f, shGap = 0f;
            for (int i = 1; i <= 7; i++)
            {
                string n = $"arm_{side}_{i}_joint";
                double gz;
                lock (gate) { if (!gazeboRad.TryGetValue(n, out gz)) continue; }
                float gzDeg = (float)(gz * Mathf.Rad2Deg);
                cmdGap = Mathf.Max(cmdGap, AngleGap(Deg(cmdBodies[n]), gzDeg));
                shGap = Mathf.Max(shGap, AngleGap(Deg(shadowBodies[n]), gzDeg));
                Vector2 r;
                range[n] = range.TryGetValue(n, out r)
                         ? new Vector2(Mathf.Min(r.x, gzDeg), Mathf.Max(r.y, gzDeg))
                         : new Vector2(gzDeg, gzDeg);
            }
            sb.Append($"{side}: cmd->gz={cmdGap:0.00} gz->shadow={shGap:0.00}  ");
            worstCmd = Mathf.Max(worstCmd, cmdGap);
            worstShadow = Mathf.Max(worstShadow, shGap);
        }

        if (phase == Phase.Move) worstMoveCmd = Mathf.Max(worstMoveCmd, worstCmd);
        if (phase == Phase.External) worstExternalShadow = Mathf.Max(worstExternalShadow, worstShadow);
        if (phase == Phase.Hold && Time.time - phaseStart > settleSeconds)
        {
            worstHoldCmd = Mathf.Max(worstHoldCmd, worstCmd);
            worstHoldShadow = Mathf.Max(worstHoldShadow, worstShadow);
        }
        Log($"{phase} t={Time.time - phaseStart:0.0}s {sb}(deg)");
    }

    /// <summary>Largest per-joint excursion of Gazebo's arms since the phase began, degrees.</summary>
    private float Travel()
    {
        float t = 0f;
        foreach (Vector2 r in range.Values) t = Mathf.Max(t, r.y - r.x);
        return t;
    }

    private static float Deg(ArticulationBody b)
    {
        return b.jointPosition.dofCount > 0 ? b.jointPosition[0] * Mathf.Rad2Deg : 0f;
    }

    private static float AngleGap(float a, float b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a, b));
    }

    private void TrySubscribe()
    {
        if (!IsConnected()) return;
        connector.RosSocket.Subscribe<Msg.JointState>("/joint_states", OnJointState, 50, 1);
        subscribed = true;
    }

    // Socket thread: copy only.
    private void OnJointState(Msg.JointState m)
    {
        if (m == null || m.name == null || m.position == null) return;
        lock (gate)
        {
            for (int i = 0; i < m.name.Length && i < m.position.Length; i++)
                if (m.position[i].HasValue) gazeboRad[m.name[i]] = m.position[i].Value;
            jointStateFrames++;
        }
    }

    private void Finish(string result)
    {
        phase = Phase.Done;
        Log("RESULT " + result);
        if (quitWhenDone) Application.Quit(result.StartsWith("PASS") ? 0 : 1);
    }

    private static ArticulationBody Find(Transform root, string name)
    {
        if (root.name == name) return root.GetComponent<ArticulationBody>();
        for (int i = 0; i < root.childCount; i++)
        {
            ArticulationBody found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void Log(string message) { Debug.Log("[TwinSyncProbe] " + message); }
}
