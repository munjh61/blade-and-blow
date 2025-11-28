using Game.Domain;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(0)]
public class GameSceneInitializer : MonoBehaviour
{
    [Header("Spawn")]
    public string playerPrefabName = "MuscleCat";
    public Transform[] spawnPoints;

    private static bool s_SpawnedOnce;
    private static bool s_HudOpened;

    private PhotonNetworkManager _photonNet;
    private TeamManager _teamMgr;
    private GameStateManager _gameState;

    private Coroutine _initCo;
    private bool _initStarted;

    public static void ResetStatics()
    {
        s_SpawnedOnce = false;
        s_HudOpened = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        _photonNet = PhotonNetworkManager.Instance;
        if (_photonNet != null)
        {
            _photonNet.JoinedRoom += HandleJoinedRoom;
            _photonNet.LeftRoom += HandleLeftRoom;
        }

        _teamMgr = TeamManager.Instance;
        _gameState = GameStateManager.Instance;

        TryKickInit("OnEnable");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        _photonNet = PhotonNetworkManager.Instance;
        if (_photonNet != null)
        {
            _photonNet.JoinedRoom -= HandleJoinedRoom;
            _photonNet.LeftRoom -= HandleLeftRoom;
        }

        StopInitRoutine();
        _initStarted = false;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryKickInit("sceneLoaded");
    }

    private void HandleJoinedRoom()
    {
        TryKickInit("JoinedRoom");
    }

    private void HandleLeftRoom()
    {
        // 룸 떠나면 코루틴 중단 및 플래그 초기화
        StopInitRoutine();
        _initStarted = false;
        // 다음 플레이를 위해 정적 플래그도 초기화
        s_SpawnedOnce = false; s_HudOpened = false;
    }

    private void TryKickInit(string reason)
    {
        // PlayScene이 아니면 패스
        if (SceneManager.GetActiveScene().name != "PlayScene") return;

        // 이미 한 번 스폰했으면(같은 씬에서 중복 방지) 패스
        if (s_SpawnedOnce)
        {
            return;
        }

        // 룸 안이 아니면 대기 — JoinedRoom에서 다시 들어옴
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        // 이미 시작했으면 패스
        if (_initStarted) return;

        _initStarted = true;
        _initCo = StartCoroutine(InitRoutine());
    }

    private void StopInitRoutine()
    {
        if (_initCo != null)
        {
            StopCoroutine(_initCo);
            _initCo = null;
        }
    }

