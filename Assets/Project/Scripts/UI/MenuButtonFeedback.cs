using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ICancelHandler,
    IMoveHandler, ISubmitHandler
{
    [SerializeField] private Image artwork;
    [SerializeField] private Sprite highlighted;
    [SerializeField] private Image inkEcho;
    [SerializeField] private MainMenuController menu;

    private Sprite normal;
    private bool hover, keyboardFocus, held;
    private float focus, press, hit;

    private void Awake() => normal = artwork.sprite;

    private void OnDisable()
    {
        hover = keyboardFocus = held = false;
        focus = press = hit = 0f;
        Apply();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (menu.IsTransitioning) return;
        hover = true;
        keyboardFocus = false;
        EventSystem.current.SetSelectedGameObject(gameObject, e);
        Apply();
    }

    public void OnPointerExit(PointerEventData e)
    {
        hover = keyboardFocus = held = false;
        Apply();
    }
    public void OnPointerDown(PointerEventData e) => held = e.button == PointerEventData.InputButton.Left;
    public void OnPointerUp(PointerEventData e) => held = false;
    public void OnSelect(BaseEventData e)
    {
        keyboardFocus = !(e is PointerEventData);
        Apply();
    }

    public void OnDeselect(BaseEventData e)
    {
        keyboardFocus = hover = held = false;
        Apply();
    }

    public void OnMove(AxisEventData e)
    {
        if (EventSystem.current.currentSelectedGameObject != gameObject) return;
        keyboardFocus = true;
        Apply();
    }

    public void OnSubmit(BaseEventData e)
    {
        keyboardFocus = true;
        Apply();
    }
    public void OnCancel(BaseEventData e) => menu.CloseCredits();

    public void Stamp() => hit = 1f;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        // EventSystem selection remains the Submit target after pointer exit.
        float target = hover || keyboardFocus ? 1f : 0f;
        float nextFocus = Mathf.MoveTowards(focus, target, dt * 8f);
        float nextPress = Mathf.MoveTowards(press, held ? 1f : 0f, dt * 18f);
        if (focus == nextFocus && press == nextPress && hit <= 0f) return;
        focus = nextFocus;
        press = nextPress;
        hit = Mathf.Max(0f, hit - dt / 0.18f);
        Apply();
    }

    private void Apply()
    {
        float f = Mathf.SmoothStep(0f, 1f, focus);
        var art = artwork.rectTransform;
        bool showHighlight = hover || keyboardFocus;
        artwork.sprite = showHighlight ? highlighted : normal;
        inkEcho.sprite = showHighlight ? normal : highlighted;
        art.anchoredPosition = new Vector2(16f * f + 4f * hit * Mathf.Sin(hit * 13f), 2f * f);
        art.localScale = Vector3.one * (1f + 0.035f * f - 0.055f * Mathf.Max(hit, press));
        art.localRotation = Quaternion.Euler(0f, 0f, -1.2f * f);
        inkEcho.enabled = hit > 0f;
        if (inkEcho.enabled)
        {
            inkEcho.rectTransform.anchoredPosition = art.anchoredPosition + new Vector2(-24f, -5f) * (1f - hit);
            inkEcho.color = new Color(1f, 0.18f, 0.69f, hit * 0.9f);
        }
    }
}
