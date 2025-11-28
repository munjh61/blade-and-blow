using Game.Domain;
using Game.Net;
using Game.Net.Pun;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public sealed class DropManagerPunBehaviour : MonoBehaviourPunCallbacks
{
    public DropFactory factory;
    
    public PunRoomBus roomBus;
    private INetCodec _codec;
    private DropManagerCore _core;
    private PlayerManagerPunBehaviour _pm;

    public IRoomBus RoomBus => roomBus;
    public INetCodec Codec => _codec;

    private void Awake()
    {
        if (roomBus == null) roomBus = GetComponent<PunRoomBus>();
        _codec = new PunHashtableCodec();

        _pm = GetComponent<PlayerManagerPunBehaviour>();

        _core = new DropManagerCore(roomBus, _codec);
        _core.OnDropSpawned += HandleDropSpawned;
        _core.OnDropRemoved += HandleDropRemoved;

        _core.SetResolvers(
        getPlayerPos: id =>
        {
            if (AvatarRegistry.TryGet(id.Value, out var h) && h?.go)
                return h.go.transform.position + h.go.transform.forward * 1f + Vector3.up * 0.5f;
            return Vector3.zero;
        },
        getWeaponKeyOfActor: actor =>
        {
            // 1) PlayerCore 캐시에서 equipId 최신값 획득
            int equipId = -1;
            if (_pm != null && _pm.TryGetEquip(new PlayerId(actor), out var eq))
                equipId = eq;
            if (equipId < 0) { Debug.Log($"[DropManager] No Player Cache equipId={equipId}"); return null; }

            // 2) 아바타가 로드되어 있으면 CE 통해 인덱스 -> key (이름) 변환
            if (AvatarRegistry.TryGet(actor, out var h) && h?.ce != null)
            {
                var arr = h.ce.weapons;
                if (equipId >= 0 && equipId < arr.Length && arr[equipId] != null)
                    return arr[equipId].name; // ex) "sword"
            }
            return null;
        });

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_core != null)
        {
            _core.OnDropSpawned -= HandleDropSpawned;
            _core.OnDropRemoved -= HandleDropRemoved;
            _core.Dispose();
        }
    }

    private void HandleDropSpawned(ulong token, string key, Vector3 pos, Quaternion rot)
    {
        var go = factory ? factory.SpawnDrop(key, pos, rot) : null;
        if (!go) return;

        if (go.TryGetComponent<DropHandle>(out var dh))
        {
            dh.Token = token;
            dh.WeaponName = key;
        }
        else
        {
            Debug.LogError("[Drop] DropHandle missing on spawned drop prefab");
        }
    }

    private void HandleDropRemoved(ulong token) => DropRegistry.RemoveAndDestroy(token);

    public void Master_Spawn(string weaponKey, Vector3 pos, Quaternion rot) => _core.Master_Spawn(weaponKey, pos, rot);
    public void Master_HandlePickup(int actor, ulong token) => _core.Master_HandlePickup(actor, token);

    private void SyncMasterFlag() => _core.SetMaster(PhotonNetwork.IsMasterClient);
    public override void OnJoinedRoom() => SyncMasterFlag();
    public override void OnLeftRoom() => _core.SetMaster(false);
    public override void OnMasterClientSwitched(Player newMasterClient) => SyncMasterFlag();
}