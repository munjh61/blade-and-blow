using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public sealed class PhotonMatchingAgent : MonoBehaviour
{
    public static PhotonMatchingAgent Instance { get; private set; }

    [Header("Settings")]
    public string gameSceneName = "PlayScene";
    public byte requiredPlayers = 10;
    public byte minPlayersToStart = 2;
    public int timeoutSeconds = 5;
    public int maxTimeoutSeconds = 30;
    public int startDelaySeconds = 5;
    public float durationSeconds = 60f;

    [Header("Wiring")]
    [SerializeField] private PunMatchInfoProvider provider;
    [SerializeField] private PhotonNetworkManager manager;

    [Header("Local Preview (optional)")]
    [SerializeField] private CharacterEquipmentReborn waitingPreview;

    private PhotonNetworkManager Manager => manager ? manager : (manager = PhotonNetworkManager.Instance);
    private RelayMatchInfoWriter _writerRelay;
    private MatchingCore _core;
    private bool _deadlinesInitializedOnce = false;

    public event Action<short, string, string> OnPrivateJoinFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RebindAll(initial: true);

        //if (!manager) manager = PhotonNetworkManager.Instance;
        //if (!manager) { Debug.LogError("[MatchingAgent] PhotonNetworkManager is required."); enabled = false; return; }

        if (!provider) provider = FindFirstObjectByType<PunMatchInfoProvider>();
        _writerRelay = new RelayMatchInfoWriter { Target = provider };

        _core = new MatchingCore(_writerRelay)
        {
            GameSceneName    = gameSceneName,
            RequiredPlayers  = requiredPlayers,
            MinPlayersToStart= minPlayersToStart,
            TimeoutSeconds   = timeoutSeconds,
            MaxTimeoutSeconds= maxTimeoutSeconds,
            StartDelaySeconds= startDelaySeconds,
            DurationSeconds  = durationSeconds,
            AppVersion       = Application.version
        };

        // Core -> Manager 라우팅
        _core.RequestJoinLobby += () => Manager?.JoinLobby();
        _core.RequestJoinRandom += expected =>
        {
            var ht = new Hashtable();
            foreach (var kv in expected) ht[kv.Key] = kv.Value;
            Manager?.JoinRandomRoom(ht, _core.RequiredPlayers);
        };
        _core.RequestJoinRoomByName += rn => Manager?.JoinRoom(rn);
        _core.RequestCreateRoom += (name, maxPlayers, visible, props) =>
        {
            var ht = new Hashtable();
            foreach (var kv in props) ht[kv.Key] = kv.Value;

            var opt = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                PublishUserId = true,
                IsVisible = visible,
                IsOpen = true,
                CustomRoomProperties = ht,
                CustomRoomPropertiesForLobby = new[] { MatchingCore.ROOM_PROP_VERSION }
            };
            Manager?.CreateRoom(name, opt);
        };
        _core.RequestSetRoomProperties += dict => Manager?.SetRoomProperties(new Dictionary<string, object>(dict));
        _core.RequestSetRoomOpenVisible += (open, visible) => Manager?.SetRoomOpenVisible(open, visible);
        _core.RequestLeaveRoom += () => Manager?.LeaveRoom();
        _core.RequestLoadScene += scene =>
        {
            if (SceneManager.GetActiveScene().name != scene) Manager?.LoadLevel(scene);
        };
        _core.PrivateJoinFailed += (code, msg, room) =>
        {
            OnPrivateJoinFailed?.Invoke(code, msg, room);
        };
    }
    
    private bool _eventsBound = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SelectedLoadout.OnChanged += OnLoadoutChanged_LocalPreview;

        _eventsBound = TryBindManagerEvents();

        StartCoroutine(BindManagerEventsRetry());

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.MatchInfoProviderChanged += OnGsmProviderChanged;

        StartCoroutine(ResolveWriterRetry());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded    -= OnSceneLoaded;
        SelectedLoadout.OnChanged   -= OnLoadoutChanged_LocalPreview;

        // 이벤트 안전 해제 (널가드)
        if (Manager != null)
        {
            Manager.ConnectedToMaster -= HandleConnectedToMaster;
            Manager.JoinedLobby -= HandleJoinedLobby;
            Manager.JoinedRoom -= HandleJoinedRoom;
            Manager.LeftRoom -= HandleLeftRoom;
            Manager.LeftLobby -= HandleLeftLobby;
            Manager.JoinRandomFailed -= HandleJoinRandomFailed;
            Manager.JoinRoomFailed -= HandleJoinRoomFailed;
            Manager.RoomPropertiesUpdated -= HandleRoomPropsUpdated;
            Manager.PlayerEnteredRoomEvent -= HandlePlayerCountsChanged;
            Manager.PlayerLeftRoomEvent -= HandlePlayerCountsChanged;
            Manager.Disconnected -= HandleDisconnected;
        }

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.MatchInfoProviderChanged -= OnGsmProviderChanged;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RebindAll(initial: false);
        TryResolveWriter("sceneLoaded");
    }

    private IEnumerator BindManagerEventsRetry()
    {
        float end = Time.unscaledTime + 2f;
        while (Time.unscaledTime < end)
        {
            if (TryBindManagerEvents()) yield break;
            yield return new WaitForSeconds(0.2f);
        }
        // 마지막 한 번 더 시도
        TryBindManagerEvents();
    }

    private bool TryBindManagerEvents()
    {
        if (Manager == null) { Debug.LogWarning("[MatchingAgent] TryBindManagerEvents: Manager is null"); return false; }


        // 중복 구독 방지 위해 한 번 해제 후 구독
        Manager.ConnectedToMaster -= HandleConnectedToMaster;
        Manager.JoinedLobby -= HandleJoinedLobby;
        Manager.JoinedRoom -= HandleJoinedRoom;
        Manager.LeftRoom -= HandleLeftRoom;
        Manager.LeftLobby -= HandleLeftLobby;
        Manager.JoinRandomFailed -= HandleJoinRandomFailed;
        Manager.JoinRoomFailed -= HandleJoinRoomFailed;
        Manager.RoomPropertiesUpdated -= HandleRoomPropsUpdated;
        Manager.PlayerEnteredRoomEvent -= HandlePlayerCountsChanged;
        Manager.PlayerLeftRoomEvent -= HandlePlayerCountsChanged;
        Manager.Disconnected -= HandleDisconnected;

        Manager.ConnectedToMaster += HandleConnectedToMaster;
        Manager.JoinedLobby += HandleJoinedLobby;
        Manager.JoinedRoom += HandleJoinedRoom;
        Manager.LeftRoom += HandleLeftRoom;
        Manager.LeftLobby += HandleLeftLobby;
        Manager.JoinRandomFailed += HandleJoinRandomFailed;
        Manager.JoinRoomFailed += HandleJoinRoomFailed;
        Manager.RoomPropertiesUpdated += HandleRoomPropsUpdated;
        Manager.PlayerEnteredRoomEvent += HandlePlayerCountsChanged;
        Manager.PlayerLeftRoomEvent += HandlePlayerCountsChanged;
        Manager.Disconnected += HandleDisconnected;

        _eventsBound = true;
        return true;
    }

    private void RebindAll(bool initial)
    {
        // Manager 재바인딩
        if (manager == null) manager = PhotonNetworkManager.Instance;

        // Provider 재바인딩 (비활성 포함)
        if (!provider)
            provider = FindFirstObjectByType<PunMatchInfoProvider>(FindObjectsInactive.Include);

        if (_writerRelay != null)
        {
            _writerRelay.Target = provider;
            if (!initial && provider != null)
                _writerRelay.FlushToTarget();
        }
    }

    private void OnGsmProviderChanged(IMatchInfoProvider _) => TryResolveWriter("gsm-change");

    private IEnumerator ResolveWriterRetry()
    {
        float end = Time.unscaledTime + 2f;
        while (Time.unscaledTime < end)
        {
            TryResolveWriter("retry");
            if (_writerRelay.Target != null) yield break;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void TryResolveWriter(string reason)
    {
        var gsm = GameStateManager.Instance;
        IMatchInfoWriter preferred = gsm?.MatchInfoProvider as IMatchInfoWriter;

        if (preferred == null)
            preferred = FindProvider();

        if (_writerRelay.Target is UnityEngine.Object oldUo && oldUo == null)
            _writerRelay.Target = null;

        if (!ReferenceEquals(preferred, _writerRelay.Target))
        {
            _writerRelay.Target = preferred;
            if (preferred != null)
            {
                _writerRelay.FlushToTarget();
                var id = (preferred as UnityEngine.Object)?.GetInstanceID();
                Debug.Log($"[MatchingAgent] Writer rebound via {reason}: {preferred} (id={id})");
            }
            else
            {
                Debug.Log($"[MatchingAgent] Writer cleared via {reason}: preferred is null");
            }
        }
    }

    private PunMatchInfoProvider FindProvider()
        => FindFirstObjectByType<PunMatchInfoProvider>(FindObjectsInactive.Include);

    private void Update()
    {
        if (Manager == null) return;
        if (!Manager.InRoom) return;
        if (_core.SceneLoadIssued) return;

        _core.Tick(Manager.Time, Manager.CurrentRoom.PlayerCount, Manager.IsMasterClient);
    }

    // ===== 외부 호출 API =====
    public void StartSingle()
    {
        if (Manager == null) return;
        Manager.AutomaticallySyncScene = true;
        // EnsureConnectedThen(_core.StartSingle);
        StartCoroutine(EnsureConnectedAndInLobbyThen(_core.StartSingle));
    }

    public void StartTeam()
    {
        if (Manager == null) return;
        Manager.AutomaticallySyncScene = true;
        // EnsureConnectedThen(_core.StartTeam);
        StartCoroutine(EnsureConnectedAndInLobbyThen(_core.StartTeam));
    }

    public void StartPrivate(string roomIdUpper)
    {
        if (Manager == null) return;
        Manager.AutomaticallySyncScene = true;
        // EnsureConnectedThen(() => _core.StartPrivate(roomIdUpper));
        StartCoroutine(EnsureConnectedAndInLobbyThen(() => _core.StartPrivate(roomIdUpper)));
    }

    /// <summary>
    /// Exit 직전에 호출되어, 매칭 코어/내부 플래그를 초기화한다.
    /// </summary>
    public void ResetForExit()
    {
        // 코어 초기화
        _core?.ResetForExit(resetUi: true);

        // 내부 플래그/상태 초기화
        _deadlinesInitializedOnce = false;

        // 다음 매치에서 씬 동기화는 시작 API에서 다시 true로 올림
        if (Manager != null) Manager.AutomaticallySyncScene = false;

        // 로비/룸 이벤트 핸들링 중이면 안전하게 카운트 UI 리셋
        _writerRelay?.SetPlayerCounts(0, 0);
    }

    public void Cancel()
    {
        if (Manager == null) return;
        var prev = Manager.AutomaticallySyncScene;
        Manager.AutomaticallySyncScene = false;
        _core.Cancel();
        Manager.AutomaticallySyncScene = prev;
    }

    // ===== Manager → Core =====
    private void HandleConnectedToMaster() => _core.OnConnectedToMaster();
    private void HandleJoinedLobby() => _core.OnJoinedLobby();

    private void HandleJoinedRoom()
    {
        var room = Manager.CurrentRoom;
        _core.OnJoinedRoom(room.PlayerCount, room.MaxPlayers, ToDict(room.CustomProperties), Manager.IsMasterClient, Manager.Time);

        // 마스터가 deadline/maxdeadline 미세팅 시 초기화
        if (Manager.IsMasterClient && !_deadlinesInitializedOnce)
        {
            var props = room.CustomProperties;
            bool hasDeadline = props.ContainsKey(MatchingCore.ROOM_PROP_DEADLINE);
            bool hasMaxDeadline = props.ContainsKey(MatchingCore.ROOM_PROP_MAXDEADLINE);

            if (!hasDeadline || !hasMaxDeadline)
            {
                double now = Manager.Time;
                double deadline = _core.Deadline > 0 ? _core.Deadline : now + _core.TimeoutSeconds;
                double maxDeadline = _core.MaxDeadline > 0 ? _core.MaxDeadline : now + _core.MaxTimeoutSeconds;

                Manager.SetRoomProperties(new Dictionary<string, object>
                {
                    { MatchingCore.ROOM_PROP_DEADLINE,    deadline },
                    { MatchingCore.ROOM_PROP_MAXDEADLINE, maxDeadline }
                });
            }

            _deadlinesInitializedOnce = true;
        }

        // Photon.Pun.PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "eq", SelectedLoadout.CurrentEquipId } });
    }

    private void HandleLeftRoom()
    {
        _deadlinesInitializedOnce = false;
        _core.OnLeftRoom();
    }
    private void HandleLeftLobby() => _core.OnLeftLobby();
    private void HandleJoinRandomFailed(short code, string msg)
    {
        Debug.Log($"[MatchingAgent] JoinRandomFailed: code={code} msg={msg}");
        Debug.Log($"[MatchingAgent] JoinRandomFailed: InLobby={Manager.InLobby}, IsConnected={Manager.IsConnected}, ClientState={Manager.ClientState}");
        Debug.Log($"[MatchingAgent] JoinRandomFailed: coreNull={_core == null}");
        _core.OnJoinRandomFailed(code, msg);
    }
    private void HandleJoinRoomFailed(short code, string msg) => _core.OnJoinRoomFailed(code, msg);
    private void HandleRoomPropsUpdated(Hashtable changed) => _core.OnRoomPropertiesUpdate(ToDict(changed));
    private void HandlePlayerCountsChanged(Player _)
    {
        if (Manager?.CurrentRoom == null) return;
        // 게임 씬 로드가 시작되었다면 플레이어 수 변경에 따른 매칭 로직을 실행하지 않습니다.
        if (_core.SceneLoadIssued) return;
        _core.OnPlayerCountsChanged(Manager.CurrentRoom.PlayerCount, Manager.CurrentRoom.MaxPlayers, Manager.IsMasterClient, Manager.Time);
    }
    private void HandleDisconnected(DisconnectCause _) => ResetForExit();

    // ===== Utils =====
    private IEnumerator EnsureConnectedAndInLobbyThen(Action beginCore)
    {
        Debug.Log($"[MatchingAgent] EnsureConnectedAndInLobbyThen: IsConnected={Manager.IsConnected}, InLobby={Manager.InLobby} ClientState={Manager.ClientState}");
        if (!Manager.IsMessageQueueRunning) Manager.IsMessageQueueRunning = true;
        bool wasInLobbyAtEnter = Manager.InLobby;
        if (Manager.ClientState == ClientState.Disconnected)
        {
            Manager.ConnectUsingSettings();
        }

        while (Manager.ClientState != ClientState.ConnectedToMasterServer &&
               Manager.ClientState != ClientState.JoinedLobby)
        {
            yield return null;
        }

        bool joinedLobbyNow = false;
        if (!Manager.InLobby)
        {
            Manager.JoinLobby();
            while (!Manager.InLobby)
                yield return null;
            joinedLobbyNow = true;
        }

        if (!_eventsBound) TryBindManagerEvents();
        if (!Manager.IsMessageQueueRunning) Manager.IsMessageQueueRunning = true;
        Debug.Log("[MatchingAgent] invoking beginCore...");
        beginCore?.Invoke();

        if (!joinedLobbyNow && Manager.InLobby)
        {
            Debug.Log("[MatchingAgent] already in lobby → manually triggering core.OnJoinedLobby()");
            _core.OnJoinedLobby();
        }
        StartJoinWatchdog();
        Debug.Log($"[MatchingAgent] EnsureConnectedAndInLobbyThenAfter: IsConnected={Manager.IsConnected}, InLobby={Manager.InLobby} ClientState={Manager.ClientState}");
    }

    private Coroutine _joinWatchdog;
    private const float JoinTimeoutSec = 6f;

    private void StartJoinWatchdog()
    {
        if (_joinWatchdog != null) StopCoroutine(_joinWatchdog);
        _joinWatchdog = StartCoroutine(JoinWatchdogRoutine());
    }

    private IEnumerator JoinWatchdogRoutine()
    {
        float until = Time.realtimeSinceStartup + JoinTimeoutSec;

        while (Time.realtimeSinceStartup < until)
        {
            if (Manager.InRoom && Manager.CurrentRoom != null) { _joinWatchdog = null; yield break; }
            if (Manager.ClientState == ClientState.JoinedLobby && !Manager.InRoom) { _joinWatchdog = null; yield break; }

            if (!Manager.IsMessageQueueRunning) Manager.IsMessageQueueRunning = true;
            yield return null;
        }

        Debug.LogWarning("[MatchingAgent] Join watchdog timeout → simulate join-failed");
        if (_core.CurrentMatchMode == MatchMode.PrivateMatch)
            _core.OnJoinRoomFailed(-1, "timeout");
        else
            _core.OnJoinRandomFailed(-1, "timeout");

        _joinWatchdog = null;
    }

    private void EnsureConnectedThen(System.Action beginCore)
    {
        if (Manager == null) return;
        Manager.GameVersion = Application.version;

        Debug.Log($"[MatchingAgent] EnsureConnectedThen: IsConnected={Manager.IsConnected}, InLobby={Manager.InLobby}");
        if (!Manager.IsConnected)
        {
            Manager.ConnectUsingSettings();
            beginCore?.Invoke();
            return;
        }

        beginCore?.Invoke();

        if (Manager.InLobby)
        {
            _core.OnJoinedLobby();
        }
        else
        {
            Manager.JoinLobby();
        }
        Debug.Log($"[MatchingAgent] EnsureConnectedThenAfter: IsConnected={Manager.IsConnected}, InLobby={Manager.InLobby}");
    }

    private static Dictionary<string, object> ToDict(Hashtable ht)
    {
        var d = new Dictionary<string, object>(ht.Count);
        //foreach (var k in ht.Keys) d[(string)k] = ht[k];
        foreach (DictionaryEntry entry in ht)
        {
            if (entry.Key is string sk)         // 커스텀 프로퍼티만
                d[sk] = entry.Value;
        }

        return d;
    }

    private void OnLoadoutChanged_LocalPreview(int equipId)
    {
        if (equipId > 2) return;
        var eq = ResolvePreviewEquipment();
        if (eq != null && equipId >= 0)
            eq.EquipToSlot(Slot.Weapon, equipId);
    }

    private CharacterEquipmentReborn ResolvePreviewEquipment()
    {
        if (waitingPreview != null) return waitingPreview;

        waitingPreview = FindFirstObjectByType<CharacterEquipmentReborn>();
        return waitingPreview;
    }
}
