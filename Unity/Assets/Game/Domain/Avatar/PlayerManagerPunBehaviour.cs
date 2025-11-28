using Game.Domain;
using Game.Net;
using Game.Net.Pun;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UI.Play;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Photon 기반 플레이어 매니저.
/// 
/// - Photon 콜백(MonoBehaviourPunCallbacks)을 받아서 내부 Core(PlayerManagerCore)로 전달.
/// - IRoomBus, INetCodec, UnityPlayerAccessor 등을 초기화하고 Core와 연결.
/// - 마스터 클라이언트 여부를 Core에 동기화.
/// - 플레이어 입장/퇴장 이벤트를 Core에 위임.
/// </summary>
//[DefaultExecutionOrder(-100)]
public class PlayerManagerPunBehaviour : MonoBehaviour
{
    public static PlayerManagerPunBehaviour Instance { get; private set; }

    public PunRoomBus roomBus;
    private PhotonNetworkManager _photonNet;
    private PlayerManagerCore _core;
    private INetCodec _codec;
    private UnityPlayerAccessor _accessor;
    private KillFeedController _killFeed;
    private Coroutine _waitKillFeed;

    /// <summary>
    /// Core 기준에서의 마스터 여부.
    /// (PhotonNetwork.IsMasterClient를 직접 노출하지 않고 Core와 동기화)
    /// </summary>
    public bool CoreIsMaster => _core?.IsMaster ?? false;

    /// <summary>
    /// 마스터 여부 동기화가 완료되었는지 여부.
    /// </summary>
    public bool MasterSynced { get; private set; }

    public IRoomBus RoomBus => roomBus;
    public INetCodec Codec => _codec;

    public PlayerId LocalId { get; private set; } = new PlayerId(-1);
    public PlayerId MasterId { get; private set; } = new PlayerId(-1);

    public bool TryGetAvatar(int actor, out AvatarRegistry.Handle h)
        => AvatarRegistry.TryGet(actor, out h);

    public bool TryGetLocalAvatar(out AvatarRegistry.Handle h)
    {
        h = default;
        if (LocalId.Value <= 0) return false;
        return AvatarRegistry.TryGet(LocalId.Value, out h);
    }

    public bool TryGetState(int actor, out PlayerInfoData d)
    {
        if (_core == null)
        {
            d = default;
            return false;
        }
        return _core.TryGet(actor, out d);
    }

