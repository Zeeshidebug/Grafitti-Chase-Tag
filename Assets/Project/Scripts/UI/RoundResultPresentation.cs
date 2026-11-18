using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoundResultPresentation : MonoBehaviour
{
    [SerializeField] private RoundResultCover cover;
    [SerializeField] private RectTransform panel, playerBanner, enemyBanner;
    [SerializeField] private TMP_Text title, winner, versus, playerScore, enemyScore;
    [SerializeField] private Image flash;
    [SerializeField] private ParticleSystem clashParticles, scoreParticles;
    [Header("Motion timing")]
    [SerializeField] private float circleDelay=.14f, circleDuration=.5f, bannerEntry=.32f, recoilDuration=.28f;
    [SerializeField] private float concludedHold=.55f, buildupDuration=.7f, scoreHold=.85f, splitDuration=.6f;
    public string Phase { get; private set; } = "Idle";
    public bool FullyCovered => cover.Circle >= 1f && cover.Opening <= 0f;
    private Coroutine impact;
    private Vector2 playerHome = new Vector2(-390f,135f), enemyHome = new Vector2(390f,-135f);
    private Vector2 scoreP, scoreE;
    private void Awake()
    {
        scoreP=playerScore.rectTransform.anchoredPosition; scoreE=enemyScore.rectTransform.anchoredPosition;
        cover.Set(0); Alpha(flash,0); panel.gameObject.SetActive(false);
    }
    public void BeginImpact()
    {
        if(impact!=null) StopCoroutine(impact);
        impact=StartCoroutine(Impact());
    }
    private IEnumerator Impact()
    {
        Phase="Impact";
        yield return Motion(circleDelay+circleDuration,t=>
        {
            float seconds=t*(circleDelay+circleDuration);
            float disk=Mathf.Clamp01((seconds-circleDelay)/circleDuration);
            cover.Set(disk*disk,0,Mathf.Clamp01(seconds/.55f),1f-Mathf.Clamp01(seconds/.6f));
        });
        cover.Set(1); impact=null; Phase="Covered";
    }
    public IEnumerator CloseCover()
    {
        if(impact==null && !FullyCovered) BeginImpact();
        while(impact!=null || GameplayPauseController.IsPaused) yield return null;
    }
    public IEnumerator ShowScore(int round, int oldPlayer, int oldEnemy, string message, Func<Vector2Int> commitScore)
    {
        yield return ShowConcluded(round, oldPlayer, oldEnemy, message);
        yield return ShowScoreBurst(oldPlayer, commitScore);
    }
    public IEnumerator ShowConcluded(int round, int oldPlayer, int oldEnemy, string message)
    {
        panel.gameObject.SetActive(true);
        playerScore.text=oldPlayer.ToString(); enemyScore.text=oldEnemy.ToString();
        title.text=$"Round {round} Concluded!";winner.text=message;versus.text="VS";
        title.transform.localScale=winner.transform.localScale=versus.transform.localScale=Vector3.zero;
        playerBanner.localScale=enemyBanner.localScale=Vector3.one;
        playerScore.rectTransform.anchoredPosition=scoreP; enemyScore.rectTransform.anchoredPosition=scoreE;
        float off=panel.rect.width*.5f+playerBanner.rect.width;
        playerBanner.anchoredPosition=new Vector2(-off,0);enemyBanner.anchoredPosition=new Vector2(off,0);
        Vector2 pClash=new Vector2(-280,0),eClash=new Vector2(280,0);
        Phase="BannerEntry";
        yield return Motion(.16f,null);
        yield return Motion(bannerEntry,t=>{float e=1-Mathf.Pow(1-t,3);playerBanner.anchoredPosition=Vector2.Lerp(new Vector2(-off,0),pClash,e);enemyBanner.anchoredPosition=Vector2.Lerp(new Vector2(off,0),eClash,e);});
        Phase="Clash"; UIParticleCanvasRenderer.PlayBurst(clashParticles);
        yield return Motion(recoilDuration,t=>
        {
            float e=Back(t); playerBanner.anchoredPosition=Vector2.LerpUnclamped(pClash,playerHome,e);enemyBanner.anchoredPosition=Vector2.LerpUnclamped(eClash,enemyHome,e);
            playerBanner.localScale=enemyBanner.localScale=new Vector3(1+.08f*(1-t),1-.06f*(1-t),1);
            Alpha(flash,.55f*Mathf.Pow(1-t,3));
        });
        playerBanner.localScale=enemyBanner.localScale=Vector3.one;Alpha(flash,0);
        yield return Pop(title,.2f); Phase="Concluded";yield return Motion(concludedHold,null);
    }
    private IEnumerator ShowScoreBurst(int oldPlayer, Func<Vector2Int> commitScore)
    {
        Phase="Buildup";
        yield return Motion(buildupDuration,t=>
        {
            Alpha(flash,.22f*t*t);playerBanner.localScale=enemyBanner.localScale=Vector3.one*(1+.075f*t*t);
            Vector2 jitter=new Vector2(Mathf.Sin(t*91),Mathf.Cos(t*127))*5*t;
            playerScore.rectTransform.anchoredPosition=scoreP+jitter;enemyScore.rectTransform.anchoredPosition=scoreE-jitter;
        });
        Phase="Burst";
        Vector2Int scores=commitScore();
        playerScore.text=scores.x.ToString();enemyScore.text=scores.y.ToString();
        playerScore.rectTransform.anchoredPosition=scoreP;enemyScore.rectTransform.anchoredPosition=scoreE;
        TMP_Text scoring=scores.x>oldPlayer?playerScore:enemyScore;
        scoreParticles.transform.position=scoring.transform.position;UIParticleCanvasRenderer.PlayBurst(scoreParticles);
        yield return Motion(.34f,t=>
        {
            float size=1+.075f*(1-Back(t));playerBanner.localScale=enemyBanner.localScale=Vector3.one*size;
            scoring.transform.localScale=Vector3.one*(1+.38f*Mathf.Sin(t*Mathf.PI));Alpha(flash,.68f*Mathf.Pow(1-t,3));
        });
        scoring.transform.localScale=playerBanner.localScale=enemyBanner.localScale=Vector3.one;Alpha(flash,0);
        yield return Pop(winner,.22f);Phase="ScoreHold";yield return Motion(scoreHold,null);
    }
    public IEnumerator Exit(bool revealMap)
    {
        Phase="Cut";
        yield return Motion(.22f,t=>{cover.Set(1,0,0,0,t);Alpha(flash,.35f*Mathf.Sin(t*Mathf.PI));});
        Alpha(flash,0);yield return Pop(versus,.18f);
        Phase="Retreat";float off=panel.rect.width*.5f+playerBanner.rect.width;
        yield return Motion(.32f,t=>
        {
            float e=t*t;playerBanner.anchoredPosition=Vector2.Lerp(playerHome,new Vector2(-off,playerHome.y),e);enemyBanner.anchoredPosition=Vector2.Lerp(enemyHome,new Vector2(off,enemyHome.y),e);
            float s=t<.25f?1+t*.35f:1.0875f*(1-Mathf.Clamp01((t-.25f)/.75f));title.transform.localScale=winner.transform.localScale=Vector3.one*s;
        });
        if(revealMap)
        {
            Phase="Split";
            yield return Motion(splitDuration,t=>cover.Set(1,Mathf.SmoothStep(0,1,t)));
        }
        yield return Motion(.15f,t=>versus.transform.localScale=Vector3.one*(1-t)*(1+.4f*Mathf.Sin(t*Mathf.PI)));
        panel.gameObject.SetActive(false);Alpha(flash,0);
        if(revealMap)cover.Set(0);
        Phase="Complete";
    }
    private static IEnumerator Pop(TMP_Text text,float duration) => Motion(duration,t=>text.transform.localScale=Vector3.one*Back(t));
    private static float Back(float t){float x=t-1;return 1+2.7f*x*x*x+1.7f*x*x;}
    private static void Alpha(Image image,float alpha){Color c=image.color;c.a=alpha;image.color=c;}
    private static IEnumerator Motion(float duration,Action<float> apply)
    {
        float elapsed=0;apply?.Invoke(0);
        while(elapsed<duration || GameplayPauseController.IsPaused)
        {
            elapsed+=GameplayPauseController.UnpausedUnscaledDeltaTime;
            apply?.Invoke(Mathf.Clamp01(elapsed/Mathf.Max(duration,.001f)));yield return null;
        }
        apply?.Invoke(1);
    }
}
