using UnityEngine;
public struct SelectWeaponArgs
{
    public CharacterEquipmentReborn character;
    public Slot targetSlot;

    public SelectWeaponArgs(CharacterEquipmentReborn ch, Slot slot)
    {
        character = ch;
        targetSlot = slot;
    }
}