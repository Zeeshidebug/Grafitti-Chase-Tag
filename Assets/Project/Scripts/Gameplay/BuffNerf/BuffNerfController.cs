using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffNerfController : MonoBehaviour
{
    private readonly List<ActiveBuffNerfEffect> activeEffects = new();
    public IReadOnlyList<ActiveBuffNerfEffect> ActiveEffects => activeEffects;

    public void ApplyEffect(BuffNerfEffect effect)
    {
        ActiveBuffNerfEffect existingEffect = null;

        foreach (ActiveBuffNerfEffect activeEffect in activeEffects)
        {
            if (activeEffect.EffectData.statType == effect.statType)
            {
                existingEffect = activeEffect;
                break;
            }
        }

        if (existingEffect == null)
        {
            ActiveBuffNerfEffect newEffect =
                new ActiveBuffNerfEffect(effect);

            activeEffects.Add(newEffect);

            StartCoroutine(
                EffectDurationRoutine(newEffect)
            );

            return;
        }

        if (existingEffect.EffectData.effectType == effect.effectType)
        {
            existingEffect.RemainingDuration += effect.duration;

            return;
        }

        RemoveEffect(existingEffect);

        ActiveBuffNerfEffect replacement =
            new ActiveBuffNerfEffect(effect);

        activeEffects.Add(replacement);

        StartCoroutine(
            EffectDurationRoutine(replacement)
        );
    }

    public float GetStatMultiplier(StatType statType)
    {
        float multiplier = 1f;

        foreach (ActiveBuffNerfEffect activeEffect in activeEffects)
        {
            if (activeEffect.EffectData.statType != statType)
                continue;

            multiplier *= activeEffect.EffectData.multiplier;
        }

        return multiplier;
    }

    private void RemoveEffect(ActiveBuffNerfEffect activeEffect)
    {
        activeEffects.Remove(activeEffect);
    }

    private IEnumerator EffectDurationRoutine(
        ActiveBuffNerfEffect activeEffect)
    {
        while (activeEffects.Contains(activeEffect) &&
               activeEffect.RemainingDuration > 0f)
        {
            activeEffect.RemainingDuration -= Time.deltaTime;

            Debug.Log(
                $"Current Effect: {activeEffect.EffectData.name}, " +
                $"Remaining Duration: {activeEffect.RemainingDuration:F2}"
            );

            yield return null;
        }

        if (activeEffects.Contains(activeEffect))
        {
            RemoveEffect(activeEffect);
        }
    }
}