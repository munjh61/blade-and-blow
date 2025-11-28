using System;
using UnityEngine;

public enum Slot : byte
{
    Weapon = 0,
    Count
}

public class CharacterEquipment : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject[] weapons;
    public GameObject[] dropWeapons; // 드롭된 무기 프리팹 배열
    public GameObject nearObject;
    public string equippedWeapon = null;
    public int equippedWeaponIndex = -1;
    public Weapon activatedWeapon;

    [Header("Slots")]
    public int[] equippedSlots = new int[(int)Slot.Count];

    private void EquipWeaponByIndex(int index)
    {
        if (weapons == null || weapons.Length == 0) return;
        if (index < 0 || index >= weapons.Length) return;


        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].SetActive(i == index);
        }

        equippedWeapon = weapons[index] != null ? weapons[index].name : null;
        activatedWeapon = weapons[index] != null ? weapons[index].GetComponent<Weapon>() : null;
        equippedWeaponIndex = index;
        Destroy(nearObject);
    }

    public void EquipToSlot(Slot slot, int itemIndex)
    {
        int s = (int)slot;
        if ((uint)s >= equippedSlots.Length) return;

        equippedSlots[s] = itemIndex;
        Debug.Log($"[CharacterEquipment] slot[{slot}] <- itemIndex {itemIndex}");

        switch (slot)
        {
            case Slot.Weapon:
                EquipWeaponByIndex(itemIndex);
                break;

            default:
                Debug.Log($"슬롯 {slot}에 대한 장착 로직이 아직 구현되지 않음");
                break;
        }
    }

    public void UnequipWeapon() {
        weapons[equippedWeaponIndex].SetActive(false);
        equippedWeapon = null;
        activatedWeapon = null;
        equippedWeaponIndex = -1;
    }

    public void Interaction() {
        if (nearObject.tag == "Weapon")
        {
            Weapon weapon = nearObject.GetComponent<Weapon>();
            EquipToSlot(Slot.Weapon, (int)weapon.type);
        }
    }
}
