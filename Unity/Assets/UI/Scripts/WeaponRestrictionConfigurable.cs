using UnityEngine;

public class WeaponRestrictionConfigurable : RoomConfigurable
{
    [Header("Weapon Selection")]
    public WeaponIconSelectionGroup weaponGroup;

    public override void ApplyToConfiguration(RoomConfiguration config)
    {
        // config.allowedWeapons = weaponGroup.GetSelectedWeaponTypes();
    }

    public override void LoadFromConfiguration(RoomConfiguration config)
    {
        // 설정에 따라 무기 선택 상태 업데이트       
        // weaponGroup.SetAllowedWeapons(config.allowedWeapons);
    }
}