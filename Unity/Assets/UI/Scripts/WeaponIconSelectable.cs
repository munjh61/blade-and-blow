using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WeaponIconSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    public GameObject borderObject; // 선택 상태용 Border
    public GameObject hoverHighlight;
    public Image iconImage; // 무기 아이콘 이미지

    [Header("Weapon Configuration")]
    public Weapon.Type weaponType = Weapon.Type.Sword;

    private bool isSelected = true; 
    internal WeaponIconSelectionGroup group;
    private Image _background;

    private void Awake()
    {
        if (group == null)
            group = GetComponentInParent<WeaponIconSelectionGroup>();

        _background = GetComponent<Image>();

        RefreshVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleSelection();
    }

    public void ToggleSelection()
    {
        SetSelected(!isSelected);

        if (group != null)
            group.OnSelectionChanged();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        Debug.Log($"WeaponIconSelectable: SetSelected to {isSelected} for {weaponType}");
        RefreshVisual();
    }
    
    public bool IsSelected()
    {
        return isSelected;
    }

    private void RefreshVisual()
    {
        if (borderObject != null && iconImage != null)
        {
            borderObject.SetActive(isSelected);
            iconImage.color = isSelected ? Color.white : new Color(1f, 1f, 1f, 0.5f); // 선택 해제된 것을 반투명
            Color bgColor = _background.color;
            bgColor.a = isSelected ? 1f : 0.5f;
            _background.color = bgColor;
        }

        if (hoverHighlight != null)
            hoverHighlight.SetActive(false);
    }
}
