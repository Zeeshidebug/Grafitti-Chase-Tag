using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Owns only the endscreen's display actors and cinematic camera.</summary>
[DisallowMultipleComponent]
public sealed class EndscreenShowcase : MonoBehaviour
{
    public enum ShowcasePhase { Idle, Gate, Dolly, Orbit }

    [Header("Camera and intro waypoints")]
    [SerializeField] private Camera showcaseCamera;
    [SerializeField] private Transform gateOpening;
    [SerializeField] private Transform gateApproach;
    [SerializeField] private Transform gatePass;
    [SerializeField] private Transform courtyardArrival;
    [SerializeField, Min(0f)] private float openingHoldSeconds = 1.3f;
    [SerializeField, Min(0.1f)] private float gateApproachSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float courtyardDollySeconds = 3.2f;
    [SerializeField, Range(20f, 80f)] private float introFieldOfView = 48f;

    [Header("Model-only actors and podium feet anchors")]
    [SerializeField] private Transform playerActor;
    [SerializeField] private Transform aiActor;
    [SerializeField] private Transform winnerAnchor;
    [SerializeField] private Transform loserAnchor;
    [SerializeField] private AnimationClip playerWinPose;
    [SerializeField] private AnimationClip playerLosePose;
    [SerializeField] private AnimationClip aiWinPose;
    [SerializeField] private AnimationClip aiLosePose;
    [SerializeField] private bool loopNonLoopingPoses;

    [Header("Side cut and continuous right-left-right arc")]
    [SerializeField] private Transform orbitCenter;
    [SerializeField, Min(1f)] private float orbitRadius = 15f;
    [SerializeField] private float orbitHeight = 2.1f;
    [SerializeField] private float orbitCenterYaw;
    [SerializeField, Range(0f, 65f)] private float orbitArcDegrees = 18f;
    [SerializeField, Min(2f)] private float orbitLoopSeconds = 18f;
    [SerializeField, Range(20f, 80f)] private float orbitFieldOfView = 42f;
    [SerializeField] private bool useHalfScreenViewport = true;
    [SerializeField, Range(0.5f, 1f)] private float showcaseViewportWidth = 0.64f;
    [SerializeField, Range(0f, .4f)] private float compositionShift = .22f;

    public ShowcasePhase Phase { get; private set; }
    public bool PlayerWon { get; private set; }
    public bool IsRevealed => Phase == ShowcasePhase.Orbit;
    public float IntroDuration => openingHoldSeconds + gateApproachSeconds + courtyardDollySeconds;

    private float elapsed;
    private PosePlayback playerPlayback;
    private PosePlayback aiPlayback;
    private Rect originalViewport;
    private float originalFieldOfView;
    private bool capturedCamera;

    public void Begin(bool playerWon, bool isDraw = false)
    {
        StopPoses();
        PlayerWon = playerWon;
        elapsed = 0f;
        Phase = ShowcasePhase.Gate;
        if (showcaseCamera != null && !capturedCamera)
        {
            originalViewport = showcaseCamera.rect;
            originalFieldOfView = showcaseCamera.fieldOfView;
            capturedCamera = true;
        }

        PlaceActor(playerActor, playerWon ? winnerAnchor : loserAnchor);
        PlaceActor(aiActor, playerWon ? loserAnchor : winnerAnchor);
        playerPlayback = CreatePose(playerActor, playerWon ? playerWinPose : playerLosePose);
        aiPlayback = CreatePose(aiActor, isDraw || !playerWon ? aiWinPose : aiLosePose);
        FrameCamera();
    }

    public void SkipToOrbit()
    {
        if (Phase == ShowcasePhase.Idle) return;
        elapsed = IntroDuration;
        Phase = ShowcasePhase.Orbit;
        FrameCamera();
    }

    private void LateUpdate()
    {
        if (Phase == ShowcasePhase.Idle) return;
        elapsed += Time.unscaledDeltaTime;
        Phase = elapsed < openingHoldSeconds ? ShowcasePhase.Gate
            : elapsed < IntroDuration ? ShowcasePhase.Dolly : ShowcasePhase.Orbit;
        EvaluatePose(playerPlayback);
        EvaluatePose(aiPlayback);
        FrameCamera();
    }

