using System.Collections.Generic;
using DT.Simulation;
using DT.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Per-arm inverse kinematics for the generated TiagoArmsModel (dual 7-DoF arms).
///
/// Each arm is solved independently: InverseKinematics.getJacobian(tip) returns the Jacobian for
/// the chain from the root down to whatever ArticulationBody GameObject you pass as `tip`, so the
/// left/right arms are isolated automatically. The solver output is written back into the matching
/// slice of robot.jointsPosition (which TiagoArmsModel.Update → RobotUpdate then commands).
///
/// Setup (Inspector):
///   • model        → the TiagoArmsModel component (its built BaseRobot is read at runtime)
///   • <arm>.tip    → the arm's LAST revolute joint GameObject (the deepest one with an
///                    ArticulationBody, e.g. "arm_left_7_joint"). Its transform is the end point.
///   • <arm>.followTarget → spawns the target (a tip clone, in this script's scene, detached from
///                    the robot, seeded at the current end-effector pose) when enabled and destroys
///                    it when disabled. Drag the spawned handle to drive the arm.
///   • <arm>.target → leave empty to use the auto-spawned handle; assign one to override it.
///   • <arm>.jointStartIndex / dof → which slice of robot.joints this arm owns (defaults below are
///                    correct for Tiago++_arms: left = 0, right = 9, 7 DOF each).
/// </summary>
public class TiagoDualArmIK : MonoBehaviour
{
    [System.Serializable]
    public class ArmIK
    {
        public string name = "arm";
        public GameObject tip;            // deepest ArticulationBody of the arm (e.g. arm_left_7_joint)
        [Tooltip("If 'tip' is empty, the joint GameObject with this name is found under the model at runtime.")]
        public string tipObjectName = "";  // e.g. "arm_left_7_joint" — the built hierarchy only exists in Play
        public GameObject target;         // pose to reach — auto-spawned at runtime if left empty
        public bool followTarget = true;
        public int jointStartIndex = 0;   // first index of this arm inside robot.joints / jointsPosition
        public int dof = 7;               // revolute DOF from root to tip (must match the Jacobian width)

        [HideInInspector] public InverseKinematics ik;
        [HideInInspector] public GameObject spawnedTarget;   // the auto-created target (null if assigned manually)
    }

    [Tooltip("The generated model component that owns the BaseRobot. If left empty, GetComponent is tried.")]
    public TiagoArmsModel model;

    public ArmIK leftArm  = new ArmIK { name = "left",  jointStartIndex = 0, dof = 7, tipObjectName = "arm_left_7_joint" };
    public ArmIK rightArm = new ArmIK { name = "right", jointStartIndex = 9, dof = 7, tipObjectName = "arm_right_7_joint" };

    [Header("Solver")]
    public InverseKinematics.Solver_typ solver = InverseKinematics.Solver_typ.QP;
    [Tooltip("Cartesian convergence gain: commanded EE velocity = gain × pose error. Higher = snappier " +
             "(joint speed is still capped by each joint's dS_max, so this can't exceed the hardware limit).")]
    public float  cartesianGain       = 3f;
    public double dlsDamping          = 0.1;
    public bool   enableRegularization = true;
    public double regularizationWeight = 0.01;
    public double[] regularizationPosition = new double[7];

    private BaseRobot robot;
    private bool ready;

    private void Update()
    {
        if (!ready && !TryInit()) return;   // wait until TiagoArmsModel has built the robot

        UpdateTarget(leftArm);
        SolveArm(leftArm);
        UpdateTarget(rightArm);
        SolveArm(rightArm);
    }

    private void OnDestroy()
    {
        DespawnTarget(leftArm);
        DespawnTarget(rightArm);
    }

    /// <summary>Target lifecycle follows followTarget: spawn it when enabled, remove it when disabled.</summary>
    private void UpdateTarget(ArmIK a)
    {
        if (a.followTarget)
        {
            if (a.target == null && a.tip != null) SpawnTarget(a);
        }
        else if (a.spawnedTarget != null)
        {
            DespawnTarget(a);
        }
    }

    // Clone the tip into a free, inert pose handle in THIS script's scene, seeded at the current
    // end-effector pose. Re-seeding on every enable means IK always starts at zero error (no jump).
    private void SpawnTarget(ArmIK a)
    {
        GameObject t = Instantiate(a.tip);
        t.name = a.tip.name + "_IK_target";
        t.transform.SetParent(null, true);   // totally outside the robot hierarchy
        t.transform.SetPositionAndRotation(a.tip.transform.position, a.tip.transform.rotation);
        t.transform.localScale = a.tip.transform.lossyScale;
        MakeInert(t);                                        // passive: no physics, no scripts
        SceneManager.MoveGameObjectToScene(t, gameObject.scene);   // same scene as this component

        a.target = t;
        a.spawnedTarget = t;
    }

    private void DespawnTarget(ArmIK a)
    {
        if (a == null || a.spawnedTarget == null) return;
        if (a.target == a.spawnedTarget) a.target = null;
        Destroy(a.spawnedTarget);
        a.spawnedTarget = null;
    }

