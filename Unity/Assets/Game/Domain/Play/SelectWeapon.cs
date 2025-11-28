using UnityEngine;

public class SelectWeapon : MonoBehaviour, IInitializable
{
    [Header("Refs")]
    public PanelSelectionGroup group;
    public PanelClickable applyButton;

    private CharacterEquipmentReborn _character;
    private Slot _targetSlot;

    private int _pendingIndex = -1;

    public void Init(object args)
    {
        if (args is SelectWeaponArgs a)
        {
            _character = a.character;
            _targetSlot = a.targetSlot;
        }
        else
        {
            Debug.LogWarning("SelectWeapon.Init: 잘못된 args 타입");
        }
    }

    private void OnEnable()
    {
        if (group != null)
            group.onSelectedIndexChanged.AddListener(OnSelectedIndexChanged);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyPending);
    }

    private void OnDisable()
    {
        if (group != null)

        group.onSelectedIndexChanged.RemoveListener(OnSelectedIndexChanged);

        if (applyButton != null)
            applyButton.onClick.RemoveListener(ApplyPending);
    }

    private void OnSelectedIndexChanged(int index) => _pendingIndex = index;    

    public void ApplyPending()
    {
        if (_pendingIndex < 0) return;
        SelectedLoadout.SetEquip(_pendingIndex);
        _pendingIndex = -1;

        UIManager.Instance.Close(MenuId.SelectWeapon);
        UIManager.Instance.Open(MenuId.MatchInfoPanel, MenuId.CurrentSelectedWeapon);
    }
}