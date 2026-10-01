using EigenCore.Core.Dense;
using DT.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Component = DT.Model.Component;
using static DT.Model.Axe;

[Serializable]
public class BaseRobot : Component
{
    [Header("---- Robot Extension -----")]
    [Header(" |-> Joints")]
    [JsonIgnore]
    public float[] jointsPosition, jointsVelocity;
    public float[] jointsActualPosition, jointsActualVelocity;
    [JsonIgnore]
    public Axe[] joints;
    [Header(" |-> Motors")]
    [JsonIgnore]
    public int numMot;
    [JsonIgnore]
    public float[] motPosition;
    [Header(" |-> Homing")]
    public float[] homePosition;   // Home pose per joint (deg or m). Editable in the Inspector; size = movable-joint count.
    [JsonIgnore]

    

    public bool goHome = false;

    [Header(" |-> Debug")]
    public bool showJointFrames = false;

    [Header(" |-> TCP")]
    [JsonIgnore]
    public GameObject TCP;
    public GameObject Base;



    public static BaseRobot FromJsonFile(string path)
    {
        BaseRobot baseRobot = null;

        FileInfo  R2json = new FileInfo(path);
        if (R2json.Exists)
        {
            using (StreamReader strReader = R2json.OpenText())
            {
                string json = strReader.ReadToEnd();
                baseRobot = FromJson(json);
            }

        }
        else Debug.LogError("no file");

        return baseRobot;
    }

    public static BaseRobot FromJson (string json)
    {
        try{
            BaseRobot loadedRobot = JsonConvert.DeserializeObject<BaseRobot>(json);
            if (loadedRobot != null)
                return loadedRobot;
        }
        catch(JsonException e) {
            Debug.LogError(e.Message);
        }

        return null;
    }

    public void ExportJson(){
        try{
            string json = JsonConvert.SerializeObject(this);
            using (StreamWriter jsonWriter = new StreamWriter(UnityEngine.Application.dataPath + "/../"+Id+".json"))
            {
                jsonWriter.Write(json);
            }
        }
        catch(JsonException e) {
            Debug.LogError(e.Message);
        }
    }


    public void SetControlType(AxeControl_enum ControlType)
    {
        if (joints!= null)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].Command.ControlType = ControlType;
            }
        }
    }
    public void InitRobot()
    {
        // if (Application.isPlaying)
        // {
        //     return;
        // }
        List<Axe> trueJoints = new();
        foreach (var item in Joints)
        {
            if (!item.Constraint && item.Type != DT.Model.Joint.JointTypes.Fixed)
            {
                trueJoints.Add(item.Axes[0]);
                
            }
        }
        joints = trueJoints.ToArray();
        jointsPosition = new float[joints.Length];
        jointsActualPosition = new float[joints.Length];
        jointsVelocity = new float[joints.Length];
        jointsActualVelocity = new float[joints.Length];
        motPosition = new float[numMot];


        // Home pose (editable in the Inspector): keep whatever the user set, resized to the joint
        // count, seeding any missing entries from each joint's PosHome.
        if (homePosition == null || homePosition.Length != joints.Length)
        {
            float[] old = homePosition;
            homePosition = new float[joints.Length];
            for (int i = 0; i < joints.Length; i++)
                homePosition[i] = (old != null && i < old.Length) ? old[i] : joints[i].Param.PosHome.Value;
        }

        // Start at the home pose.
        for (int i = 0; i < jointsPosition.Length; i++)
            jointsPosition[i] = homePosition[i];
    }

    public void RobotUpdate()
    {
        if (jointsPosition == null)
            return;
        if (goHome)
        {
            GoHomePosition();
            return;
        }

        for (int i = 0; i < jointsPosition.Length; i++)
        {
            if (jointsPosition[i] != float.NaN)
            {
                joints[i].Command.S_Cmd.Value = (jointsPosition[i]);
            }

            joints[i].Command.dS_Cmd.Value = (jointsVelocity[i]);

            jointsActualVelocity[i] = joints[i].Info.dS_act.Value;


            jointsActualPosition[i] = joints[i].Info.S_act.Value;
        }

    }

    public void SetTCP(GameObject tcp)
    {
        // Search for the first link in TCP parents
        this.TCP = tcp.GetComponentInParent<ArticulationBody>().gameObject;
    }
    public void SetBase(GameObject baseObject)
    {
        // Search for the first link in TCP parents
        this.Base = baseObject.GetComponentInParent<ArticulationBody>().gameObject;
    }
    public GameObject GetTCP() { return this.TCP; }

    private void GoHomePosition()
    {
        // Command every joint to the Inspector-defined home pose.
        if (homePosition != null)
            for (int i = 0; i < jointsPosition.Length && i < homePosition.Length; i++)
                jointsPosition[i] = homePosition[i];
        goHome = false;
    }

    public virtual VectorXD DirectModel(VectorXD q)
    {
        throw new System.Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + "Need to bee implemented for each robot");

    }

    public virtual VectorXD InverseModel(VectorXD x)
    {
        throw new System.Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + "Need to bee implemented for each robot");

    }

    public virtual MatrixXD jacobian(VectorXD q)
    {
        throw new System.Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + "Need to bee implemented for each robot");
    }

    public VectorXD InverseKinematics(VectorXD dX)
    {
        throw new System.Exception(System.Reflection.MethodBase.GetCurrentMethod().Name + "Need to bee implemented for each robot");
    }

    public VectorXD InverseKinematics(VectorXD q, VectorXD dX)
    {

        MatrixXD j = jacobian(q);

        // Classical PseudoInv of jac
        MatrixXD pinvJac = j.PseudoInverse();
        VectorXD qDest = (pinvJac * dX) + q;

        return qDest;
    }

    public VectorXD InverseKinematics(VectorXD current_q, VectorXD current_x, VectorXD new_x)
    {
        MatrixXD j = jacobian(current_q);

        // Classical PseudoInv of jac
        MatrixXD pinvJac = j.PseudoInverse();
        VectorXD dx = (new_x - current_x);

        VectorXD qDest = ((pinvJac * dx) + current_q * Mathf.Deg2Rad) * Mathf.Rad2Deg;

        return qDest;
    }


    public VectorXD InverseKinematics(VectorXD current_q, VectorXD current_x, VectorXD new_x, float damping)
    {
        MatrixXD j = jacobian(current_q);

        // Classical PseudoInv of jac
        MatrixXD pinvJac = j.PseudoDampInv(damping);
        VectorXD dx = (new_x - current_x);

        VectorXD qDest = ((pinvJac * dx) + current_q * Mathf.Deg2Rad) * Mathf.Rad2Deg;

        return qDest;
    }

}