    public bool TryGetEquip(PlayerId id, out int equipId)
    {
        equipId = -1;
        if (_core == null) return false;
        if (_core.TryGet(id.Value, out var d))
        {
            equipId = d.equipId;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 초기화 완료 이벤트.
    /// 외부 컴포넌트가 구독해서 Core 준비 완료 시점을 알 수 있음.
    /// </summary>
    public event Action OnInitialized;
    public bool IsInitialized { get; private set; }

    public event Action<PlayerId, int> EquipApplied;
    private Action<PlayerId, bool> _authChangedHandler;

    [Header("Camera Binding")]
    [SerializeField] private string previewSceneName = "MainScene";

    // 플레이어 오브젝트에 권한 설정
    private void ApplyAuthorityToAvatar(int actor, bool isMine)
    {
        if (!AvatarRegistry.TryGet(actor, out var h) || h?.go == null) return;
        var go = h.go;

        bool isPreview = SceneManager.GetActiveScene().name == previewSceneName;
        bool allowControls = isMine || isPreview;

        var tpc = h.go.GetComponent<StarterAssets.ThirdPersonControllerReborn>();
        if (tpc != null) tpc.SetAuthority(allowControls);

        var nrd = go.GetComponent<NetReplicationDriver>();
        if (nrd != null) nrd.SetWriteAuthority(allowControls);

        var binder = go.GetComponent<CameraBinder>();
        if (binder != null) binder.ApplyOwnerGate(allowControls);
    }

    private void Awake()
    {
        // PlayerManagerPunBehaviour 가 생성한 Instance는 하나로만 관리
        if (Instance != null && Instance != this)
        {
            return;
        }
        Instance = this;

        if (roomBus == null) roomBus = GetComponent<PunRoomBus>();
        _codec = new PunHashtableCodec();
        _core = new PlayerManagerCore(roomBus, _codec, null);
        _accessor = new UnityPlayerAccessor(_core);
        _core.SetAccessor(_accessor);

        _core.OnEquipApplied += HandleEquipApplied;
        _core.OnPlayerDied += HandlePlayerDied;
        _authChangedHandler = (id, isMine) =>
        {
            ApplyAuthorityToAvatar(id.Value, isMine);
        };
        _core.OnAuthorityChanged += _authChangedHandler;

        IsInitialized = true;
        OnInitialized?.Invoke();

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        AvatarRegistry.OnRegistered += OnAvatarRegistered;
        _photonNet = PhotonNetworkManager.Instance;

        if (_photonNet != null)
        {
            _photonNet.LocalPlayerReady += OnLocalPlayerReady;
            _photonNet.LocalPlayerPropertiesUpdated += OnLocalPropsChanged;
            _photonNet.MasterClientSwitched += OnMasterChanged;
            _photonNet.JoinedRoom += OnJoinedRoom;
            _photonNet.LeftRoom += OnLeftRoom;
            _photonNet.PlayerLeftRoomEvent += OnPlayerLeftRoom;
        }

        if (_killFeed == null)
            _waitKillFeed = StartCoroutine(WaitKillFeed());
    }

    private void OnDisable()
    {
        AvatarRegistry.OnRegistered -= OnAvatarRegistered;

        if (_photonNet != null)
        {
            _photonNet.LocalPlayerReady -= OnLocalPlayerReady;
            _photonNet.LocalPlayerPropertiesUpdated -= OnLocalPropsChanged;
            _photonNet.MasterClientSwitched -= OnMasterChanged;
            _photonNet.JoinedRoom -= OnJoinedRoom;
            _photonNet.LeftRoom -= OnLeftRoom;
            _photonNet.PlayerLeftRoomEvent -= OnPlayerLeftRoom;
        }
    }

    private void Start()
    {
        SyncMasterFlag();
    }

    private void OnDestroy()
    {
        if (_core != null)
        {
            _core.OnEquipApplied -= HandleEquipApplied;
            _core.OnAuthorityChanged -= _authChangedHandler;
            _core.OnPlayerDied -= HandlePlayerDied;
            _core?.Dispose();
        }

        _accessor?.Dispose();
    }

    // 대충 빠르게 구현, 나중에 분리 해야됨
    IEnumerator WaitKillFeed()
    {
        while ((_killFeed = FindFirstObjectByType<KillFeedController>(FindObjectsInactive.Include)) == null)
            yield return null;
        _waitKillFeed = null;
    }

    private void FixedUpdate()
    {
        if (CoreIsMaster && _core != null)
        {
            _core.Master_Tick(Time.realtimeSinceStartup);
        }
    }

    private void OnAvatarRegistered(int actor)
    {
        var id = new PlayerId(actor);

        bool inRoom = PhotonNetwork.InRoom;
        int localNr = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
        bool isMine = (!inRoom) || (actor == localNr);
        bool forceLocalInMain = SceneManager.GetActiveScene().name == previewSceneName;
        _core.SetMine(id, isMine || forceLocalInMain);
        ApplyAuthorityToAvatar(actor, _core.IsMine(id));
    }

    private void HandlePlayerDied(Kill e)
    {
        bool isTeamKill = e.killerTeam != TeamId.None && e.killerTeam == e.victimTeam;
        KillEvent key = new KillEvent
        {
            killerName = e.killerName,
            victimName = e.victimName,
            weaponIcon = _killFeed.weaponIcons[e.weaponId],
            teamKill = isTeamKill,
            killerTeam = e.killerTeam,
            victimTeam = e.victimTeam
        };
        _killFeed.Show(key);
    }


    private void HandleEquipApplied(PlayerId id, int equip) => EquipApplied?.Invoke(id, equip);

    /// <summary>
    /// Photon의 IsMasterClient 플래그를 Core에 동기화.
    /// </summary>
    private void SyncMasterFlag()
    {
        var m = PhotonNetwork.IsMasterClient;
        _core.SetMaster(m);
        MasterSynced = true;
    }

    /// <summary>
    /// 로컬 플레이어 등록 (최초 진입 시).
    /// </summary>
    public void RegisterLocal(PlayerId id, PlayerInfoData initial)
    {
        _core.Local_Register(id, initial);
    }

    public void EnsureLocalDefaults()
    {
        var id = LocalId.Value;      // ← LocalId는 PM이 보유
        if (id > 0) _core.EnsureDefaults(id);
    }

    public bool TryGetLocalState(out PlayerInfoData d)
    {
        var id = LocalId.Value;
        if (id > 0) return _core.TryGetState(id, out d);
        d = default;
        return false;
    }

    /// <summary>
    /// 마스터 권한으로 특정 플레이어의 장비 상태를 설정.
    /// </summary>
    public void Master_SetEquip(PlayerId id, int equip) => _core.Master_SetEquip(id, equip);
    public void Master_SetAlive(PlayerId id) => _core.Master_SetAlive(id);
    public void Master_SyncCacheAndAlive() => _core.Master_SyncCacheAndAlive();

    public void Master_ArmSuddenDeath(double dotAtSec, int intervalMs, int damage, double nowSec)
        => _core?.Master_ArmSuddenDeath(dotAtSec, intervalMs, damage, nowSec);

    public void Master_TickSuddenDeath(double nowSec)
        => _core?.Master_TryApplyGlobalDot(nowSec);


    public void Local_SendSprint(bool start)
    {
        if (_core == null || LocalId.Value <= 0) return;
        _core.Local_SendStaminaIntent(LocalId, start ? StaminaIntentType.StartSprint
                                                     : StaminaIntentType.StopSprint, 0);
    }

    // Ctrl 구르기 등 코스트 소비 시도 인텐트
    public void Local_TrySpendStamina(int cost)
    {
        if (_core == null || LocalId.Value <= 0) return;
        _core.Local_SendStaminaIntent(LocalId, StaminaIntentType.TryAction, cost);
    }

    public event Action<PlayerId, int> StaminaApplied   // UI가 구독하기 쉽게 이벤트 노출
    {
        add { _core.OnStaminaApplied += value; }
        remove { _core.OnStaminaApplied -= value; }
    }

    // =============================
    // 네트워크 상으로 클라이언트 -> 마스터(혹은 마스터가 바로 처리) 하는 작업
    // =============================
    public void Master_RequestHit(PlayerId targetId, int damage, int attackerId) => _core.Master_RequestHit(targetId, damage, attackerId);

    // =============================
    // Photon 콜백 → Core 전달부
    // =============================

    //활 조준 시 척추를 비틀기 위함
    public void Local_RequestAim()
    {
        _core.Local_RequestAim(LocalId);
    }

    // 즉사 요청 -> 마스터에게 요청하거나, 마스터면 바로 처리
    public void RequestInstantKill(string causeOfDeath)
    {
        if (_core == null || LocalId.Value <= 0 || string.IsNullOrEmpty(causeOfDeath)) return;

        _core.Local_RequestInstantKill(LocalId, causeOfDeath);
    }
    public void Local_RequestShoot(Transform aimOrigin, float changeDuration)
    {
        _core.Local_RequestShoot(LocalId, aimOrigin.position, aimOrigin.rotation, changeDuration);
    }
    // public override void OnPlayerEnteredRoom(Player newPlayer)
    // {
    //     // 마스터일 경우에만 신규 플레이어 처리
    //     //if (!CoreIsMaster) return;

    //     //_core.Master_OnPlayerJoined(new PlayerId(newPlayer.ActorNumber));
    // }

    // public override void OnJoinedRoom()
    // {
    //     // 방 참가 시 MasterClient 상태를 동기화
    //     SyncMasterFlag();
    //     LocalId = new PlayerId(PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
    //     MasterId = new PlayerId(PhotonNetwork.MasterClient?.ActorNumber ?? -1);

    //     if (LocalId.Value > 0)
    //     {
    //         // Core에 내 소유권 알림 -> OnAuthorityChanged 발생
    //         _core.SetMine(LocalId, true);

    //         // 혹시 모를 레이스 대비 - 즉시 한 번 더 강제 적용
    //         if (AvatarRegistry.TryGet(LocalId.Value, out var h) && h?.go)
    //         {
    //             ApplyAuthorityToAvatar(LocalId.Value, true);
    //         }
    //     }
    // }

    // public override void OnLeftRoom()
    // {
    //     // 방을 나가면 MasterClient 플래그 해제
    //     _core.SetMaster(false);
    //     MasterSynced = false;
    //     LocalId = new PlayerId(-1);
    //     MasterId = new PlayerId(-1);

    //     _core.ClearAuthorities();
    // }

    // public override void OnPlayerLeftRoom(Player otherPlayer)
    // {
    //     // 플레이어 퇴장 시 Core에서 제거
    //     _core.Unregister(new PlayerId(otherPlayer.ActorNumber));
    // }

    // public override void OnMasterClientSwitched(Player newMasterClient)
    // {
    //     // 마스터 클라이언트 변경 시 재동기화
    //     SyncMasterFlag();
    //     MasterId = new PlayerId(newMasterClient?.ActorNumber ?? -1);
    // }

    private void OnLeftRoom()
    {
        _core.SetMaster(false);
        MasterSynced = false;
        LocalId = new PlayerId(-1);
        MasterId = new PlayerId(-1);

        _core.ClearAuthorities();
    }

    private void OnPlayerLeftRoom(Player otherPlayer)
    {
        _core.Unregister(new PlayerId(otherPlayer.ActorNumber));
    }
    private void OnJoinedRoom()
    {
        SyncMasterFlag();
        _core.ClearCache();   // 혹시 모를 잔재 제거
        LocalId = new PlayerId(_photonNet.LocalPlayer?.ActorNumber ?? -1);
        MasterId = new PlayerId(_photonNet.MasterClient?.ActorNumber ?? -1);

        if (LocalId.Value > 0)
        {
            // Core에 내 소유권 알림 -> OnAuthorityChanged 발생
            _core.SetMine(LocalId, true);

            // 혹시 모를 레이스 대비 - 즉시 한 번 더 강제 적용
            if (AvatarRegistry.TryGet(LocalId.Value, out var h) && h?.go)
            {
                ApplyAuthorityToAvatar(LocalId.Value, true);
            }
        }
    }
    
    private void OnLocalPlayerReady(Player lp, int actor)
    {
        Debug.Log($"[MySys] Local ready: actor={actor}, userId={lp?.UserId}");
        LocalId = new PlayerId(actor);
    }

    private void OnLocalPropsChanged(Hashtable changed)
    {
        Debug.Log($"[MySys] Local props changed: {changed.ToStringFull()}");
    }

    private void OnMasterChanged(Player newMaster)
    {
        Debug.Log($"[MySys] Master switched -> actor={newMaster?.ActorNumber}");
        SyncMasterFlag();
        MasterId = new PlayerId(newMaster?.ActorNumber ?? -1);
    }
}
