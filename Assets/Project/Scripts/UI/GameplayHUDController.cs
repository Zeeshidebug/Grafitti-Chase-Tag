using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class GameplayHUDController : MonoBehaviour
{
    [Header("Active Effect UI")]
    [SerializeField] private BuffNerfController playerBuffNerfController;
    [SerializeField] private Transform activeEffectContainer;
    [SerializeField] private ActiveEffectUI activeEffectEntryPrefab;

    private readonly Dictionary<ActiveBuffNerfEffect, ActiveEffectUI>
        activeEffectEntries = new();

    private void Update()
    {
        UpdateActiveEffects();
    }

    private void UpdateActiveEffects()
    {
        IReadOnlyList<ActiveBuffNerfEffect> activeEffects =
            playerBuffNerfController.ActiveEffects;

        // Create entries for newly active effects.
        foreach (ActiveBuffNerfEffect activeEffect in activeEffects)
        {
            if (activeEffectEntries.ContainsKey(activeEffect))
                continue;

            ActiveEffectUI entry = Instantiate(
                activeEffectEntryPrefab,
                activeEffectContainer
            );

            entry.Setup(activeEffect);
            activeEffectEntries.Add(activeEffect, entry);
        }

        // Update durations and remove expired effects.
        List<ActiveBuffNerfEffect> effectsToRemove = new();

        foreach (var pair in activeEffectEntries)
        {
            ActiveBuffNerfEffect activeEffect = pair.Key;
            ActiveEffectUI entry = pair.Value;

            if (!activeEffects.Contains(activeEffect))
            {
                effectsToRemove.Add(activeEffect);
                continue;
            }

            entry.UpdateDuration(activeEffect.RemainingDuration);
        }

        foreach (ActiveBuffNerfEffect activeEffect in effectsToRemove)
        {
            Destroy(activeEffectEntries[activeEffect].gameObject);
            activeEffectEntries.Remove(activeEffect);
        }
    }
}
