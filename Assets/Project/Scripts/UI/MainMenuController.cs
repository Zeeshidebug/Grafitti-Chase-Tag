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

    [Header("Controls")]
    [SerializeField] private MenuButtonFeedback play;
    [SerializeField] private MenuButtonFeedback credits;
    [SerializeField] private MenuButtonFeedback back;
    [SerializeField] private MenuButtonFeedback exit;

    public bool IsTransitioning { get; private set; }

    private enum Destination { Gameplay, Credits, Home, Exit }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current.SetSelectedGameObject(play.gameObject);
    }

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
