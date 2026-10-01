using System;
using UnityEngine;


namespace DT.Simulation
{
    [Serializable]

    public class FixedJointToUnity : JointController
    {
        public static JointController Create(GameObject linkObject)
        {
            FixedJointToUnity joint = new FixedJointToUnity();

            joint.artBody = linkObject.GetComponent<ArticulationBody>();
            if (joint.artBody == null) joint.artBody = linkObject.AddComponent<ArticulationBody>();
            joint.artBody.jointType = ArticulationJointType.FixedJoint;
            joint.artBody.useGravity = false;
            joint.jointHelper = linkObject.GetComponent<UnityJointHelper>();
            if (joint.jointHelper == null) joint.jointHelper = linkObject.AddComponent<UnityJointHelper>();

            Quaternion motion = new Quaternion();
            motion.SetFromToRotation(new Vector3(1, 0, 0), -1 * new Vector3(0, 0, 1).ToIndirectCoor());
            joint.artBody.anchorRotation = motion;

            return joint;
        }
    }
}
