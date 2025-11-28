using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomWeaponManager : MonoBehaviour
{
    public static RoomWeaponManager Instance { get; private set; }

    [Header("Scene Weapons (auto if empty)")]
    public List<Weapon> allWeapons = new();

    public Weapon.Type? CurrentType { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (allWeapons == null || allWeapons.Count == 0)
            allWeapons = FindObjectsOfType<Weapon>(includeInactive: true).ToList();
    }


}
