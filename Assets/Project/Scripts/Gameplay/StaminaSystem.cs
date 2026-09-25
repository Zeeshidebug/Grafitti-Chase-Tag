using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenerationRate = 0.1f;

    private BuffNerfController buffNerfController;

    private float currentStamina;

    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public float RegenerationRate => regenerationRate;

    private bool isExhausted;
    public bool IsExhausted => isExhausted;

    private void Awake()
    {
        currentStamina = maxStamina;
        buffNerfController = GetComponent<BuffNerfController>();
    }

    private void Update()
    {

    }

    public bool CanAfford(float amount)
    {
        return currentStamina >= amount;
    }

    public bool TryConsume(float amount)
    {
        float finalCost =
            amount *
            buffNerfController.GetStatMultiplier(
                StatType.StaminaCost
            );

        if (!CanAfford(finalCost))
            return false;

        currentStamina -= finalCost;

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isExhausted = true;
        }

        return true;
    }

    public void ConsumeContinuous(float amount)
    {
        float finalAmount =
            amount *
            buffNerfController.GetStatMultiplier(
                StatType.StaminaCost
            );

        currentStamina = Mathf.Max(
            currentStamina - finalAmount,
            0f
        );

        if (currentStamina <= 0f)
        {
            isExhausted = true;
        }
    }

    public void Restore(float amount)
    {
        currentStamina = Mathf.Min(
            currentStamina + amount,
            maxStamina
        );

        if (currentStamina >= maxStamina)
        {
            isExhausted = false;
        }
    }

    public void Regenerate(float deltaTime)
    {
        float regenerationMultiplier =
            buffNerfController.GetStatMultiplier(
                StatType.StaminaRegen
            );

        float regenerationAmount =
            MaxStamina *
            regenerationRate *
            regenerationMultiplier *
            deltaTime;

        Restore(regenerationAmount);
    }
}