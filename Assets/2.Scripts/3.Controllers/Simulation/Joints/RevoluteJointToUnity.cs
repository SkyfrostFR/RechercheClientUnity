using DT.Model;
using DT.Tools;
using Newtonsoft.Json.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;
using Joint = DT.Model.Joint;

namespace DT.Simulation
{
    [Serializable]

    public class RevoluteJointToUnity : JointController
    {
        public static JointController Create(GameObject linkObject)
        {
            RevoluteJointToUnity joint = new RevoluteJointToUnity();

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

            artBody.jointType = ArticulationJointType.RevoluteJoint;

            // Signed URDF axis — keeps the rotation direction (and therefore the limit
            // range) correct for axes like "0 0 -1".
            axisofMotion = GetAxisOfMotion(joint.Axes[0]);

            artBody.linearLockX = ArticulationDofLock.LockedMotion;
            artBody.linearLockY = ArticulationDofLock.LockedMotion;
            artBody.linearLockZ = ArticulationDofLock.LockedMotion;

            if (isLimited(joint.Axes[0].Param))
            {
                artBody.twistLock = ArticulationDofLock.LimitedMotion;
            }
            else
            {
                artBody.twistLock = ArticulationDofLock.FreeMotion;
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

                // Custom drive param for Revolute 
                drive.lowerLimit = joint.Axes[0].Param.Neg_sw_end.ConvertTo(Units.GetUnitFromId("DD"));
                drive.upperLimit = joint.Axes[0].Param.Pos_sw_end.ConvertTo(Units.GetUnitFromId("DD"));
                drive.target = joint.Axes[0].Param.PosHome.ConvertTo(Units.GetUnitFromId("DD"));

                
                artBody.xDrive = drive;

                // Unity's ArticulationBody.maxAngularVelocity is in rad/s (URDF <limit velocity> is rad/s
                // too). dS_max is stored in rad/s, so ConvertToSI keeps rad/s. Converting to °/s here
                // (Id "E96") made the cap ~57× too high, so the velocity limit never engaged.
                artBody.maxAngularVelocity = joint.Axes[0].Param.dS_max.ConvertToSI();

            }

        }

       

    }
}

