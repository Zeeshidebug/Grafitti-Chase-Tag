
using UnityEngine;

[RequireComponent(typeof(StaminaSystem))]
public class StaminaUtilityEvaluator : MonoBehaviour
{
    [Header("Sprint Eligibility")]
    [SerializeField, Range(0f, 1f)]
    private float minimumSprintStaminaRatio = 0.2f;

    [Header("Sprint Utility")]
    [SerializeField]
    private AnimationCurve sprintUtilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public AnimationCurve SprintUtilityCurve => sprintUtilityCurve;

    private StaminaSystem staminaSystem;

    public float StaminaRatio { get; private set; }
    public float SprintUtility { get; private set; }
    public bool CanSprint { get; private set; }

    private void Awake()
    {
        staminaSystem = GetComponent<StaminaSystem>();
    }

    private void Update()
    {
        Evaluate();
    }

    public void Evaluate()
    {
        float maxStamina = staminaSystem.MaxStamina;

        StaminaRatio = maxStamina > 0f
            ? Mathf.Clamp01(staminaSystem.CurrentStamina / maxStamina)
            : 0f;

        SprintUtility = Mathf.Clamp01(
            sprintUtilityCurve.Evaluate(StaminaRatio)
        );

        CanSprint =
            !staminaSystem.IsExhausted &&
            StaminaRatio >= minimumSprintStaminaRatio;
    }
}
