using DT.Simulation;
using DT.Tools;
using System.Collections.Generic;
using UnityEngine;
using static DT.Simulation.Scheduler;

public class IKTest : MonoBehaviour
{

    public InverseKinematics ik;
    public GameObject tip = null;
    public GameObject target;


    public BaseRobot robot = null;

    public Ruckig otg;

    public Ruckig.Result result;

    public bool GenPath = false;
    public List<Vector3> points = new List<Vector3>();
    public int index = 0;
    public bool followPath = false;
    public bool step = false;   
    public LineRenderer lineRenderer = null;

    public float delta_time = 0.001f;

    public double[] j;

    // Start is called before the first frame update
    void Start()
    {

        robot = GetComponent<TiagoArmsModel>().robot;


        ik = new InverseKinematics(robot.joints.Length, 6, InverseKinematics.Solver_typ.QP);
        ik.JointConstraints.qmin = new double[robot.joints.Length];
        ik.JointConstraints.qmax = new double[robot.joints.Length];
        ik.JointConstraints.dqmax = new double[robot.joints.Length];
        for (int i = 0; i < robot.joints.Length; i++)
        {
            ik.JointConstraints.qmin[i] = robot.joints[i].Param.Neg_sw_end.ConvertTo(Units.GetUnitFromId("DD"));
            ik.JointConstraints.qmax[i] = robot.joints[i].Param.Pos_sw_end.ConvertTo(Units.GetUnitFromId("DD"));
            ik.JointConstraints.dqmax[i] = robot.joints[i].Param.dS_max.ConvertTo(Units.GetUnitFromName("°/s"));
        }


        ik.SetConstraints();

        ik.Input.dlsDamping = 0.1;

        ik.Input.enableRegularization = true;
        ik.Input.regularizationWeight = 0.1;
        ik.Input.q0[3] = 90;


    }

    // Update is called once per frame
    void Update()
    {
        robot = GetComponent<TiagoArmsModel>().robot;
        


        if (tip != null )
        {
            if (GenPath)
            {
                // Generate trajectory
                points = TrajectoryGenerator();
                GenPath = false;  
            }

            FollowPath();
            FollowTarget();
        }
        else
        {
            tip = robot.TCP;
            
        }
    }


    public bool followTarget;
    void FollowTarget()
    {
        if (followTarget)
        {
            if (target == null)
            {
                target = new GameObject("Target");
                target.transform.SetParent(transform, true);
                target.transform.position = tip.transform.position;
                target.transform.rotation = tip.transform.rotation;
            }
            j = InverseKinematics.getJacobian(tip);
            ik.Input.Jacobian = j;

            Vector3 dx = (target.transform.position - tip.transform.position);

            ik.Input.dXdesired[0] = dx.x;
            ik.Input.dXdesired[1] = dx.y;
            ik.Input.dXdesired[2] = dx.z;

            Vector3 euler = InverseKinematics.toABC(target.gameObject.transform.rotation * Quaternion.Inverse(tip.gameObject.transform.rotation));

            ik.Input.dXdesired[3] = euler.z * Mathf.PI / 180.0f;
            ik.Input.dXdesired[4] = euler.y * Mathf.PI / 180.0f;
            ik.Input.dXdesired[5] = euler.x * Mathf.PI / 180.0f;


            // Get current position
            List<double> q = new List<double>();
            List<double> dq = new List<double>();
            foreach (var joint in robot.joints)
            {
                // Jacobian is in deg/s
                q.Add(joint.Info.S_act.ConvertTo(Units.GetUnitFromId("DD")));
                dq.Add(joint.Info.dS_act.ConvertTo(Units.GetUnitFromName("°/s")));
            }
            ik.Input.q = q.ToArray();
            ik.Input.dq = dq.ToArray();

            if (ik.Output.status == InverseKinematics.SolverStatus_typ.RESULT_OK) // at least one loop
            {
               ik.Input.q = ik.Output.q;
               ik.Input.dq = ik.Output.dq;
            }

            // Get deltaTime
            ik.Input.deltaTime = delta_time;


            // Compute IK
            ik.Update();

            // Get & apply result
            for (int i = 0; i < robot.joints.Length; i++)
            {
                if (ik.Output.status == InverseKinematics.SolverStatus_typ.RESULT_OK)
                {
                    robot.jointsPosition[i] = ((float)ik.Output.q[i]);
                }
            }


        }

    }

    void FollowPath()
    {
        if (followPath && step)
        {

            // Wait for robot to achieve position goal 
            bool positionOK = true;
            for (int i = 0;i < robot.joints.Length;i++)
            {
                positionOK &= Mathf.Abs(robot.jointsPosition[i] - robot.jointsActualPosition[i])<0.001;
            }
            if (!positionOK) return;

            //step = false;
            j = InverseKinematics.getJacobian(tip);
            ik.Input.Jacobian = j;


            Vector3 dx = (points[index] - tip.transform.position);

            ik.Input.dXdesired[0] = dx.x;
            ik.Input.dXdesired[1] = dx.y;
            ik.Input.dXdesired[2] = dx.z;


            ik.Input.dXdesired[3] = 0;
            ik.Input.dXdesired[4] = 0f;
            ik.Input.dXdesired[5] = 0f;




            if (index < (points.Count - 1))
            {
                index++;
            }

            // Get current position
            List<double> q = new List<double>();
            List<double> dq = new List<double>();
            foreach (var joint in robot.joints)
            {
                // Jacobian is in deg/s
                q.Add(joint.Info.S_act.ConvertTo(Units.GetUnitFromId("DD")));
                dq.Add(joint.Info.dS_act.ConvertTo(Units.GetUnitFromName("°/s")));
            }
            ik.Input.q = q.ToArray();
            ik.Input.dq = dq.ToArray();
            if (ik.Output.status == InverseKinematics.SolverStatus_typ.RESULT_OK) // at least one loop
            {
                ik.Input.q = ik.Output.q;
                ik.Input.dq = ik.Output.dq;
               
            }

            // Get deltaTime
            ik.Input.deltaTime = 0.001;

            // Compute IK
            ik.Update();

            // Get & apply result
            for (int i = 0; i < robot.joints.Length; i++)
            {
                if (ik.Output.status == InverseKinematics.SolverStatus_typ.RESULT_OK)
                {
                    robot.jointsPosition[i] = ((float)ik.Output.q[i]);
                }
            }

            
        }
    }

    List<Vector3> TrajectoryGenerator()
    {


        otg = new Ruckig(3, delta_time);
        otg.Input.synchronization = Ruckig.Synchronization.Phase;
        otg.Input.control_interface = Ruckig.ControlInterface.Position;



        otg.Input.max_velocity[0] = 2;
        otg.Input.max_velocity[1] = 2;
        otg.Input.max_velocity[2] = 2;

        otg.Input.max_acceleration[0] = 0.5;
        otg.Input.max_acceleration[1] = 0.5;
        otg.Input.max_acceleration[2] = 0.5;

        otg.Input.max_jerk[0] = 1;
        otg.Input.max_jerk[1] = 1;
        otg.Input.max_jerk[2] = 1;


        List<Vector3> traj = new List<Vector3>();   
        otg.Input.current_position = tip.transform.position.toDouble();
        otg.Input.target_position = target.transform.position.toDouble();


        while (otg.Update() == Ruckig.Result.Working)
        {
            otg.OutputToInput();
            traj.Add(otg.Output.new_position.toVector3());
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, otg.Output.new_position.toVector3());
        }
        otg.OutputToInput();
        traj.Add(otg.Output.new_position.toVector3());
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, otg.Output.new_position.toVector3());

        return traj;

    }


    


}
