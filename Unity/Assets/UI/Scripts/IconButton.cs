using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class IconButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Button Images")]
    public Image buttonImage;           // 메인 아이콘
    // public Image glowImage;            // 뒤쪽 빛나는 효과 (선택사항)

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);
    public Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Glow Effect")]
    public Color glowNormalColor = new Color(1f, 1f, 1f, 0f);
    public Color glowHoverColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Scale Effect")]
    public float hoverScale = 1.1f;
    public float scaleLerpSpeed = 18f;

    [Header("Events")]
    public UnityEvent onClick;

    private Vector3 _baseScale;
    private bool _hover;
    private bool _pressed;

    void Start()
    {
        _baseScale = transform.localScale;
        ApplyVisual(normalColor, glowNormalColor);
    }

    void Update()
    {
        // 스케일 애니메이션
        Vector3 targetScale = _hover ? _baseScale * hoverScale : _baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);
    }

    void ApplyVisual(Color buttonColor, Color glowColor)
    {
        if (buttonImage != null)
            buttonImage.color = buttonColor;
            
        // if (glowImage != null)
        //     glowImage.color = glowColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hover = true;
        if (!_pressed)
            ApplyVisual(hoverColor, glowHoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hover = false;
        _pressed = false;
        ApplyVisual(normalColor, glowNormalColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        ApplyVisual(pressedColor, glowHoverColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        ApplyVisual(_hover ? hoverColor : normalColor, _hover ? glowHoverColor : glowNormalColor);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }
}