    private void Start()
    {
        if (s_SpawnedOnce) { enabled = false; return; }
        PhotonNetworkManager.Instance.AutomaticallySyncScene = false;
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        if (s_SpawnedOnce) yield break;
        //while (PhotonNetwork.LevelLoadingProgress < 1f) yield return null;
        if (!_photonNet.IsMessageQueueRunning)
        {
            _photonNet.IsMessageQueueRunning = true;
        }

        while (SceneManager.GetActiveScene().name != "PlayScene") yield return null;

        if (!s_HudOpened)
        {
            UIManager.Instance.Open(MenuId.HUD);
            s_HudOpened = true;
        }

        while (!_photonNet.IsConnected || !_photonNet.InRoom )
            yield return null;

        yield return null;
        PlayerManagerPunBehaviour pm = null;
        while (pm == null) { pm = FindFirstObjectByType<PlayerManagerPunBehaviour>(); yield return null; }
        while (!pm.IsInitialized) yield return null;
        while (!pm.MasterSynced) yield return null;

        int myActor = pm.LocalId;

        bool isTeamMode = (_gameState.currentMatchMode == MatchMode.TeamMatch);

        // 스폰 포인트 설정
        var map = FindFirstObjectByType<MapSpawnPoints>();
        if (map != null)
        {
            spawnPoints = map.spawnPoints;
        }
        // 마스터인경우 다른 클라이언트의 위치를 설정해줌.
        if (pm.CoreIsMaster)
        {
            AssignSpawnPositions();
            if (isTeamMode) _teamMgr.MasterAssignIfNeeded();
        }
        // 전체 플레이어에게 할당이 완료되고 난 뒤 플래그 프로퍼티가 설정될 때까지 대기
        while (!_photonNet.CurrentRoom.CustomProperties.ContainsKey("spawnAssignmentDone") ||
               !(bool)_photonNet.CurrentRoom.CustomProperties["spawnAssignmentDone"]       ||
               (isTeamMode &&
               (!_photonNet.CurrentRoom.CustomProperties.ContainsKey("teamAssignmentDone") ||
               !(bool)_photonNet.CurrentRoom.CustomProperties["teamAssignmentDone"])))
        {
            yield return null; // 계속 대기
        }

        AvatarRegistry.Handle myHandle;
        if (!AvatarRegistry.TryGet(myActor, out myHandle) || myHandle?.go == null || myHandle?.view == null)
        {
            bool got = false;
            void OnReg(int actor) { if (actor == myActor) got = true; }
            AvatarRegistry.OnRegistered += OnReg;

            GameObject spawned = null;
            try
            {
                // 할당된 스폰 포인트 가져오기
                var spawn = GetMySpawnPoint();
                spawned = PhotonNetwork.Instantiate(playerPrefabName, spawn.position, spawn.rotation);
            }
            catch (System.SystemException e)
            {
                AvatarRegistry.OnRegistered -= OnReg;
                Debug.LogException(e);
                yield break;
            }

            const float timeout = 6f;
            string sceneAtStart = SceneManager.GetActiveScene().name;
            float end = Time.realtimeSinceStartup + timeout;

            while (!got && Time.realtimeSinceStartup < end && _photonNet.InRoom)
            {
                if (SceneManager.GetActiveScene().name != sceneAtStart) { got = false; break; }
                yield return null;
            }

            AvatarRegistry.OnRegistered -= OnReg;

            if (!got || !AvatarRegistry.TryGet(myActor, out myHandle) || myHandle?.view == null)
            {
                yield break;
            }
        }
        while (!pm.IsInitialized) yield return null;
        while (!pm.MasterSynced) yield return null;

        PhotonNetwork.LocalPlayer.TagObject = myHandle.go;

        s_SpawnedOnce = true;

        int equip = -1;

        if (SelectedLoadout.CurrentEquipId >= 0)
            equip = SelectedLoadout.CurrentEquipId;

        // if (PhotonNetwork.LocalPlayer.CustomProperties != null &&
        //     PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("eq", out var eObj))
        // {
        //     if (eObj is int eInt) equip = eInt;
        // }

        if (equip > 2)
        {
            equip = Random.Range(0, 3);
            SelectedLoadout.SetEquip(equip);
        }
        var myId = new PlayerId(myHandle.view.OwnerActorNr);
        var pNick = PhotonNetwork.NickName;
        var myName = !string.IsNullOrWhiteSpace(pNick)
            ? pNick
            : (!string.IsNullOrWhiteSpace(UserSession.Nickname)
                ? UserSession.Nickname
                : $"Player{myId.Value}");
        var myUserId = !string.IsNullOrWhiteSpace(UserSession.Username)
            ? UserSession.Username
            : $"{PhotonNetwork.CurrentRoom.Name}_guest{myId.Value}";
        TeamId myTeam = TeamId.None;
        if (isTeamMode)
        {
            _teamMgr.TryGetTeamFromRoomProps(myId.Value, out myTeam);
        }
        var initial = new PlayerInfoData { actor = myId.Value, name = myName, userId = myUserId, team = myTeam, hp = 100, stamina = 100, equipId = equip };
        pm.RegisterLocal(myId, initial);

        // 플레이어의 장비 정보를 CustomProperties에 설정
        // var props = PhotonNetwork.LocalPlayer.CustomProperties ?? new ExitGames.Client.Photon.Hashtable();
        // props["eq"] = equip;
        // PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties != null && p.CustomProperties.TryGetValue("eq", out var vv) && vv is int eq)
                {
                    pm.Master_SetEquip(new PlayerId(p.ActorNumber), eq);
                }
                pm.Master_SetAlive(new PlayerId(p.ActorNumber));
            }
            pm.Master_SyncCacheAndAlive();
        }
    }
    // 마스터 클라이언트가 각 플레이어에게 랜덤으로 스폰 포인트를 할당
    private void AssignSpawnPositions()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++) indices.Add(i);

        // 랜덤 셔플
        for (int i = 0; i < indices.Count; i++)
        {
            int swap = Random.Range(i, indices.Count);
            (indices[i], indices[swap]) = (indices[swap], indices[i]);
        }

        // 플레이어별로 CustomProperties에 spawnIndex 할당
        int idx = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            var props = new ExitGames.Client.Photon.Hashtable { { "spawnIndex", indices[idx++] } };
            player.SetCustomProperties(props);
        }

        // 배정 완료 플래그
        _photonNet.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "spawnAssignmentDone", true } });
    }

    // 자신의 CustomProperties에서 spawnIndex를 읽어 해당 스폰 포인트 반환
    private Transform GetMySpawnPoint()
    {
        // spawnPoints 배열 유효성 체크
        if (spawnPoints == null || spawnPoints.Length == 0)
            return LogAndReturnTemp("[Spawn] spawnPoints 배열이 비어있습니다. 임시 위치 사용");

        // LocalPlayer spawnIndex 가져오기
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("spawnIndex", out var idxObj))
            return LogAndReturnTemp("[Spawn] LocalPlayer에 spawnIndex가 없습니다. 임시 위치 사용");

        int spawnIndex = (int)idxObj;

        // 범위 체크
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
            return LogAndReturnTemp($"[Spawn] spawnIndex {spawnIndex} 범위 벗어남. fallback 위치 사용");

        // 정상적인 spawnIndex 반환
        return spawnPoints[spawnIndex];
    }

    // 로그와 함께 임시 위치 생성
    private Transform LogAndReturnTemp(string message)
    {
        Debug.LogWarning(message);
        var temp = new GameObject("SpawnTemp").transform;
        temp.position = Vector3.zero;
        temp.rotation = Quaternion.identity;
        return temp;
    }
}
