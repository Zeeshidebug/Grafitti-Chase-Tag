public class ActiveBuffNerfEffect
{
    public BuffNerfEffect EffectData { get; }
    public float RemainingDuration { get; set; }

    public ActiveBuffNerfEffect(BuffNerfEffect effectData)
    {
        EffectData = effectData;
        RemainingDuration = effectData.duration;
    }
}