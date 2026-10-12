using UnityEngine;

public class TaggingController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private RoleController roleController;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private PlayerMovementIntent movementIntent;
    [SerializeField] private BuffNerfController buffNerfController;
    [SerializeField] private Animator animator;

    [Header("Tagging")]
    [SerializeField] private float staminaCostMultiplier = 0.5f;
    [SerializeField] private float cooldownDuration = 5f;
    [SerializeField] private float sprayRange = 3f;
    [SerializeField] private float sprayAngle = 45f;
    [SerializeField] private LayerMask targetLayer;

    private float cooldownTimer;

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
        if (!movementIntent.Tagging)
            return false;

        if (!roleController.IsChaser)
            return false;

        if (IsOnCooldown)
            return false;

        float staminaCost =
            staminaSystem.MaxStamina *
            staminaCostMultiplier;

        if (!staminaSystem.TryConsume(staminaCost))
            return false;

        cooldownTimer = cooldownDuration;

        PerformTagSpray();

        animator.SetTrigger("Tagging");

        return true;
    }

    private void PerformTagSpray()
    {
        float finalSprayRange =
        sprayRange *
        buffNerfController.GetStatMultiplier(StatType.TagSprayRange);

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