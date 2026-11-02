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

    private void Update()
    {
        if (playerMovement == null || characterState == null) return;

        bool isGrounded = characterState.CurrentState == LocomotionState.Grounded;

        bool shouldSpawnDust = playerMovement.Sprinting && isGrounded;

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
