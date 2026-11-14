using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public sealed class EnemyMarkerController : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RoleController enemy;
    [SerializeField] private Transform head;
    [SerializeField] private float fallbackHeadHeight = 2.3f;
    [SerializeField] private RectTransform marker;
    [SerializeField] private EnemyMarkerGraphic border;
    [SerializeField] private EnemyMarkerGraphic portraitMask;
    [SerializeField] private Image portrait;
    [SerializeField] private Sprite chaserBorder, evaderBorder, chaserFace, evaderFace;
    [SerializeField] private RectTransform activeEffectContainer;
    [SerializeField] private Vector4 screenPadding = new Vector4(20f, 205f, 20f, 205f);
    [SerializeField, Min(0.01f)] private float smoothing = 0.10f;
    private Canvas canvas;
    private RectTransform surface;
    private Vector2 arrow = Vector2.down;
    private bool initialized, onScreen, preferAboveCards;
    private float opening;
    private readonly Vector3[] cardCorners = new Vector3[4];

    public bool IsOnScreen => onScreen;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        surface = (RectTransform)marker.parent;
    }

    private void LateUpdate()
    {
        if (enemy == null || worldCamera == null)
        {
            marker.gameObject.SetActive(false);
            return;
        }
        marker.gameObject.SetActive(true);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector3 worldHead = head != null ? head.position + Vector3.up * 0.25f : enemy.transform.position + Vector3.up * fallbackHeadHeight;
        Vector3 projected = worldCamera.WorldToScreenPoint(worldHead);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, projected, uiCamera, out Vector2 target);
        Rect screen = worldCamera.pixelRect;
        Rect safe = Screen.safeArea;
        Vector2 screenMin = Vector2.Max(screen.min, safe.min), screenMax = Vector2.Min(screen.max, safe.max);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, screenMin, uiCamera, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, screenMax, uiCamera, out Vector2 max);
        Vector2 center = (min + max) * 0.5f;
        Vector2 halfMarker = marker.rect.size * 0.5f + Vector2.one * 30f;
        Vector2 lower = min + halfMarker + new Vector2(screenPadding.x, screenPadding.y);
        Vector2 upper = max - halfMarker - new Vector2(screenPadding.z, screenPadding.w);
        float margin = onScreen ? -12f : 12f;
        onScreen = projected.z > worldCamera.nearClipPlane &&
            target.x > min.x + margin && target.x < max.x - margin &&
            target.y > min.y + margin && target.y < max.y - margin;

        Vector2 desiredDirection = Vector2.down;
        if (onScreen)
        {
            target += Vector2.up * (halfMarker.y + 20f);
            target = Vector2.Max(lower, Vector2.Min(upper, target));
        }
        else
        {
            desiredDirection = target - center;
            if (projected.z < 0f) desiredDirection = -desiredDirection;
            if (desiredDirection.sqrMagnitude < 0.01f) desiredDirection = Vector2.down;
            desiredDirection.Normalize();
            float horizontal = Mathf.Abs(desiredDirection.x) < 0.0001f ? float.PositiveInfinity :
                (desiredDirection.x > 0f ? upper.x - center.x : lower.x - center.x) / desiredDirection.x;
            float vertical = Mathf.Abs(desiredDirection.y) < 0.0001f ? float.PositiveInfinity :
                (desiredDirection.y > 0f ? upper.y - center.y : lower.y - center.y) / desiredDirection.y;
            target = center + desiredDirection * Mathf.Min(horizontal, vertical);
            AvoidEffectCards(ref target, lower, upper, halfMarker);
        }
        float blend = initialized ? 1f - Mathf.Exp(-GameplayPauseController.UnpausedUnscaledDeltaTime / smoothing) : 1f;
        marker.anchoredPosition = Vector2.Lerp(marker.anchoredPosition, target, blend);
        float angle = Mathf.LerpAngle(Mathf.Atan2(arrow.y, arrow.x) * Mathf.Rad2Deg,
            Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg, blend);
        arrow = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        opening = Mathf.Lerp(opening, onScreen ? 0.28f : 1f, blend);
        marker.localScale = Vector3.Lerp(marker.localScale, Vector3.one * (onScreen ? 0.82f : 1f), blend);
        border.SetArrow(arrow, opening);
        portraitMask.SetArrow(arrow, opening);
        portrait.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.42f, opening);
        border.SetSprite(enemy.IsChaser ? chaserBorder : evaderBorder);
        portrait.sprite = enemy.IsChaser ? chaserFace : evaderFace;
        initialized = true;
    }

    private void AvoidEffectCards(ref Vector2 target, Vector2 lower, Vector2 upper, Vector2 halfMarker)
    {
        if (activeEffectContainer == null || activeEffectContainer.childCount == 0) return;
        Vector2 occupied = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (RectTransform card in activeEffectContainer)
        {
            if (!card.gameObject.activeInHierarchy) continue;
            card.GetWorldCorners(cardCorners);
            occupied = Vector2.Max(occupied, surface.InverseTransformPoint(cardCorners[2]));
        }
        Vector2 clearance = occupied + halfMarker + Vector2.one * 20f;
        if (target.x >= clearance.x || target.y >= clearance.y) return;

        // Stay on the safe screen perimeter: use the nearest free position above
        // the stack on the left edge, or beside the stack on the bottom edge.
        Vector2 beside = new Vector2(Mathf.Min(clearance.x, upper.x), lower.y);
        Vector2 above = new Vector2(lower.x, clearance.y);
        bool roomAbove = clearance.y <= upper.y;
        float aboveDistance = Vector2.Distance(above, target);
        float besideDistance = Vector2.Distance(beside, target);
        // A small dead band prevents alternate edges flickering at the corner.
        preferAboveCards = roomAbove && aboveDistance < besideDistance + (preferAboveCards ? 24f : -24f);
        target = preferAboveCards ? above : beside;
    }
}
