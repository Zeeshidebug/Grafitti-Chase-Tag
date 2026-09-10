using UnityEngine;

public class TicTacEvaluator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxTicTacDistance = 1.5f;
    [SerializeField] private float launchSpeedMultiplier = 1f;

    public TicTacCandidate Evaluate(
        ParkourObstacleData obstacle,
        Vector3 currentVelocity)
    {
        TicTacCandidate candidate =
            new TicTacCandidate();

        if (obstacle == null ||
            !obstacle.IsValid)
        {
            candidate.IsValid = false;
            candidate.Result =
                TicTacValidationResult.NoWall;

            return candidate;
        }

        if (obstacle.Distance > maxTicTacDistance)
        {
            candidate.IsValid = false;
            candidate.Result =
                TicTacValidationResult.TooFar;

            return candidate;
        }

        Vector3 horizontalVelocity =
            new Vector3(
                currentVelocity.x,
                0f,
                currentVelocity.z
            );

        if (horizontalVelocity.sqrMagnitude < 0.001f)
        {
            candidate.IsValid = false;
            candidate.Result =
                TicTacValidationResult.NoWall;

            return candidate;
        }

        Vector3 incomingDirection =
            horizontalVelocity.normalized;

        Vector3 ticTacDirection =
            Vector3.Reflect(
                incomingDirection,
                obstacle.SurfaceNormal
            ).normalized;

        candidate.IsValid = true;
        candidate.Result =
            TicTacValidationResult.Valid;

        candidate.TicTacDirection =
            ticTacDirection;

        candidate.WallNormal =
            obstacle.SurfaceNormal;

        candidate.LaunchSpeed =
            horizontalVelocity.magnitude *
            launchSpeedMultiplier;

        return candidate;
    }
}