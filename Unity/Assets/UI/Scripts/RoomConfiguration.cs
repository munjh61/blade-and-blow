using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomConfiguration
{
    [Header("Room Basic Settings")]
    public string roomName;
    public string roomCode;
    public int maxPlayers = 10;
    public bool isPrivate = true;
    public string password = "";

    [Header("Game Settings")]
    public GameMode gameMode = GameMode.Deathmatch;
    public MapType selectedMap = MapType.Default;

    [Header("Weapon Restrictions")]
    public List<Weapon.Type> allowedWeapons = new();
    public bool allowAllWeapons = true;

    public RoomConfiguration()
    {
        // 기본적으로 모든 무기 허용
        allowedWeapons.Add(Weapon.Type.Sword);
        allowedWeapons.Add(Weapon.Type.Bow);
        allowedWeapons.Add(Weapon.Type.Wand);
    }

    public bool IsWeaponAllowed(Weapon.Type weapon) => allowAllWeapons || allowedWeapons.Contains(weapon);
    public string GenerateRoomCode() => UnityEngine.Random.Range(100000, 999999).ToString();
}

[System.Serializable]
public enum GameMode
{
    Deathmatch,
    TeamDeathmatch,
    CaptureTheFlag,
    KingOfTheHill
}

[System.Serializable]
public enum MapType
{
    Default,
    Arena,
    Forest,
    Castle
}