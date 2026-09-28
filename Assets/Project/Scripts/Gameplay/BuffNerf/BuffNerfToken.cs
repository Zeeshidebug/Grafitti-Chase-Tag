using UnityEngine;

public class BuffNerfToken : MonoBehaviour
{
    [SerializeField] private BuffNerfEffect effect;

    private void OnTriggerEnter(Collider other)
    {
        RoleController roleController =
         other.GetComponentInParent<RoleController>();

        if (roleController == null)
            return;

        BuffNerfController controller =
            other.GetComponentInParent<BuffNerfController>();

        if (controller == null)
            return;

        if (effect.statType == StatType.TagSprayRange &&
            roleController.IsEvader || effect.statType == StatType.TagSprayRadius &&
            roleController.IsEvader)
        {
            Destroy(gameObject);
            return;
        }

        controller.ApplyEffect(effect);

        Destroy(gameObject);
    }
}