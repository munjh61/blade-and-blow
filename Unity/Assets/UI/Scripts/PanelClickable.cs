using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class PanelClickable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IPointerClickHandler
{
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.92f, 0.96f, 1f);
    public Color pressedColor = new Color(0.85f, 0.90f, 1f);

    [Header("Events")]
    public UnityEvent onClick; // Submit 동작 연결

    private Image _bg;
    private bool _isHover;
    private bool _isPressed;

    private void Awake()
    {
        _bg = GetComponent<Image>();
        if (_bg == null) Debug.LogError($"{name}: Image 컴포넌트 필요");
        _bg.color = normalColor;
    }

    private void UpdateVisual()
    {
        if (_isPressed) _bg.color = pressedColor;
        else if (_isHover) _bg.color = hoverColor;
        else _bg.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        _isHover = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData e)
    {
        _isHover = false;
        _isPressed = false;
        UpdateVisual();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        _isPressed = true;
        UpdateVisual();
    }

    public void OnPointerUp(PointerEventData e)
    {
        _isPressed = false;
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        onClick?.Invoke();
    }
}