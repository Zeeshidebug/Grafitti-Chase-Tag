
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaminaHUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private Image staminaBar;
    [SerializeField] private RectTransform staminaBarTransform;
    [SerializeField] private TMP_Text warningText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;

    [Header("Feedback Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 8f;
    [SerializeField] private float warningDuration = 1.2f;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float exhaustedBlinkInterval = 0.4f;
    [SerializeField] private float popupRiseDistance = 45f;
    [SerializeField] private float popupHorizontalOffset = 18f;

    private Vector2 originalBarPosition;
    private Coroutine lowStaminaRoutine;
    private Coroutine exhaustedRoutine;
    private RectTransform warningTextTransform;
    private Vector2 originalWarningPosition;

    private bool wasExhausted;

    private void Awake()
    {
        originalBarPosition = staminaBarTransform.anchoredPosition;
        warningTextTransform = warningText.rectTransform;
        originalWarningPosition = warningTextTransform.anchoredPosition;

        staminaBar.fillAmount = 1f;
        staminaBar.color = normalColor;

        warningText.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateStaminaBar();
        HandleExhaustedState();
    }

    private void UpdateStaminaBar()
    {
        float staminaRatio =
            staminaSystem.CurrentStamina / staminaSystem.MaxStamina;

        staminaBar.fillAmount = Mathf.Clamp01(staminaRatio);
    }

    private void HandleExhaustedState()
    {
        bool isExhausted = staminaSystem.IsExhausted;

        if (isExhausted == wasExhausted)
            return;

        wasExhausted = isExhausted;

        if (isExhausted)
        {
            if (lowStaminaRoutine != null)
            {
                StopCoroutine(lowStaminaRoutine);
                lowStaminaRoutine = null;
            }

            StopLowStaminaFeedback();
            exhaustedRoutine = StartCoroutine(ExhaustedFeedback());
        }
        else
        {
            if (exhaustedRoutine != null)
            {
                StopCoroutine(exhaustedRoutine);
                exhaustedRoutine = null;
            }

            staminaBar.color = normalColor;
            warningText.gameObject.SetActive(false);
        }
    }

    public void NotifyInsufficientStamina()
    {
        if (staminaSystem.IsExhausted)
            return;

        if (lowStaminaRoutine != null)
            StopCoroutine(lowStaminaRoutine);

        lowStaminaRoutine = StartCoroutine(LowStaminaFeedback());
    }


    private IEnumerator LowStaminaFeedback()
    {
        warningText.text = "Low Stamina!";

        // Reset visual state so previous exhausted blinking
        // cannot affect the next warning.
        warningText.color = Color.white;
        warningTextTransform.anchoredPosition = originalWarningPosition;
        warningText.gameObject.SetActive(true);

        // Random horizontal direction and distance for this popup.
        float randomOffset = Random.Range(
            -popupHorizontalOffset,
            popupHorizontalOffset
        );

        Vector2 startPosition = originalWarningPosition;

        Vector2 targetPosition = startPosition + new Vector2(
            randomOffset,
            popupRiseDistance
        );

        float elapsed = 0f;
        float flashTimer = 0f;
        bool red = false;

        while (elapsed < warningDuration &&
               !staminaSystem.IsExhausted)
        {
            elapsed += Time.deltaTime;
            flashTimer += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / warningDuration);

            // Ease-out movement: fast at first, slower near the end.
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            warningTextTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                easedProgress
            );

            // Flash the stamina bar.
            if (flashTimer >= flashDuration)
            {
                flashTimer = 0f;
                red = !red;
                staminaBar.color = red ? warningColor : normalColor;
            }

            // Shake the stamina bar briefly.
            float offsetX = 0f;

            if (elapsed < shakeDuration)
            {
                float shakeProgress = elapsed / shakeDuration;

                offsetX = Random.Range(-1f, 1f) *
                          shakeMagnitude *
                          (1f - shakeProgress);
            }

            staminaBarTransform.anchoredPosition =
                originalBarPosition + Vector2.right * offsetX;

            yield return null;
        }

        StopLowStaminaFeedback();
        lowStaminaRoutine = null;

        if (!staminaSystem.IsExhausted)
            warningText.gameObject.SetActive(false);
    }


    private IEnumerator ExhaustedFeedback()
    {
        warningText.text = "Empty Stamina!";
        warningText.gameObject.SetActive(true);

        bool red = false;

        while (staminaSystem.IsExhausted)
        {
            red = !red;

            staminaBar.color = red ? warningColor : normalColor;
            warningText.color = red ? warningColor : Color.white;

            yield return new WaitForSeconds(exhaustedBlinkInterval);
        }

        staminaBar.color = normalColor;
        warningText.color = Color.white;
        warningTextTransform.anchoredPosition = originalWarningPosition;
        warningText.gameObject.SetActive(false);

        exhaustedRoutine = null;
    }

    private void StopLowStaminaFeedback()
    {
        staminaBarTransform.anchoredPosition = originalBarPosition;
        staminaBar.color = normalColor;

        warningText.color = Color.white;
        warningTextTransform.anchoredPosition = originalWarningPosition;
    }

    private void OnDisable()
    {
        if (lowStaminaRoutine != null)
            StopCoroutine(lowStaminaRoutine);

        if (exhaustedRoutine != null)
            StopCoroutine(exhaustedRoutine);

        staminaBarTransform.anchoredPosition = originalBarPosition;
        staminaBar.color = normalColor;
        warningText.color = Color.white;
    }
}
