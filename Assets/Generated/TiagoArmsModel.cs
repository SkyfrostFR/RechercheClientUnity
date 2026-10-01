using UnityEngine;
using DT.Controller;
using DT.Model;
using DT.Tools;
using System.Collections.Generic;
using Joint = DT.Model.Joint;

/// <summary>
/// Auto-generated from URDF by UrdfCodeGenerator (FrankaModel-style).
/// Builds the robot in code; wire each link's visual & collision meshes in the Inspector.
/// </summary>
public class TiagoArmsModel : MonoBehaviour
{
    [System.Serializable]
    public class VisualComponents
    {
        public List<GameObject> torso_lift_link_visuals = new List<GameObject>();
        public List<GameObject> TIAGo_left_arm_visuals = new List<GameObject>();
        public List<GameObject> TIAGo_left_arm_1_visuals = new List<GameObject>();
        public List<GameObject> arm_left_2_link_visuals = new List<GameObject>();
        public List<GameObject> arm_left_3_link_visuals = new List<GameObject>();
        public List<GameObject> arm_left_4_link_visuals = new List<GameObject>();
        public List<GameObject> arm_left_5_link_visuals = new List<GameObject>();
        public List<GameObject> arm_left_6_link_visuals = new List<GameObject>();
        public List<GameObject> arm_left_7_link_visuals = new List<GameObject>();
        public List<GameObject> wrist_left_ft_tool_link_visuals = new List<GameObject>();
        public List<GameObject> left_visuals = new List<GameObject>();
        public List<GameObject> gripper_right_finger_link_visuals = new List<GameObject>();
        public List<GameObject> gripper_left_finger_link_visuals = new List<GameObject>();
        public List<GameObject> TIAGo_right_arm_visuals = new List<GameObject>();
        public List<GameObject> TIAGo_right_arm_1_visuals = new List<GameObject>();
        public List<GameObject> arm_right_2_link_visuals = new List<GameObject>();
        public List<GameObject> arm_right_3_link_visuals = new List<GameObject>();
        public List<GameObject> arm_right_4_link_visuals = new List<GameObject>();
        public List<GameObject> arm_right_5_link_visuals = new List<GameObject>();
        public List<GameObject> arm_right_6_link_visuals = new List<GameObject>();
        public List<GameObject> arm_right_7_link_visuals = new List<GameObject>();
        public List<GameObject> wrist_right_ft_tool_link_visuals = new List<GameObject>();
        public List<GameObject> right_visuals = new List<GameObject>();
        public List<GameObject> gripper_right_finger_link_3_visuals = new List<GameObject>();
        public List<GameObject> gripper_left_finger_link_4_visuals = new List<GameObject>();
    }
    public VisualComponents visualComponents = new VisualComponents();

    [System.Serializable]
    public class CollisionComponents
    {
        public List<GameObject> torso_lift_link_collisions = new List<GameObject>();
        public List<GameObject> TIAGo_left_arm_collisions = new List<GameObject>();
        public List<GameObject> TIAGo_left_arm_1_collisions = new List<GameObject>();
        public List<GameObject> arm_left_2_link_collisions = new List<GameObject>();
        public List<GameObject> arm_left_3_link_collisions = new List<GameObject>();
        public List<GameObject> arm_left_4_link_collisions = new List<GameObject>();
        public List<GameObject> arm_left_5_link_collisions = new List<GameObject>();
        public List<GameObject> arm_left_6_link_collisions = new List<GameObject>();
        public List<GameObject> arm_left_7_link_collisions = new List<GameObject>();
        public List<GameObject> wrist_left_ft_tool_link_collisions = new List<GameObject>();
        public List<GameObject> left_collisions = new List<GameObject>();
        public List<GameObject> gripper_right_finger_link_collisions = new List<GameObject>();
        public List<GameObject> gripper_left_finger_link_collisions = new List<GameObject>();
        public List<GameObject> TIAGo_right_arm_collisions = new List<GameObject>();
        public List<GameObject> TIAGo_right_arm_1_collisions = new List<GameObject>();
        public List<GameObject> arm_right_2_link_collisions = new List<GameObject>();
        public List<GameObject> arm_right_3_link_collisions = new List<GameObject>();
        public List<GameObject> arm_right_4_link_collisions = new List<GameObject>();
        public List<GameObject> arm_right_5_link_collisions = new List<GameObject>();
        public List<GameObject> arm_right_6_link_collisions = new List<GameObject>();
        public List<GameObject> arm_right_7_link_collisions = new List<GameObject>();
        public List<GameObject> wrist_right_ft_tool_link_collisions = new List<GameObject>();
        public List<GameObject> right_collisions = new List<GameObject>();
        public List<GameObject> gripper_right_finger_link_3_collisions = new List<GameObject>();
        public List<GameObject> gripper_left_finger_link_4_collisions = new List<GameObject>();
    }
    public CollisionComponents collisionComponents = new CollisionComponents();

    [Header("---- Model ----")]
    [SerializeField] public BaseRobot robot = new BaseRobot();

    [SerializeField] public ComponentController controller;

