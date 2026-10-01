using System.Collections.Generic;
using UnityEngine;
using DT.Model;
using Component = DT.Model.Component;
using DT.Simulation;

namespace DT.Controller
{
public class ComponentController : BaseController<Component>
{

        public List<ComponentController> SubComponentControllers = new List<ComponentController>();
        public List<JointController> JointControllers = new List<JointController>();


        public ComponentController(Component model, GameObject Root)
        {

            // Wait for scheduler 


            Setup(model, Root);
        }
        public ComponentController()
        {
           
        }

        public void Setup(Component model, GameObject root = null)
        {
            base.Setup(model);

            // Create controllers for subComponents (recursive)
            if (Model.Components!= null)
            {
                foreach (var subComponent in Model.Components)
                {

                    SubComponentControllers.Add(new ComponentController(subComponent, root));
                }

            }

            // Parse Component Tree : Links / Joints + Add JointControllers
            var GO = BuildLinkTree(root);


            // Register value update to Scheduler (null in edit mode — safe to skip)
            Scheduler.Instance?.RegisterMethod(ComputeModel, Model.CyclicTime);



        }

        #region Tree_architecture_generator

        /// <summary>
        /// Convert list of child/parent ref to tree
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private GameObject BuildLinkTree(GameObject root_ = null)
        {
            if (root_ == null)
            {
                root_ = new GameObject(Model.Name);
            }
            else
            {
                root_.name = Model.Name;
            }

            Link root = Model.Links[0];
            foreach (var link in Model.Links)
            {
                if (link.IsRoot)
                {
                    root = link;
                    break;
                }
            }
            
            CreateGameObjects(root, root_.transform);
            
            return root_;
        }

