using System.Text;
using UnityEngine;

/// <summary>
/// Forward-kinematics check of the twin against Gazebo. Only installed with <c>-twinFkProbe</c>.
/// Unity sends nothing (TiagoDualArmRosSync is held Off, no homing); the shadow robot shows
/// /joint_states, and every second this logs, in the robot frame with ROS axes, where the
/// shadow puts each left-arm joint frame. Compare with `tf_echo base_footprint arm_left_N_link`
/// in Gazebo: same joint values must give the same positions.
/// </summary>
public class TwinFkProbe : MonoBehaviour
{
    private TiagoShadowFromRos shadow;
    private Transform root;
    private float next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-twinFkProbe") < 0) return;
        foreach (var s in FindObjectsByType<TiagoDualArmRosSync>(FindObjectsSortMode.None))
        {
            s.homeOnStart = false;
            s.mode = TiagoDualArmRosSync.FollowMode.Off;
            s.modeAfterHome = TiagoDualArmRosSync.FollowMode.Off;
        }
        new GameObject("TwinFkProbe").AddComponent<TwinFkProbe>();
    }

    private void Start()
    {
        shadow = FindAnyObjectByType<TiagoShadowFromRos>();
        root = shadow != null ? shadow.transform.root : null;
    }

    private void Update()
    {
        if (shadow == null || shadow.shadowModel == null || Time.time < next) return;
        next = Time.time + 1f;
        var sb = new StringBuilder("[TwinFkProbe]");
        foreach (string n in new[] { "arm_left_1_joint", "arm_left_2_joint", "arm_left_3_joint", "arm_left_4_joint",
                                     "arm_left_5_joint", "arm_left_6_joint", "arm_left_7_joint",
                                     "left_hand_gripper_left_finger_joint", "left_hand_gripper_right_finger_joint" })
        {
            Transform t = Find(shadow.shadowModel.transform, n);
            if (t == null) { sb.Append($" {n}=?"); continue; }
            Vector3 p = root.InverseTransformPoint(t.position);
            sb.Append($" {n.Replace("arm_left_", "L").Replace("_joint", "")}=({p.z:0.000},{-p.x:0.000},{p.y:0.000})");
        }
        Debug.Log(sb.ToString());

        // Both models, side by side: where their bases and shoulders are.
        var sync = FindAnyObjectByType<TiagoDualArmRosSync>();
        var models = new[] { ("shadow", shadow.shadowModel), ("twin", sync != null ? sync.model : null) };
        var sb2 = new StringBuilder("[TwinFkProbe] models:");
        foreach (var (label, m) in models)
        {
            if (m == null) continue;
            Vector3 mp = root.InverseTransformPoint(m.transform.position);
            Transform l1 = Find(m.transform, "arm_left_1_joint");
            Vector3 p1 = l1 != null ? root.InverseTransformPoint(l1.position) : Vector3.zero;
            ArticulationBody rb = null;
            foreach (var b in m.GetComponentsInChildren<ArticulationBody>()) if (b.isRoot) { rb = b; break; }
            sb2.Append($" {label}: model.y={mp.y:0.000} L1.y={p1.y:0.000} root={(rb != null ? rb.name + " immovable=" + rb.immovable + " y=" + root.InverseTransformPoint(rb.transform.position).y.ToString("0.000") : "none")}");
        }
        Debug.Log(sb2.ToString());
    }

    private static Transform Find(Transform r, string name)
    {
        if (r.name == name) return r;
        for (int i = 0; i < r.childCount; i++)
        {
            Transform f = Find(r.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }
}
