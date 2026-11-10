using UnityEngine;

public class SlideExecutor : MonoBehaviour
{
    [SerializeField]
    private CharacterState characterState;
    [SerializeField] private Animator animator;
    [SerializeField] private StaminaHUDController staminaHUDController;

    [SerializeField]
    private float slideHeightRatio = 0.5f;

    [SerializeField]
    private float slideDuration = 0.6f;

    [SerializeField]
    private float slideSpeedMultiplier = 1.8f;

    [SerializeField]
    private float staminaCostMultiplier = 0.3f;

    [SerializeField]
    private LayerMask standingClearanceLayer;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;

    public float SlideSpeedMultiplier =>
    slideSpeedMultiplier;

    private CharacterController characterController;
    private StaminaSystem staminaSystem;
    private AINavMeshDriver aiNavMeshDriver;

    private float standingHeight;
    private Vector3 standingCenter;

    private bool isExecuting;
    private float elapsedTime;


    private Vector3 slideDirection;

    public bool IsExecuting => isExecuting;

    public Vector3 CurrentSlideDirection =>
        slideDirection;


    public float Progress
    {
        get
        {
            if (slideDuration <= 0f)
                return 1f;

            return Mathf.Clamp01(
                elapsedTime / slideDuration
            );
        }
    }

    public float GetCurrentSpeedMultiplier()
    {
        float t = Progress;

        if (t < 0.2f)
        {
            float burstT = t / 0.2f;

            return Mathf.Lerp(
                1f,
                slideSpeedMultiplier,
                burstT
            );
        }

        float decelerationT =
            (t - 0.2f) / 0.8f;

        return Mathf.Lerp(
            slideSpeedMultiplier,
            1f,
            decelerationT
        );
    }

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        staminaSystem =
            GetComponent<StaminaSystem>();

        aiNavMeshDriver =
            GetComponent<AINavMeshDriver>();

        standingHeight =
            characterController.height;

        standingCenter =
            characterController.center;

    }

    public bool TryExecute(Vector3 direction)
    {
        if (isExecuting)
            return false;

        if (characterController == null || characterState == null || staminaSystem == null)
            return false;

        float slideCost =
        staminaSystem.MaxStamina *
        staminaCostMultiplier;

        if (!staminaSystem.TryConsume(slideCost))
        {
            if (staminaHUDController != null)
                staminaHUDController.NotifyInsufficientStamina();
            return false;
        }


        slideDirection = direction;

        isExecuting = true;
        elapsedTime = 0f;

        ApplySlidingGeometry();

        if (aiNavMeshDriver != null)
            aiNavMeshDriver.BeginParkourTraversal();

        characterState.SetState(
            LocomotionState.Sliding
        );

        if (animator != null)
            animator.SetTrigger("Slide");

        return true;
    }

    public bool CanPassUnderObstacle(
        ParkourObstacleData obstacle,
        Vector3 direction,
        LayerMask obstacleLayer)
    {
        if (characterController == null ||
            obstacle == null ||
            !obstacle.IsValid ||
            obstacleLayer.value == 0 ||
            !ObstacleProjectionUtility.TryGetProjectedFarEdge(
                obstacle,
                direction,
                out float farEdgeProjection))
        {
            return false;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        direction.Normalize();

        float slideHeight = Mathf.Max(
            characterController.radius * 2f,
            standingHeight * slideHeightRatio);

        float standingBottomOffset =
            standingCenter.y - standingHeight * 0.5f;

        Vector3 slideCenter = standingCenter;
        slideCenter.y = standingBottomOffset + slideHeight * 0.5f;

        Vector3 worldCenter = transform.TransformPoint(slideCenter);
        float radius = characterController.radius;
        float segmentHalfHeight = Mathf.Max(0f, slideHeight * 0.5f - radius);
        Vector3 point1 = worldCenter + transform.up * segmentHalfHeight;
        Vector3 point2 = worldCenter - transform.up * segmentHalfHeight;

        float distancePastObstacle =
            farEdgeProjection
            - Vector3.Dot(transform.position, direction)
            + radius
            + 0.05f;

        if (distancePastObstacle <= 0f)
            return false;

        return !Physics.CapsuleCast(
            point1,
            point2,
            radius,
            direction,
            distancePastObstacle,
            obstacleLayer,
            QueryTriggerInteraction.Ignore);
    }

    public void CancelForSpawn()
    {
        if (isExecuting)
            RestoreStandingGeometry();
        isExecuting = false;
    }

    private void Update()
    {
        if (!isExecuting || GameplayPauseController.IsPaused)
            return;

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= slideDuration)
        {
            FinishSlide();
        }
    }

    private void FinishSlide()
    {
        if (!CanStand())
            return;

        RestoreStandingGeometry();

        isExecuting = false;

        characterState.SetState(
            LocomotionState.Grounded
        );

        if (aiNavMeshDriver != null &&
            !aiNavMeshDriver.RecoverAfterParkour(navMeshSampleRadius))
        {
            Debug.LogWarning(
                $"SLIDE NAVMESH RECOVERY FAILED | position={transform.position}",
                this);
        }
    }

    private void ApplySlidingGeometry()
    {
        float slideHeight =
            standingHeight * slideHeightRatio;

        float standingBottomOffset =
            standingCenter.y -
            standingHeight * 0.5f;

        Vector3 slideCenter =
            standingCenter;

        slideCenter.y =
            standingBottomOffset +
            slideHeight * 0.5f;

        characterController.height =
            slideHeight;

        characterController.center =
            slideCenter;
    }

    private void RestoreStandingGeometry()
    {
        characterController.height =
            standingHeight;

        characterController.center =
            standingCenter;
    }

    private bool CanStand()
    {
        float radius = characterController.radius;

        float halfHeight =
            standingHeight * 0.5f;

        Vector3 worldCenter =
            transform.position +
            standingCenter;

        Vector3 point1 =
            worldCenter +
            Vector3.up * (halfHeight - radius);

        Vector3 point2 =
            worldCenter -
            Vector3.up * (halfHeight - radius);

        return !Physics.CheckCapsule(
            point1,
            point2,
            radius,
            standingClearanceLayer,
            QueryTriggerInteraction.Ignore
        );
    }

}
