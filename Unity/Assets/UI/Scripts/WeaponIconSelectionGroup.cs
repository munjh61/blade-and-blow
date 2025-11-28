using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class WeaponIconSelectionGroup : MonoBehaviour
{
    private readonly List<WeaponIconSelectable> weapons = new();

    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }
    public IntEvent onSelectionChanged = new();

    public void Register(WeaponIconSelectable weapon)
    {
        if (weapon == null || weapons.Contains(weapon)) return;
        weapons.Add(weapon);
        weapon.group = this;
    }

    public void Unregister(WeaponIconSelectable weapon)
    {
        if (weapon == null) return;
        weapons.Remove(weapon);
    }

    public void OnSelectionChanged()
    {
        int selectedCount = GetSelectedWeapons().Count;
        onSelectionChanged.Invoke(selectedCount);
    }

    public List<WeaponIconSelectable> GetSelectedWeapons()
    {
        return weapons.FindAll(w => w.IsSelected());
    }

    public void SelectAllWeapons()
    {
        foreach (var weapon in weapons) weapon.SetSelected(true);

        OnSelectionChanged();
    }

    public void DeselectAllWeapons()
    {
        foreach (var weapon in weapons) weapon.SetSelected(false);

        OnSelectionChanged();
    }

    public List<Weapon.Type> GetSelectedWeaponTypes()
    {
        return weapons.FindAll(w => w.IsSelected())
                      .Select(w => w.weaponType)
                      .ToList();
    }
}
