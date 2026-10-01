using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public class InverseKinematics
{
    private int jointDof, opDof;
    private IntPtr ptr = IntPtr.Zero;
    private Solver_typ solver;

    InternInput_typ _Input;
    InternOutput_typ _Output;
    InternJointConstraints_typ _JointConstraints;

    public Input_typ Input;
    public Output_typ Output;
    public JointConstraints_typ JointConstraints;

    #region Types
    [Serializable]
    public enum Solver_typ
    {
        QP,
        DLS,
        PINV
    };
    [Serializable]
    public enum SolverStatus_typ
    {
        NONE,
        INITIATED,
        RESULT_OK,
        ERROR,
        NOT_IMPLEMENTED
    };

    

    [Serializable]
    public struct JointConstraints_typ
    {
        
        public double[] qmin;
        public double[] qmax;
         
        public double[] dqmax;
        public double[] ddqmax;

        public JointConstraints_typ(int dofs)
        {
            qmin = new double[dofs];
            qmax = new double[dofs];
            dqmax = new double[dofs];
            ddqmax = new double[dofs];
            
            for (int i = 0; i < dofs; i++)
            {
                qmin[i] = 0.0;
                qmax[i] = 0.0;
                dqmax[i] = 0.0;
                ddqmax[i] = 0.0;
            }
        }
    };
    [Serializable, StructLayout(LayoutKind.Sequential)]
    private struct InternJointConstraints_typ
    {
        public IntPtr qmin;
        public IntPtr qmax;
        public IntPtr dqmax;
        public IntPtr ddqmax;
       

        public InternJointConstraints_typ(int jointDOF)
        {
            qmin = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
            qmax = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
            dqmax = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
            ddqmax = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
        }
        public void FromExternStructure(JointConstraints_typ jointContraint)
        {
            Marshal.Copy(jointContraint.qmin, 0, qmin, jointContraint.qmin.Length);
            Marshal.Copy(jointContraint.qmax, 0, qmax, jointContraint.qmax.Length);
            Marshal.Copy(jointContraint.dqmax, 0, dqmax, jointContraint.dqmax.Length);
            Marshal.Copy(jointContraint.ddqmax, 0, ddqmax, jointContraint.ddqmax.Length);


        }
    }

    [Serializable]
    public struct Input_typ
    {
        public double[] Jacobian;
        public double[] dXdesired;
        public double[] q;
        public double[] dq;
        public double[] q0;


        // DeltaTime in seconds
        public double deltaTime;

        // DLS Section
        public double dlsDamping;

        public double regularizationWeight;

        public bool enableRegularization;

        public Input_typ(int jointDOF, int opDOF)
        {
            Jacobian = new double[jointDOF*opDOF];
            dXdesired = new double[opDOF];
            q = new double[jointDOF];
            dq = new double[jointDOF];
            q0 = new double[jointDOF];
            deltaTime = 0.0;
            dlsDamping = 0.0;
            regularizationWeight = 0.0;
            enableRegularization = false;

            for (int i = 0; i < jointDOF; i++)
            {
                q[i] = 0.0;
                dq[i] = 0.0;
                q0[i] = 0.0;
                for (int j = 0; j < opDOF; j++)
                {
                    Jacobian[j * jointDOF + i] = 0.0;
                    if (i == 0)
                    {
                        dXdesired[j] = 0.0;
                    }
                }
            }
        }
    }
    [Serializable, StructLayout(LayoutKind.Sequential)]
    private struct InternInput_typ
    {
        public IntPtr Jacobian;
        public IntPtr dXdesired;
        public IntPtr q;
        public IntPtr dq;
        public IntPtr q0;

        public double deltaTime;
        public double dlsDamping;
        public double regularizationWeight;
        public bool enableRegularization;

        public InternInput_typ(int jointDOF, int opDOF)
        {
            Jacobian = Marshal.AllocHGlobal(sizeof(double) * jointDOF * opDOF);
            dXdesired = Marshal.AllocHGlobal(sizeof(double) * opDOF);
            q = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
            dq = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
            q0 = Marshal.AllocHGlobal(sizeof(double) * jointDOF);

            deltaTime = 0.0;
            dlsDamping = 0.0;
            regularizationWeight = 0.0;
            enableRegularization = false;
        }
        public void FromExternStructure(Input_typ input)
        {
            Marshal.Copy(input.Jacobian, 0, Jacobian, input.Jacobian.Length);
            Marshal.Copy(input.dXdesired, 0, dXdesired, input.dXdesired.Length);
            Marshal.Copy(input.q, 0, q, input.q.Length);
            Marshal.Copy(input.dq, 0, dq, input.dq.Length);
            Marshal.Copy(input.q0, 0, q0, input.q0.Length);

            deltaTime = input.deltaTime;
            dlsDamping = input.dlsDamping;
            regularizationWeight = input.regularizationWeight;
            enableRegularization = input.enableRegularization;
        }

    }

    [Serializable]
    public struct Output_typ
    {
        public SolverStatus_typ status;
        public double computationTime;
        public double[] dq;
        public double[] q;

        public Output_typ(int jointDOF)
        {
            status = SolverStatus_typ.NONE;
            computationTime = 0.0;
            q = new double[jointDOF];
            dq = new double[jointDOF];
            for (int i = 0; i < jointDOF; i++)
            {
                q[i] = 0.0;
                dq[i] = 0.0;
            }
        }

        
    }
    [Serializable, StructLayout(LayoutKind.Sequential)]
    struct InternOutput_typ
    {
        public SolverStatus_typ status;
        public double computationTime;
        public IntPtr dq;
        public IntPtr q;

        public InternOutput_typ(int jointDOF)
        {
            q = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
            dq = Marshal.AllocHGlobal(sizeof(double) * jointDOF);
            status = SolverStatus_typ.NONE;
            computationTime = 0.0;
        }
        public void FromExternStructure(Output_typ output)
        {
            Marshal.Copy(output.q, 0, q, output.q.Length);
            Marshal.Copy(output.dq, 0, dq, output.dq.Length);

            status = output.status;
            computationTime = output.computationTime;
        }
    }
    #endregion

    #region InterOp
    // Import  constructor
    [DllImport("InverseKinematics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr IKNew(int jointDOF, int OpDOF, Solver_typ Solver);
    // Import delete function
    [DllImport("InverseKinematics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void IKDelete(IntPtr ptr);
    // Import constraints function
    [DllImport("InverseKinematics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void IKSetConstraints(IntPtr ptr, ref InternJointConstraints_typ jConstraints);
    // Import the Update function
    [DllImport("InverseKinematics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Update(IntPtr ptr, ref InternInput_typ input, ref InternOutput_typ output);
    #endregion


    #region Class
    public InverseKinematics(int jointDOF, int opDOF, Solver_typ Solver)
    {
        jointDof = jointDOF;
        opDof = opDOF;
        solver = Solver;
        ptr = IKNew(jointDof, opDof, solver);

        if (ptr != IntPtr.Zero)
        {
            Input = new Input_typ(jointDof, opDof);
            Output = new Output_typ(jointDof);
            JointConstraints = new JointConstraints_typ(jointDof);

            _Input = new InternInput_typ(jointDof, opDof);
            _Output = new InternOutput_typ(jointDof);
            _JointConstraints = new InternJointConstraints_typ(jointDof);
        }
        
    }

    public void SetConstraints()
    {
        if (ptr != IntPtr.Zero)
        {
            // Convert extern structure to Marshal structure
            _JointConstraints.FromExternStructure(JointConstraints);
            IKSetConstraints(ptr, ref _JointConstraints);
        }
    }


    public void Update()
    {
        // Update 
        if (ptr != IntPtr.Zero)
        {
            // Convert extern to intern 
            _Input.FromExternStructure(Input);
            _Output.FromExternStructure(Output);


            
            Update(ptr, ref _Input, ref _Output);



            // Convert output to extern
            Marshal.Copy(_Output.dq, Output.dq, 0, jointDof);
            Marshal.Copy(_Output.q, Output.q, 0, jointDof);
            Output.status = _Output.status;
            Output.computationTime = _Output.computationTime;
        }

    }
    ~InverseKinematics()
    {
        if (ptr != IntPtr.Zero)
        {
            IKDelete(ptr);
        }
    }


    #endregion


    #region Helpers

    public static Vector3 toABC(Quaternion q)

    {

        // Convert quaternion to rotation matrix
        Matrix4x4 rot = new Matrix4x4();
        rot[0, 0] = 1 - 2 * q.y * q.y - 2 * q.z * q.z;
        rot[0, 1] = 2 * q.x * q.y - 2 * q.z * q.w;
        rot[0, 2] = 2 * q.x * q.z + 2 * q.y * q.w;
        rot[1, 0] = 2 * q.x * q.y + 2 * q.z * q.w;
        //rot[1, 1] = 1 - 2 * q.x * q.x - 2 * q.z * q.z;    // unused
        //rot[1, 2] = 2 * q.y * q.z - 2 * q.x * q.w;        // unused
        rot[2, 0] = 2 * q.x * q.z - 2 * q.y * q.w;
        rot[2, 1] = 2 * q.y * q.z + 2 * q.x * q.w;
        rot[2, 2] = 1 - 2 * q.x * q.x - 2 * q.y * q.y;

        Vector3 result = new Vector3();

        float sb = rot[2, 0];
        float cb = Mathf.Sqrt(1.0f - sb * sb);
        float ca = rot[0, 0];
        float sa = -rot[1, 0];
        float cc = rot[2, 2];
        float sc = -rot[2, 1];
        result.x = Mathf.Atan2(sa, ca) * -180 / Mathf.PI;
        result.y = Mathf.Atan2(sb, cb) * -180 / Mathf.PI;
        result.z = Mathf.Atan2(sc, cc) * -180 / Mathf.PI;

        return result;

    }


    public static double[] getJacobian(GameObject gameObject)
    {
        double[] result = null;

        ArticulationBody articulationBody = gameObject.GetComponent<ArticulationBody>();
        while (articulationBody == null)
        {
            articulationBody = gameObject.GetComponentInParent<ArticulationBody>();
            gameObject = gameObject.transform.parent.gameObject;
        }
        ArticulationJacobian jacobian = new ArticulationJacobian(1,1);
        List<int> dofStartIndices = new List<int>();

        if (articulationBody != null)
        {
            articulationBody.GetDofStartIndices(dofStartIndices);
            articulationBody.GetDenseJacobian(ref jacobian);

            // Get indexes of every ArticualtionBody in hierarchy up to root
            List<int> indexes = new List<int>();
            indexes = GetRecursiveIndex(gameObject, indexes);


            // Parse jacobian dense matrix to use colums related to this kinematic chain.
            // Column = the body's DOF start index (NOT body.index - 1): fixed bodies have an
            // ArticulationBody but 0 DOF, so any fixed link in/before the chain (arm mounts,
            // wrist, the other arm…) would otherwise shift every column and corrupt the Jacobian.
            float[,] jac = new float[6, indexes.Count];
            for (int i = 0; i < indexes.Count; i++)
            {
                for (int j = 0; j < 6; j++)
                {

                    jac[j,i] =  jacobian[j + jacobian.rows - 6 * (dofStartIndices.Count - articulationBody.index), dofStartIndices[indexes[i]]];
                }
            }

            // Flatten matrix
            result = jac.Flatten().toDouble();

        }


        return result;
    }
    private static List<int> GetRecursiveIndex(GameObject GO, List<int> indexes)
    {
        // Walk from this body up to the articulation root, collecting the index of every body
        // that actually has a DOF. Fixed bodies (dofCount == 0) are skipped — so any number of
        // fixed links in the chain is tolerated — and THIS body's own DOF is included (it moves
        // the end-effector), so a 7-DOF arm yields 7 columns whether you pass the last joint or a
        // fixed wrist/TCP body past it.
        for (Transform t = GO.transform; t != null; t = t.parent)
        {
            if (t.TryGetComponent<ArticulationBody>(out var artbody))
            {
                if (artbody.dofCount != 0) indexes.Add(artbody.index);
                if (artbody.isRoot) break;
            }
        }
        indexes.Sort();
        return indexes;


    }
    
    #endregion
}
