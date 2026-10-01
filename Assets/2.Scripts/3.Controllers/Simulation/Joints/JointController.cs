using DT.Controller;
using DT.Model;
using DT.Tools;
using System;
using Unity.VisualScripting;
using UnityEngine;
using static DT.Model.Joint;
using Component = DT.Model.Component;
using Joint = DT.Model.Joint;

/// <summary>
/// Convert joint to ArticulationBody
/// </summary>
/// 
/// 

namespace DT.Simulation
{
    public abstract class JointController : BaseController<Joint>
    {
        protected int axisNum;
        protected float previousPosition, previousSpeed;
        protected ArticulationBody artBody;
        protected UnityJointHelper jointHelper;
        protected GameObject link;
        protected Vector3 axisofMotion;

        Ruckig otg;
        double deltaTime = 0.001; // 1ms



        public static JointController CreateJoint(GameObject linkObj, Joint joint)
        {
            JointController j = AddJointType(linkObj, joint.Type);            
            j.Setup(joint);
            j.SetJointData(joint);
            j.otg = new Ruckig(1, j.deltaTime);

            return j;

        }






        #region SetParam
        
        private static JointController AddJointType(GameObject linkObject, JointTypes jointType)
        {
            JointController joint = null;

            switch (jointType)
            {
                case JointTypes.Fixed:
                    joint = FixedJointToUnity.Create(linkObject);
                    break;
                case JointTypes.Revolute:
                    joint = RevoluteJointToUnity.Create(linkObject);
                    break;
                case JointTypes.Prismatic:
                    joint = PrismaticJointToUnity.Create(linkObject);
                    break;
                case JointTypes.Planar:
                    joint = PlanarJointToUnity.Create(linkObject);
                    break;
            }

            return joint;
        }

        protected virtual void SetJointData(Joint joint) { 
            this.Model = joint;
            
        }


        protected void SetArticulationBody(ArticulationBody body)
        {
            this.artBody = body;
        }


        protected ArticulationDrive setDriveParam(ArticulationDrive drive, Axe.AxeParam param)
        {
            drive.lowerLimit = param.Neg_sw_end.ConvertToSI();
            drive.upperLimit = param.Pos_sw_end.ConvertToSI();
            drive.target = param.PosHome.ConvertToSI();
            drive.forceLimit = param.C_max.ConvertToSI();
            drive.stiffness = param.stiffness;
            drive.damping = param.damping;

            if (drive.forceLimit == 0.0f)
            {
                drive.forceLimit = param.defaultForceLimit;
            }

            return drive;
        }
        protected static bool isLimited(Axe.AxeParam param)
        {
            return ((param.Neg_sw_end.Value != 0.0f || param.Pos_sw_end.Value != 0.0f)  && param.Neg_sw_end.Value < param.Pos_sw_end.Value) == true;
        }

        /// <summary>
        /// Joint axis of motion in ROS/URDF convention, preserving the URDF sign.
        /// Prefers the full signed vector imported from &lt;axis xyz&gt; (Axe.AxisVector);
        /// falls back to the unsigned Axe_enum for legacy data where AxisVector is unset.
        /// Without the sign, axis="0 0 -1" was treated as +Z, mirroring the joint's motion
        /// and its limit range.
        /// </summary>
        protected static Vector3 GetAxisOfMotion(Axe axe)
        {
            if (axe.AxisVector != Vector3.zero) return axe.AxisVector;

            switch (axe.Type)
            {
                case Axe.Axe_enum.RotX:
                case Axe.Axe_enum.LinX: return new Vector3(1, 0, 0);
                case Axe.Axe_enum.RotY:
                case Axe.Axe_enum.LinY: return new Vector3(0, 1, 0);
                case Axe.Axe_enum.RotZ:
                case Axe.Axe_enum.LinZ: return new Vector3(0, 0, 1);
                default:                return new Vector3(1, 0, 0);
            }
        }

        /// <summary>
        /// Clamp an SI position command to the joint's software end stops.
        /// Ruckig (community edition) has no native position limits — ErrorPositionalLimits
        /// is Pro-only — so the trajectory target must be pre-clamped here. Ruckig then plans
        /// a smooth velocity/acceleration/jerk-limited move that stops exactly at the limit.
        /// No-op for unlimited (e.g. continuous) joints.
        /// </summary>
        protected double ClampToLimitsSI(double targetSI)
        {
            var param = Model.Axes[0].Param;
            if (!isLimited(param)) return targetSI;

            double lower = param.Neg_sw_end.ConvertToSI();
            double upper = param.Pos_sw_end.ConvertToSI();
            if (targetSI < lower) return lower;
            if (targetSI > upper) return upper;
            return targetSI;
        }


