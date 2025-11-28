using Game.Domain;
using Game.Net;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 로컬 무기 선택(SelectedLoadout.OnChanged)을 네트워크 동기화 흐름에 연결하는 브릿지.
/// - 초기화 시 PlayerManagerPunBehaviour에서 RoomBus/Codec을 가져와 세팅함
/// - 로컬에서 장비 선택 변경 시:
///   * Core가 Master이면 즉시 Master_SetEquip 호출(권위 적용)
///   * Core가 Master가 아니면 Master에게 EquipRequest 이벤트 전송
/// </summary>
public sealed class LoadoutSyncBridge : MonoBehaviour
{
    [Header("Refs")]
    private PlayerManagerPunBehaviour _pm;
    private IRoomBus _bus;
    private INetCodec _codec;

    private bool _isSetup;

    // 중복 전송/적용 방지용 로컬 캐시
    private int _lastAppliedEquipId = int.MinValue;
    private int _lastSentEquipId    = int.MinValue;

    private void Awake()
    {
        _pm = GetComponent<PlayerManagerPunBehaviour>();
        if (_pm == null) return;

        // PlayerManager 준비 여부에 따라 즉시/지연 세팅
        if (_pm.IsInitialized) Setup();
        else _pm.OnInitialized += Setup;
    }

    private void OnDestroy()
    {
        if (_isSetup)
        {
            SelectedLoadout.OnChanged -= OnLocalEquipChanged;
            _pm.EquipApplied -= OnEquipAppliedByAuthority;
        }

        if (_pm != null)
            _pm.OnInitialized -= Setup;
    }

    /// <summary>
    /// PlayerManager로부터 RoomBus/Codec를 가져와 브릿지 연결을 완료.
    /// </summary>
    private void Setup()
    {
        if (_isSetup) return;

        _bus = _pm.RoomBus;
        if (_bus == null) return;

        _codec = _pm.Codec;
        if (_codec == null) return;

        SelectedLoadout.OnChanged += OnLocalEquipChanged;
        _pm.EquipApplied += OnEquipAppliedByAuthority;

        if (_pm.TryGetEquip(_pm.LocalId, out var cur)) _lastAppliedEquipId = cur;

        _isSetup = true;
    }

    /// <summary>
    /// 로컬에서 무기 선택이 바뀔 때 호출됨.
    /// - Core가 Master이면 즉시 적용
    /// - 그렇지 않으면 Master에게 EquipRequest 전송
    /// </summary>
    private void OnLocalEquipChanged(int equipId)
    {
        if (!_isSetup) return;
        if (!PhotonNetwork.InRoom || _pm == null) return;

        // Master 여부 동기화 전이면 무시(초기 진입 시점 안전장치)
        if (!_pm.MasterSynced) return;
        if (_pm.LocalId.Value <= 0) return;

        var myId = _pm.LocalId;
        // Core 캐시 기준 현재값 조회해 변화가 없으면 스킵
        if (_pm.TryGetEquip(myId, out var currentEquip) && currentEquip == equipId)
        {
            return;
        }

        // 로컬 캐시로 중복 전송/적용 방지
        if (equipId == _lastAppliedEquipId && equipId == _lastSentEquipId)
        {
            return;
        }

        if (_pm.CoreIsMaster)
        {
            // 권위가 로컬에 있을 때: 즉시 Core에 반영
            _pm.Master_SetEquip(myId, equipId);
            _lastAppliedEquipId = equipId;
        }
        else
        {
            // 권위가 원격(Master)일 때: 마스터에게 요청 전송
            if (_pm.MasterId.Value <= 0) return;
            _bus.SendTo(_pm.MasterId, NetEvt.EquipRequest, _codec.EncodeEquipId(equipId));
            _lastSentEquipId = equipId;
        }
    }

    private void OnEquipAppliedByAuthority(PlayerId id, int equipId)
    {
        if (_pm == null) return;
        if (id.Value != _pm.LocalId.Value) return;
        
        _lastAppliedEquipId = equipId;
    }
}
