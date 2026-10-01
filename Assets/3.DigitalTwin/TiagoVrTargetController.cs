using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the TiagoDualArmIK target handles with VR controllers (Quest 3, or any OpenXR
/// device), through a per-axis position / velocity / acceleration limiter.
///
/// GRAB SEMANTICS. The handle does NOT jump to the controller. On grip press both poses are
/// recorded, and while the grip is held the controller's DELTA is applied. Mapping
/// absolutely would teleport the IK target to wherever your hand happened to be, and the
/// solver would chase that error at full joint speed — on the real arm.
///
/// THE LIMITER, in order, every frame, per axis:
///   1. desired position from the controller delta (masked to the enabled axes)
///   2. clamp into the Cartesian box
///   3. desired velocity = (desired - current) / dt
///   4. clamp ACCELERATION, then re-derive velocity from it
///   5. clamp velocity
///   6. integrate to a new position
///   7. clamp into the box again
///   8. re-derive the velocity actually achieved, and keep THAT as state
///
/// Step 4 is what a hand-tracked target normally lacks: without it, a flick of the wrist is
/// a step in velocity, and the arm answers with a jerk. Step 8 matters just as much — when
/// the box truncates a move, the stored velocity has to reflect what actually happened, or
/// the acceleration limiter keeps integrating from a speed the handle never reached and the
/// handle crawls away from the wall it just hit.
///
/// Everything is computed in the reference frame, so X/Y/Z limits mean the robot's axes
/// rather than the player's.
///
/// Depends on com.unity.inputsystem only. Give it a Transform per hand that tracks the
/// physical controller: XRI's Left/Right Controller objects, or a bare GameObject with the
/// Input System's TrackedPoseDriver.
/// </summary>
public class TiagoVrTargetController : MonoBehaviour
{
    [System.Serializable]
    public class Limits
    {
        [Header("Axes the hand may move")]
        public bool allowX = true;
        public bool allowY = true;
        public bool allowZ = true;

        [Header("Cartesian box, metres, relative to the reference")]
        public Vector3 min = new Vector3(-0.35f, -0.35f, -0.35f);
        public Vector3 max = new Vector3(0.35f, 0.35f, 0.35f);

        [Header("Per-axis kinematic limits")]
        [Tooltip("m/s. Bounds how fast the IK target travels, and so how fast the arm moves.")]
        public Vector3 maxVelocity = new Vector3(0.3f, 0.3f, 0.3f);

        [Tooltip("m/s². The one your SteamVR script left commented out. Low values feel " +
                 "heavy and damp hand tremor; high values feel direct but pass jerk through.")]
        public Vector3 maxAcceleration = new Vector3(1.2f, 1.2f, 1.2f);

        [Tooltip("deg/s on the handle's orientation. 0 disables rotation entirely.")]
        public float maxAngularSpeedDeg = 90f;
    }

    [System.Serializable]
    public class Hand
    {
        public string name = "right";

        [Tooltip("Transform tracking the physical controller. XRI's controller object, or a " +
                 "GameObject with a TrackedPoseDriver.")]
        public Transform controller;

        [Tooltip("Bind to XRI LeftHand/RightHand Interaction > Select Value, or " +
                 "<XRController>{RightHand}/grip.")]
        public InputActionProperty gripAction;

        [Range(0.05f, 0.95f)] public float gripThreshold = 0.5f;

        [Header("Status (read-only)")]
        public bool holding;
        public Vector3 velocity;          // in reference space, m/s
        public Vector3 acceleration;      // in reference space, m/s²

        // Grab reference, reference space.
        [HideInInspector] public Vector3 grabControllerRef, grabTargetRef;
        [HideInInspector] public Quaternion grabControllerRot, grabTargetRot;

        // Limiter state, reference space.
        [HideInInspector] public Vector3 prevPos, prevVel;
        [HideInInspector] public GameObject knownTarget;
        [HideInInspector] public Vector3 spawnRefOrigin;
    }

    [Header("Target")]
    public TiagoDualArmIK armIK;

    [Header("Hands")]
    public Hand leftHand = new Hand { name = "left" };
    public Hand rightHand = new Hand { name = "right" };

    [Header("Limits")]
    public Limits leftLimits = new Limits();
    public Limits rightLimits = new Limits();

    [Header("Mapping")]
    [Tooltip("Controller travel to handle travel. Below 1 trades range for precision.")]
    [Range(0.1f, 2f)] public float positionScale = 0.5f;

    [Tooltip("Frame the box and the per-axis limits are expressed in — use the twin's " +
             "torso_lift_link so X/Y/Z mean the robot's axes. If empty, the box is " +
             "world-axis-aligned and centred on where the handle spawned.")]
    public Transform limitReference;

    private void OnEnable()
    {
        EnableAction(leftHand);
        EnableAction(rightHand);
    }

    private void OnDisable()
    {
        // Drop any grab so a handle is never left mid-drag with a stale reference pose.
        leftHand.holding = false;
        rightHand.holding = false;
        DisableAction(leftHand);
        DisableAction(rightHand);
    }

    private void Update()
    {
        if (armIK == null) return;
        Tick(leftHand, leftLimits, armIK.leftArm);
        Tick(rightHand, rightLimits, armIK.rightArm);
    }

    // ------------------------------------------------------------- reference frame

    private Vector3 ToRef(Hand hand, Vector3 world)
    {
        return limitReference != null
             ? limitReference.InverseTransformPoint(world)
             : world - hand.spawnRefOrigin;
    }

    private Vector3 ToWorld(Hand hand, Vector3 local)
    {
        return limitReference != null
             ? limitReference.TransformPoint(local)
             : local + hand.spawnRefOrigin;
    }

    // -------------------------------------------------------------------- per hand

    private void Tick(Hand hand, Limits lim, TiagoDualArmIK.ArmIK arm)
    {
        if (hand == null || arm == null || hand.controller == null) return;

        // The handle only exists while the IK is following: TiagoDualArmIK spawns it in
        // UpdateTarget and destroys it when followTarget goes false.
        GameObject targetGo = arm.followTarget ? arm.target : null;
        if (targetGo == null)
        {
            hand.holding = false;
            hand.knownTarget = null;
            hand.velocity = hand.acceleration = Vector3.zero;
            return;
        }

        Transform target = targetGo.transform;

        // A freshly spawned handle carries no history, and in world-box mode it also
        // defines the box centre.
        if (hand.knownTarget != targetGo)
        {
            hand.knownTarget = targetGo;
            hand.spawnRefOrigin = target.position;
            hand.prevPos = ToRef(hand, target.position);
            hand.prevVel = Vector3.zero;
            hand.velocity = hand.acceleration = Vector3.zero;
            hand.holding = false;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        bool pressed = ReadGrip(hand) >= hand.gripThreshold;

        if (pressed && !hand.holding)
        {
            hand.holding = true;
            hand.grabControllerRef = ToRef(hand, hand.controller.position);
            hand.grabControllerRot = hand.controller.rotation;
            hand.grabTargetRef = ToRef(hand, target.position);
            hand.grabTargetRot = target.rotation;
            hand.prevPos = hand.grabTargetRef;
            hand.prevVel = Vector3.zero;              // start from rest, never from a kick
            return;                                   // no motion on the frame of the grab
        }

        if (!pressed)
        {
            // Releasing stops the handle. Stopping a target is safe; the arm simply
            // decelerates to it. Zeroing the state means the next grab starts from rest.
            hand.holding = false;
            hand.prevPos = ToRef(hand, target.position);
            hand.prevVel = Vector3.zero;
            hand.velocity = hand.acceleration = Vector3.zero;
            return;
        }

        // 1. desired position from the controller delta, masked to the enabled axes
        Vector3 controllerRef = ToRef(hand, hand.controller.position);
        Vector3 delta = (controllerRef - hand.grabControllerRef) * positionScale;
        if (!lim.allowX) delta.x = 0f;
        if (!lim.allowY) delta.y = 0f;
        if (!lim.allowZ) delta.z = 0f;
        Vector3 desired = ClampBox(hand.grabTargetRef + delta, lim);   // 2.

        // 3. desired velocity, 4. acceleration clamp, 5. velocity clamp
        Vector3 vDesired = (desired - hand.prevPos) / dt;
        Vector3 accel = (vDesired - hand.prevVel) / dt;
        accel = ClampPerAxis(accel, lim.maxAcceleration);
        Vector3 v = ClampPerAxis(hand.prevVel + accel * dt, lim.maxVelocity);

        // 6. integrate, 7. clamp into the box again
        Vector3 pos = ClampBox(hand.prevPos + v * dt, lim);

        // 8. keep the velocity actually achieved, not the one we asked for — otherwise
        // hitting a box wall leaves the limiter integrating from a phantom speed.
        Vector3 vAchieved = (pos - hand.prevPos) / dt;
        hand.acceleration = (vAchieved - hand.prevVel) / dt;
        hand.velocity = vAchieved;
        hand.prevVel = vAchieved;
        hand.prevPos = pos;

        target.position = ToWorld(hand, pos);

        if (lim.maxAngularSpeedDeg > 0f)
        {
            Quaternion wanted = (hand.controller.rotation *
                                 Quaternion.Inverse(hand.grabControllerRot)) * hand.grabTargetRot;
            // RotateTowards is already a per-frame angular-rate limit.
            target.rotation = Quaternion.RotateTowards(target.rotation, wanted,
                                                       lim.maxAngularSpeedDeg * dt);
        }
    }

    // ----------------------------------------------------------------------- helpers

    private static Vector3 ClampBox(Vector3 p, Limits lim)
    {
        return new Vector3(Mathf.Clamp(p.x, lim.min.x, lim.max.x),
                           Mathf.Clamp(p.y, lim.min.y, lim.max.y),
                           Mathf.Clamp(p.z, lim.min.z, lim.max.z));
    }

    private static Vector3 ClampPerAxis(Vector3 v, Vector3 limit)
    {
        return new Vector3(Mathf.Clamp(v.x, -Mathf.Abs(limit.x), Mathf.Abs(limit.x)),
                           Mathf.Clamp(v.y, -Mathf.Abs(limit.y), Mathf.Abs(limit.y)),
                           Mathf.Clamp(v.z, -Mathf.Abs(limit.z), Mathf.Abs(limit.z)));
    }

    private static float ReadGrip(Hand hand)
    {
        InputAction a = hand.gripAction.action;
        if (a == null || !a.enabled) return 0f;
        // Works for an analog axis and for a button, which reads back as 0 or 1.
        return a.ReadValue<float>();
    }

    private static void EnableAction(Hand hand)
    {
        if (hand != null && hand.gripAction.action != null) hand.gripAction.action.Enable();
    }

    private static void DisableAction(Hand hand)
    {
        if (hand != null && hand.gripAction.action != null) hand.gripAction.action.Disable();
    }
}
