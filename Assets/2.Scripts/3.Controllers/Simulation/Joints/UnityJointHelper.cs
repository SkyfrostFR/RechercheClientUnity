using UnityEngine;


namespace DT.Simulation
{
    public class UnityJointHelper : MonoBehaviour
    {
        public float position;
        public float velocity;
        public float acceleration;
        public float force;

        public ArticulationDrive xDrive;
        public ArticulationDrive yDrive;
        public ArticulationDrive zDrive;

        private ArticulationDrive _xDrive;
        private ArticulationDrive _yDrive;
        private ArticulationDrive _zDrive;


        ArticulationDrive xClone;
        private bool xChange, yChange, zChange;

        int axisNum = 0;

        ArticulationBody artBody;

        private void Awake()
        {
            artBody = GetComponent<ArticulationBody>();

        }


        private void LateUpdate()
        {

            if (artBody.jointType == ArticulationJointType.FixedJoint)
            {
                return;
            }

            position = artBody.jointPosition[axisNum];
            velocity = artBody.jointVelocity[axisNum];
            acceleration = artBody.jointAcceleration[axisNum];
            force = artBody.jointForce[axisNum];



            xDrive = artBody.xDrive;
            yDrive = artBody.yDrive;
            zDrive = artBody.zDrive;

            if (xChange)
            {
                // Sync enhancement 
                xClone = artBody.xDrive;
                xClone.target = _xDrive.target;
                xClone.targetVelocity = _xDrive.targetVelocity;
                xClone.driveType = _xDrive.driveType;
                artBody.xDrive = xClone;
                xChange = false;
            }
            if (yChange)
            {
                xClone = artBody.yDrive;
                xClone.target = _yDrive.target;
                xClone.targetVelocity = _yDrive.targetVelocity;
                xClone.driveType = _yDrive.driveType;
                artBody.yDrive = xClone;
                yChange = false;

            }
            if (zChange)
            {
                xClone = artBody.zDrive;
                xClone.target = _zDrive.target;
                xClone.targetVelocity = _zDrive.targetVelocity;
                xClone.driveType = _zDrive.driveType;
                artBody.zDrive = xClone;
                zChange = false;
            }

        }

        public void setXDrive(ArticulationDrive drive)
        {
            _xDrive = drive;
            xChange = true;
        }
        public void setYDrive(ArticulationDrive drive)
        {
            _yDrive = drive;
            yChange = true;
        }
        public void setZDrive(ArticulationDrive drive)
        {
            _zDrive = drive;
            zChange = true;
        }


    }

}
