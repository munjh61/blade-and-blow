using Game.Domain;
using UnityEngine;
// 플레이어 상태 변경을 위한 인터페이스
namespace Game.Domain
{
    public interface IPlayerAccessor
    {
        void ApplyImmediate(PlayerId id, PlayerInfoData d);

        void DropImmediate(PlayerId id);

        void ApplyDeath(PlayerId id, PlayerInfoData pd);

        void ShootImmediate(PlayerId id, Vector3 aimOriginPosition, Quaternion aimOriginRotation, float changeDuration);

        void AimImmediate(PlayerId id);
    }
}
