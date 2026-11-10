using UnityEngine;

public class BuffNerfVFXController : MonoBehaviour
{
    [Header("Main Reference")]
    [SerializeField] private BuffNerfController buffNerfController;

    [Header("Particle System References")]
    [SerializeField] private ParticleSystem buffVFX;
    [SerializeField] private ParticleSystem nerfVFX;

    private void Update()
    {
        if (buffNerfController == null) return;

        bool hasBuff = false;
        bool hasNerf = false;

        var activeEffects = buffNerfController.ActiveEffects;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].EffectData != null)
            {
                if (activeEffects[i].EffectData.effectType == EffectType.Buff)
                {
                    hasBuff = true;
                }
                else if (activeEffects[i].EffectData.effectType == EffectType.Nerf)
                {
                    hasNerf = true;
                }
            }
        }

        ToggleSingleParticle(buffVFX, hasBuff);
        ToggleSingleParticle(nerfVFX, hasNerf);
    }

    private void ToggleSingleParticle(ParticleSystem ps, bool active)
    {
        if (ps == null) return;

        var emission = ps.emission;
        if (emission.enabled != active)
        {
            emission.enabled = active;
        }
    }
}