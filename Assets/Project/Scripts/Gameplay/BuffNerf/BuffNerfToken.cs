using UnityEngine;

public class BuffNerfToken : MonoBehaviour
{
    [SerializeField] private BuffNerfEffect effect;

    private void OnTriggerEnter(Collider other)
    {
        BuffNerfController controller =
            other.GetComponent<BuffNerfController>();

        if (controller == null)
            return;

        controller.ApplyEffect(effect);

        Debug.Log(
            $"Applied effect: {effect.name}"
        );

        Destroy(gameObject);
    }
}