using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine;

public class RoundTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundOutcomeVFX roundOutcomeVFX;
    [SerializeField] private RoundResultPanelUI roundResultPanelUI;
    [SerializeField] private GameController gameController;

    [Header("Wipe")]
    [SerializeField] private RectTransform wipeImage;
    [SerializeField] private Vector2 wipeOpenPosition;
    [SerializeField] private Vector2 wipeClosedPosition;
    [SerializeField] private float wipeDuration = 0.5f;

    [Header("Round Text")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private float roundTextDuration = 0.35f;
    [SerializeField] private float roundTextHoldDuration = 0.6f;

    [Header("Start Text")]
    [SerializeField] private TMP_Text startText;
    [SerializeField] private float startTextDuration = 0.4f;

    [Header("Result Panel")]
    [SerializeField] private float resultDisplayDuration = 1.5f;

    private Coroutine transitionRoutine;
    private bool transitionInProgress;

    private Vector3 roundTextOriginalScale;
    private Vector3 startTextOriginalScale;

    private Color roundTextOriginalColor;
    private Color startTextOriginalColor;

    private void Awake()
    {
        roundTextOriginalScale = roundText.rectTransform.localScale;
        startTextOriginalScale = startText.rectTransform.localScale;

        roundTextOriginalColor = roundText.color;
        startTextOriginalColor = startText.color;

        ResetAnnouncementTexts();

        wipeImage.anchoredPosition = wipeOpenPosition;
        roundResultPanelUI.HidePanel();
    }

    public void PlayRoundTransition(RoundWinner winner)
    {
        if (transitionInProgress)
            return;

        transitionInProgress = true;

        transitionRoutine = StartCoroutine(
            PlayTransitionRoutine(winner)
        );
    }

    private IEnumerator PlayTransitionRoutine(RoundWinner winner)
    {
        // 1. Tutup layar dengan wipe.
        yield return AnimateWipe(
            wipeOpenPosition,
            wipeClosedPosition
        );

        // 2. Outcome sudah selesai ditampilkan.
        roundOutcomeVFX.HideOutcome();

        // 3. Proses skor tepat satu kali.
        bool matchFinished =
            gameController.ProcessRoundResult(winner);

        // 4. Tampilkan hasil ronde saat layar tertutup.
        roundResultPanelUI.ShowRoundResult(
            gameController.CurrentRound,
            gameController.PlayerScore,
            gameController.AIScore,
            gameController.GetRoundWinnerMessage(winner)
        );

        yield return WaitUnscaled(resultDisplayDuration);

        roundResultPanelUI.HidePanel();

        // 5. Jika match selesai, jangan mulai ronde baru.
        if (matchFinished)
        {
            SceneManager.LoadScene("Endscreen");
            yield break;
        }

        // 6. Buka layar.
        yield return AnimateWipe(
            wipeClosedPosition,
            wipeOpenPosition
        );

        // 7. Tampilkan ROUND berikutnya.
        roundText.text =
            $"ROUND {gameController.NextRoundNumber}";

        yield return AnimateTextIn(
            roundText,
            roundTextOriginalScale,
            roundTextOriginalColor,
            roundTextDuration
        );

        yield return WaitUnscaled(roundTextHoldDuration);

        // 8. Tampilkan START!
        startText.text = "START!";

        yield return AnimateTextIn(
            startText,
            startTextOriginalScale,
            startTextOriginalColor,
            startTextDuration
        );

        // 9. Gameplay baru dimulai setelah START! selesai.
        ResetAnnouncementTexts();

        transitionInProgress = false;
        transitionRoutine = null;

        gameController.OnRoundFinished();
    }

    private IEnumerator AnimateWipe(
        Vector2 from,
        Vector2 to)
    {
        float elapsed = 0f;

        wipeImage.anchoredPosition = from;

        while (elapsed < wipeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / wipeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            wipeImage.anchoredPosition =
                Vector2.LerpUnclamped(from, to, t);

            yield return null;
        }

        wipeImage.anchoredPosition = to;
    }

    private IEnumerator AnimateTextIn(
        TMP_Text target,
        Vector3 originalScale,
        Color originalColor,
        float duration)
    {
        float elapsed = 0f;

        Vector3 startScale = originalScale * 0.35f;

        target.rectTransform.localScale = startScale;

        Color color = originalColor;
        color.a = 0f;
        target.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            target.rectTransform.localScale =
                Vector3.LerpUnclamped(startScale, originalScale, easedT);

            color.a = Mathf.Lerp(0f, originalColor.a, t);
            target.color = color;

            yield return null;
        }

        target.rectTransform.localScale = originalScale;
        target.color = originalColor;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ResetAnnouncementTexts()
    {
        roundText.rectTransform.localScale = roundTextOriginalScale;
        startText.rectTransform.localScale = startTextOriginalScale;

        Color roundColor = roundTextOriginalColor;
        roundColor.a = 0f;
        roundText.color = roundColor;

        Color startColor = startTextOriginalColor;
        startColor.a = 0f;
        startText.color = startColor;
    }
}
