using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class EndScreenPanelUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text enemyScoreText;
    [SerializeField] private EndscreenPresentation presentation;
    [Header("Scene exit")]
    [SerializeField] private MenuInkWipe exitWipe;
    [Min(0.01f)] [SerializeField] private float exitWipeDuration = 0.35f;
    private bool leaving;
    public bool IsLeaving => leaving;

    private void Awake()
    {
        if (exitWipe == null) return;
        exitWipe.SetCoverage(0f);
        exitWipe.raycastTarget = false;
    }

    private void Start()
    {
        if (presentation == null) ShowEndscreen();
    }

    public void ShowEndscreen()
    {
        if (presentation != null) return;
        int playerScore = MatchResultData.PlayerScore;
        int enemyScore = MatchResultData.AIScore;

        playerScoreText.text = playerScore.ToString();
        enemyScoreText.text = enemyScore.ToString();

        if (playerScore > enemyScore)
        {
            resultText.text = "PLAYER WINS!";
        }
        else if (enemyScore > playerScore)
        {
            resultText.text = "ENEMY WINS!";
        }
        else
        {
            resultText.text = "DRAW!";
        }

        gameObject.SetActive(true);
    }

    public void PlayAgain()
    {
        Leave("Gameplay");
    }

    public void ReturnToMainMenu()
    {
        Leave("MainMenu");
    }

    private void Leave(string scene)
    {
        if (leaving || (presentation != null && !presentation.Ready)) return;
        leaving = true;
        if (presentation != null) presentation.PrepareForExit();
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = false;
            EventSystem.current.SetSelectedGameObject(null);
        }
        if (exitWipe != null) exitWipe.raycastTarget = true;
        StartCoroutine(Load(scene));
    }

    private IEnumerator Load(string scene)
    {
        if (exitWipe != null)
        {
            for (float elapsed = 0f; elapsed < exitWipeDuration; elapsed += Time.unscaledDeltaTime)
            {
                exitWipe.SetCoverage(Mathf.SmoothStep(0f, 1f, elapsed / exitWipeDuration));
                yield return null;
            }
            exitWipe.SetCoverage(1f);
            // Render an opaque frame before the destination establishes its cover.
            yield return null;
        }
        else if (presentation != null) yield return presentation.CoverForExit();
        MatchResultData.Reset();
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
    }
}
