using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private MenuInkWipe wipe;

    [Header("Scene entry")]
    [SerializeField] private RoundIrisOverlay entryIris;
    [Tooltip("Disable when an intro will call RevealMenu after it finishes.")]
    [SerializeField] private bool autoRevealOnStart = true;
    [Min(0f)] [SerializeField] private float entryHoldDuration = 0.12f;
    [Min(0.01f)] [SerializeField] private float entryRevealDuration = 0.65f;

    [Header("Controls")]
    [SerializeField] private MenuButtonFeedback play;
    [SerializeField] private MenuButtonFeedback credits;
    [SerializeField] private MenuButtonFeedback back;
    [SerializeField] private MenuButtonFeedback exit;

    public bool IsTransitioning { get; private set; }
    public string EntryPhase { get; private set; } = "Covered";
    private bool revealRequested;

    private enum Destination { Gameplay, Credits, Home, Exit }

    private void Awake()
    {
        wipe.SetCoverage(0f);
        wipe.raycastTarget = false;
        IsTransitioning = entryIris != null;
        if (entryIris != null) entryIris.SetReveal(0f);
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var events = EventSystem.current;
        if (entryIris != null)
        {
            if (events != null)
            {
                events.sendNavigationEvents = false;
                events.SetSelectedGameObject(null);
            }
            // Always present a covered frame, even when launched directly.
            yield return null;
            if (autoRevealOnStart) revealRequested = true;
            while (!revealRequested) yield return null;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, entryHoldDuration));
            EntryPhase = "Opening";
            for (float elapsed = 0f; elapsed < entryRevealDuration; elapsed += Time.unscaledDeltaTime)
            {
                entryIris.SetReveal(Mathf.SmoothStep(0f, 1f, elapsed / entryRevealDuration));
                yield return null;
            }
            entryIris.SetReveal(1f);
        }
        EntryPhase = "Ready";
        IsTransitioning = false;
        if (events != null)
        {
            events.sendNavigationEvents = true;
            events.SetSelectedGameObject(play.gameObject);
        }
    }

    // Safe before Start or when called repeatedly by a future intro/Timeline.
    public void RevealMenu() => revealRequested = true;

    public void PlayGame() => Go(Destination.Gameplay, play);
    public void OpenCredits() => Go(Destination.Credits, credits);
    public void ExitGame() => Go(Destination.Exit, exit);
    public void CloseCredits()
    {
        if (creditsPanel.activeSelf) Go(Destination.Home, back);
    }

    private void Go(Destination destination, MenuButtonFeedback button)
    {
        if (!IsTransitioning && button.isActiveAndEnabled)
            StartCoroutine(Transition(destination, button));
    }

    private IEnumerator Transition(Destination destination, MenuButtonFeedback button)
    {
        IsTransitioning = true;
        var events = EventSystem.current;
        events.sendNavigationEvents = false;
        wipe.raycastTarget = true;
        button.Stamp();
        yield return new WaitForSecondsRealtime(0.1f);
        yield return Sweep(0f, 1f, 0.30f);
        yield return null; // Present a fully covered frame before Unity activates another scene.

        if (destination == Destination.Gameplay)
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield break;
        }
        if (destination == Destination.Exit)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            // Some hosts keep the player running after a quit request.
            yield return null;
        }
        else
        {
            bool showCredits = destination == Destination.Credits;
            events.SetSelectedGameObject(null);
            mainMenuPanel.SetActive(!showCredits);
            creditsPanel.SetActive(showCredits);
            events.SetSelectedGameObject((showCredits ? back : credits).gameObject);
        }
        yield return Sweep(1f, 0f, 0.26f);
        wipe.raycastTarget = false;
        events.sendNavigationEvents = true;
        IsTransitioning = false;
    }

    private IEnumerator Sweep(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            wipe.SetCoverage(Mathf.SmoothStep(from, to, elapsed / duration));
            yield return null;
        }
        wipe.SetCoverage(to);
    }
}
