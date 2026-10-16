using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RoundOutcomeVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform outcomeTextTransform;
    [SerializeField] private TMP_Text outcomeText;

    [Header("Animation")]
    [SerializeField] private float entranceDuration = 0.35f;
    [SerializeField] private float settleDuration = 0.18f;
    [SerializeField] private float shakeDuration = 0.45f;
    [SerializeField] private float shakeMagnitude = 12f;
    [SerializeField] private float shakeFrequency = 32f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.25f;
    [SerializeField] private float overshootScale = 1.35f;
    [SerializeField] private float finalScale = 0.9f;

    private Vector2 originalPosition;
    private Coroutine animationRoutine;

    private void Awake()
    {
        originalPosition = outcomeTextTransform.anchoredPosition;

        canvasGroup.alpha = 0f;
        outcomeTextTransform.localScale = Vector3.one * startScale;
        gameObject.SetActive(true);
    }

    public void ShowOutcome(
        RoundWinner winner,
        Action onComplete = null)
    {
        if (winner == RoundWinner.None)
            return;

        gameObject.SetActive(true);

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        string message =
            winner == RoundWinner.Chaser
                ? "TAGGED!"
                : "TIME OUT!";

        animationRoutine = StartCoroutine(
            PlayOutcomeRoutine(message, onComplete)
        );
    }

    private IEnumerator PlayOutcomeRoutine(string message, Action onComplete)
    {
        gameObject.SetActive(true);

        outcomeText.text = message;
        outcomeTextTransform.anchoredPosition = originalPosition;
        outcomeTextTransform.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 0f;

        // Entrance: fade in and grow past the target size.
        float elapsed = 0f;

        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float eased = EaseOutCubic(t);

            canvasGroup.alpha = eased;

            float scale = Mathf.Lerp(
                startScale,
                overshootScale,
                eased
            );

            outcomeTextTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        // Settle: shrink slightly after the impact.
        elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / settleDuration);
            float eased = EaseOutCubic(t);

            float scale = Mathf.Lerp(
                overshootScale,
                finalScale,
                eased
            );

            outcomeTextTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        outcomeTextTransform.localScale = Vector3.one * finalScale;

        // Dramatic shake: strong at first, then gradually settles.
        elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / shakeDuration);
            float strength = 1f - t;

            float offsetX =
                Mathf.Sin(elapsed * shakeFrequency) *
                shakeMagnitude * strength;

            float offsetY =
                Mathf.Sin(elapsed * shakeFrequency * 1.37f) *
                shakeMagnitude * 0.35f * strength;

            outcomeTextTransform.anchoredPosition =
                originalPosition + new Vector2(offsetX, offsetY);

            yield return null;
        }

        outcomeTextTransform.anchoredPosition = originalPosition;

        animationRoutine = null;
        onComplete?.Invoke();
    }

    public void HideOutcome()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        canvasGroup.alpha = 0f;
        outcomeTextTransform.anchoredPosition = originalPosition;
        outcomeTextTransform.localScale = Vector3.one * startScale;
        gameObject.SetActive(false);
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    [ContextMenu("Test Tagged")]
    private void TestTagged()
    {
        ShowOutcome(RoundWinner.Chaser);
    }

    [ContextMenu("Test Time Out")]
    private void TestTimeOut()
    {
        ShowOutcome(RoundWinner.Evader);
    }
}