    private void FrameCamera()
    {
        if (showcaseCamera == null) return;
        float width = Mathf.Clamp(showcaseViewportWidth, 0.5f, 1f);
        showcaseCamera.rect = useHalfScreenViewport
            ? new Rect(PlayerWon ? 0f : 1f - width, 0f, width, 1f)
            : new Rect(0f, 0f, 1f, 1f);
        if (Phase != ShowcasePhase.Orbit)
        {
            showcaseCamera.fieldOfView = introFieldOfView;
            if (elapsed <= openingHoldSeconds) CopyWaypoint(gateOpening);
            else if (elapsed < openingHoldSeconds + gateApproachSeconds)
            {
                float t = Mathf.SmoothStep(0f, 1f,
                    (elapsed - openingHoldSeconds) / Mathf.Max(0.1f, gateApproachSeconds));
                BlendWaypoints(gateOpening, gateApproach, t);
            }
            else
            {
                float t = Mathf.Clamp01((elapsed - openingHoldSeconds - gateApproachSeconds)
                    / Mathf.Max(0.1f, courtyardDollySeconds));
                // Two straight legs keep the lens inside the gate opening; no spline overshoot.
                if (t < 0.35f) BlendWaypoints(gateApproach, gatePass, t / 0.35f);
                else BlendWaypoints(gatePass, courtyardArrival, (t - 0.35f) / 0.65f);
            }
            ApplyComposition();
            return;
        }

        showcaseCamera.fieldOfView = orbitFieldOfView;
        if (orbitCenter == null) return;
        float orbitTime = Mathf.Max(0f, elapsed - IntroDuration);
        float yaw = orbitCenterYaw + orbitArcDegrees
            * Mathf.Cos(orbitTime / Mathf.Max(2f, orbitLoopSeconds) * Mathf.PI * 2f);
        Vector3 radial = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 position = orbitCenter.position + radial * orbitRadius + Vector3.up * orbitHeight;
        showcaseCamera.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(orbitCenter.position - position, Vector3.up));
        ApplyComposition();
    }

    private void ApplyComposition()
    {
        // The viewport extends beneath the diagonal. Offset the lens so the
        // actors sit in the center of the actually visible portion of that view.
        showcaseCamera.ResetProjectionMatrix();
        if (!useHalfScreenViewport) return;
        Matrix4x4 projection = showcaseCamera.projectionMatrix;
        projection.m02 += PlayerWon ? compositionShift : -compositionShift;
        showcaseCamera.projectionMatrix = projection;
    }

    private void CopyWaypoint(Transform waypoint)
    {
        if (waypoint != null) showcaseCamera.transform.SetPositionAndRotation(waypoint.position, waypoint.rotation);
    }

    private void BlendWaypoints(Transform from, Transform to, float amount)
    {
        if (from == null || to == null) return;
        showcaseCamera.transform.SetPositionAndRotation(Vector3.Lerp(from.position, to.position, amount),
            Quaternion.Slerp(from.rotation, to.rotation, amount));
    }

    private static void PlaceActor(Transform actor, Transform anchor)
    {
        if (actor == null || anchor == null) return;
        actor.SetPositionAndRotation(anchor.position, anchor.rotation);
        actor.gameObject.SetActive(true);
    }

    private PosePlayback CreatePose(Transform actor, AnimationClip clip)
    {
        if (actor == null || clip == null) return null;
        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if (animator == null) return null;
        animator.enabled = true;
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.fireEvents = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PlayableGraph graph = PlayableGraph.Create("Endscreen " + actor.name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        playable.SetSpeed(0d);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Pose", animator);
        output.SetSourcePlayable(playable);
        graph.Play();
        var result = new PosePlayback { Graph = graph, Playable = playable, Clip = clip };
        EvaluatePose(result);
        return result;
    }

    private void EvaluatePose(PosePlayback pose)
    {
        if (pose == null || !pose.Graph.IsValid()) return;
        float length = Mathf.Max(0.001f, pose.Clip.length);
        // Replay the performance at the close-up cut, after the establishing move.
        float performanceTime = Phase == ShowcasePhase.Orbit ? Mathf.Max(0f,elapsed-IntroDuration) : elapsed;
        double poseTime = pose.Clip.isLooping || loopNonLoopingPoses
            ? Mathf.Repeat(performanceTime, length) : Mathf.Min(performanceTime, Mathf.Max(0f, length - 0.001f));
        pose.Playable.SetTime(poseTime);
        pose.Graph.Evaluate(0f);
    }

    private void StopPoses()
    {
        if (playerPlayback != null && playerPlayback.Graph.IsValid()) playerPlayback.Graph.Destroy();
        if (aiPlayback != null && aiPlayback.Graph.IsValid()) aiPlayback.Graph.Destroy();
        playerPlayback = null;
        aiPlayback = null;
    }

    private void OnDisable()
    {
        StopPoses();
        Phase = ShowcasePhase.Idle;
        if (showcaseCamera == null || !capturedCamera) return;
        showcaseCamera.rect = originalViewport;
        showcaseCamera.fieldOfView = originalFieldOfView;
        showcaseCamera.ResetProjectionMatrix();
        capturedCamera = false;
    }

    private sealed class PosePlayback
    {
        public PlayableGraph Graph;
        public AnimationClipPlayable Playable;
        public AnimationClip Clip;
    }
}