    private bool TryInit()
    {
        if (model == null) model = GetComponent<TiagoArmsModel>();
        if (model == null || model.robot == null || model.robot.joints == null) return false;

        robot = model.robot;
        InitArm(leftArm);
        InitArm(rightArm);
        ready = true;
        return true;
    }

    private void InitArm(ArmIK a)
    {
        // Resolve the tip from the runtime-built hierarchy if it wasn't assigned in the Inspector.
        if (a.tip == null && !string.IsNullOrEmpty(a.tipObjectName))
        {
            Transform t = FindDeep(model.transform, a.tipObjectName);
            if (t != null) a.tip = t.gameObject;
            else Debug.LogWarning($"[TiagoDualArmIK] {a.name}: could not find tip '{a.tipObjectName}' under {model.name}.");
        }

        a.ik = new InverseKinematics(a.dof, 6, solver);
        a.ik.JointConstraints.qmin  = new double[a.dof];
        a.ik.JointConstraints.qmax  = new double[a.dof];
        a.ik.JointConstraints.dqmax = new double[a.dof];

        for (int i = 0; i < a.dof; i++)
        {
            var p = robot.joints[a.jointStartIndex + i].Param;
            a.ik.JointConstraints.qmin[i]  = p.Neg_sw_end.ConvertTo(Units.GetUnitFromId("DD"));   // deg
            a.ik.JointConstraints.qmax[i]  = p.Pos_sw_end.ConvertTo(Units.GetUnitFromId("DD"));   // deg
            a.ik.JointConstraints.dqmax[i] = p.dS_max.ConvertTo(Units.GetUnitFromName("°/s"));
        }
        a.ik.SetConstraints();

        a.ik.Input.dlsDamping           = dlsDamping;
        a.ik.Input.enableRegularization = enableRegularization;
        a.ik.Input.regularizationWeight = regularizationWeight;
        a.ik.Input.q0 = regularizationPosition;
    }

    private void SolveArm(ArmIK a)
    {
        if (!a.followTarget || a.ik == null || a.tip == null || a.target == null) return;

        // 1. Jacobian of this arm's chain (root → tip). 6 rows × dof columns, flattened.
        double[] J = InverseKinematics.getJacobian(a.tip);
        if (J == null) return;
        if (J.Length != a.dof * 6)
        {
            Debug.LogError($"[TiagoDualArmIK] {a.name}: Jacobian has {J.Length / 6} DOF but dof={a.dof}. " +
                           "Point 'tip' at the arm's last revolute joint and set dof to match.");
            return;
        }
        a.ik.Input.Jacobian = J;

        // 2. Desired Cartesian velocity = gain × pose error (position m/s, orientation rad/s).
        Vector3 dx = (a.target.transform.position - a.tip.transform.position) * cartesianGain;
        a.ik.Input.dXdesired[0] = dx.x;
        a.ik.Input.dXdesired[1] = dx.y;
        a.ik.Input.dXdesired[2] = dx.z;

        Vector3 euler = InverseKinematics.toABC(a.target.transform.rotation *
                                                Quaternion.Inverse(a.tip.transform.rotation));
        a.ik.Input.dXdesired[3] = euler.z * Mathf.Deg2Rad * cartesianGain;
        a.ik.Input.dXdesired[4] = euler.y * Mathf.Deg2Rad * cartesianGain;
        a.ik.Input.dXdesired[5] = euler.x * Mathf.Deg2Rad * cartesianGain;

        // 3. Current joint state (deg, deg/s) for this arm.
        for (int i = 0; i < a.dof; i++)
        {
            var info = robot.joints[a.jointStartIndex + i].Info;
            a.ik.Input.q[i]  = info.S_act.ConvertTo(Units.GetUnitFromId("DD"));
            a.ik.Input.dq[i] = info.dS_act.ConvertTo(Units.GetUnitFromName("°/s"));
        }
        // Warm-start from the previous solution once the solver is running.
        if (a.ik.Output.status == InverseKinematics.SolverStatus_typ.RESULT_OK)
        {
            a.ik.Input.q  = a.ik.Output.q;
            a.ik.Input.dq = a.ik.Output.dq;
        }
        a.ik.Input.deltaTime = Time.deltaTime;   // real elapsed time → real-time motion (not slow-mo)

        // 4. Solve and write the new joint targets back into this arm's slice.
        a.ik.Update();
        if (a.ik.Output.status == InverseKinematics.SolverStatus_typ.RESULT_OK)
            for (int i = 0; i < a.dof; i++)
                robot.jointsPosition[a.jointStartIndex + i] = (float)a.ik.Output.q[i];
    }

    /// <summary>
    /// Turn a cloned subtree into a passive visual handle: remove the articulation, colliders and
    /// scripts so it carries no physics and runs no robot logic — only its transform & meshes remain.
    /// ArticulationBodies are destroyed leaf→root so Unity never sees a non-root body destroyed first.
    /// </summary>
    private static void MakeInert(GameObject root)
    {
        var bodies = root.GetComponentsInChildren<ArticulationBody>(true);
        for (int i = bodies.Length - 1; i >= 0; i--) Destroy(bodies[i]);
        foreach (var col in root.GetComponentsInChildren<Collider>(true)) Destroy(col);
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
    }

    /// <summary>Depth-first search for a descendant transform by exact name.</summary>
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
