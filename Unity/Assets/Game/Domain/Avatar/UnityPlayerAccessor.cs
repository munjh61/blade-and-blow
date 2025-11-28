using Game.Domain;
using System;
using UnityEngine;

public class UnityPlayerAccessor : IPlayerAccessor, IDisposable
{
    private readonly IPlayerStateView _stateView;

    public UnityPlayerAccessor(IPlayerStateView stateView)
    {
        _stateView = stateView;
        AvatarRegistry.OnRegistered += OnHandleRegistered;
    }

    public void Dispose()
    {
        AvatarRegistry.OnRegistered -= OnHandleRegistered;
    }

    // 포톤 네트워크에 전송하기 전 클라이언트에 선제 적용
    public void ApplyImmediate(PlayerId id, PlayerInfoData d)
    {
        if (!AvatarRegistry.TryGet(id.Value, out var h) || h.ce == null) return;
        h.ce.ApplyEquipImmediate(d.equipId);
    }

    public void DropImmediate(PlayerId id)
    {
        if (!AvatarRegistry.TryGet(id.Value, out var h) || h.ce == null) return;

        h.ce.ApplyEquipImmediate(-1);
    }

    public void AimImmediate(PlayerId id)
    {
        if (!AvatarRegistry.TryGet(id.Value, out var h) || h.ce == null) return;
        if (h.view.IsMine) return;

        h.tpc.aimProjectile();
    }
    public void ShootImmediate(PlayerId id, Vector3 aimOriginPosition, Quaternion aimOriginRotation, float changeDuration)
    {
        if (!AvatarRegistry.TryGet(id.Value, out var h) || h.ce == null) return;
        if (h.view.IsMine) return;
        //딱 놓는 순간 호출되므로 -> 발사체 발사
        GameObject aimOrigin = new GameObject("AimOrigin");
        aimOrigin.transform.SetPositionAndRotation(aimOriginPosition, aimOriginRotation);

        h.tpc.shootProjectile(aimOrigin.transform, changeDuration, id.Value);
        GameObject.Destroy(aimOrigin);
    }
    // 최초 등록 이벤트 발생 시 즉시 적용
    // 나중에 들어온 플레이어도 이미 존재하는 상태에 맞춰서 정합성 유지
    private void OnHandleRegistered(int actorNumber)
    {
        if (!_stateView.TryGet(actorNumber, out var d)) return;
        if (!AvatarRegistry.TryGet(actorNumber, out var h) || h.ce == null) return;

        h.ce.ApplyEquipImmediate(d.equipId);
    }

    // 네트워크에서 받은 정보를, 
    // Local에 적용.
    public void ApplyDeath(PlayerId id, PlayerInfoData pd)
    {
        if (!AvatarRegistry.TryGet(id.Value, out var h) || h.go == null) return;

        if (pd.hp <= 0) {

            //UnityEngine.Object.Destroy(h.go); 플레이어 오브젝트를 파괴하면 안됨.
            //h.go = null;

            // 캐릭터가 죽었을 때, 후처리 로직 호출
            h.tpc.OnDeath();
            
        }        
    }

   
   


}
