using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenerationRate = 0.1f;

    private float currentStamina;

    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public float RegenerationRate => regenerationRate;

    private bool isExhausted;
    public bool IsExhausted => isExhausted;

    private void Awake()
    {
        currentStamina = maxStamina;
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
        if (!CanAfford(amount))
            return false;

        currentStamina -= amount;

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isExhausted = true;
        }

        return true;
    }

    public void ConsumeContinuous(float amount)
    {
        currentStamina = Mathf.Max(
            currentStamina - amount,
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
        float regenerationAmount =
            MaxStamina * regenerationRate * deltaTime;

        Restore(regenerationAmount);
    }
}