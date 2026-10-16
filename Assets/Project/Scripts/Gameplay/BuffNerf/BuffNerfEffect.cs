using UnityEngine;

[CreateAssetMenu(
    fileName = "NewBuffNerfEffect",
    menuName = "Grafitti Chase Tag/Buff Nerf Effect"
)]
public class BuffNerfEffect : ScriptableObject
{
    public string effectID;


    public Sprite iconSprite;
    public EffectType effectType;
    public StatType statType;

    [Min(0f)]
    public float multiplier = 1f;

    [Min(0f)]
    public float duration = 5f;
}