        /// <summary>
        /// Create GameObject and joint controllers on tree
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parentTransform"></param>
        /// <returns></returns>
        private void CreateGameObjects(Link pLink, Transform parentTransform)
        {
            // ── Link GO ── find existing (editor-built) or create fresh ──────────
            var existingLink = parentTransform.Find(pLink.Name);
            var gameObject   = existingLink != null
                             ? existingLink.gameObject
                             : new GameObject(pLink.Name);
            if (existingLink == null)
            {
                gameObject.transform.SetParent(parentTransform, false);
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
            }
            if (pLink.IsRoot)
            {
                ArticulationBody body = gameObject.GetComponent<ArticulationBody>();
                if (body == null) body = gameObject.AddComponent<ArticulationBody>();
                body.immovable = true;
            }

            // ── Visual ───────────────────────────────────────────────────────────
            var existingVis = gameObject.transform.Find("Visual");
            var visual      = existingVis != null ? existingVis.gameObject : new GameObject("Visual");
            if (existingVis == null) visual.transform.SetParent(gameObject.transform, false);
            if (pLink.LinkModel.Visual != null)
            {
                if (existingVis == null)
                {
                    visual.transform.localPosition = pLink.LinkModel.Visual.Origin.XYZ.ToIndirectCoor();
                    visual.transform.localRotation = pLink.LinkModel.Visual.Origin.RPY.ToIndirectRot();
                    visual.SetActive(pLink.LinkModel.Visual.Show);
                }
                foreach (var geo in pLink.LinkModel.Visual.Geometry)
                {
                    if (geo.gameObject != null && geo.gameObject.transform.parent != visual.transform)
                    {
                        geo.EnsureLoaded();
                        // Keep the geometry's own local position & scale (SetParent(false) preserves
                        // the local TRS) so multiple meshes can be laid out within one link's Visual.
                        // Orientation still comes from the Visual node's origin, so only rotation is reset.
                        geo.gameObject.transform.SetParent(visual.transform, false);
                        geo.gameObject.transform.localRotation = Quaternion.identity;
                    }
                }
            }

            // ── Collision ────────────────────────────────────────────────────────
            var existingCol = gameObject.transform.Find("Collision");
            var collision   = existingCol != null ? existingCol.gameObject : new GameObject("Collision");
            if (existingCol == null) collision.transform.SetParent(gameObject.transform, false);
            if (pLink.LinkModel.Collision != null)
            {
                if (existingCol == null)
                {
                    collision.transform.localPosition = pLink.LinkModel.Collision.Origin.XYZ.ToIndirectCoor();
                    collision.transform.localRotation = pLink.LinkModel.Collision.Origin.RPY.ToIndirectRot();
                }
                foreach (var geo in pLink.LinkModel.Collision.Geometry)
                {
                    if (geo.gameObject != null && geo.gameObject.transform.parent != collision.transform)
                    {
                        geo.EnsureLoaded();
                        geo.gameObject.transform.SetParent(collision.transform, false);
                        geo.gameObject.transform.localPosition = Vector3.zero;
                        geo.gameObject.transform.localRotation = Quaternion.identity;
                    }
                }
            }

            // ── Frames ───────────────────────────────────────────────────────────
            var existingFrames = gameObject.transform.Find("Frames");
            var frames         = existingFrames != null ? existingFrames.gameObject : new GameObject("Frames");
            if (existingFrames == null)
            {
                frames.transform.SetParent(gameObject.transform, false);
                frames.transform.localPosition = Vector3.zero;
                frames.transform.localRotation = Quaternion.identity;
            }
            if (pLink.LinkModel.Visual != null && frames.transform.Find("Visual") == null)
            {
                var visFrame = new GameObject("Visual");
                visFrame.transform.SetParent(frames.transform, false);
                visFrame.transform.localPosition = pLink.LinkModel.Visual.Origin.XYZ.ToIndirectCoor();
                visFrame.transform.localRotation = pLink.LinkModel.Visual.Origin.RPY.ToIndirectRot();
            }
            if (pLink.LinkModel.Collision != null && frames.transform.Find("Collision") == null)
            {
                var colFrame = new GameObject("Collision");
                colFrame.transform.SetParent(frames.transform, false);
                colFrame.transform.localPosition = pLink.LinkModel.Collision.Origin.XYZ.ToIndirectCoor();
                colFrame.transform.localRotation = pLink.LinkModel.Collision.Origin.RPY.ToIndirectRot();
            }

            // ── Joints ───────────────────────────────────────────────────────────
            for (int i = 0; i < Model.Joints.Count; i++)
            {
                bool parentMatch = Model.Joints[i].ParentId == pLink.Name
                                || Model.Joints[i].ParentId == pLink.Id;
                if (!parentMatch) continue;

                var existingJoint = gameObject.transform.Find(Model.Joints[i].Name);
                var jointGO       = existingJoint != null
                                  ? existingJoint.gameObject
                                  : new GameObject(Model.Joints[i].Name);
                if (existingJoint == null)
                {
                    jointGO.transform.SetParent(gameObject.transform, false);
                    if (Model.Joints[i].ChildOrigin != null)
                    {
                        jointGO.transform.localPosition = Model.Joints[i].ChildOrigin.XYZ.ToIndirectCoor();
                        jointGO.transform.localRotation = Model.Joints[i].ChildOrigin.RPY.ToIndirectRot();
                    }
                }

                JointController j = JointController.CreateJoint(jointGO, Model.Joints[i]);
                j.SetParentInfo(pLink);
                JointControllers.Add(j);

                Link child = Model.Links.Find(l => l.Name == Model.Joints[i].ChildId)
                          ?? Model.Links.FindIdInList(Model.Joints[i].ChildId);
                if (child == null)
                    Debug.LogError("[ComponentController] child link not found for ChildId: " + Model.Joints[i].ChildId);
                else
                    CreateGameObjects(child, jointGO.transform);
            }
        }
        #endregion

        // Computation
        public virtual void ComputeModel()
        {
            
            // Compute models for subcomponents
            foreach (var subController in SubComponentControllers)
            {
                subController.ComputeModel(); // Model is not computed automatically if Model.CyclicTime = 0.0f
            }
            // Update Vars / Param conntected through OPCUA
            // TODO : is it RTCommunication Manager ? Is it triggered by Component ?
            

            // Update Joints 
            foreach (var joint in JointControllers)
            {
                // Joint compute and apply position change
                joint.Compute(Model.CyclicTime);

            }

            // Update Vars / Param after
            // TODO 



        }


        public void RegisterCyclicComponent(float time)
        {
            
        }
    }
}

