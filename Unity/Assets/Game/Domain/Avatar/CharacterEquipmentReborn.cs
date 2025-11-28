using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterEquipmentReborn : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject[] weapons;
    public Weapon activatedWeapon;
    public string equippedWeapon = null;

    [Header("Runtime (Read-only)")]
    [SerializeField, Tooltip("현재 활성화된 무기 인덱스(-1 = 없음)")]
    private int _equippedWeaponIndex = -1;

    private readonly Dictionary<string, int> _weaponIndexByKey = new(StringComparer.OrdinalIgnoreCase);

    [Header("Slots")]
    public int[] equippedSlots = new int[(int)Slot.Count];

    public int GetEquippedId() => _equippedWeaponIndex;

    private void Awake() => RebuildWeaponCache();

    private void OnValidate()
    {
        // 슬롯 배열 길이 보정
        if (equippedSlots == null || equippedSlots.Length != (int)Slot.Count)
            equippedSlots = new int[(int)Slot.Count];

        RebuildWeaponCache();
    }

    private void RebuildWeaponCache()
    {
        _weaponIndexByKey.Clear();

        if (weapons == null || weapons.Length == 0) return;

        for (int i = 0; i < weapons.Length; i++)
        {
            var go = weapons[i];
            if (!go) continue;

            string key = NormalizeWeaponKey(go.name);

            if (!string.IsNullOrEmpty(key))
                _weaponIndexByKey[key] = i;
        }
    }

    private static string NormalizeWeaponKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var s = raw;

        if (s.StartsWith("drop_", StringComparison.OrdinalIgnoreCase))
            s = s.Substring("drop_".Length);

        var us = s.IndexOf('_');
        if (us >= 0 && us < s.Length - 1 && IsAllDigits(s.AsSpan(0, us)))
            s = s.Substring(us + 1);

        return s.Trim().ToLowerInvariant();
    }

    private static bool IsAllDigits(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
            if (!char.IsDigit(span[i])) return false;
        return true;
    }

    public int FindWeaponIndexByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return -1;

        var key = NormalizeWeaponKey(name);
        return _weaponIndexByKey.TryGetValue(key, out var idx) ? idx : -1;
    }

    public void ApplyEquipByNameImmediate(string nameOrNull)
    {
        if (string.IsNullOrEmpty(nameOrNull)) { ApplyEquipImmediate(-1); return; }
        int idx = FindWeaponIndexByName(nameOrNull);
        ApplyEquipImmediate(idx);
    }

    public void ApplyEquipImmediate(int equipId)
    {
        if (_equippedWeaponIndex == equipId) return;
        EquipToSlot(Slot.Weapon, equipId);
    }

    private void EquipWeaponByIndex(int index)
    {
        if (weapons == null || weapons.Length == 0)
        {
            _equippedWeaponIndex = -1;
            activatedWeapon = null;
            equippedWeapon = null;
            return;
        }

        if (index < 0 || index >= weapons.Length)
        {
            // 범위를 벗어나면 전부 끄고 해제
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null) weapons[i].SetActive(false);
            }
            _equippedWeaponIndex = -1;
            activatedWeapon = null;
            equippedWeapon = null;
            return;
        }


        for (int i = 0; i < weapons.Length; i++)
        {
            var go = weapons[i];
            if (!go)  continue;
            bool willActive = (i == index);
            go.SetActive(willActive);
        }

        _equippedWeaponIndex = index;
        activatedWeapon = weapons[index] != null ? weapons[index].GetComponent<Weapon>() : null;
        equippedWeapon = weapons[index] != null ? weapons[index].name : null;
    }

    public void EquipToSlot(Slot slot, int itemIndex)
    {
        int s = (int)slot;
        if (equippedSlots == null || s < 0 || s >= equippedSlots.Length) return;

        equippedSlots[s] = itemIndex;

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
}
