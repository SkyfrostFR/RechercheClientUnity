using DT.Controller;
using DT.Model;
using DT.Tools;
using System;
using UnityEngine;
using Joint = DT.Model.Joint;

namespace DT.Simulation
{
    [Serializable]

    public class PlanarJointToUnity : JointController
    {
        public static JointController Create(GameObject linkObject)
        {
            PlanarJointToUnity joint = new PlanarJointToUnity();

            joint.artBody = linkObject.GetComponent<ArticulationBody>();
            if (joint.artBody == null) joint.artBody = linkObject.AddComponent<ArticulationBody>();
            joint.axisNum = 1;
            joint.artBody.useGravity = false;
            joint.jointHelper = linkObject.GetComponent<UnityJointHelper>();
            if (joint.jointHelper == null) joint.jointHelper = linkObject.AddComponent<UnityJointHelper>();

            return joint;
        }


        protected override void SetJointData(Joint joint)
        {
            base.SetJointData(joint);
            artBody.jointType = ArticulationJointType.PrismaticJoint;

            // Start with Xdrive
            switch (joint.Axes[0].Type)
            {
                case Axe.Axe_enum.LinX:
                    axisofMotion = new Vector3(1, 0, 0);
                    break;
                case Axe.Axe_enum.LinY:
                    axisofMotion = new Vector3(0, 1, 0);
                    break;
                case Axe.Axe_enum.LinZ:
                    axisofMotion = new Vector3(0, 0, 1);
                    break;
                default:
                    axisofMotion = new Vector3(1, 0, 0);
                    break;
            }

            
            artBody.linearLockZ = ArticulationDofLock.LockedMotion;

            if (isLimited(joint.Axes[0].Param))
            {
                artBody.linearLockX = ArticulationDofLock.LimitedMotion;
            }
            else
            {
                artBody.linearLockX = ArticulationDofLock.FreeMotion;
            }
            if (isLimited(joint.Axes[1].Param))
            {
                artBody.linearLockY = ArticulationDofLock.LimitedMotion;
            }
            else
            {
                artBody.linearLockY = ArticulationDofLock.FreeMotion;
            }

            // Rotate anchor to match Xdrive Axis
            // TODO : Test That !!!
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

            if (joint.Axes[1] != null)
            {
                ArticulationDrive drive = artBody.xDrive;
                drive = setDriveParam(drive, joint.Axes[1].Param);
                artBody.xDrive = drive;
                artBody.maxLinearVelocity = joint.Axes[1].Param.dS_max.ConvertToSI();
            }


        }

    }
}
