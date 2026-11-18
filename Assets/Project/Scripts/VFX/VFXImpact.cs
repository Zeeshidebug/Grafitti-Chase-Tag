using UnityEngine;

public class ParkourVFXController : MonoBehaviour
{
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject windImpactPrefab;

    [Header("Bone References (Titik Kontak)")]
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;
    [SerializeField] private Transform leftFootTransform;
    [SerializeField] private Transform rightFootTransform;

    // --- Pemanggilan via Animation Event ---

    public void TriggerLeftHandImpact()
    {
        SpawnImpact(leftHandTransform);
    }

    public void TriggerRightHandImpact()
    {
        SpawnImpact(rightHandTransform);
    }

    public void TriggerLeftFootImpact()
    {
        SpawnImpact(leftFootTransform);
    }

    public void TriggerRightFootImpact()
    {
        SpawnImpact(rightFootTransform);
    }

    private void SpawnImpact(Transform point)
    {
        if (windImpactPrefab == null || point == null) return;

        GameObject vfxInstance = Instantiate(windImpactPrefab, point.position, point.rotation);
        
        Destroy(vfxInstance, 1f);
    }
}