        public void SetParentInfo(Link parent)
        {
            if (parent == null) return;
            if (parent.LinkModel == null) return;
            if (parent.LinkModel.Inertial == null) return;

            // Guard: ArticulationBody requires mass > 0; zero/NaN mass causes physics NaN
            // that cascades to all descendants (common for sensor/frame links in URDF).
            float mass = parent.LinkModel.Inertial.Mass;
            if (mass > 0f && !float.IsNaN(mass))
                artBody.mass = mass;

            // Center of mass must be expressed in Unity (left-hand, Y-up) space.
            artBody.ResetCenterOfMass();
            if (parent.LinkModel.Inertial.Origin != null)
                artBody.centerOfMass = parent.LinkModel.Inertial.Origin.XYZ.ToIndirectCoor();

            // Guard: all three diagonal components must be > 0; a zero component
            // produces a singular inertia matrix and NaN angular accelerations.
            float ixx = parent.LinkModel.Inertial.Ixx;
            float iyy = parent.LinkModel.Inertial.Iyy;
            float izz = parent.LinkModel.Inertial.Izz;
            bool validInertia = ixx > 0f && iyy > 0f && izz > 0f
                             && !float.IsNaN(ixx) && !float.IsNaN(iyy) && !float.IsNaN(izz);
            if (validInertia)
            {
                artBody.inertiaTensor         = new Vector3(ixx, iyy, izz);
                artBody.inertiaTensorRotation = parent.LinkModel.Inertial.Origin.RPY.ToIndirectRot();
            }
            else
            {
                artBody.ResetInertiaTensor();
            }
        }


        #endregion

        #region Runtime


        public void Compute(float timeDelta = 0.01f)
        {


            if (Model.Constraint)
            {
                setPosition(Model.factorAndOffset.x * Model.couplingVar.ConvertToSI() + Model.factorAndOffset.y);

                UpdateAxesInfo();

                return;
            }


            if (Model.Type != JointTypes.Fixed)
            {
                // Pass param to OTG
                if (timeDelta != 0.0f)
                {
                    otg.UpdateDeltaTime(timeDelta);
                }
                otg.Input.synchronization = Ruckig.Synchronization.None;

               
                /*otg.Input.current_position = new double[] { GetPosition().ConvertToSI(Model.Axes[0].Info.S_act.Unit) };
                otg.Input.current_velocity = new double[] { GetVelocity().ConvertToSI(Model.Axes[0].Info.dS_act.Unit) };
                otg.Input.current_acceleration = new double[] { GetAcceleration().ConvertToSI(Model.Axes[0].Info.d2S_act.Unit) };*/
                
                otg.Input.max_jerk = new double[] { Model.Axes[0].Param.d3S_max.Value };
                otg.Input.max_velocity = new double[] { Model.Axes[0].Param.dS_max.ConvertToSI() };
                otg.Input.max_acceleration = new double[] { Model.Axes[0].Param.d2S_max.ConvertToSI() };
                



                if (axisNum == 0)
                {

                    switch (Model.Axes[0].Command.ControlType)
                    {
                        case Axe.AxeControl_enum.Position:
                            otg.Input.control_interface = Ruckig.ControlInterface.Position;
                            // Pre-clamp the target to the joint's end stops: Ruckig (community)
                            // has no native position limits, so feeding it a target beyond the
                            // limit would plan a trajectory that overshoots. Clamping makes Ruckig
                            // decelerate smoothly and stop exactly at the limit.
                            otg.Input.target_position = new double[] { ClampToLimitsSI(Model.Axes[0].Command.S_Cmd.ConvertToSI()) };
                            otg.Input.target_velocity = new double[] { Model.Axes[0].Command.dS_Cmd.ConvertToSI() };
                            ArticulationDrive currentDrive = jointHelper.xDrive;
                            currentDrive.driveType = ArticulationDriveType.Target;
                            jointHelper.setXDrive(currentDrive);
                            break;
                        case Axe.AxeControl_enum.Velocity:
                            otg.Input.control_interface = Ruckig.ControlInterface.Velocity;
                            otg.Input.target_velocity = new double[] { Model.Axes[0].Command.dS_Cmd.ConvertToSI() };
                            ArticulationDrive currentDrive_ = jointHelper.xDrive;
                            currentDrive_.driveType = ArticulationDriveType.Velocity;
                            jointHelper.setXDrive(currentDrive_);
                            break;
                        case Axe.AxeControl_enum.Manual:
                            // Get direction
                            int direction = 0;
                            if (Model.Axes[0].Command.BpNeg)
                            {
                                direction = -1;
                                Model.Axes[0].Command.BpNeg.Value = false;
                            }
                            else if (Model.Axes[0].Command.BpPos)
                            {
                                direction = 1;
                                Model.Axes[0].Command.BpPos.Value = false;
                            }
                            
                            otg.Input.control_interface = Ruckig.ControlInterface.Position;
                            if (direction != 0)
                            {
                                otg.Input.target_position = new double[] { (int)direction * timeDelta * Model.Axes[0].Command.dS_Cmd.ConvertToSI() };
                                otg.Input.target_velocity = new double[] { Model.Axes[0].Command.dS_Cmd.ConvertToSI() };
                            }
                            else
                            {
                                otg.Input.target_position = new double[] { 0.0 };
                                otg.Input.target_velocity = new double[] { 0.0 };
                            }
                            

                            break;
                        default:
                            break;
                    }

                    otg.Update();
                    otg.OutputToInput();

                    setPosition((float)otg.Output.new_position[0]);
                    
                    UpdateAxesInfo();

                }
                
                
            }
        }


