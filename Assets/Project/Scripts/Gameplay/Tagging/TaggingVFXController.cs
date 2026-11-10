
using System.Collections;
using UnityEngine;

public class TaggingVFXController : MonoBehaviour
{
    [Header("Normal Spray")]
    [SerializeField] private ParticleSystem sprayNormalLeft;
    [SerializeField] private ParticleSystem sprayNormalRight;
    [SerializeField] private ParticleSystem fogNormalLeft;
    [SerializeField] private ParticleSystem fogNormalRight;

    [Header("Buff Spray")]
    [SerializeField] private ParticleSystem sprayBuffLeft;
    [SerializeField] private ParticleSystem sprayBuffRight;
    [SerializeField] private ParticleSystem fogBuffLeft;
    [SerializeField] private ParticleSystem fogBuffRight;

    [Header("Nerf Spray")]
    [SerializeField] private ParticleSystem sprayNerfLeft;
    [SerializeField] private ParticleSystem sprayNerfRight;
    [SerializeField] private ParticleSystem fogNerfLeft;
    [SerializeField] private ParticleSystem fogNerfRight;

    [Header("Spray Animation Timing")]
    [SerializeField] private float animationFrameRate = 24f;
    [SerializeField] private int sprayStartFrame = 6;
    [SerializeField] private int sprayEndFrame = 20;

    private Coroutine sprayCoroutine;

    private void Start()
    {
        StopAllSpraySystems(
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    public void PlaySprayEffect(float rangeMultiplier)
    {
        // Hentikan efek sebelumnya jika masih berjalan.
        if (sprayCoroutine != null)
        {
            StopCoroutine(sprayCoroutine);
            sprayCoroutine = null;
        }

        StopAllSpraySystems(
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        // Pilih variasi berdasarkan multiplier.
        ParticleSystem sprayLeft;
        ParticleSystem sprayRight;
        ParticleSystem fogLeft;
        ParticleSystem fogRight;

        if (rangeMultiplier > 1.001f)
        {
            sprayLeft = sprayBuffLeft;
            sprayRight = sprayBuffRight;
            fogLeft = fogBuffLeft;
            fogRight = fogBuffRight;
        }
        else if (rangeMultiplier < 0.999f)
        {
            sprayLeft = sprayNerfLeft;
            sprayRight = sprayNerfRight;
            fogLeft = fogNerfLeft;
            fogRight = fogNerfRight;
        }
        else
        {
            sprayLeft = sprayNormalLeft;
            sprayRight = sprayNormalRight;
            fogLeft = fogNormalLeft;
            fogRight = fogNormalRight;
        }

        sprayCoroutine = StartCoroutine(
            SprayEffectRoutine(
                sprayLeft,
                sprayRight,
                fogLeft,
                fogRight
            )
        );
    }

    private IEnumerator SprayEffectRoutine(
        ParticleSystem sprayLeft,
        ParticleSystem sprayRight,
        ParticleSystem fogLeft,
        ParticleSystem fogRight)
    {
        float frameRate = Mathf.Max(1f, animationFrameRate);

        int startFrame = Mathf.Max(0, sprayStartFrame);
        int endFrame = Mathf.Max(startFrame, sprayEndFrame);

        // Tunggu sampai frame spray dimulai.
        float sprayStartTime = startFrame / frameRate;

        yield return new WaitForSeconds(sprayStartTime);

        // Mainkan spray kiri dan kanan.
        if (sprayLeft != null)
            sprayLeft.Play(false);

        if (sprayRight != null)
            sprayRight.Play(false);

        // Mainkan fog kiri dan kanan.
        if (fogLeft != null)
            fogLeft.Play(false);

        if (fogRight != null)
            fogRight.Play(false);

        // Hitung durasi berdasarkan frame animasi.
        float sprayDuration =
            (endFrame - startFrame) / frameRate;

        yield return new WaitForSeconds(sprayDuration);

        // Hentikan emisi efek yang sedang digunakan.
        StopSelectedSystems(
            sprayLeft,
            sprayRight,
            fogLeft,
            fogRight,
            ParticleSystemStopBehavior.StopEmitting
        );

        sprayCoroutine = null;
    }

    private void StopSelectedSystems(
        ParticleSystem sprayLeft,
        ParticleSystem sprayRight,
        ParticleSystem fogLeft,
        ParticleSystem fogRight,
        ParticleSystemStopBehavior stopBehavior)
    {
        if (sprayLeft != null)
            sprayLeft.Stop(false, stopBehavior);

        if (sprayRight != null)
            sprayRight.Stop(false, stopBehavior);

        if (fogLeft != null)
            fogLeft.Stop(false, stopBehavior);

        if (fogRight != null)
            fogRight.Stop(false, stopBehavior);
    }

    private void StopAllSpraySystems(
        ParticleSystemStopBehavior stopBehavior)
    {
        // Normal
        StopSelectedSystems(
            sprayNormalLeft,
            sprayNormalRight,
            fogNormalLeft,
            fogNormalRight,
            stopBehavior
        );

        // Buff
        StopSelectedSystems(
            sprayBuffLeft,
            sprayBuffRight,
            fogBuffLeft,
            fogBuffRight,
            stopBehavior
        );

        // Nerf
        StopSelectedSystems(
            sprayNerfLeft,
            sprayNerfRight,
            fogNerfLeft,
            fogNerfRight,
            stopBehavior
        );
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
