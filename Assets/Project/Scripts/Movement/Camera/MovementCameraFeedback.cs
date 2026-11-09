using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Movement presentation only. Cinemachine's input controller owns orbit/look;
/// this extension never writes input axes, the actor, or the rendered camera.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class MovementCameraFeedback : CinemachineExtension
{
    [System.Serializable]
    private struct TraversalShot
    {
        [Min(0f)] public float lift;
        [Min(0f)] public float fov;
        [Min(0.01f)] public float enterTime;
        [Min(0.01f)] public float returnTime;
        [Min(0.01f)] public float fovExitTime;
    }

    private enum Traversal { None, Vault, Climb, Launch, PoleSpin }

    [Header("Comfort")]
    [SerializeField, Range(0f, 1f)] private float dynamicFovIntensity = 1f;
    [SerializeField, Range(0f, 1f)] private float slideOffsetIntensity = 1f;
    [SerializeField, Range(0f, 1f)] private float landingIntensity = 1f;
    [Tooltip("Strength of traversal camera positions and pole framing. FOV uses Dynamic Fov Intensity.")]
    [SerializeField, Range(0f, 1f)] private float traversalPositionIntensity = 1f;

    [Header("Vault and climb (metres, vertical degrees, seconds)")]
    [SerializeField] private TraversalShot vaultShot = new TraversalShot
        { lift = 0.30f, fov = 8f, enterTime = 0.12f, returnTime = 0.65f, fovExitTime = 0.45f };
    [SerializeField] private TraversalShot climbShot = new TraversalShot
        { lift = 0.45f, fov = 7f, enterTime = 0.15f, returnTime = 0.80f, fovExitTime = 0.55f };

    [Header("TicTac and Wall Rebound")]
    [SerializeField] private TraversalShot launchShot = new TraversalShot
        { lift = 4f, fov = 5f, enterTime = 0.5f, returnTime = 1.5f, fovExitTime = 1f };
    [SerializeField, Min(0f)] private float launchDelay = 0.15f;
    [SerializeField, Min(0f)] private float holdBehindTime = 0.2f;

    [Header("Pole Spin")]
    [SerializeField] private TraversalShot poleShot = new TraversalShot
        { lift = 5f, fov = 5f, enterTime = 0.5f, returnTime = 1.5f, fovExitTime = 1f };
    [SerializeField, Range(0f, 0.95f)] private float poleTurnFraction = 0.25f;
    [SerializeField, Min(0f)] private float poleMaxDegreesPerSecond = 60f;

    [Header("Sprint FOV (additive vertical degrees)")]
    [SerializeField, Min(0f)] private float sprintFov = 5f;
    [SerializeField, Min(0.01f)] private float fovEnterTime = 0.2f;
    [SerializeField, Min(0.01f)] private float fovExitTime = 0.35f;
    [SerializeField, Min(0f)] private float airborneSprintGrace = 0.15f;

    [Header("Slide")]
    [Tooltip("Total additive FOV while sliding; it does not stack with sprint FOV.")]
    [SerializeField, Min(0f)] private float slideFov = 7f;
    [SerializeField, Min(0f)] private float slideDrop = 0.45f;
    [SerializeField, Min(0.01f)] private float slideEnterTime = 0.1f;
    [SerializeField, Min(0.01f)] private float slideExitTime = 0.22f;

    [Header("Landing")]
    [SerializeField, Min(0f)] private float landingDrop = 0.08f;
    [SerializeField, Min(0f)] private float minimumLandingSpeed = 4f;
    [SerializeField, Min(0.01f)] private float landingRecoveryTime = 0.18f;

    private Transform trackedTarget;
    private PlayerMovementIntent player;
    private CharacterMovement movement;
    private CharacterController controller;
    private CharacterState characterState;
    private StaminaSystem stamina;
    private SlideExecutor slide;
    private VaultExecutor vault;
    private ClimbExecutor climb;
    private TicTacExecutor ticTac;
    private WallReboundExecutor rebound;
    private PoleSpinExecutor pole;
    private uint seenTicTac, seenRebound;
    private Behaviour launchSource;
    private readonly ActionCameraOrbit actionOrbit = new ActionCameraOrbit();
    private bool orbitReturn;
    private Collider framedPole;
    internal Collider FramedPole => traversalPositionIntensity > 0f ? framedPole : null;
    private Vector3 lastFocusShift, focusShiftAtEntry;
    private Traversal traversal;
    private TraversalShot shot;
    private bool returning;
    private float phaseTime;
    private Vector3 shotOrigin;
    private Vector3 liveOrbitAtEntry;
    private Vector3 lastLiveOrbit;
    private Vector3 lastCameraPosition;
    private bool hasCameraPosition;
    private Vector3 previousPosition;
    private float previousVerticalSpeed;
    private bool wasGrounded;
    private float airTime;
    private float sprintGraceRemaining;
    private float fovOffset;
    private float slideOffset;
    private float landingOffset;
    private int sampledFrame = -1;
    private bool hasSample;

    public float CurrentFovOffset => fovOffset * dynamicFovIntensity;
    public float CurrentHeightOffset =>
        slideOffset * slideOffsetIntensity + landingOffset * landingIntensity;

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetFeedback();
    }

    private void OnDisable() => ResetFeedback();

    /// <summary>Call on respawn/camera cuts if not already notifying Cinemachine of a warp.</summary>
    public void ResetFeedback()
    {
        fovOffset = slideOffset = landingOffset = 0f;
        previousVerticalSpeed = airTime = sprintGraceRemaining = 0f;
        sampledFrame = -1;
        hasSample = false;
        traversal = Traversal.None;
        returning = hasCameraPosition = false;
        orbitReturn = false;
        framedPole = null;
        lastFocusShift = focusShiftAtEntry = Vector3.zero;
        seenTicTac = ticTac != null ? ticTac.LaunchCount : 0;
        seenRebound = rebound != null ? rebound.LaunchCount : 0;
        launchSource = null;
        phaseTime = 0f;
    }

    public override void OnTargetObjectWarped(
        CinemachineVirtualCameraBase vcam, Transform target, Vector3 positionDelta)
    {
        if (target == trackedTarget)
            ResetFeedback();
    }

    public override void PrePipelineMutateCameraStateCallback(
        CinemachineVirtualCameraBase vcam, ref CameraState state, float deltaTime)
    {
        if (!Application.isPlaying || vcam != ComponentOwner)
            return;

        if (trackedTarget != vcam.Follow || player == null)
            BindPlayer(vcam.Follow);

        if (player == null || !player.isActiveAndEnabled || controller == null || movement == null)
        {
            ResetFeedback();
            return;
        }

        if (deltaTime < 0f || !vcam.PreviousStateIsValid)
            ResetFeedback();

        // Cinemachine can evaluate a camera more than once during one rendered frame.
        if (sampledFrame != Time.frameCount)
        {
            SampleMovement(Mathf.Max(0f, Time.deltaTime));
            sampledFrame = Time.frameCount;
        }

        state.Lens.FieldOfView = Mathf.Clamp(
            state.Lens.FieldOfView + CurrentFovOffset, 1f, 179f);

        // Apply before Body/Aim: the composer and deoccluder see the same lowered
        // focus and camera pose, irrespective of extension registration order.
        Vector3 offset = state.ReferenceUp * CurrentHeightOffset;
        state.PositionCorrection += offset;
        if (state.HasLookAt())
        {
            // RotationComposer adds its authored focus height after this callback.
            Vector3 focusTarget = traversal == Traversal.PoleSpin
                ? (actionOrbit.Pivot - state.ReferenceLookAt) * traversalPositionIntensity
                : Vector3.zero;
            lastFocusShift = (traversal == Traversal.None && !returning)
                ? Vector3.zero
                : Vector3.Lerp(focusShiftAtEntry, focusTarget, ShotBlend(returning ? shot.returnTime : shot.enterTime));
            state.ReferenceLookAt += offset + lastFocusShift;
            if (traversal != Traversal.PoleSpin && lastFocusShift.sqrMagnitude < 0.000001f)
                framedPole = null;
        }
    }

    private void BindPlayer(Transform target)
    {
        trackedTarget = target;
        player = target != null ? target.GetComponentInParent<PlayerMovementIntent>() : null;
        movement = player != null ? player.GetComponent<CharacterMovement>() : null;
        controller = player != null ? player.GetComponent<CharacterController>() : null;
        characterState = player != null ? player.GetComponent<CharacterState>() : null;
        stamina = player != null ? player.GetComponent<StaminaSystem>() : null;
        slide = player != null ? player.GetComponent<SlideExecutor>() : null;
        vault = player != null ? player.GetComponent<VaultExecutor>() : null;
        climb = player != null ? player.GetComponent<ClimbExecutor>() : null;
        ticTac = player != null ? player.GetComponent<TicTacExecutor>() : null;
        rebound = player != null ? player.GetComponent<WallReboundExecutor>() : null;
        pole = player != null ? player.GetComponent<PoleSpinExecutor>() : null;
        ResetFeedback();
    }

    private void UpdateShot(float deltaTime)
    {
        // A rebound can finish and start again within one camera sample.
        bool newRebound = rebound != null && rebound.isActiveAndEnabled && rebound.LaunchCount != seenRebound;
        bool newTicTac = ticTac != null && ticTac.isActiveAndEnabled && ticTac.LaunchCount != seenTicTac;
        seenRebound = rebound != null ? rebound.LaunchCount : 0;
        seenTicTac = ticTac != null ? ticTac.LaunchCount : 0;

        Traversal next = Traversal.None;
        if (vault != null && vault.isActiveAndEnabled && vault.IsExecuting)
            next = Traversal.Vault;
        else if (climb != null && climb.isActiveAndEnabled && climb.IsExecuting)
            next = Traversal.Climb;
        else if (pole != null && pole.isActiveAndEnabled && pole.IsExecuting)
            next = Traversal.PoleSpin;
        else if (newRebound || newTicTac ||
            (traversal == Traversal.Launch && launchSource != null && launchSource.isActiveAndEnabled &&
             phaseTime + deltaTime < launchDelay + launchShot.enterTime + holdBehindTime))
            next = Traversal.Launch;

        bool changedPole = next == Traversal.PoleSpin && traversal == Traversal.PoleSpin &&
            pole.CurrentPolePosition != actionOrbit.Pivot;
        if (next != traversal || changedPole || (next == Traversal.Launch && (newTicTac || newRebound)))
        {
            // Start both entry and recovery at the last visible pose, including collision.
            shotOrigin = lastCameraPosition;
            liveOrbitAtEntry = lastLiveOrbit;
            phaseTime = 0f;
            returning = next == Traversal.None;
            orbitReturn = returning && (traversal == Traversal.Launch || traversal == Traversal.PoleSpin);
            focusShiftAtEntry = lastFocusShift;
            traversal = next;
            switch (next)
            {
                case Traversal.Vault: shot = vaultShot; break;
                case Traversal.Climb: shot = climbShot; break;
                case Traversal.Launch:
                    shot = launchShot;
                    launchSource = newRebound ? (Behaviour)rebound : ticTac;
                    Vector3 direction = newRebound ? rebound.CurrentReboundDirection : ticTac.CurrentTicTacDirection;
                    actionOrbit.BeginLaunch(shotOrigin, lastLiveOrbit - player.transform.position, direction, player.transform.forward);
                    break;
                case Traversal.PoleSpin:
                    shot = poleShot;
                    framedPole = pole.CurrentPoleCollider;
                    actionOrbit.BeginPole(shotOrigin, lastLiveOrbit - player.transform.position, pole.CurrentPolePosition, player.transform.position);
                    break;
                case Traversal.None:
                    if (orbitReturn)
                        actionOrbit.BeginReturn(shotOrigin, player.transform.position);
                    break;
            }
        }
        else if (returning || traversal != Traversal.None)
            phaseTime += deltaTime;

        if (returning && phaseTime >= shot.returnTime)
            returning = false;
        if (traversal == Traversal.PoleSpin)
            actionOrbit.AdvancePole(player.transform.position, poleTurnFraction, poleMaxDegreesPerSecond * deltaTime);
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (!Application.isPlaying || vcam != ComponentOwner || player == null || !player.isActiveAndEnabled)
            return;

        if (stage == CinemachineCore.Stage.Finalize)
        {
            lastCameraPosition = state.GetFinalPosition();
            hasCameraPosition = true;
        }
    }

    internal void ApplyTraversalBody(ref CameraState state)
    {
        Vector3 liveOrbit = state.GetCorrectedPosition();
        lastLiveOrbit = liveOrbit;
        if (player == null || !hasCameraPosition || traversalPositionIntensity <= 0f ||
            (traversal == Traversal.None && !returning))
            return;

        // At partial intensity, follow a fraction of the orbit's travel since entry.
        // Recovery starts from the already blended, visible pose at any intensity.
        Vector3 desired;
        if (returning)
        {
            float t = ShotBlend(shot.returnTime);
            desired = orbitReturn ? actionOrbit.ReturnPosition(player.transform.position, liveOrbit, t)
                : Vector3.Lerp(shotOrigin, liveOrbit, t);
        }
        else
        {
            Vector3 fullShot;
            if (traversal == Traversal.Launch)
                fullShot = actionOrbit.LaunchPosition(player.transform.position, launchDelay, phaseTime, shot.enterTime, shot.lift);
            else if (traversal == Traversal.PoleSpin)
                fullShot = actionOrbit.PolePosition(shot.lift, ShotBlend(shot.enterTime));
            else
                fullShot = shotOrigin + state.ReferenceUp * (shot.lift * ShotBlend(shot.enterTime));
            desired = shotOrigin + (liveOrbit - liveOrbitAtEntry) * (1f - traversalPositionIntensity)
                + (fullShot - shotOrigin) * traversalPositionIntensity;
        }
        state.PositionCorrection += desired - liveOrbit;
    }

    private float ShotBlend(float duration) =>
        Mathf.SmoothStep(0f, 1f, phaseTime / Mathf.Max(0.01f, duration));

    private void SampleMovement(float deltaTime)
    {
        Vector3 position = player.transform.position;
        bool grounded = controller.isGrounded;
        Vector3 displacement = position - previousPosition;

        // A long suspension or teleport must not become a speed/landing impulse.
        if (!hasSample || deltaTime > 0.25f || displacement.sqrMagnitude > 25f)
        {
            ResetFeedback();
            previousPosition = position;
            wasGrounded = grounded;
            hasSample = true;
            return;
        }
        if (deltaTime <= 0f)
        {
            previousPosition = position;
            return;
        }

        Vector3 velocity = displacement / deltaTime;
        UpdateShot(deltaTime);
        float planarSpeed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
        bool sliding = slide != null && slide.IsExecuting;
        bool normalLocomotion = characterState != null &&
            (characterState.CurrentState == LocomotionState.Grounded ||
             characterState.CurrentState == LocomotionState.Airborne);
        bool sprintRequested = player.Sprinting && stamina != null && !stamina.IsExhausted;
        float speedWeight = Mathf.Clamp01(planarSpeed / Mathf.Max(0.01f, movement.SprintSpeed));

        if (normalLocomotion && grounded && sprintRequested && planarSpeed > 0.1f)
            sprintGraceRemaining = airborneSprintGrace;
        else
            sprintGraceRemaining = Mathf.Max(0f, sprintGraceRemaining - deltaTime);

        float targetFov = 0f;
        if (traversal != Traversal.None)
            targetFov = shot.fov;
        else if (sliding)
            targetFov = slideFov * speedWeight;
        else if (normalLocomotion && sprintRequested && (grounded || sprintGraceRemaining > 0f))
            targetFov = sprintFov * speedWeight;

        float fovTime = targetFov > fovOffset ? fovEnterTime : fovExitTime;
        if (traversal != Traversal.None)
            fovTime = shot.enterTime;
        else if (returning)
            fovTime = shot.fovExitTime;
        fovOffset = Damp(fovOffset, targetFov, fovTime, deltaTime);
        slideOffset = Damp(slideOffset, sliding ? -slideDrop : 0f,
            sliding ? slideEnterTime : slideExitTime, deltaTime);
        landingOffset = Damp(landingOffset, 0f, landingRecoveryTime, deltaTime);

        bool landingAllowed = normalLocomotion && traversal != Traversal.Launch &&
            traversal != Traversal.PoleSpin && !(returning && orbitReturn);
        if (landingAllowed && grounded && !wasGrounded && airTime >= 0.12f)
        {
            float impact = Mathf.InverseLerp(minimumLandingSpeed, minimumLandingSpeed + 8f,
                -previousVerticalSpeed);
            landingOffset = -landingDrop * impact;
        }

        airTime = landingAllowed && !grounded ? airTime + deltaTime : 0f;
        previousVerticalSpeed = velocity.y;
        previousPosition = position;
        wasGrounded = grounded;
    }

    // The configured duration reaches 99% of the target, independent of frame rate.
    private static float Damp(float current, float target, float duration, float deltaTime)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-4.60517f * deltaTime / Mathf.Max(0.01f, duration)));
    }
}
