using UnityEngine;

public class PlayerSprintEffectsController : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private PlayerMovementIntent playerMovement;

    [Header("Ground State Reference")]
    [SerializeField] private CharacterState characterState;

    [Header("Effects Components")]
    [SerializeField] private ParticleSystem[] sprintParticles; 
    [SerializeField] private TrailRenderer[] sprintTrails; 

    [Header("Emission Points")]
    [Tooltip("One Transform per entry in Sprint Particles, in the same order.")]
    [SerializeField] private Transform[] sprintParticlePoints;
    [Tooltip("One Transform per entry in Sprint Trails, in the same order.")]
    [SerializeField] private Transform[] sprintTrailPoints;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovementIntent>();
        if (characterState == null) characterState = GetComponentInParent<CharacterState>();
        var movement = GetComponentInParent<CharacterMovement>();
        var model = movement != null ? movement.CharacterAnimator : null;
        BindPoints(sprintParticles, sprintParticlePoints, model);
        BindPoints(sprintTrails, sprintTrailPoints, model);
        ToggleParticles(false);
        ToggleTrails(false);
    }

    private static void BindPoints(Component[] emitters, Transform[] points, Animator model)
    {
        if (emitters == null) return;
        for (int i = 0; i < emitters.Length; i++)
        {
            Transform point = points != null && i < points.Length ? points[i] : null;
            FollowTarget.Bind(emitters[i], point, model);
        }
    }

    private void Update()
    {
        if (playerMovement == null || characterState == null) return;

        bool isGrounded = characterState.CurrentState == LocomotionState.Grounded;

        bool isSliding = characterState.CurrentState == LocomotionState.Sliding;
        
        bool shouldSpawnDust = (playerMovement.Sprinting && isGrounded) || isSliding;

        ToggleParticles(shouldSpawnDust);
        ToggleTrails(playerMovement.Sprinting);

    }


    private void ToggleParticles(bool active)
    {
        if (sprintParticles == null) return;

        for (int i = 0; i < sprintParticles.Length; i++)
        {
            if (sprintParticles[i] != null)
            {
                var emission = sprintParticles[i].emission;
                
                if (emission.enabled != active)
                {
                    emission.enabled = active;
                }
            }
        }
    }

    private void ToggleTrails(bool active)
    {
        if (sprintTrails == null) return;
        for (int i = 0; i < sprintTrails.Length; i++)
        {
            if (sprintTrails[i] != null)
            {
                sprintTrails[i].emitting = active;
            }
        }
    }

}
