using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class EndscreenPresentation : MonoBehaviour
{
    [Header("Result artwork")]
    [SerializeField] private RectTransform panel, playerBanner, enemyBanner, controls;
    [SerializeField] private TMP_Text title, winner, playerScore, enemyScore;
    [SerializeField] private EndscreenCurtain curtain;
    [SerializeField] private Image flash;
    [SerializeField] private ParticleSystem scoreParticles;
    [SerializeField] private CanvasGroup controlsGroup;
    [SerializeField] private Button playAgain, mainMenu;
    [SerializeField] private EndscreenShowcase showcase;
    [Header("Final result timing")]
    [Min(.01f)] [SerializeField] private float settleDuration = .5f;
    [Min(.01f)] [SerializeField] private float buildupDuration = 1.65f;
    [Min(.01f)] [SerializeField] private float burstDuration = .36f;
    [Min(.01f)] [SerializeField] private float winnerHold = .45f;
    [Min(.01f)] [SerializeField] private float revealDuration = 1.05f;
    [Header("Direct scene preview (no saved match)")]
    [SerializeField] private bool previewPlayerWins = true;

    public string Phase { get; private set; } = "Covered";
    public bool Ready => Phase == "Showcase";
    public bool PlayerWon { get; private set; }
    public bool IsDraw { get; private set; }
    public int DisplayedPlayerScore => int.TryParse(playerScore.text, out int value) ? value : 0;
    public int DisplayedAIScore => int.TryParse(enemyScore.text, out int value) ? value : 0;
    private int finalPlayer, finalAI;
    private bool playerScored, aiScored;
    private Vector2 playerHome, enemyHome, titleHome, scoreP, scoreE;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        finalPlayer = MatchResultData.HasResult ? MatchResultData.PlayerScore : previewPlayerWins ? 3 : 2;
        finalAI = MatchResultData.HasResult ? MatchResultData.AIScore : previewPlayerWins ? 2 : 3;
        PlayerWon = finalPlayer > finalAI;
        IsDraw = finalPlayer == finalAI;
        int previousPlayer = MatchResultData.HasPresentation ? MatchResultData.PreviousPlayerScore : finalPlayer;
        int previousAI = MatchResultData.HasPresentation ? MatchResultData.PreviousAIScore : finalAI;
        playerScored = finalPlayer > previousPlayer;
        aiScored = finalAI > previousAI;
        playerScore.text = previousPlayer.ToString(); enemyScore.text = previousAI.ToString();
        title.text = $"Round {(MatchResultData.Round > 0 ? MatchResultData.Round : 5)} Concluded!";
        winner.text = IsDraw ? "Draw!" : PlayerWon ? "Player Wins!" : "Enemy Wins!";
        playerHome = new Vector2(-390,135); enemyHome = new Vector2(390,-135);
        playerBanner.anchoredPosition = playerHome; enemyBanner.anchoredPosition = enemyHome;
        playerBanner.localScale = enemyBanner.localScale = title.transform.localScale = Vector3.one;
        titleHome = title.rectTransform.anchoredPosition;
        scoreP = playerScore.rectTransform.anchoredPosition; scoreE = enemyScore.rectTransform.anchoredPosition;
        winner.transform.localScale = Vector3.zero;
        controlsGroup.alpha = 0; controlsGroup.interactable = controlsGroup.blocksRaycasts = false;
        curtain.SetReveal(0,PlayerWon || IsDraw); SetFlash(0);
        panel.gameObject.SetActive(true);
    }

    private IEnumerator Start()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        yield return null; // Match the final fully covered Gameplay frame.
        Phase = "Recenter";
        Vector2 pCenter = new Vector2(-420,0), eCenter = new Vector2(420,0);
        yield return Motion(settleDuration,t =>
        {
            float ease=Mathf.SmoothStep(0,1,t);
            playerBanner.anchoredPosition=Vector2.Lerp(playerHome,pCenter,ease);
            enemyBanner.anchoredPosition=Vector2.Lerp(enemyHome,eCenter,ease);
        });
        Phase = "Buildup";
        yield return Motion(buildupDuration,t =>
        {
            float intensity=t*t;
            SetFlash(.28f*intensity);
            playerBanner.localScale=enemyBanner.localScale=Vector3.one*(1+.095f*intensity);
            Vector2 jitter=new Vector2(Mathf.Sin(t*119),Mathf.Cos(t*157))*7*intensity;
            playerScore.rectTransform.anchoredPosition=scoreP+jitter;
            enemyScore.rectTransform.anchoredPosition=scoreE-jitter;
        });
        Phase = "Burst";
        playerScore.text=finalPlayer.ToString(); enemyScore.text=finalAI.ToString();
        playerScore.rectTransform.anchoredPosition=scoreP; enemyScore.rectTransform.anchoredPosition=scoreE;
        title.text="Game Concluded!";
        var scoring=playerScored?playerScore:aiScored?enemyScore:PlayerWon?playerScore:enemyScore;
        scoreParticles.transform.position=scoring.transform.position;
        UIParticleCanvasRenderer.PlayBurst(scoreParticles);
        yield return Motion(burstDuration,t =>
        {
            SetFlash(.78f*Mathf.Pow(1-t,3));
            title.transform.localScale=Vector3.one*(1+.24f*Mathf.Sin(t*Mathf.PI));
            scoring.transform.localScale=Vector3.one*(1+.42f*Mathf.Sin(t*Mathf.PI));
            playerBanner.localScale=enemyBanner.localScale=Vector3.one*(1+.095f*(1-t));
            winner.transform.localScale=Vector3.one*Back(Mathf.Clamp01(t*1.4f));
        });
        SetFlash(0); scoring.transform.localScale=Vector3.one;
        playerBanner.localScale=enemyBanner.localScale=title.transform.localScale=winner.transform.localScale=Vector3.one;
        Phase="Winner"; yield return Motion(winnerHold,null);
        Phase="Reveal";
        showcase.Begin(PlayerWon || IsDraw, IsDraw);
        float direction=PlayerWon || IsDraw?1:-1;
        float width=panel.rect.width, height=panel.rect.height;
        float sideX=width*.267f*direction;
        float bannerScale=Mathf.Min(.9f,width/1920f*.9f);
        Vector2 titleTarget=new Vector2(sideX,height*.34f);
        Vector2 winnerStart=winner.rectTransform.anchoredPosition;
        Vector2 winnerTarget=new Vector2(sideX,height*.265f);
        var winningBanner=PlayerWon || IsDraw?playerBanner:enemyBanner;
        var losingBanner=PlayerWon || IsDraw?enemyBanner:playerBanner;
        Vector2 winStart=winningBanner.anchoredPosition, loseStart=losingBanner.anchoredPosition;
        Vector2 winEnd=new Vector2(sideX,-height*.015f);
        Vector2 loseEnd=new Vector2(direction*(width*.5f+losingBanner.rect.width),0);
        controls.anchoredPosition=new Vector2(sideX,-height*.315f);
        controls.localScale=Vector3.one*Mathf.Min(1f,width/1920f);
        yield return Motion(revealDuration,t =>
        {
            float ease=Mathf.SmoothStep(0,1,t);
            curtain.SetReveal(ease,direction>0);
            title.rectTransform.anchoredPosition=Vector2.Lerp(titleHome,titleTarget,ease);
            title.transform.localScale=Vector3.one*Mathf.Lerp(1,.82f*bannerScale/.9f,ease);
            winner.rectTransform.anchoredPosition=Vector2.Lerp(winnerStart,winnerTarget,ease);
            winner.transform.localScale=Vector3.one*Mathf.Lerp(1,.8f*bannerScale/.9f,ease);
            winningBanner.anchoredPosition=Vector2.Lerp(winStart,winEnd,ease);
            winningBanner.localScale=Vector3.one*Mathf.Lerp(1,bannerScale,ease);
            losingBanner.anchoredPosition=Vector2.Lerp(loseStart,loseEnd,ease);
            if (IsDraw)
            {
                playerBanner.anchoredPosition=Vector2.Lerp(pCenter,new Vector2(sideX,height*.105f),ease);
                enemyBanner.anchoredPosition=Vector2.Lerp(eCenter,new Vector2(sideX,-height*.145f),ease);
                playerBanner.localScale=enemyBanner.localScale=Vector3.one*Mathf.Lerp(1,bannerScale*.4f,ease);
            }
        });
        if(!IsDraw)losingBanner.gameObject.SetActive(false);
        yield return Motion(.3f,t=>controlsGroup.alpha=t);
        controlsGroup.interactable=controlsGroup.blocksRaycasts=true;
        Phase="Showcase";
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(playAgain.gameObject);
    }

    public IEnumerator CoverForExit()
    {
        Phase="Leaving";
        controlsGroup.interactable=controlsGroup.blocksRaycasts=false;
        float from=curtain.Reveal;
        yield return Motion(.3f,t=>curtain.SetReveal(Mathf.Lerp(from,0,Mathf.SmoothStep(0,1,t)),PlayerWon || IsDraw));
        yield return null;
    }

    private void SetFlash(float alpha) { Color tint=flash.color;tint.a=alpha;flash.color=tint; }
    private static float Back(float t) { float x=t-1;return 1+2.7f*x*x*x+1.7f*x*x; }
    private static IEnumerator Motion(float duration,Action<float> apply)
    {
        float elapsed=0;apply?.Invoke(0);
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;
            apply?.Invoke(Mathf.Clamp01(elapsed/Mathf.Max(duration,.001f)));yield return null;
        }
        apply?.Invoke(1);
    }
}
