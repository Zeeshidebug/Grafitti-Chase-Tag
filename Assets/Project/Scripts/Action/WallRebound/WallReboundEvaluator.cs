using UnityEngine;

public class WallReboundEvaluator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minimumReboundSpeed = 4f;
    [SerializeField] private float minimumWallAngle = 70f;

    public WallReboundCandidate Evaluate(
        ParkourObstacleData obstacle,
        Vector3 velocity)
    {
        WallReboundCandidate candidate =
            new WallReboundCandidate();

        if (obstacle == null ||
            !obstacle.IsValid)
        {
            candidate.IsValid = false;
            candidate.Result =
                WallReboundValidationResult.NoWall;

            return candidate;
        }

        Vector3 horizontalVelocity =
    new Vector3(
        velocity.x,
        0f,
        velocity.z
    );

        float speed =
            horizontalVelocity.magnitude;

        if (speed < minimumReboundSpeed)
        {
            candidate.IsValid = false;
            candidate.Result =
                WallReboundValidationResult.TooSlow;

            return candidate;
        }

        float surfaceAngle =
        Vector3.Angle(
            obstacle.SurfaceNormal,
            Vector3.up
        );

        if (surfaceAngle < minimumWallAngle)
        {
            candidate.IsValid = false;
            candidate.Result =
                WallReboundValidationResult.InvalidSurface;

            return candidate;
        }

        Vector3 incomingDirection =
            horizontalVelocity.normalized;

        Vector3 reboundDirection =
            Vector3.Reflect(
                incomingDirection,
                obstacle.SurfaceNormal
            );

        candidate.IsValid = true;
        candidate.Result =
            WallReboundValidationResult.Valid;

        candidate.WallNormal =
            obstacle.SurfaceNormal;

        candidate.ReboundDirection =
            reboundDirection.normalized;

        candidate.ImpactSpeed =
            speed;

        return candidate;
    }
}