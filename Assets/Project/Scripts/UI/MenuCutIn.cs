using UnityEngine;
using UnityEngine.InputSystem;

// The RectTransform in the scene is the settled pose. Reopening a panel replays its cut-ins.
[RequireComponent(typeof(CanvasGroup))]
public sealed class MenuCutIn : MonoBehaviour
{
    [SerializeField] private Vector2 entranceOffset;
    [SerializeField] private float angle;
    [SerializeField, Min(0f)] private float delay;
    [SerializeField, Min(0.05f)] private float duration = 0.34f;
    [SerializeField] private float pointerTravel;

    private RectTransform rect;
    private CanvasGroup group;
    private Vector2 rest;
    private Vector2 drift;
    private float elapsed;

    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        rest = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        elapsed = 0f;
        drift = Vector2.zero;
        Apply(0f);
    }

    private void Update()
    {
        if (elapsed >= delay + duration && pointerTravel == 0f) return;
        elapsed += Time.unscaledDeltaTime;
        if (pointerTravel != 0f && Mouse.current != null)
        {
            Vector2 p = Mouse.current.position.ReadValue();
            var aim = new Vector2(Mathf.Clamp(2f * p.x / Mathf.Max(1, Screen.width) - 1f, -1f, 1f),
                Mathf.Clamp(2f * p.y / Mathf.Max(1, Screen.height) - 1f, -1f, 1f));
            drift = Vector2.Lerp(drift, aim * pointerTravel, 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
        }
        Apply(Mathf.Clamp01((elapsed - delay) / duration));
    }

    private void Apply(float t)
    {
        // A small overshoot gives the paper cutout a snap when it lands.
        float tail = (1f - t) * (1f - t) * (1f - 1.7f * t);
        rect.anchoredPosition = rest + entranceOffset * tail + drift * t;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle * tail);
        group.alpha = Mathf.Clamp01(t * 3f);
        group.blocksRaycasts = t >= 0.5f;
    }
}
