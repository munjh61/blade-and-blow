using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMP2DButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.92f, 0.96f, 1f);
    public Color pressedColor = new Color(0.75f, 0.86f, 1f);

    [Header("Outline (TMP SDF)")]
    [Range(0f, 1f)] public float normalOutline = 0.00f;
    [Range(0f, 1f)] public float hoverOutline = 0.18f;
    [Range(0f, 1f)] public float pressedOutline = 0.30f;

    [Header("Hover Scale")]
    [Tooltip("호버 시 스케일 배수")]
    public float hoverScaleMultiplier = 1.05f;
    [Tooltip("스케일 전환 속도(부드럽게)")]
    public float scaleLerpSpeed = 16f;

    [Header("Disabled Style")]
    public Color disabledColor = new Color(1f, 1f, 1f, 0.5f);
    [Range(0f, 1f)] public float disabledOutline = 0.0f;
    [Tooltip("CanvasGroup 사용 시 비활성 알파")]
    [Range(0f, 1f)] public float disabledAlpha = 0.5f;


    [Header("Events")]
    public UnityEvent onClick;

    [Header("State (read-only)")]
    [SerializeField] private bool _interactable = true;
    public bool interactable
    {
        get => _interactable;
        set => SetInteractable(value);
    }

    private TextMeshProUGUI _tmp;
    private Vector3 _baseScale;
    private Material _runtimeMat;
    private bool _hover;
    private bool _pressed;
    private CanvasGroup _cg;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        _baseScale = transform.localScale;

        // 머티리얼 인스턴스화 (공유 머티 수정 방지)
        _runtimeMat = Instantiate(_tmp.fontMaterial);
        _tmp.fontMaterial = _runtimeMat;

        ApplyVisual(normalColor, normalOutline);
        ApplyInteractable(_interactable, applyVisual: true);
    }

    void OnEnable()
    {
        ApplyInteractable(_interactable, applyVisual: true);
    }

    void Update()
    {
        // 스케일 부드럽게 보간
        var targetScale = (!_interactable)
            ? _baseScale
            : (_hover ? _baseScale * hoverScaleMultiplier : _baseScale);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);
    }

    void ApplyVisual(Color color, float outline)
    {
        _tmp.color = color;
        _tmp.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outline);
        // 필요하면 여기서 Outline Color도 바꿀 수 있음:
        // _tmp.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
    }

    public void SetInteractable(bool value)
    {
        if (_interactable == value) return;
        _interactable = value;
        ApplyInteractable(value, applyVisual: true);
    }

    private void ApplyInteractable(bool value, bool applyVisual)
    {
        if (_cg != null)
        {
            _cg.interactable = value;
            _cg.blocksRaycasts = value;
            _cg.alpha = value ? 1f : disabledAlpha;
        }

        if (!value)
        {
            _hover = false;
            _pressed = false;
            if (applyVisual) ApplyVisual(disabledColor, disabledOutline);
        }
        else
        {
            if (applyVisual) ApplyVisual(normalColor, normalOutline);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_interactable) return;
        _hover = true;
        if (!_pressed) ApplyVisual(hoverColor, hoverOutline);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_interactable) return;
        _hover = false;
        _pressed = false;
        ApplyVisual(normalColor, normalOutline);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_interactable) return;
        _pressed = true;
        ApplyVisual(pressedColor, pressedOutline);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_interactable) return;
        _pressed = false;
        ApplyVisual(_hover ? hoverColor : normalColor, _hover ? hoverOutline : normalOutline);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable) return;
        onClick?.Invoke();
    }
}