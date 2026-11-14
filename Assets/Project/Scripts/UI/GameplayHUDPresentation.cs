using System.Collections;
using UnityEngine;

public sealed class GameplayHUDPresentation : MonoBehaviour
{
    [SerializeField] private RoundController roundController;
    [SerializeField] private RectTransform leftBar;
    [SerializeField] private RectTransform rightBar;
    [SerializeField] private CanvasGroup roleGroup;
    [SerializeField] private CanvasGroup trackingGroup;
    [SerializeField] private RectTransform timer;
    [SerializeField] private ParticleSystem leftBurst;
    [SerializeField] private ParticleSystem rightBurst;
    [SerializeField, Min(0.1f)] private float entranceDuration = 0.48f;
    [SerializeField, Min(0.1f)] private float exitDuration = 0.32f;
    private Vector2 leftHome, rightHome;
    private RoundState previousState;
    private Coroutine motion;
    private bool visible;
    private float timeoutElapsed = 1f;

    private void Awake()
    {
        leftHome = leftBar.anchoredPosition;
        rightHome = rightBar.anchoredPosition;
        roleGroup.alpha = trackingGroup.alpha = 0f;
        SetPose(0f, 1f, 1f);
    }

    private void Update()
    {
        var state = roundController.CurrentState;
        if (state == RoundState.Playing && previousState != RoundState.Playing)
        {
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Enter());
        }
        if (state == RoundState.Finished && previousState == RoundState.Playing &&
            roundController.RoundWinner == RoundWinner.Evader)
            timeoutElapsed = 0f;
        previousState = state;
        if (timeoutElapsed < 0.5f)
        {
            timeoutElapsed += GameplayPauseController.UnpausedUnscaledDeltaTime;
            float t = Mathf.Clamp01(timeoutElapsed / 0.5f);
            timer.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.18f);
            timer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 6f * Mathf.PI) * 4f * (1f - t));
        }
    }

    private float Travel => ((RectTransform)leftBar.parent).rect.width * 0.5f + leftBar.rect.width;

    private void SetPose(float progress, float scaleX, float scaleY)
    {
        leftBar.anchoredPosition = leftHome + Vector2.left * Travel * (1f - progress);
        rightBar.anchoredPosition = rightHome + Vector2.right * Travel * (1f - progress);
        leftBar.localScale = rightBar.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private IEnumerator Enter()
    {
        visible = true;
        roleGroup.alpha = trackingGroup.alpha = 1f;
        float elapsed = 0f;
        bool burst = false;
        while (elapsed < entranceDuration)
        {
            elapsed += GameplayPauseController.UnpausedUnscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float tail = (1f - t) * (1f - t) * (1f - 1.7f * t);
            float stretch = Mathf.Sin(t * Mathf.PI * 2f) * 0.10f;
            SetPose(1f - tail, 1f + stretch, 1f - stretch);
            if (!burst && t > 0.7f)
            {
                UIParticleCanvasRenderer.PlayBurst(leftBurst);
                UIParticleCanvasRenderer.PlayBurst(rightBurst);
                burst = true;
            }
            yield return null;
        }
        SetPose(1f, 1f, 1f);
        motion = null;
    }

    public IEnumerator HideForResult()
    {
        if (!visible) yield break;
        if (motion != null) StopCoroutine(motion);
        motion = null;
        Vector2 leftStart = leftBar.anchoredPosition, rightStart = rightBar.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < exitDuration)
        {
            elapsed += GameplayPauseController.UnpausedUnscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);
            float eased = t * t * t;
            leftBar.anchoredPosition = Vector2.LerpUnclamped(leftStart, leftHome + Vector2.left * Travel, eased);
            rightBar.anchoredPosition = Vector2.LerpUnclamped(rightStart, rightHome + Vector2.right * Travel, eased);
            leftBar.localScale = rightBar.localScale = new Vector3(1f + 0.12f * t, 1f - 0.12f * t, 1f);
            trackingGroup.alpha = 1f - t;
            yield return null;
        }
        roleGroup.alpha = trackingGroup.alpha = 0f;
        visible = false;
    }
}
