using System;

public static class SelectedLoadout
{
    // 장착할 equipId (-1 = 없음)
    public static int CurrentEquipId { get; private set; } = -1;

    public static event Action<int> OnChanged;

    public static void SetEquip(int equipId)
    {
        if (CurrentEquipId == equipId) return;
        CurrentEquipId = equipId;
        OnChanged?.Invoke(equipId);
    }
}