    private void Model()
    {
        float[] keepHome = robot != null ? robot.homePosition : null; // preserve Inspector home pose
        robot = new BaseRobot();
        if (keepHome != null && keepHome.Length > 0) robot.homePosition = keepHome;
        robot.Name = "tiago_dual_arms";
        robot.Links = new List<Link>();

        Link torso_lift_link = new Link();
        torso_lift_link.Name = "torso_lift_link";
        torso_lift_link.LinkModel.Name = "torso_lift_link";
        if (visualComponents.torso_lift_link_visuals != null)
        {
            foreach (var go in visualComponents.torso_lift_link_visuals)
                if (go != null) torso_lift_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        torso_lift_link.LinkModel.Visual.Show = true;
        if (collisionComponents.torso_lift_link_collisions != null)
        {
            foreach (var go in collisionComponents.torso_lift_link_collisions)
                if (go != null) torso_lift_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        torso_lift_link.LinkModel.Collision.Show = false;
        torso_lift_link.IsRoot = true;
        robot.Links.Add(torso_lift_link);

        Link TIAGo_left_arm = new Link();
        TIAGo_left_arm.Name = "TIAGo left arm";
        TIAGo_left_arm.LinkModel.Name = "TIAGo left arm";
        if (visualComponents.TIAGo_left_arm_visuals != null)
        {
            foreach (var go in visualComponents.TIAGo_left_arm_visuals)
                if (go != null) TIAGo_left_arm.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        TIAGo_left_arm.LinkModel.Visual.Show = true;
        if (collisionComponents.TIAGo_left_arm_collisions != null)
        {
            foreach (var go in collisionComponents.TIAGo_left_arm_collisions)
                if (go != null) TIAGo_left_arm.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        TIAGo_left_arm.LinkModel.Collision.Show = false;
        robot.Links.Add(TIAGo_left_arm);

        Link TIAGo_left_arm_1 = new Link();
        TIAGo_left_arm_1.Name = "TIAGo_left_arm";
        TIAGo_left_arm_1.LinkModel.Name = "TIAGo_left_arm";
        if (visualComponents.TIAGo_left_arm_1_visuals != null)
        {
            foreach (var go in visualComponents.TIAGo_left_arm_1_visuals)
                if (go != null) TIAGo_left_arm_1.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        TIAGo_left_arm_1.LinkModel.Visual.Show = true;
        if (collisionComponents.TIAGo_left_arm_1_collisions != null)
        {
            foreach (var go in collisionComponents.TIAGo_left_arm_1_collisions)
                if (go != null) TIAGo_left_arm_1.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        TIAGo_left_arm_1.LinkModel.Collision.Show = false;
        robot.Links.Add(TIAGo_left_arm_1);

        Link arm_left_2_link = new Link();
        arm_left_2_link.Name = "arm_left_2_link";
        arm_left_2_link.LinkModel.Name = "arm_left_2_link";
        if (visualComponents.arm_left_2_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_left_2_link_visuals)
                if (go != null) arm_left_2_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_left_2_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_left_2_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_left_2_link_collisions)
                if (go != null) arm_left_2_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_left_2_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_left_2_link);

        Link arm_left_3_link = new Link();
        arm_left_3_link.Name = "arm_left_3_link";
        arm_left_3_link.LinkModel.Name = "arm_left_3_link";
        if (visualComponents.arm_left_3_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_left_3_link_visuals)
                if (go != null) arm_left_3_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_left_3_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_left_3_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_left_3_link_collisions)
                if (go != null) arm_left_3_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_left_3_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_left_3_link);

        Link arm_left_4_link = new Link();
        arm_left_4_link.Name = "arm_left_4_link";
        arm_left_4_link.LinkModel.Name = "arm_left_4_link";
        if (visualComponents.arm_left_4_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_left_4_link_visuals)
                if (go != null) arm_left_4_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_left_4_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_left_4_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_left_4_link_collisions)
                if (go != null) arm_left_4_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_left_4_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_left_4_link);

        Link arm_left_5_link = new Link();
        arm_left_5_link.Name = "arm_left_5_link";
        arm_left_5_link.LinkModel.Name = "arm_left_5_link";
        if (visualComponents.arm_left_5_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_left_5_link_visuals)
                if (go != null) arm_left_5_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_left_5_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_left_5_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_left_5_link_collisions)
                if (go != null) arm_left_5_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_left_5_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_left_5_link);

        Link arm_left_6_link = new Link();
        arm_left_6_link.Name = "arm_left_6_link";
        arm_left_6_link.LinkModel.Name = "arm_left_6_link";
        if (visualComponents.arm_left_6_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_left_6_link_visuals)
                if (go != null) arm_left_6_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_left_6_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_left_6_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_left_6_link_collisions)
                if (go != null) arm_left_6_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_left_6_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_left_6_link);

        Link arm_left_7_link = new Link();
        arm_left_7_link.Name = "arm_left_7_link";
        arm_left_7_link.LinkModel.Name = "arm_left_7_link";
        if (visualComponents.arm_left_7_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_left_7_link_visuals)
                if (go != null) arm_left_7_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_left_7_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_left_7_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_left_7_link_collisions)
                if (go != null) arm_left_7_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_left_7_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_left_7_link);

        Link wrist_left_ft_tool_link = new Link();
        wrist_left_ft_tool_link.Name = "wrist_left_ft_tool_link";
        wrist_left_ft_tool_link.LinkModel.Name = "wrist_left_ft_tool_link";
        if (visualComponents.wrist_left_ft_tool_link_visuals != null)
        {
            foreach (var go in visualComponents.wrist_left_ft_tool_link_visuals)
                if (go != null) wrist_left_ft_tool_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        wrist_left_ft_tool_link.LinkModel.Visual.Show = true;
        if (collisionComponents.wrist_left_ft_tool_link_collisions != null)
        {
            foreach (var go in collisionComponents.wrist_left_ft_tool_link_collisions)
                if (go != null) wrist_left_ft_tool_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        wrist_left_ft_tool_link.LinkModel.Collision.Show = false;
        robot.Links.Add(wrist_left_ft_tool_link);

        Link left = new Link();
        left.Name = "left";
        left.LinkModel.Name = "left";
        if (visualComponents.left_visuals != null)
        {
            foreach (var go in visualComponents.left_visuals)
                if (go != null) left.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        left.LinkModel.Visual.Show = true;
        if (collisionComponents.left_collisions != null)
        {
            foreach (var go in collisionComponents.left_collisions)
                if (go != null) left.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        left.LinkModel.Collision.Show = false;
        robot.Links.Add(left);

        Link gripper_right_finger_link = new Link();
        gripper_right_finger_link.Name = "gripper_right_finger_link";
        gripper_right_finger_link.LinkModel.Name = "gripper_right_finger_link";
        if (visualComponents.gripper_right_finger_link_visuals != null)
        {
            foreach (var go in visualComponents.gripper_right_finger_link_visuals)
                if (go != null) gripper_right_finger_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        gripper_right_finger_link.LinkModel.Visual.Show = true;
        if (collisionComponents.gripper_right_finger_link_collisions != null)
        {
            foreach (var go in collisionComponents.gripper_right_finger_link_collisions)
                if (go != null) gripper_right_finger_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        gripper_right_finger_link.LinkModel.Collision.Show = false;
        robot.Links.Add(gripper_right_finger_link);

        Link gripper_left_finger_link = new Link();
        gripper_left_finger_link.Name = "gripper_left_finger_link";
        gripper_left_finger_link.LinkModel.Name = "gripper_left_finger_link";
        if (visualComponents.gripper_left_finger_link_visuals != null)
        {
            foreach (var go in visualComponents.gripper_left_finger_link_visuals)
                if (go != null) gripper_left_finger_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        gripper_left_finger_link.LinkModel.Visual.Show = true;
        if (collisionComponents.gripper_left_finger_link_collisions != null)
        {
            foreach (var go in collisionComponents.gripper_left_finger_link_collisions)
                if (go != null) gripper_left_finger_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        gripper_left_finger_link.LinkModel.Collision.Show = false;
        robot.Links.Add(gripper_left_finger_link);

        Link TIAGo_right_arm = new Link();
        TIAGo_right_arm.Name = "TIAGo right arm";
        TIAGo_right_arm.LinkModel.Name = "TIAGo right arm";
        if (visualComponents.TIAGo_right_arm_visuals != null)
        {
            foreach (var go in visualComponents.TIAGo_right_arm_visuals)
                if (go != null) TIAGo_right_arm.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        TIAGo_right_arm.LinkModel.Visual.Show = true;
        if (collisionComponents.TIAGo_right_arm_collisions != null)
        {
            foreach (var go in collisionComponents.TIAGo_right_arm_collisions)
                if (go != null) TIAGo_right_arm.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        TIAGo_right_arm.LinkModel.Collision.Show = false;
        robot.Links.Add(TIAGo_right_arm);

        Link TIAGo_right_arm_1 = new Link();
        TIAGo_right_arm_1.Name = "TIAGo_right_arm";
        TIAGo_right_arm_1.LinkModel.Name = "TIAGo_right_arm";
        if (visualComponents.TIAGo_right_arm_1_visuals != null)
        {
            foreach (var go in visualComponents.TIAGo_right_arm_1_visuals)
                if (go != null) TIAGo_right_arm_1.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        TIAGo_right_arm_1.LinkModel.Visual.Show = true;
        if (collisionComponents.TIAGo_right_arm_1_collisions != null)
        {
            foreach (var go in collisionComponents.TIAGo_right_arm_1_collisions)
                if (go != null) TIAGo_right_arm_1.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        TIAGo_right_arm_1.LinkModel.Collision.Show = false;
        robot.Links.Add(TIAGo_right_arm_1);

        Link arm_right_2_link = new Link();
        arm_right_2_link.Name = "arm_right_2_link";
        arm_right_2_link.LinkModel.Name = "arm_right_2_link";
        if (visualComponents.arm_right_2_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_right_2_link_visuals)
                if (go != null) arm_right_2_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_right_2_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_right_2_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_right_2_link_collisions)
                if (go != null) arm_right_2_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_right_2_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_right_2_link);

        Link arm_right_3_link = new Link();
        arm_right_3_link.Name = "arm_right_3_link";
        arm_right_3_link.LinkModel.Name = "arm_right_3_link";
        if (visualComponents.arm_right_3_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_right_3_link_visuals)
                if (go != null) arm_right_3_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_right_3_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_right_3_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_right_3_link_collisions)
                if (go != null) arm_right_3_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_right_3_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_right_3_link);

        Link arm_right_4_link = new Link();
        arm_right_4_link.Name = "arm_right_4_link";
        arm_right_4_link.LinkModel.Name = "arm_right_4_link";
        if (visualComponents.arm_right_4_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_right_4_link_visuals)
                if (go != null) arm_right_4_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_right_4_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_right_4_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_right_4_link_collisions)
                if (go != null) arm_right_4_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_right_4_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_right_4_link);

        Link arm_right_5_link = new Link();
        arm_right_5_link.Name = "arm_right_5_link";
        arm_right_5_link.LinkModel.Name = "arm_right_5_link";
        if (visualComponents.arm_right_5_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_right_5_link_visuals)
                if (go != null) arm_right_5_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_right_5_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_right_5_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_right_5_link_collisions)
                if (go != null) arm_right_5_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_right_5_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_right_5_link);

        Link arm_right_6_link = new Link();
        arm_right_6_link.Name = "arm_right_6_link";
        arm_right_6_link.LinkModel.Name = "arm_right_6_link";
        if (visualComponents.arm_right_6_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_right_6_link_visuals)
                if (go != null) arm_right_6_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_right_6_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_right_6_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_right_6_link_collisions)
                if (go != null) arm_right_6_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_right_6_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_right_6_link);

        Link arm_right_7_link = new Link();
        arm_right_7_link.Name = "arm_right_7_link";
        arm_right_7_link.LinkModel.Name = "arm_right_7_link";
        if (visualComponents.arm_right_7_link_visuals != null)
        {
            foreach (var go in visualComponents.arm_right_7_link_visuals)
                if (go != null) arm_right_7_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        arm_right_7_link.LinkModel.Visual.Show = true;
        if (collisionComponents.arm_right_7_link_collisions != null)
        {
            foreach (var go in collisionComponents.arm_right_7_link_collisions)
                if (go != null) arm_right_7_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        arm_right_7_link.LinkModel.Collision.Show = false;
        robot.Links.Add(arm_right_7_link);

        Link wrist_right_ft_tool_link = new Link();
        wrist_right_ft_tool_link.Name = "wrist_right_ft_tool_link";
        wrist_right_ft_tool_link.LinkModel.Name = "wrist_right_ft_tool_link";
        if (visualComponents.wrist_right_ft_tool_link_visuals != null)
        {
            foreach (var go in visualComponents.wrist_right_ft_tool_link_visuals)
                if (go != null) wrist_right_ft_tool_link.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        wrist_right_ft_tool_link.LinkModel.Visual.Show = true;
        if (collisionComponents.wrist_right_ft_tool_link_collisions != null)
        {
            foreach (var go in collisionComponents.wrist_right_ft_tool_link_collisions)
                if (go != null) wrist_right_ft_tool_link.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        wrist_right_ft_tool_link.LinkModel.Collision.Show = false;
        robot.Links.Add(wrist_right_ft_tool_link);

        Link right = new Link();
        right.Name = "right";
        right.LinkModel.Name = "right";
        if (visualComponents.right_visuals != null)
        {
            foreach (var go in visualComponents.right_visuals)
                if (go != null) right.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        right.LinkModel.Visual.Show = true;
        if (collisionComponents.right_collisions != null)
        {
            foreach (var go in collisionComponents.right_collisions)
                if (go != null) right.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        right.LinkModel.Collision.Show = false;
        robot.Links.Add(right);

        Link gripper_right_finger_link_3 = new Link();
        gripper_right_finger_link_3.Name = "gripper_right_finger_link_3";
        gripper_right_finger_link_3.LinkModel.Name = "gripper_right_finger_link_3";
        if (visualComponents.gripper_right_finger_link_3_visuals != null)
        {
            foreach (var go in visualComponents.gripper_right_finger_link_3_visuals)
                if (go != null) gripper_right_finger_link_3.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        gripper_right_finger_link_3.LinkModel.Visual.Show = true;
        if (collisionComponents.gripper_right_finger_link_3_collisions != null)
        {
            foreach (var go in collisionComponents.gripper_right_finger_link_3_collisions)
                if (go != null) gripper_right_finger_link_3.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        gripper_right_finger_link_3.LinkModel.Collision.Show = false;
        robot.Links.Add(gripper_right_finger_link_3);

        Link gripper_left_finger_link_4 = new Link();
        gripper_left_finger_link_4.Name = "gripper_left_finger_link_4";
        gripper_left_finger_link_4.LinkModel.Name = "gripper_left_finger_link_4";
        if (visualComponents.gripper_left_finger_link_4_visuals != null)
        {
            foreach (var go in visualComponents.gripper_left_finger_link_4_visuals)
                if (go != null) gripper_left_finger_link_4.LinkModel.Visual.Geometry.Add(new Geometry(go));
        }
        gripper_left_finger_link_4.LinkModel.Visual.Show = true;
        if (collisionComponents.gripper_left_finger_link_4_collisions != null)
        {
            foreach (var go in collisionComponents.gripper_left_finger_link_4_collisions)
                if (go != null) gripper_left_finger_link_4.LinkModel.Collision.Geometry.Add(new Geometry(go));
        }
        gripper_left_finger_link_4.LinkModel.Collision.Show = false;
        robot.Links.Add(gripper_left_finger_link_4);

        robot.Joints = new List<Joint>();

        Joint torso_lift_link_TIAGo_left_arm_joint = new Joint();
        torso_lift_link_TIAGo_left_arm_joint.Name = "torso_lift_link_TIAGo left arm_joint";
        torso_lift_link_TIAGo_left_arm_joint.Description = "Imported from URDF (fixed)";
        torso_lift_link_TIAGo_left_arm_joint.ParentId = torso_lift_link.Id;
        torso_lift_link_TIAGo_left_arm_joint.ChildId = TIAGo_left_arm.Id;
        torso_lift_link_TIAGo_left_arm_joint.ChildOrigin = new Origin();
        torso_lift_link_TIAGo_left_arm_joint.Type = Joint.JointTypes.Fixed;
        robot.Joints.Add(torso_lift_link_TIAGo_left_arm_joint);

        Joint arm_left_1_joint = new Joint();
        arm_left_1_joint.Name = "arm_left_1_joint";
        arm_left_1_joint.Description = "Imported from URDF (revolute)";
        arm_left_1_joint.ParentId = TIAGo_left_arm.Id;
        arm_left_1_joint.ChildId = TIAGo_left_arm_1.Id;
        arm_left_1_joint.ChildOrigin = new Origin();
        arm_left_1_joint.ChildOrigin.XYZ = new Vector3(0.025f, 0.19f, -0.17f);
        arm_left_1_joint.ChildOrigin.RPY = new Vector3(0f, -180.000412f, -90.000206f);
        arm_left_1_joint.Type = Joint.JointTypes.Revolute;
        arm_left_1_joint.Axes = new List<Axe> { new Axe() };
        arm_left_1_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_left_1_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_left_1_joint.Axes[0].Param.Neg_sw_end.Value = -1.11f;
        arm_left_1_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_1_joint.Axes[0].Param.Pos_sw_end.Value = 1.5f;
        arm_left_1_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_1_joint.Axes[0].Param.dS_max.Value = 1.95f;
        arm_left_1_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_left_1_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_left_1_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_left_1_joint);

        Joint arm_left_2_joint = new Joint();
        arm_left_2_joint.Name = "arm_left_2_joint";
        arm_left_2_joint.Description = "Imported from URDF (revolute)";
        arm_left_2_joint.ParentId = TIAGo_left_arm_1.Id;
        arm_left_2_joint.ChildId = arm_left_2_link.Id;
        arm_left_2_joint.ChildOrigin = new Origin();
        arm_left_2_joint.ChildOrigin.XYZ = new Vector3(0.125f, -0.0195f, 0.031f);
        arm_left_2_joint.ChildOrigin.RPY = new Vector3(90.000206f, 0f, 0f);
        arm_left_2_joint.Type = Joint.JointTypes.Revolute;
        arm_left_2_joint.Axes = new List<Axe> { new Axe() };
        arm_left_2_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_left_2_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_left_2_joint.Axes[0].Param.Neg_sw_end.Value = -1.11f;
        arm_left_2_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_2_joint.Axes[0].Param.Pos_sw_end.Value = 1.5f;
        arm_left_2_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_2_joint.Axes[0].Param.dS_max.Value = 1.95f;
        arm_left_2_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_left_2_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_left_2_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_left_2_joint);

        Joint arm_left_3_joint = new Joint();
        arm_left_3_joint.Name = "arm_left_3_joint";
        arm_left_3_joint.Description = "Imported from URDF (revolute)";
        arm_left_3_joint.ParentId = arm_left_2_link.Id;
        arm_left_3_joint.ChildId = arm_left_3_link.Id;
        arm_left_3_joint.ChildOrigin = new Origin();
        arm_left_3_joint.ChildOrigin.XYZ = new Vector3(0.0895f, 0f, 0f);
        arm_left_3_joint.ChildOrigin.RPY = new Vector3(-90.00348f, 180.000412f, -90.00348f);
        arm_left_3_joint.Type = Joint.JointTypes.Revolute;
        arm_left_3_joint.Axes = new List<Axe> { new Axe() };
        arm_left_3_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_left_3_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_left_3_joint.Axes[0].Param.Neg_sw_end.Value = -0.72f;
        arm_left_3_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_3_joint.Axes[0].Param.Pos_sw_end.Value = 3.86f;
        arm_left_3_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_3_joint.Axes[0].Param.dS_max.Value = 2.35f;
        arm_left_3_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_left_3_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_left_3_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_left_3_joint);

        Joint arm_left_4_joint = new Joint();
        arm_left_4_joint.Name = "arm_left_4_joint";
        arm_left_4_joint.Description = "Imported from URDF (revolute)";
        arm_left_4_joint.ParentId = arm_left_3_link.Id;
        arm_left_4_joint.ChildId = arm_left_4_link.Id;
        arm_left_4_joint.ChildOrigin = new Origin();
        arm_left_4_joint.ChildOrigin.XYZ = new Vector3(-0.02f, -0.027f, 0.2220003f);
        arm_left_4_joint.ChildOrigin.RPY = new Vector3(0f, -90.00033f, 90.00033f);
        arm_left_4_joint.Type = Joint.JointTypes.Revolute;
        arm_left_4_joint.Axes = new List<Axe> { new Axe() };
        arm_left_4_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_left_4_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_left_4_joint.Axes[0].Param.Neg_sw_end.Value = -0.32f;
        arm_left_4_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_4_joint.Axes[0].Param.Pos_sw_end.Value = 2.29f;
        arm_left_4_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_4_joint.Axes[0].Param.dS_max.Value = 2.35f;
        arm_left_4_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_left_4_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_left_4_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_left_4_joint);

        Joint arm_left_5_joint = new Joint();
        arm_left_5_joint.Name = "arm_left_5_joint";
        arm_left_5_joint.Description = "Imported from URDF (revolute)";
        arm_left_5_joint.ParentId = arm_left_4_link.Id;
        arm_left_5_joint.ChildId = arm_left_5_link.Id;
        arm_left_5_joint.ChildOrigin = new Origin();
        arm_left_5_joint.ChildOrigin.XYZ = new Vector3(0.162f, -0.02f, -0.027f);
        arm_left_5_joint.ChildOrigin.RPY = new Vector3(90.00004f, -90.000206f, -90.000206f);
        arm_left_5_joint.Type = Joint.JointTypes.Revolute;
        arm_left_5_joint.Axes = new List<Axe> { new Axe() };
        arm_left_5_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_left_5_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_left_5_joint.Axes[0].Param.Neg_sw_end.Value = -2.07f;
        arm_left_5_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_5_joint.Axes[0].Param.Pos_sw_end.Value = 2.07f;
        arm_left_5_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_5_joint.Axes[0].Param.dS_max.Value = 1.95f;
        arm_left_5_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_left_5_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_left_5_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_left_5_joint);

        Joint arm_left_6_joint = new Joint();
        arm_left_6_joint.Name = "arm_left_6_joint";
        arm_left_6_joint.Description = "Imported from URDF (revolute)";
        arm_left_6_joint.ParentId = arm_left_5_link.Id;
        arm_left_6_joint.ChildId = arm_left_6_link.Id;
        arm_left_6_joint.ChildOrigin = new Origin();
        arm_left_6_joint.ChildOrigin.XYZ = new Vector3(0f, 0f, -0.15f);
        arm_left_6_joint.ChildOrigin.RPY = new Vector3(90.0007858f, -90.0007858f, 0f);
        arm_left_6_joint.Type = Joint.JointTypes.Revolute;
        arm_left_6_joint.Axes = new List<Axe> { new Axe() };
        arm_left_6_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_left_6_joint.Axes[0].AxisVector = new Vector3(0f, 0f, -1f);
        arm_left_6_joint.Axes[0].Param.Neg_sw_end.Value = -1.39f;
        arm_left_6_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_6_joint.Axes[0].Param.Pos_sw_end.Value = 1.39f;
        arm_left_6_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_6_joint.Axes[0].Param.dS_max.Value = 1.76f;
        arm_left_6_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_left_6_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_left_6_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_left_6_joint);

        Joint arm_left_7_joint = new Joint();
        arm_left_7_joint.Name = "arm_left_7_joint";
        arm_left_7_joint.Description = "Imported from URDF (revolute)";
        arm_left_7_joint.ParentId = arm_left_6_link.Id;
        arm_left_7_joint.ChildId = arm_left_7_link.Id;
        arm_left_7_joint.ChildOrigin = new Origin();
        arm_left_7_joint.ChildOrigin.RPY = new Vector3(-90.00015f, 0f, 90.00015f);
        arm_left_7_joint.Type = Joint.JointTypes.Revolute;
        arm_left_7_joint.Axes = new List<Axe> { new Axe() };
        arm_left_7_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_left_7_joint.Axes[0].AxisVector = new Vector3(0f, 0f, -1f);
        arm_left_7_joint.Axes[0].Param.Neg_sw_end.Value = -2.07f;
        arm_left_7_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_7_joint.Axes[0].Param.Pos_sw_end.Value = 2.07f;
        arm_left_7_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_left_7_joint.Axes[0].Param.dS_max.Value = 1.76f;
        arm_left_7_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_left_7_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_left_7_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_left_7_joint);

        Joint arm_left_7_link_wrist_left_ft_tool_link_joint = new Joint();
        arm_left_7_link_wrist_left_ft_tool_link_joint.Name = "arm_left_7_link_wrist_left_ft_tool_link_joint";
        arm_left_7_link_wrist_left_ft_tool_link_joint.Description = "Imported from URDF (fixed)";
        arm_left_7_link_wrist_left_ft_tool_link_joint.ParentId = arm_left_7_link.Id;
        arm_left_7_link_wrist_left_ft_tool_link_joint.ChildId = wrist_left_ft_tool_link.Id;
        arm_left_7_link_wrist_left_ft_tool_link_joint.ChildOrigin = new Origin();
        arm_left_7_link_wrist_left_ft_tool_link_joint.ChildOrigin.XYZ = new Vector3(0f, 0f, 0.05385f);
        arm_left_7_link_wrist_left_ft_tool_link_joint.Type = Joint.JointTypes.Fixed;
        robot.Joints.Add(arm_left_7_link_wrist_left_ft_tool_link_joint);

        Joint wrist_left_ft_tool_link_left_joint = new Joint();
        wrist_left_ft_tool_link_left_joint.Name = "wrist_left_ft_tool_link_left_joint";
        wrist_left_ft_tool_link_left_joint.Description = "Imported from URDF (fixed)";
        wrist_left_ft_tool_link_left_joint.ParentId = wrist_left_ft_tool_link.Id;
        wrist_left_ft_tool_link_left_joint.ChildId = left.Id;
        wrist_left_ft_tool_link_left_joint.ChildOrigin = new Origin();
        wrist_left_ft_tool_link_left_joint.ChildOrigin.XYZ = new Vector3(0f, 0f, 0.0227f);
        wrist_left_ft_tool_link_left_joint.ChildOrigin.RPY = new Vector3(-180.000412f, 0f, -90.00015f);
        wrist_left_ft_tool_link_left_joint.Type = Joint.JointTypes.Fixed;
        robot.Joints.Add(wrist_left_ft_tool_link_left_joint);

        Joint left_hand_gripper_right_finger_joint = new Joint();
        left_hand_gripper_right_finger_joint.Name = "left_hand_gripper_right_finger_joint";
        left_hand_gripper_right_finger_joint.Description = "Imported from URDF (prismatic)";
        left_hand_gripper_right_finger_joint.ParentId = left.Id;
        left_hand_gripper_right_finger_joint.ChildId = gripper_right_finger_link.Id;
        left_hand_gripper_right_finger_joint.ChildOrigin = new Origin();
        left_hand_gripper_right_finger_joint.Type = Joint.JointTypes.Prismatic;
        left_hand_gripper_right_finger_joint.Axes = new List<Axe> { new Axe() };
        left_hand_gripper_right_finger_joint.Axes[0].Type = Axe.Axe_enum.LinX;
        left_hand_gripper_right_finger_joint.Axes[0].AxisVector = new Vector3(1f, 0f, 0f);
        left_hand_gripper_right_finger_joint.Axes[0].Param.Neg_sw_end.Value = 0f;
        left_hand_gripper_right_finger_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("m");
        left_hand_gripper_right_finger_joint.Axes[0].Param.Pos_sw_end.Value = 0.045f;
        left_hand_gripper_right_finger_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("m");
        left_hand_gripper_right_finger_joint.Axes[0].Param.dS_max.Value = 0.05f;
        left_hand_gripper_right_finger_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("m/s");
        left_hand_gripper_right_finger_joint.Axes[0].Param.C_max.Value = 600000f;
        left_hand_gripper_right_finger_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N");
        robot.Joints.Add(left_hand_gripper_right_finger_joint);

        Joint left_hand_gripper_left_finger_joint = new Joint();
        left_hand_gripper_left_finger_joint.Name = "left_hand_gripper_left_finger_joint";
        left_hand_gripper_left_finger_joint.Description = "Imported from URDF (prismatic)";
        left_hand_gripper_left_finger_joint.ParentId = left.Id;
        left_hand_gripper_left_finger_joint.ChildId = gripper_left_finger_link.Id;
        left_hand_gripper_left_finger_joint.ChildOrigin = new Origin();
        left_hand_gripper_left_finger_joint.Type = Joint.JointTypes.Prismatic;
        left_hand_gripper_left_finger_joint.Axes = new List<Axe> { new Axe() };
        left_hand_gripper_left_finger_joint.Axes[0].Type = Axe.Axe_enum.LinX;
        left_hand_gripper_left_finger_joint.Axes[0].AxisVector = new Vector3(-1f, 0f, 0f);
        left_hand_gripper_left_finger_joint.Axes[0].Param.Neg_sw_end.Value = 0f;
        left_hand_gripper_left_finger_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("m");
        left_hand_gripper_left_finger_joint.Axes[0].Param.Pos_sw_end.Value = 0.045f;
        left_hand_gripper_left_finger_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("m");
        left_hand_gripper_left_finger_joint.Axes[0].Param.dS_max.Value = 0.05f;
        left_hand_gripper_left_finger_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("m/s");
        left_hand_gripper_left_finger_joint.Axes[0].Param.C_max.Value = 600000f;
        left_hand_gripper_left_finger_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N");
        robot.Joints.Add(left_hand_gripper_left_finger_joint);

        Joint torso_lift_link_TIAGo_right_arm_joint = new Joint();
        torso_lift_link_TIAGo_right_arm_joint.Name = "torso_lift_link_TIAGo right arm_joint";
        torso_lift_link_TIAGo_right_arm_joint.Description = "Imported from URDF (fixed)";
        torso_lift_link_TIAGo_right_arm_joint.ParentId = torso_lift_link.Id;
        torso_lift_link_TIAGo_right_arm_joint.ChildId = TIAGo_right_arm.Id;
        torso_lift_link_TIAGo_right_arm_joint.ChildOrigin = new Origin();
        torso_lift_link_TIAGo_right_arm_joint.Type = Joint.JointTypes.Fixed;
        robot.Joints.Add(torso_lift_link_TIAGo_right_arm_joint);

        Joint arm_right_1_joint = new Joint();
        arm_right_1_joint.Name = "arm_right_1_joint";
        arm_right_1_joint.Description = "Imported from URDF (revolute)";
        arm_right_1_joint.ParentId = TIAGo_right_arm.Id;
        arm_right_1_joint.ChildId = TIAGo_right_arm_1.Id;
        arm_right_1_joint.ChildOrigin = new Origin();
        arm_right_1_joint.ChildOrigin.XYZ = new Vector3(0.025f, -0.19f, -0.17f);
        arm_right_1_joint.ChildOrigin.RPY = new Vector3(0f, 0f, -90.000206f);
        arm_right_1_joint.Type = Joint.JointTypes.Revolute;
        arm_right_1_joint.Axes = new List<Axe> { new Axe() };
        arm_right_1_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_right_1_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_right_1_joint.Axes[0].Param.Neg_sw_end.Value = -1.11f;
        arm_right_1_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_1_joint.Axes[0].Param.Pos_sw_end.Value = 1.5f;
        arm_right_1_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_1_joint.Axes[0].Param.dS_max.Value = 1.95f;
        arm_right_1_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_right_1_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_right_1_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_right_1_joint);

        Joint arm_right_2_joint = new Joint();
        arm_right_2_joint.Name = "arm_right_2_joint";
        arm_right_2_joint.Description = "Imported from URDF (revolute)";
        arm_right_2_joint.ParentId = TIAGo_right_arm_1.Id;
        arm_right_2_joint.ChildId = arm_right_2_link.Id;
        arm_right_2_joint.ChildOrigin = new Origin();
        arm_right_2_joint.ChildOrigin.XYZ = new Vector3(0.125f, -0.0195f, -0.031f);
        arm_right_2_joint.ChildOrigin.RPY = new Vector3(-90.000206f, 0f, 0f);
        arm_right_2_joint.Type = Joint.JointTypes.Revolute;
        arm_right_2_joint.Axes = new List<Axe> { new Axe() };
        arm_right_2_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_right_2_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_right_2_joint.Axes[0].Param.Neg_sw_end.Value = -1.11f;
        arm_right_2_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_2_joint.Axes[0].Param.Pos_sw_end.Value = 1.5f;
        arm_right_2_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_2_joint.Axes[0].Param.dS_max.Value = 1.95f;
        arm_right_2_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_right_2_joint.Axes[0].Param.C_max.Value = 600000f;
        arm_right_2_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_right_2_joint);

        Joint arm_right_3_joint = new Joint();
        arm_right_3_joint.Name = "arm_right_3_joint";
        arm_right_3_joint.Description = "Imported from URDF (revolute)";
        arm_right_3_joint.ParentId = arm_right_2_link.Id;
        arm_right_3_joint.ChildId = arm_right_3_link.Id;
        arm_right_3_joint.ChildOrigin = new Origin();
        arm_right_3_joint.ChildOrigin.XYZ = new Vector3(0.0895f, 0f, 0f);
        arm_right_3_joint.ChildOrigin.RPY = new Vector3(90.00015f, 180.000412f, -90.00015f);
        arm_right_3_joint.Type = Joint.JointTypes.Revolute;
        arm_right_3_joint.Axes = new List<Axe> { new Axe() };
        arm_right_3_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_right_3_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_right_3_joint.Axes[0].Param.Neg_sw_end.Value = -0.72f;
        arm_right_3_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_3_joint.Axes[0].Param.Pos_sw_end.Value = 3.86f;
        arm_right_3_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_3_joint.Axes[0].Param.dS_max.Value = 2.35f;
        arm_right_3_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_right_3_joint.Axes[0].Param.C_max.Value = 260000f;
        arm_right_3_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_right_3_joint);

        Joint arm_right_4_joint = new Joint();
        arm_right_4_joint.Name = "arm_right_4_joint";
        arm_right_4_joint.Description = "Imported from URDF (revolute)";
        arm_right_4_joint.ParentId = arm_right_3_link.Id;
        arm_right_4_joint.ChildId = arm_right_4_link.Id;
        arm_right_4_joint.ChildOrigin = new Origin();
        arm_right_4_joint.ChildOrigin.XYZ = new Vector3(-0.02f, -0.027f, -0.22f);
        arm_right_4_joint.ChildOrigin.RPY = new Vector3(0f, -90.00033f, -90.000206f);
        arm_right_4_joint.Type = Joint.JointTypes.Revolute;
        arm_right_4_joint.Axes = new List<Axe> { new Axe() };
        arm_right_4_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_right_4_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_right_4_joint.Axes[0].Param.Neg_sw_end.Value = -0.32f;
        arm_right_4_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_4_joint.Axes[0].Param.Pos_sw_end.Value = 2.29f;
        arm_right_4_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_4_joint.Axes[0].Param.dS_max.Value = 2.35f;
        arm_right_4_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_right_4_joint.Axes[0].Param.C_max.Value = 260000f;
        arm_right_4_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_right_4_joint);

        Joint arm_right_5_joint = new Joint();
        arm_right_5_joint.Name = "arm_right_5_joint";
        arm_right_5_joint.Description = "Imported from URDF (revolute)";
        arm_right_5_joint.ParentId = arm_right_4_link.Id;
        arm_right_5_joint.ChildId = arm_right_5_link.Id;
        arm_right_5_joint.ChildOrigin = new Origin();
        arm_right_5_joint.ChildOrigin.XYZ = new Vector3(-0.162f, 0.02f, 0.027f);
        arm_right_5_joint.ChildOrigin.RPY = new Vector3(89.9996948f, -90.000206f, -90.000206f);
        arm_right_5_joint.Type = Joint.JointTypes.Revolute;
        arm_right_5_joint.Axes = new List<Axe> { new Axe() };
        arm_right_5_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_right_5_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_right_5_joint.Axes[0].Param.Neg_sw_end.Value = -2.07f;
        arm_right_5_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_5_joint.Axes[0].Param.Pos_sw_end.Value = 2.07f;
        arm_right_5_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_5_joint.Axes[0].Param.dS_max.Value = 1.95f;
        arm_right_5_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_right_5_joint.Axes[0].Param.C_max.Value = 30000f;
        arm_right_5_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_right_5_joint);

        Joint arm_right_6_joint = new Joint();
        arm_right_6_joint.Name = "arm_right_6_joint";
        arm_right_6_joint.Description = "Imported from URDF (revolute)";
        arm_right_6_joint.ParentId = arm_right_5_link.Id;
        arm_right_6_joint.ChildId = arm_right_6_link.Id;
        arm_right_6_joint.ChildOrigin = new Origin();
        arm_right_6_joint.ChildOrigin.XYZ = new Vector3(0f, 0f, 0.15f);
        arm_right_6_joint.ChildOrigin.RPY = new Vector3(-90.00107f, -90.0007858f, 0f);
        arm_right_6_joint.Type = Joint.JointTypes.Revolute;
        arm_right_6_joint.Axes = new List<Axe> { new Axe() };
        arm_right_6_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_right_6_joint.Axes[0].AxisVector = new Vector3(0f, 0f, 1f);
        arm_right_6_joint.Axes[0].Param.Neg_sw_end.Value = -1.39f;
        arm_right_6_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_6_joint.Axes[0].Param.Pos_sw_end.Value = 1.39f;
        arm_right_6_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_6_joint.Axes[0].Param.dS_max.Value = 1.76f;
        arm_right_6_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_right_6_joint.Axes[0].Param.C_max.Value = 60000f;
        arm_right_6_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_right_6_joint);

        Joint arm_right_7_joint = new Joint();
        arm_right_7_joint.Name = "arm_right_7_joint";
        arm_right_7_joint.Description = "Imported from URDF (revolute)";
        arm_right_7_joint.ParentId = arm_right_6_link.Id;
        arm_right_7_joint.ChildId = arm_right_7_link.Id;
        arm_right_7_joint.ChildOrigin = new Origin();
        arm_right_7_joint.ChildOrigin.RPY = new Vector3(-90.000206f, 0f, 90.000206f);
        arm_right_7_joint.Type = Joint.JointTypes.Revolute;
        arm_right_7_joint.Axes = new List<Axe> { new Axe() };
        arm_right_7_joint.Axes[0].Type = Axe.Axe_enum.RotZ;
        arm_right_7_joint.Axes[0].AxisVector = new Vector3(0f, 0f, -1f);
        arm_right_7_joint.Axes[0].Param.Neg_sw_end.Value = -2.07f;
        arm_right_7_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_7_joint.Axes[0].Param.Pos_sw_end.Value = 2.07f;
        arm_right_7_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("rad");
        arm_right_7_joint.Axes[0].Param.dS_max.Value = 1.76f;
        arm_right_7_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("rad/s");
        arm_right_7_joint.Axes[0].Param.C_max.Value = 60000f;
        arm_right_7_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N*m");
        robot.Joints.Add(arm_right_7_joint);

        Joint arm_right_7_link_wrist_right_ft_tool_link_joint = new Joint();
        arm_right_7_link_wrist_right_ft_tool_link_joint.Name = "arm_right_7_link_wrist_right_ft_tool_link_joint";
        arm_right_7_link_wrist_right_ft_tool_link_joint.Description = "Imported from URDF (fixed)";
        arm_right_7_link_wrist_right_ft_tool_link_joint.ParentId = arm_right_7_link.Id;
        arm_right_7_link_wrist_right_ft_tool_link_joint.ChildId = wrist_right_ft_tool_link.Id;
        arm_right_7_link_wrist_right_ft_tool_link_joint.ChildOrigin = new Origin();
        arm_right_7_link_wrist_right_ft_tool_link_joint.ChildOrigin.XYZ = new Vector3(0f, 0f, -0.05385f);
        arm_right_7_link_wrist_right_ft_tool_link_joint.Type = Joint.JointTypes.Fixed;
        robot.Joints.Add(arm_right_7_link_wrist_right_ft_tool_link_joint);

        Joint wrist_right_ft_tool_link_right_joint = new Joint();
        wrist_right_ft_tool_link_right_joint.Name = "wrist_right_ft_tool_link_right_joint";
        wrist_right_ft_tool_link_right_joint.Description = "Imported from URDF (fixed)";
        wrist_right_ft_tool_link_right_joint.ParentId = wrist_right_ft_tool_link.Id;
        wrist_right_ft_tool_link_right_joint.ChildId = right.Id;
        wrist_right_ft_tool_link_right_joint.ChildOrigin = new Origin();
        wrist_right_ft_tool_link_right_joint.ChildOrigin.XYZ = new Vector3(0f, 0f, -0.0227f);
        wrist_right_ft_tool_link_right_joint.ChildOrigin.RPY = new Vector3(0f, 0f, -90.000206f);
        wrist_right_ft_tool_link_right_joint.Type = Joint.JointTypes.Fixed;
        robot.Joints.Add(wrist_right_ft_tool_link_right_joint);

        Joint right_hand_gripper_right_finger_joint = new Joint();
        right_hand_gripper_right_finger_joint.Name = "right_hand_gripper_right_finger_joint";
        right_hand_gripper_right_finger_joint.Description = "Imported from URDF (prismatic)";
        right_hand_gripper_right_finger_joint.ParentId = right.Id;
        right_hand_gripper_right_finger_joint.ChildId = gripper_right_finger_link_3.Id;
        right_hand_gripper_right_finger_joint.ChildOrigin = new Origin();
        right_hand_gripper_right_finger_joint.ChildOrigin.RPY = new Vector3(0f, 0f, 179.99469f);
        right_hand_gripper_right_finger_joint.Type = Joint.JointTypes.Prismatic;
        right_hand_gripper_right_finger_joint.Axes = new List<Axe> { new Axe() };
        right_hand_gripper_right_finger_joint.Axes[0].Type = Axe.Axe_enum.LinX;
        right_hand_gripper_right_finger_joint.Axes[0].AxisVector = new Vector3(1f, 0f, 0f);
        right_hand_gripper_right_finger_joint.Axes[0].Param.Neg_sw_end.Value = 0f;
        right_hand_gripper_right_finger_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("m");
        right_hand_gripper_right_finger_joint.Axes[0].Param.Pos_sw_end.Value = 0.045f;
        right_hand_gripper_right_finger_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("m");
        right_hand_gripper_right_finger_joint.Axes[0].Param.dS_max.Value = 0.05f;
        right_hand_gripper_right_finger_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("m/s");
        right_hand_gripper_right_finger_joint.Axes[0].Param.C_max.Value = 16000f;
        right_hand_gripper_right_finger_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N");
        robot.Joints.Add(right_hand_gripper_right_finger_joint);

        Joint right_hand_gripper_left_finger_joint = new Joint();
        right_hand_gripper_left_finger_joint.Name = "right_hand_gripper_left_finger_joint";
        right_hand_gripper_left_finger_joint.Description = "Imported from URDF (prismatic)";
        right_hand_gripper_left_finger_joint.ParentId = right.Id;
        right_hand_gripper_left_finger_joint.ChildId = gripper_left_finger_link_4.Id;
        right_hand_gripper_left_finger_joint.ChildOrigin = new Origin();
        right_hand_gripper_left_finger_joint.ChildOrigin.RPY = new Vector3(0f, 0f, 179.99469f);
        right_hand_gripper_left_finger_joint.Type = Joint.JointTypes.Prismatic;
        right_hand_gripper_left_finger_joint.Axes = new List<Axe> { new Axe() };
        right_hand_gripper_left_finger_joint.Axes[0].Type = Axe.Axe_enum.LinX;
        right_hand_gripper_left_finger_joint.Axes[0].AxisVector = new Vector3(-1f, 0f, 0f);
        right_hand_gripper_left_finger_joint.Axes[0].Param.Neg_sw_end.Value = 0f;
        right_hand_gripper_left_finger_joint.Axes[0].Param.Neg_sw_end.Unit = Units.GetUnitFromName("m");
        right_hand_gripper_left_finger_joint.Axes[0].Param.Pos_sw_end.Value = 0.045f;
        right_hand_gripper_left_finger_joint.Axes[0].Param.Pos_sw_end.Unit = Units.GetUnitFromName("m");
        right_hand_gripper_left_finger_joint.Axes[0].Param.dS_max.Value = 0.05f;
        right_hand_gripper_left_finger_joint.Axes[0].Param.dS_max.Unit = Units.GetUnitFromName("m/s");
        right_hand_gripper_left_finger_joint.Axes[0].Param.C_max.Value = 16000f;
        right_hand_gripper_left_finger_joint.Axes[0].Param.C_max.Unit = Units.GetUnitFromName("N");
        robot.Joints.Add(right_hand_gripper_left_finger_joint);

        robot.CyclicTime = 0.01f;
    }

    private void Start()
    {
        Model();
        controller = new ComponentController(robot, gameObject);
        robot.InitRobot();
        robot.SetControlType(Axe.AxeControl_enum.Position);
    }

    private void Update()
    {
        robot.RobotUpdate();
    }
}
