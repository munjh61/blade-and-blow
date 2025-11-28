using Game.Domain;
using UnityEngine;

namespace Game.Net
{
    public interface INetCodec
    {
        // 게임 전체 스냅샷
        object EncodeSnapshot(GameSnapshotData s);
        GameSnapshotData DecodeSnapshot(object payload);

        // 플레이어 데이터
        object EncodeDelta(PlayerInfoData d);
        PlayerInfoData DecodeDelta(object payload);

        // 장비 인덱스 데이터
        object EncodeEquipId(int equipId);
        int DecodeEquipId(object payload);

        // 장비 착용 데이터
        object EncodeEquipDelta(int actor, int equipId);
        (int actor, int equipId) DecodeEquipDelta(object payload);

        // 킬 이벤트 데이터
        object EncodeKillEvent(Kill kill);
        Kill DecodeKillEvent(object payload);

        // 마스터 → 전체 : 드랍 스폰(토큰 주입)
        object EncodeDropSpawned(ulong token, string weaponKey, Vector3 pos, Quaternion rot);
        (ulong token, string weaponKey, Vector3 pos, Quaternion rot) DecodeDropSpawned(object payload);

        // 드랍 요청 데이터
        object EncodeDropRequest(int actor);
        int DecodeDropRequest(object payload);

        // 조준 요청 데이터
        object EncodeAimWeaponContext(int actor);

        int DecodeAimWeaponContext(object payload);
        // 발사 요청 데이터
        object EncodeActorWeaponContext(int actor, Vector3 aimOriginPosition, Quaternion aimOriginRotation, float changeDuration);

        (int actor, float changeDuration, Vector3 aimOriginPosition, Quaternion aimOriginRotation) DecodeActorWeaponContext(object payload);

        // 클라 → 마스터 : 픽업 요청(어떤 토큰을, 어떤 인덱스로 장착하려는지)
        object EncodePickupRequest(int actor, ulong token, int equipId);
        (int actor, ulong token, int equipId) DecodePickupRequest(object payload);

        // 마스터 → 전체 : 드랍 제거(토큰 기반)
        object EncodeDropRemoved(ulong token);
        ulong DecodeDropRemoved(object payload);

        // 클라 → 마스터 : 스태미너 소모
        object EncodeStaminaIntent(byte intentType, int payload/*cost 등*/);
        (byte intentType, int payload) DecodeStaminaIntent(object payload);

        // 마스터 → 클라: 스태미너 상태 전파
        object EncodeStaminaDelta(int actor, int stamina);
        (int actor, int stamina) DecodeStaminaDelta(object payload);
        
        // 클라이언트 -> 마스터: 공격 Request
        object EncodeHitRequest(int attackerId, int targetId, int damage);
        (int attackerId, int targetId, int damage) DecodeHitRequest(object payload);

        // 마스터 → 클라: 체력 상태 전파
        object EncodeHealthDelta(int actor, int hp);
        (int actor, int hp) DecodeHealthDelta(object payload);

        // 마스터 -> 클라: 생존자 목록 전파
        object EncodeAlivePlayers(int[] alivePlayerActors);
        int[] DecodeAlivePlayers(object payload);

    }
}
