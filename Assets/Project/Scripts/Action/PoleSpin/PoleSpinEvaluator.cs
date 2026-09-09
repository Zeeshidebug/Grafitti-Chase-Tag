using UnityEngine;

public class PoleSpinEvaluator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxSpinDistance = 1.5f;

    public PoleSpinCandidate Evaluate(
        ParkourObstacleData pole)
    {
        PoleSpinCandidate candidate =
            new PoleSpinCandidate();

        if (pole == null ||
            !pole.IsValid)
        {
            candidate.IsValid = false;
            candidate.Result =
                PoleSpinValidationResult.NoPole;

            return candidate;
        }

        if (pole.Distance > maxSpinDistance)
        {
            candidate.IsValid = false;
            candidate.Result =
                PoleSpinValidationResult.TooFar;

            return candidate;
        }

        candidate.IsValid = true;
        candidate.Result =
            PoleSpinValidationResult.Valid;

        candidate.PolePosition =
            pole.HitPoint;

        candidate.PoleNormal =
            pole.SurfaceNormal;

        return candidate;
    }
}