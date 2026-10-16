
using TMPro;
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

    public void Setup(ActiveBuffNerfEffect activeEffect)
    {
        BuffNerfEffect effectData = activeEffect.EffectData;

        effectIcon.sprite = effectData.iconSprite;
        effectIcon.enabled = effectData.iconSprite != null;

        effectBorder.sprite =
            effectData.effectType == EffectType.Buff
                ? buffBorder
                : nerfBorder;

        UpdateDuration(activeEffect.RemainingDuration);
    }

    public void UpdateDuration(float remainingDuration)
    {
        durationText.text =
            Mathf.CeilToInt(Mathf.Max(0f, remainingDuration))
            .ToString();
    }
}
