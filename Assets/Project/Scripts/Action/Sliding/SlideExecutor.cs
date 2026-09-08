using UnityEngine;

public class SlideExecutor : MonoBehaviour
{
    [SerializeField]
    private CharacterState characterState;

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

    public float SlideSpeedMultiplier =>
    slideSpeedMultiplier;

    private CharacterController characterController;
    private StaminaSystem staminaSystem;

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

        standingHeight =
            characterController.height;

        standingCenter =
            characterController.center;

    }

    public bool TryExecute(Vector3 direction)
    {
        if (isExecuting)
            return false;

        float slideCost =
        staminaSystem.MaxStamina *
        staminaCostMultiplier;

        if (!staminaSystem.TryConsume(slideCost))
            return false;

        slideDirection = direction;

        isExecuting = true;
        elapsedTime = 0f;

        ApplySlidingGeometry();

        characterState.SetState(
            LocomotionState.Sliding
        );

        return true;
    }

    private void Update()
    {
        if (!isExecuting)
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