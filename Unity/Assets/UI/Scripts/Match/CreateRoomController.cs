using UnityEngine;
using Cinemachine;
using StarterAssets;
using System.Diagnostics;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections.Generic;
using TMPro;

public class CreateRoomController : MonoBehaviour
{
    [Header("Menu Button References")]
    public TMP_Dropdown GameModeDropdown;
    public TMP_Dropdown MapDropdown;
    public TMP_InputField RoomNameInput;
    public TMP2DButton CreateButton;
    public TMP2DButton CancelButton;

    [Header("Weapon Selection")]
    public WeaponIconSelectionGroup weaponSelectionGroup;

    private void OnEnable()
    {
        // if (GameModeDropdown != null)
        //     GameModeDropdown.onClick.AddListener(OnJoinRoomClick);

        if (CreateButton != null)
            CreateButton.onClick.AddListener(OnCreateClick);

        if (CancelButton != null)
            CancelButton.onClick.AddListener(BackToPrivateMatchButton);
    }

    private void OnDisable()
    {
        // if (GameModeDropdown != null)
        //     GameModeDropdown.onClick.RemoveAllListeners();

        if (CreateButton != null)
            CreateButton.onClick.RemoveAllListeners();

        if (CancelButton != null)
            CancelButton.onClick.RemoveAllListeners();
    }

    public void OnCreateClick()
    {
        var roomConfig = CreateRoomConfiguration();
        if (roomConfig == null) return;

        // RoomCreator를 통해 방 생성
        var roomCreator = Game.Domain.Match.RoomCreator.Instance;
        if (roomCreator == null)
        {
            UnityEngine.Debug.LogError("[CreateRoomController] RoomCreator not found!");
            return;
        }

        roomCreator.CreatePrivateRoom(roomConfig);
    }

    /// <summary>
    /// UI 입력값들을 수집하여 RoomConfiguration 생성
    /// </summary>
    private RoomConfiguration CreateRoomConfiguration()
    {
        try
        {
            var config = new RoomConfiguration();

            // 방 기본 설정
            config.roomName = !string.IsNullOrEmpty(RoomNameInput?.text) ? RoomNameInput.text : "New Room";
            config.isPrivate = true; // CreateRoom은 항상 Private 방

            // 게임 모드 설정
            if (GameModeDropdown != null && GameModeDropdown.options.Count > GameModeDropdown.value)
            {
                config.gameMode = (GameMode)GameModeDropdown.value;
            }

            // 맵 설정
            if (MapDropdown != null && MapDropdown.options.Count > MapDropdown.value)
            {
                config.selectedMap = (MapType)MapDropdown.value;
            }

            // 무기 제한 설정
            CollectWeaponRestrictions(config);

            UnityEngine.Debug.Log($"[CreateRoomController] Room config created: {config.roomName}, Mode: {config.gameMode}, Map: {config.selectedMap}, Weapons: {config.allowedWeapons.Count}");

            return config;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[CreateRoomController] Failed to create room configuration: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// WeaponIconSelectionGroup에서 선택된 무기들을 수집
    /// </summary>
    private void CollectWeaponRestrictions(RoomConfiguration config)
    {
        if (weaponSelectionGroup != null)
        {
            var selectedWeapons = weaponSelectionGroup.GetSelectedWeaponTypes();

            if (selectedWeapons != null && selectedWeapons.Count > 0)
            {
                config.allowedWeapons = selectedWeapons;
                config.allowAllWeapons = false;
                UnityEngine.Debug.Log($"[CreateRoomController] Selected weapons: {string.Join(", ", selectedWeapons)}");
            }
            else
            {
                // 아무것도 선택되지 않았으면 모든 무기 허용
                config.allowAllWeapons = true;
                UnityEngine.Debug.Log("[CreateRoomController] No weapons selected, allowing all weapons");
            }
        }
        else
        {
            // WeaponSelectionGroup이 없으면 기본값 사용
            config.allowAllWeapons = true;
            UnityEngine.Debug.LogWarning("[CreateRoomController] WeaponIconSelectionGroup not assigned");
        }
    }

    public void BackToPrivateMatchButton()
    {
        UIManager.Instance.Open(MenuId.PrivateMatchMenus);
    }

}
