
using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ActiveEffectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image effectBorder;
    [SerializeField] private Image effectIcon;
    [SerializeField] private TMP_Text durationText;

    [Header("Border Sprites")]
    [SerializeField] private Sprite buffBorder;
    [SerializeField] private Sprite nerfBorder;

    [Header("Pop Animation")]
    [SerializeField] private RectTransform visual;
    [SerializeField] private CanvasGroup visualGroup;
    [SerializeField] private ParticleSystem particles;
    [SerializeField, Min(0f)] private float warningSeconds = 3f;
    private float age, warningPulse;
    private int displayedSeconds = -1;
    private Color textColor;
    private Color effectColor;
    private bool dismissing;

    public void Setup(ActiveBuffNerfEffect activeEffect)
    {
        BuffNerfEffect effectData = activeEffect.EffectData;

        effectIcon.sprite = effectData.iconSprite;
        effectIcon.enabled = effectData.iconSprite != null;

        effectBorder.sprite =
            effectData.effectType == EffectType.Buff
                ? buffBorder
                : nerfBorder;

        textColor = visual != null && effectData.effectType == EffectType.Nerf ? Color.white : durationText.color;
        durationText.color = textColor;
        effectColor = effectData.effectType == EffectType.Buff ? Color.cyan : new Color(1f, 0.2f, 0.4f);
        age = 0f;
        if (visual != null) visual.localScale = Vector3.one * 0.25f;
        UpdateDuration(activeEffect.RemainingDuration);
    }

    public void UpdateDuration(float remainingDuration)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingDuration));
        if (displayedSeconds != seconds)
        {
            durationText.text = seconds.ToString();
            if (remainingDuration > 0f && remainingDuration <= warningSeconds)
                warningPulse = 1f;
            displayedSeconds = seconds;
        }
        if (remainingDuration > warningSeconds) warningPulse = 0f;
    }

    private void Update()
    {
        if (visual == null || dismissing) return;
        float dt = GameplayPauseController.UnpausedUnscaledDeltaTime;
        float previousAge = age;
        age += dt;
        float t = Mathf.Clamp01(age / 0.3f);
        float tail = (1f - t) * (1f - t) * (1f - 2.6f * t);
        visual.localScale = new Vector3(1f - 0.75f * tail, 1f - 0.75f * tail + Mathf.Sin(t * Mathf.PI * 2f) * 0.09f, 1f);
        visual.anchoredPosition = new Vector2(-42f * (1f - t) * (1f - t), 0f);
        if (previousAge < 0.2f && age >= 0.2f) PlayParticles();
        warningPulse = Mathf.Max(0f, warningPulse - dt / 0.42f);
        float beat = Mathf.Sin(warningPulse * Mathf.PI);
        effectBorder.color = new Color(1f, 1f, 1f, 1f - 0.55f * beat);
        effectIcon.color = new Color(1f, 1f, 1f, 1f - 0.55f * beat);
        durationText.color = Color.Lerp(textColor, Color.white, beat);
        durationText.rectTransform.localScale = Vector3.one * (1f + 0.18f * beat);
    }

    public void Dismiss()
    {
        if (dismissing) return;
        dismissing = true;
        if (visual == null) Destroy(gameObject);
        else StartCoroutine(PopOut());
    }

    private IEnumerator PopOut()
    {
        PlayParticles();
        float elapsed = 0f;
        while (elapsed < 0.38f)
        {
            elapsed += GameplayPauseController.UnpausedUnscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.28f);
            float scale = (2f + 0.4f * Mathf.Sin(t * Mathf.PI)) * (1f - t * t);
            visual.localScale = new Vector3(scale, scale * (1f - 0.2f * t), 1f);
            visual.anchoredPosition = Vector2.left * (30f * t * t);
            visualGroup.alpha = 1f - t * t;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void PlayParticles()
    {
        if (particles == null) return;
        var main = particles.main;
        main.startColor = effectColor;
        UIParticleCanvasRenderer.PlayBurst(particles);
    }
}
