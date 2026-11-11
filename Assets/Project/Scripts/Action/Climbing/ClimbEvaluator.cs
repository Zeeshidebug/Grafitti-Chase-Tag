using UnityEngine;

public class ClimbEvaluator : MonoBehaviour
{
    [Header("AI Climb Reach")]
    [SerializeField, Min(0f)] private float directClimbExtraHeight = 0.75f;
    [SerializeField, Min(0f)] private float jumpClimbExtraHeight = 1.75f;
    [SerializeField, Min(0f)] private float minimumJumpObstacleHeight = 0.25f;

    public ClimbCandidate Evaluate(
        ParkourObstacleData obstacle)
    {
        ClimbCandidate candidate =
            new ClimbCandidate();

        if (obstacle == null ||
            !obstacle.IsValid)
        {
            candidate.IsValid = false;
            candidate.Result =
                ClimbValidationResult.NoObstacle;

            return candidate;
        }

        candidate.IsValid = true;
        candidate.Result =
            ClimbValidationResult.Valid;

        candidate.TopPosition =
            obstacle.TopPosition;
        candidate.Obstacle = obstacle;

        return candidate;
    }

    public ClimbCandidate EvaluateForAI(
        ParkourObstacleData obstacle,
        float jumpHeight)
    {
        ClimbCandidate candidate = Evaluate(obstacle);
        if (!candidate.IsValid)
            return candidate;

        float directClimbLimit = jumpHeight + directClimbExtraHeight;
        float jumpClimbLimit = Mathf.Max(
            directClimbLimit,
            jumpHeight + jumpClimbExtraHeight);

        if (obstacle.Height <= jumpHeight || obstacle.Height > jumpClimbLimit)
        {
            candidate.IsValid = false;
            candidate.Result = ClimbValidationResult.HeightOutsideAIRange;
            return candidate;
        }

        candidate.RequiresJump = obstacle.Height > directClimbLimit;
        return candidate;
    }

    public bool IsJumpCandidate(
        ParkourObstacleData obstacle,
        float jumpHeight)
    {
        return obstacle != null &&
            obstacle.IsValid &&
            obstacle.Height >= minimumJumpObstacleHeight &&
            obstacle.Height <= jumpHeight;
    }
}
