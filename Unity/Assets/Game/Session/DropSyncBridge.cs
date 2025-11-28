using Game.Domain;
using Game.Net;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 드랍/픽업 의도를 네트워크 흐름에 싱크:
///  - 마스터면: DropManager(Master*) 직접 호출(즉시 수행)
///  - 비마스터면: 마스터에게 버스로 요청 전송
/// </summary>
public sealed class DropSyncBridge : MonoBehaviour
{
    private PlayerManagerPunBehaviour _pm;    // 마스터/로컬/버스/코덱 정보
    private DropManagerPunBehaviour _dm;    // 드랍 수행 (Core 호출)

    private IRoomBus _bus;
    private INetCodec _codec;
    private bool _setup;

    private void Awake()
    {
        _pm = GetComponent<PlayerManagerPunBehaviour>();
        _dm = GetComponent<DropManagerPunBehaviour>();

        if (_pm == null) { Debug.LogError("[DropSyncBridge] PlayerManagerPunBehaviour missing."); return; }
        if (_dm == null) { Debug.LogError("[DropSyncBridge] DropManagerPunBehaviour missing."); return; }

        if (_pm.IsInitialized) Setup();
        else _pm.OnInitialized += Setup;
    }

    private void OnDestroy()
    {
        if (_setup)
        {
            DropSignals.OnRequested -= OnDropRequested;
            PickupSignals.OnRequested -= OnPickupRequested;
        }
        if (_pm != null) _pm.OnInitialized -= Setup;
    }

    private void Setup()
    {
        if (_setup) return;

        _bus = _pm.RoomBus;
        _codec = _pm.Codec;
        if (_bus == null || _codec == null)
        {
            Debug.LogError("[DropSyncBridge] Bus/Codec is null.");
            return;
        }

        DropSignals.OnRequested += OnDropRequested;
        PickupSignals.OnRequested += OnPickupRequested;

        _setup = true;
        Debug.Log("[DropSyncBridge] Setup complete");
    }

    // ============= 드랍 의도 =============
    private void OnDropRequested(string weaponKey, Vector3 pos, Quaternion rot)
    {
        if (!_setup || !PhotonNetwork.InRoom) return;

        int actor = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
        if (actor <= 0) return;

        // 마스터면 즉시 수행
        if (_pm.CoreIsMaster)
        {
            _dm.Master_Spawn(weaponKey, pos, rot);
        }
        else
        {
            if (_pm.MasterId.Value <= 0) return;
            _bus.SendTo(_pm.MasterId, NetEvt.DropRequest, _codec.EncodeDropRequest(actor));
        }
    }

    // ============= 픽업 의도 =============
    private void OnPickupRequested(ulong token, int equipId)
    {
        if (!_setup || !PhotonNetwork.InRoom) return;

        int actor = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
        if (actor <= 0) return;

        // 마스터면 즉시 수행(드랍 제거 브로드캐스트) + 장비는 기존 Loadout 경로로 이미 전송됨
        if (_pm.CoreIsMaster)
        {
            _dm.Master_HandlePickup(actor, token);
        }
        else
        {
            if (_pm.MasterId.Value <= 0) return;
            _bus.SendTo(_pm.MasterId, NetEvt.PickupRequest, _codec.EncodePickupRequest(actor, token, equipId));
        }
    }
}
