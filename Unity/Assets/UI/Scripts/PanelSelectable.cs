using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PanelSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Colors")]
    public Color normalColor = Color.white;                  // 기본 배경색
    public Color hoverColor = new Color(0.92f, 0.96f, 1f);  // 호버 시 배경색

    [Header("Border")]
    public Image border;

    [Header("Events")]
    public UnityEvent onClick; // 선택 시 실행할 로직 (인스펙터에서 연결 가능)

    // 내부 상태
    [HideInInspector] public PanelSelectionGroup group;

    private Image _background;
    private bool _isSelected;

    void Awake()
    {
        _background = GetComponent<Image>();
        _background.color = normalColor;

        if (border != null)
            border.enabled = false;

        if (group == null) group = GetComponentInParent<PanelSelectionGroup>();
    }

    private void OnEnable()
    {
        if (group != null) group.Register(this);
    }

    private void OnDisable()
    {
        if (group != null) group.Unregister(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isSelected) _background.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelected) _background.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // ✅ 무조건 그룹을 통해서만 선택되게
        if (group != null) group.Select(this);
        else SetSelected(true);

        onClick?.Invoke();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (border != null) border.enabled = selected;
        if (!selected) _background.color = normalColor;
    }
}
