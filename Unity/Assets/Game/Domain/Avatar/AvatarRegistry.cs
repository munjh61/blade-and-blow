using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using StarterAssets;

// 클라이언트 로컬 캐릭터 오브젝트 매핑용 Registry
public static class AvatarRegistry
{
    // 캐릭터 오브젝트 매핑 클래스
    public class Handle
    {
        public GameObject go;
        public PhotonView view;
        public CharacterEquipmentReborn ce;
        public ThirdPersonControllerReborn tpc;
    }

    // 전체 플레이어 정보
    private static readonly Dictionary<int, Handle> _byActor = new Dictionary<int, Handle>();

    // race 방지용 락 오브젝트
    private static readonly object _gate = new object();

    // 등록시 전파를 위한 이벤트
    public static event Action<int> OnRegistered;

    // 캐릭터 오브젝트 등록
    public static void Register(int actorNumber, Handle h)
    {
        lock (_gate) _byActor[actorNumber] = h;
        OnRegistered?.Invoke(actorNumber);
    }

    // 캐릭터 오브젝트 삭제
    public static void Unregister(int actorNumber, Handle h)
    {
        lock (_gate)
        {
            if (_byActor.TryGetValue(actorNumber, out var cur) && ReferenceEquals(cur, h)) _byActor.Remove(actorNumber);
        }
    }

    // thread-safe하게 읽기
    // if(--)형식으로 사용하고 특정 캐릭터 오브젝트 정보에 접근 가능
    public static bool TryGet(int actorNumber, out Handle h)
    {
        lock (_gate) return _byActor.TryGetValue(actorNumber, out h);
    }
}
