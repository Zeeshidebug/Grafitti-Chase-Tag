using UnityEngine;

public class TaggingController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private RoleController roleController;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private PlayerMovementIntent movementIntent;
    [SerializeField] private BuffNerfController buffNerfController;
    [SerializeField] private Animator animator;
    [SerializeField] private StaminaHUDController staminaHUDController;

    [Header("Tagging")]
    [SerializeField] private float staminaCostMultiplier = 0.5f;
    [SerializeField] private float cooldownDuration = 5f;
    [SerializeField] private float sprayRange = 3f;
    [SerializeField] private float sprayAngle = 45f;
    [SerializeField] private LayerMask targetLayer;

    [Tooltip("Assign the opposing receiver to request a tag automatically within Spray Range.")]
    [SerializeField] private TagReceiver proximityTarget;

    private TagReceiver ownReceiver;
    private RoleController proximityTargetRole;
    private float cooldownTimer;

    private float EffectiveRange => sprayRange *
        buffNerfController.GetStatMultiplier(StatType.TagSprayRange);

    private void Awake()
    {
        ownReceiver = GetComponent<TagReceiver>();
        if (proximityTarget != null)
            proximityTargetRole = proximityTarget.GetComponent<RoleController>();
    }

    public bool IsOnCooldown =>
        cooldownTimer > 0f;

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        TryTag();
    }

    public bool TryTag()
    {
        if (GameplayPauseController.IsPaused || !roleController.IsChaser || IsOnCooldown ||
            (ownReceiver != null && !ownReceiver.RoundIsPlaying))
            return false;

        if (proximityTarget != null)
        {
            if (!proximityTarget.isActiveAndEnabled || proximityTargetRole == null || !proximityTargetRole.IsEvader)
                return false;

            float range = EffectiveRange;
            if ((targetLayer.value & (1 << proximityTarget.gameObject.layer)) == 0 ||
                (proximityTarget.transform.position - transform.position).sqrMagnitude > range * range)
                return false;
        }
        else if (movementIntent == null || !movementIntent.Tagging)
        {
            return false;
        }

        float staminaCost =
            staminaSystem.MaxStamina *
            staminaCostMultiplier;

        if (!staminaSystem.TryConsume(staminaCost))
        {
            staminaHUDController?.NotifyInsufficientStamina();
            return false;
        }

        cooldownTimer = cooldownDuration;

        if (proximityTarget != null)
            proximityTarget.ReceiveTag();
        else
            PerformTagSpray();

        animator.SetTrigger("Tagging");

        return true;
    }

    private void PerformTagSpray()
    {
        float finalSprayRange = EffectiveRange;

        float finalSprayAngle =
        sprayAngle *
        buffNerfController.GetStatMultiplier(StatType.TagSprayRadius);

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            finalSprayRange,
            targetLayer
        );

        foreach (Collider hit in hits)
        {
            RoleController targetRole =
                hit.GetComponentInParent<RoleController>();

            if (targetRole == null)
                continue;

            if (!targetRole.IsEvader)
                continue;

            Vector3 directionToTarget =
                hit.transform.position - transform.position;

            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude <= 0f)
                continue;

            float angle = Vector3.Angle(
                transform.forward,
                directionToTarget
            );

            if (angle > finalSprayAngle * 0.5f)
                continue;

            TagReceiver receiver =
                hit.GetComponentInParent<TagReceiver>();

            if (receiver == null)
                continue;

            receiver.ReceiveTag();

            break;
        }
    }
}
