using System.Collections;
using TMPro;
using UnityEngine;

public sealed class RoundEntryPresentation : MonoBehaviour
{
    [SerializeField] private RoleController playerRole;
    [SerializeField] private RoundIrisOverlay iris;
    [SerializeField] private CanvasGroup roleGroup;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text currentRole;
    [SerializeField] private TMP_Text incomingRole;
    [SerializeField] private TMP_Text instruction;
    [SerializeField] private float headingFade = 0.28f;
    [SerializeField] private float headingHold = 0.22f;
    [SerializeField] private float confirmHold = 0.65f;
    [SerializeField] private float revealDuration = 0.6f;
    public string Phase { get; private set; } = "Covered";
    public bool IsCovered => iris.Reveal <= 0f;

    private void Awake()
    {
        iris.SetReveal(0f);
        roleGroup.alpha = 0f;
        currentRole.text = incomingRole.text = instruction.text = "";
    }

    public IEnumerator ShowAssignment(bool firstRound)
    {
        // Later rounds are revealed by the result wipe, not this first-entry iris.
        iris.SetReveal(firstRound ? 0f : 1f);
        Phase = "Heading";
        heading.text = "Your Role is:";
        currentRole.text = incomingRole.text = instruction.text = "";
        roleGroup.alpha = 0f;
        yield return Fade(0f, 1f, headingFade);
        yield return Wait(headingHold);
        Phase = "Rolling";
        CharacterRole actual = playerRole.CurrentRole;
        const int steps = 8;
        const float travel = 145f;
        for (int i = 0; i < steps; i++)
        {
            bool finalParity = (steps - 1 - i) % 2 == 0;
            CharacterRole candidate = finalParity ? actual :
                (actual == CharacterRole.Chaser ? CharacterRole.Evader : CharacterRole.Chaser);
            incomingRole.text = candidate == CharacterRole.Chaser ? "CHASER" : "EVADER";
            incomingRole.color = RoleColor(candidate);
            float duration = Mathf.Lerp(0.045f, 0.19f, Mathf.Pow(i / (float)(steps - 1), 2f));
            float elapsed = 0f;
            while (elapsed < duration || GameplayPauseController.IsPaused)
            {
                elapsed += GameplayPauseController.UnpausedUnscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                currentRole.rectTransform.anchoredPosition = Vector2.down * (travel * t);
                incomingRole.rectTransform.anchoredPosition = Vector2.up * (travel * (1f - t));
                yield return null;
            }
            currentRole.text = incomingRole.text;
            currentRole.color = incomingRole.color;
            currentRole.rectTransform.anchoredPosition = Vector2.zero;
            incomingRole.text = "";
        }
        Phase = "Confirmed";
        instruction.text = actual == CharacterRole.Chaser ? "TAG THE ENEMY" : "STAY UNTAGGED";
        float settle = 0f;
        while (settle < 0.18f || GameplayPauseController.IsPaused)
        {
            settle += GameplayPauseController.UnpausedUnscaledDeltaTime;
            float t = Mathf.Clamp01(settle / 0.18f);
            currentRole.rectTransform.localScale = Vector3.one * (1f + 0.09f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        currentRole.rectTransform.localScale = Vector3.one;
        yield return Wait(confirmHold);
        yield return Fade(1f, 0f, 0.18f);
        if (firstRound)
        {
            Phase = "Reveal";
            float elapsed = 0f;
            while (elapsed < revealDuration || GameplayPauseController.IsPaused)
            {
                elapsed += GameplayPauseController.UnpausedUnscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / revealDuration);
                iris.SetReveal(t * t * (3f - 2f * t));
                yield return null;
            }
        }
        iris.SetReveal(1f);
        Phase = "Complete";
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration || GameplayPauseController.IsPaused)
        {
            elapsed += GameplayPauseController.UnpausedUnscaledDeltaTime;
            roleGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        roleGroup.alpha = to;
    }

    private static Color RoleColor(CharacterRole role) => role == CharacterRole.Chaser
        ? new Color32(255, 75, 99, 255) : new Color32(0, 226, 244, 255);

    private static IEnumerator Wait(float duration)
    {
        for (float elapsed = 0f; elapsed < duration || GameplayPauseController.IsPaused; elapsed += GameplayPauseController.UnpausedUnscaledDeltaTime)
            yield return null;
    }
}
