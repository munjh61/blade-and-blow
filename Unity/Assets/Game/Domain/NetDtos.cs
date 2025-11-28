using System;
using UnityEngine;

// 정보 전달용 DTO 클래스
namespace Game.Domain
{
    // 플레이어 정보
    [Serializable]
    public struct PlayerInfoData 
    {
        public int actor;
        public string name;
        public string userId;
        public int hp;
        public int stamina;
        public TeamId team;
        public int equipId;
        public int kill;
        public int death;
        public int totDamage;
        public int consecutiveKill;
        public int lastKillInterval;
    }   

    // 게임 전체 스냅샷
    [Serializable]
    public struct GameSnapshotData
    {
        public PlayerInfoData[] players;
        public int[] alivePlayerActors;
    }

    // 플레이어 상태(위치 값)
    [Serializable]
    public struct PlayerState
    {
        public Vector3 position;
        public Vector3 velocity;
        public Quaternion rotation;
    }

    [Serializable]
    public enum StaminaIntentType : byte
    {
        StartSprint = 1,
        StopSprint = 2,
        TryAction = 3,
    }

    [Serializable]
    public enum TeamId : byte
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Spectator = 99,
    }

    [Serializable]
    public struct Kill
    {
        public int killerActor;
        public string killerId;
        public string killerName;
        public TeamId killerTeam;
        public int victimActor;
        public string victimId;
        public string victimName;
        public TeamId victimTeam;
        public int weaponId;
        public string weapon;
    }

    // 이벤트 구분용
    // PUN 은 1~199 까지만 커스텀 가능
    public enum NetEvt : byte
    {
        FullSnapshot    = 1,
        PlayerDelta     = 2,
        EquipRequest    = 3,
        EquipDelta      = 4,
        KillEvent       = 5,
        DropRequest     = 10,
        DropSpawned     = 11,
        PickupRequest   = 12,
        DropRemoved     = 13,
        DropDelta       = 14,
        PickupDelta     = 15,
        HitRequestDelta = 16,
        ShootRequest    = 17,
        ShootDelta      = 18,
        AimRequest      = 19,
        AimDelta        = 20,
        PlayerStateUpdate = 21,
        InstantKillRequest = 22,
        StaminaIntent   = 101,
        StaminaDelta    = 102,
        HealthDelta     = 103,
        AlivePlayersUpdate = 104, // 생존자 목록 동기화 이벤트
        PlayerIntent    = 197,
        GlobalDotTick   = 198,
        GameOver        = 199,
    }

    // 플레이어 Id,
    // Player.ActorNumber로 불러올 수 있지만
    // 안전 타입을 위한 래퍼 구조
    // + Photon 결합도를 낮추기 위함
    public readonly struct PlayerId
    {
        public readonly int Value;
        public PlayerId(int v) => Value = v;
        public static implicit operator int(PlayerId id) => id.Value;
        public override string ToString() => Value.ToString();
    }
}
