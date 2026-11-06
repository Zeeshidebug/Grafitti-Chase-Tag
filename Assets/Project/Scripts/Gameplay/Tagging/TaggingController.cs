
using System.Collections;
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

    [Header("Spray Effect")]
    [SerializeField] private ParticleSystem sprayParticleSystem;
    [SerializeField] private ParticleSystem sprayParticleSystemRight;

    [Header("Spray Fog Effect")]
    [SerializeField] private ParticleSystem sprayFogLeft;
    [SerializeField] private ParticleSystem sprayFogRight;

    [Header("Spray Animation Timing")]
    [SerializeField] private float animationFrameRate = 24f;
    [SerializeField] private int sprayStartFrame = 6;
    [SerializeField] private int sprayEndFrame = 20;

    private float cooldownTimer;
    private Coroutine sprayCoroutine;

    public bool IsOnCooldown =>
        cooldownTimer > 0f;

    private void Start()
    {
        StopAllSpraySystems(
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

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
        {
            staminaHUDController.NotifyInsufficientStamina();
            return false;
        }

        cooldownTimer = cooldownDuration;

        PerformTagSpray();

        animator.SetTrigger("Tagging");

        PlaySprayEffect();

        return true;
    }

    private void PerformTagSpray()
    {
        float finalSprayRange =
            sprayRange *
            buffNerfController.GetStatMultiplier(
                StatType.TagSprayRange
            );

        float finalSprayAngle =
            sprayAngle *
            buffNerfController.GetStatMultiplier(
                StatType.TagSprayRadius
            );

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

    private void PlaySprayEffect()
    {
        if (sprayCoroutine != null)
        {
            StopCoroutine(sprayCoroutine);
            sprayCoroutine = null;
        }

        StopAllSpraySystems(
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        sprayCoroutine = StartCoroutine(
            SprayEffectRoutine()
        );
    }

    private IEnumerator SprayEffectRoutine()
    {
        float frameRate = Mathf.Max(1f, animationFrameRate);

        int startFrame = Mathf.Max(0, sprayStartFrame);
        int endFrame = Mathf.Max(startFrame, sprayEndFrame);

        // Tunggu sampai frame spray dimulai.
        float sprayStartTime = startFrame / frameRate;

        yield return new WaitForSeconds(sprayStartTime);

        // Aktifkan spray tangan kiri.
        if (sprayParticleSystem != null)
        {
            sprayParticleSystem.Play(true);
        }

        // Aktifkan spray tangan kanan.
        if (sprayParticleSystemRight != null)
        {
            sprayParticleSystemRight.Play(true);
        }

        // Aktifkan fog tangan kiri.
        if (sprayFogLeft != null)
        {
            sprayFogLeft.Play(true);
        }

        // Aktifkan fog tangan kanan.
        if (sprayFogRight != null)
        {
            sprayFogRight.Play(true);
        }

        // Hitung durasi berdasarkan frame animasi.
        float sprayDuration =
            (endFrame - startFrame) / frameRate;

        yield return new WaitForSeconds(sprayDuration);

        // Hentikan emisi keempat efek.
        StopAllSpraySystems(
            ParticleSystemStopBehavior.StopEmitting
        );

        sprayCoroutine = null;
    }

    private void StopAllSpraySystems(
        ParticleSystemStopBehavior stopBehavior)
    {
        // Spray kiri.
        if (sprayParticleSystem != null)
        {
            sprayParticleSystem.Stop(
                true,
                stopBehavior
            );
        }

        // Spray kanan.
        if (sprayParticleSystemRight != null)
        {
            sprayParticleSystemRight.Stop(
                true,
                stopBehavior
            );
        }

        // Fog kiri.
        if (sprayFogLeft != null)
        {
            sprayFogLeft.Stop(
                true,
                stopBehavior
            );
        }

        // Fog kanan.
        if (sprayFogRight != null)
        {
            sprayFogRight.Stop(
                true,
                stopBehavior
            );
        }
    }

    private void OnDisable()
    {
        if (sprayCoroutine != null)
        {
            StopCoroutine(sprayCoroutine);
            sprayCoroutine = null;
        }

        StopAllSpraySystems(
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }
}