        void setPosition(float targetPos)
        {
            switch (Model.Type)
            {
                case JointTypes.Fixed:
                    break;
                case JointTypes.Revolute:
                    // Convert SI to degrees for revolute joints
                    targetPos = Units.ConvertFromSI(targetPos, Units.GetUnitFromId("DD"));
                    break;
                case JointTypes.Prismatic:
                    break;
                case JointTypes.Planar:
                    break;
                default:
                    break;
            }

            ArticulationDrive currentDrive = jointHelper.xDrive;
            currentDrive.targetVelocity = Model.Axes[0].Command.dS_Cmd;

                // Saturate position
                if (isLimited(Model.Axes[0].Param))
                {
                    if (targetPos  > currentDrive.upperLimit)
                    {
                        currentDrive.target = currentDrive.upperLimit;
                    }
                    else if (targetPos < currentDrive.lowerLimit)
                    {
                        currentDrive.target = currentDrive.lowerLimit;
                    }
                    else
                    {
                        currentDrive.target = targetPos;
                    }
                }
                else
                {
                    currentDrive.target = targetPos;
                }

            
            jointHelper.setXDrive(currentDrive);
        }


        void UpdateAxesInfo()
        {

            GetForce();
            GetDerivativeForce();
            GetPosition();
            GetVelocity();
            GetAcceleration();

            Model.Axes[0].Info.LimitSwitchPos.Value = (Model.Axes[0].Info.S_act.Value == Model.Axes[0].Param.Pos_sw_end.Value);
            Model.Axes[0].Info.LimitSwitchNeg.Value = (Model.Axes[0].Info.S_act.Value == Model.Axes[0].Param.Neg_sw_end.Value);

        }


        public virtual float GetPosition()
        {
            if (Model.Axes.Count != 0 && jointHelper != null)
            {
                Model.Axes[0].Info.S_act.Value = Units.ConvertFromSI(jointHelper.position, Model.Axes[0].Info.S_act.Unit);
                return Model.Axes[0].Info.S_act;
            }
            
            return 0;
        }

        public virtual float GetVelocity()
        {
            if (Model.Axes.Count != 0 && jointHelper != null)
            {
                Model.Axes[0].Info.dS_act.Value = Units.ConvertFromSI(jointHelper.velocity, Model.Axes[0].Info.dS_act.Unit);
                return Model.Axes[0].Info.dS_act.Value;
            }
            return 0;
        }
        public virtual float GetAcceleration()
        {
            if (Model.Axes.Count != 0 && jointHelper != null)
            {
                Model.Axes[0].Info.d2S_act.Value = Units.ConvertFromSI(jointHelper.acceleration, Model.Axes[0].Info.d2S_act.Unit);
                return Model.Axes[0].Info.d2S_act;
            }
            return 0;
        }

        public virtual float GetForce()
        {
            if (Model.Axes.Count != 0 && jointHelper != null)
            {
                Model.Axes[0].Info.C_act.Value = Units.ConvertFromSI(jointHelper.force, Model.Axes[0].Info.C_act.Unit);
                return Model.Axes[0].Info.C_act;
            }
            return 0;
        }
        public virtual float GetDerivativeForce()
        {
            if (Model.Axes.Count != 0)
            {
                Model.Axes[0].Info.dC_act.Value = Model.Axes[0].Info.C_act.DerivativeValue;
                return Model.Axes[0].Info.dC_act;
            }
            return 0;
        }
        #endregion


        


    }
}
