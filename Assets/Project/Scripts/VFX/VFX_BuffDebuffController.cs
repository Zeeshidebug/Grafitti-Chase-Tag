using UnityEngine;

public class BuffNerfVFXController : MonoBehaviour
{
    [Header("Main Reference")]
    [SerializeField] private BuffNerfController buffNerfController;

    [Header("Particle System References")]
    [SerializeField] private ParticleSystem buffVFX;
    [SerializeField] private ParticleSystem nerfVFX;

    [Header("Emission Points")]
    [SerializeField] private Transform buffPoint;
    [SerializeField] private Transform nerfPoint;
    [Tooltip("Character surface used by the two skinned mesh particle shapes.")]
    [SerializeField] private SkinnedMeshRenderer auraMesh;
    [Tooltip("Fallback path from the character Animator to the aura mesh when the scene reference is missing.")]
    [SerializeField] private string auraMeshPath;

    private void Awake()
    {
        if (buffNerfController == null) buffNerfController = GetComponentInParent<BuffNerfController>();
        var movement = GetComponentInParent<CharacterMovement>();
        var model = movement != null ? movement.CharacterAnimator : null;
        if (auraMesh == null && model != null && !string.IsNullOrEmpty(auraMeshPath))
        {
            var meshPoint = model.transform.Find(auraMeshPath);
            if (meshPoint != null) auraMesh = meshPoint.GetComponent<SkinnedMeshRenderer>();
        }
        BindAura(buffVFX, buffPoint, model);
        BindAura(nerfVFX, nerfPoint, model);
    }

    private void BindAura(ParticleSystem particles, Transform point, Animator model)
    {
        if (particles == null) return;
        FollowTarget.Bind(particles, point, model);
        var shape = particles.shape;
        if (auraMesh != null)
            shape.skinnedMeshRenderer = auraMesh;
        ToggleSingleParticle(particles, false);
    }

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
