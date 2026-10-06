using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the whole twin rigidly — robot, cameras, VR rig — and everything that has to ride
/// along with it, so the arms do not react to a base motion:
///   • the prefab root transform (robot models, XR Origin, cameras are all below it);
///   • every ArticulationBody root below it, through TeleportRoot, otherwise PhysX keeps
///     the articulations where they were;
///   • the IK target handles, which TiagoDualArmIK keeps outside the robot hierarchy: left
///     behind, the IK would stretch the arms back towards them;
///   • the reference-space state of TiagoVrTargetController (box centre, grab poses), which
///     is world-relative when it has no limitReference.
/// </summary>
public class TwinRobotMover
{
    private readonly Transform root;
    private readonly TiagoDualArmIK ik;
    private readonly TiagoVrTargetController vr;
    private readonly List<ArticulationBody> articulationRoots = new List<ArticulationBody>();

    public Transform Root { get { return root; } }

    public TwinRobotMover(Transform root, TiagoDualArmIK ik, TiagoVrTargetController vr)
    {
        this.root = root;
        this.ik = ik;
        this.vr = vr;
    }

    /// <summary>Move the root to (position, yaw about +y in degrees), keeping it upright.</summary>
    public void MoveTo(Vector3 position, float yawDeg)
    {
        Quaternion target = Quaternion.Euler(0f, yawDeg, 0f) * TiltOf(root.rotation);
        Quaternion dRot = target * Quaternion.Inverse(root.rotation);
        Vector3 pivot = root.position;
        Vector3 dPos = position - pivot;
        if (dPos.sqrMagnitude < 1e-10f && Quaternion.Angle(dRot, Quaternion.identity) < 1e-3f) return;

        root.SetPositionAndRotation(position, target);
        TeleportArticulations();

        if (ik != null)
        {
            MoveHandle(ik.leftArm, pivot, dPos, dRot);
            MoveHandle(ik.rightArm, pivot, dPos, dRot);
        }
        // With a limitReference below the root, the stored poses already move with it.
        if (vr != null && (vr.limitReference == null || !vr.limitReference.IsChildOf(root)))
        {
            ShiftHand(vr.leftHand, pivot, dPos, dRot);
            ShiftHand(vr.rightHand, pivot, dPos, dRot);
        }
    }

    // The ArticulationBodies are built at runtime by TiagoArmsModel, so look them up lazily.
    private void TeleportArticulations()
    {
        if (articulationRoots.Count == 0)
            foreach (ArticulationBody b in root.GetComponentsInChildren<ArticulationBody>(true))
                if (b.isRoot) articulationRoots.Add(b);

        foreach (ArticulationBody b in articulationRoots)
            if (b != null && b.isActiveAndEnabled)
                b.TeleportRoot(b.transform.position, b.transform.rotation);
    }

    private void MoveHandle(TiagoDualArmIK.ArmIK arm, Vector3 pivot, Vector3 dPos, Quaternion dRot)
    {
        if (arm == null || arm.target == null) return;
        Transform t = arm.target.transform;
        if (t.IsChildOf(root)) return;      // already moved with the root
        t.SetPositionAndRotation(pivot + dPos + dRot * (t.position - pivot), dRot * t.rotation);
    }

    /// <summary>
    /// TiagoVrTargetController without a limitReference stores positions as
    /// (world - spawnRefOrigin): moving the origin rigidly and rotating every stored vector
    /// by the same rotation keeps them describing the same robot-relative quantities.
    /// </summary>
    private static void ShiftHand(TiagoVrTargetController.Hand hand, Vector3 pivot, Vector3 dPos, Quaternion dRot)
    {
        if (hand == null) return;
        hand.spawnRefOrigin = pivot + dPos + dRot * (hand.spawnRefOrigin - pivot);
        hand.prevPos = dRot * hand.prevPos;
        hand.prevVel = dRot * hand.prevVel;
        hand.velocity = dRot * hand.velocity;
        hand.grabControllerRef = dRot * hand.grabControllerRef;
        hand.grabTargetRef = dRot * hand.grabTargetRef;
        hand.grabControllerRot = dRot * hand.grabControllerRot;
        hand.grabTargetRot = dRot * hand.grabTargetRot;
    }

    private static Quaternion TiltOf(Quaternion q)
    {
        return Quaternion.Inverse(Quaternion.Euler(0f, q.eulerAngles.y, 0f)) * q;
    }
}
