using UnityEngine;
using Joint = DT.Model.Joint;
using DT.Model;
using DT.Tools;
using System;

namespace DT.Simulation
{
    [Serializable]

    public class PrismaticJointToUnity : JointController
    {
        public static JointController Create(GameObject linkObject)
        {
            PrismaticJointToUnity joint = new PrismaticJointToUnity();

            joint.artBody = linkObject.GetComponent<ArticulationBody>();
            if (joint.artBody == null) joint.artBody = linkObject.AddComponent<ArticulationBody>();
            joint.axisNum = 0;
            joint.artBody.useGravity = false;
            joint.jointHelper = linkObject.GetComponent<UnityJointHelper>();
            if (joint.jointHelper == null) joint.jointHelper = linkObject.AddComponent<UnityJointHelper>();

            return joint;
        }

        protected override void SetJointData(Joint joint)
        {
            base.SetJointData(joint);
            artBody.jointType = ArticulationJointType.PrismaticJoint;

            // Signed URDF axis — preserves the translation direction for axes like "0 0 -1".
            axisofMotion = GetAxisOfMotion(joint.Axes[0]);

            artBody.linearLockY = ArticulationDofLock.LockedMotion;
            artBody.linearLockZ = ArticulationDofLock.LockedMotion;

            if (isLimited(joint.Axes[0].Param))
            {
                artBody.linearLockX = ArticulationDofLock.LimitedMotion;
            }
            else
            {
                artBody.linearLockX = ArticulationDofLock.FreeMotion;
            }

            // Rotate anchor to match Xdrive Axis
            Vector3 axisofMotionUnity = axisofMotion.ToIndirectCoor();
            Quaternion motion = new Quaternion();
            motion.SetFromToRotation(new Vector3(1, 0, 0), -1 * axisofMotionUnity);
            artBody.anchorRotation = motion;

            if (joint.Axes[0] != null)
            {

                ArticulationDrive drive = artBody.xDrive;
                drive = setDriveParam(drive, joint.Axes[0].Param);
                artBody.xDrive = drive;

                artBody.maxLinearVelocity = joint.Axes[0].Param.dS_max.ConvertToSI();

            }

        }

    }
}
