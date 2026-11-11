
using UnityEngine;

public class AIMovementTestDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AIMovementIntent movementIntent;
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private bool sprint;

    private Vector3 lastFacingDirection;

    private void Update()
    {
        if (movementIntent == null || target == null)
            return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <=
            stoppingDistance * stoppingDistance)
        {
            movementIntent.StopMovement();
            return;
        }

        Vector3 direction = toTarget.normalized;
        lastFacingDirection = direction;

        movementIntent.SetMovementIntent(
            direction,
            lastFacingDirection,
            sprint
        );
    }
}
