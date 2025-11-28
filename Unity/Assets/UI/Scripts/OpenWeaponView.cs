using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OpenWeaponView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public GameObject hoverHighlight;
    public Sprite[] weaponIconImages;
    public Image targetImage;
    public TextMeshProUGUI targetText;


    private void Start()
    {
        targetText.text = "Press 'Alt' to select Weapon";
        SelectedLoadout.OnChanged += OnSelectedLoadoutChanged;
        if (hoverHighlight != null)
            hoverHighlight.SetActive(false);
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
        UIManager.Instance.Close(MenuId.CurrentSelectedWeapon);
        UIManager.Instance.Open(MenuId.MatchInfoPanel, MenuId.SelectWeapon);
    }

    private void OnSelectedLoadoutChanged(int equipId)
    {
       targetImage.sprite = weaponIconImages[equipId];
       targetImage.enabled = true;
         targetText.text = "Click 'Icon' to change Weapon";
    }


    private void OnDestroy()
    {
        SelectedLoadout.OnChanged -= OnSelectedLoadoutChanged;
    }
}
