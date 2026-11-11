using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuMapFlyover : MonoBehaviour
{
    [SerializeField] private Vector3 arenaCenter = new Vector3(1f, 6f, 0f);
    [SerializeField] private Vector2 orbitRadii = new Vector2(36f, 22f);
    [SerializeField] private float heightAboveCenter = 20f;
    [SerializeField] private float startingAngle = -145f;
    [SerializeField, Min(5f)] private float loopSeconds = 96f;

    private float phase;

    private void OnEnable()
    {
        phase = 0f;
        FrameCamera();
    }

    private void LateUpdate()
    {
        phase = Mathf.Repeat(phase + Time.unscaledDeltaTime / loopSeconds, 1f);
        FrameCamera();
    }

    private void FrameCamera()
    {
        float angle = phase * 2f * Mathf.PI + startingAngle * Mathf.Deg2Rad;
        var radial = new Vector3(Mathf.Cos(angle) * orbitRadii.x, 0f, Mathf.Sin(angle) * orbitRadii.y);
        var position = arenaCenter + radial + Vector3.up * (heightAboveCenter + 3f * Mathf.Sin(2f * angle));
        var target = arenaCenter - radial * 0.4f;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
    }
}
