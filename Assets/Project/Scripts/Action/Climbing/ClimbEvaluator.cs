using UnityEngine;

public class ClimbEvaluator : MonoBehaviour
{
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

        return candidate;
    }
}