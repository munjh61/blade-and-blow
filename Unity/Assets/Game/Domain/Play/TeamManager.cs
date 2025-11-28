using Game.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine;

public sealed class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    public PhotonNetworkManager _photonNet;
    public GameStateManager _gameState;

    public const string KEY_DONE = "teamAssignmentDone";
    public const string KEY_TEAM_PREFIX = "t";
    
    private TeamId _currentTeam = TeamId.None;
    public TeamId CurrentTeamId => _currentTeam;

    // 비밀방에서만 쏘는 팀 변경 이벤트
    public event Action<int, TeamId> OnTeamChanged;

    // ----- 외부 의존 (프로젝트의 실제 접근자 이름에 맞게 쓰면 됨)
    private bool IsTeamMode() =>
        _gameState.currentMatchMode == MatchMode.TeamMatch ? true : false;

    private bool IsPrivateRoom() =>
        _gameState.currentMatchMode == MatchMode.PrivateMatch ? true : false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 마스터가 아직 팀 배정 안 했으면 일괄 배정 후 완료 플래그를 설정
    /// </summary>
    public void MasterAssignIfNeeded()
    {
        if (!IsTeamMode()) return;
        if (!_photonNet.IsMasterClient) return;
        var room = _photonNet.CurrentRoom;
        if (room == null) return;

        var props = room.CustomProperties ?? new Hashtable();
        if (props.TryGetValue(KEY_DONE, out var doneObj) && doneObj is bool done && done) return;

        // 안정적 순서 보장 위해 ActorNumber 기준 정렬
        var players = new List<Player>(_photonNet.PlayerList);
        players.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        int n = players.Count;
        if (n <= 0) return;
        
        int redCount = (n + 1) / 2;
        int blueCount = n - redCount;

        // RED/BLUE 스왑
        List<TeamId> teams = new List<TeamId>(n);
        for (int i = 0; i < redCount;  i++) teams.Add(TeamId.Red);
        for (int i = 0; i < blueCount; i++) teams.Add(TeamId.Blue);

        for (int i = 0; i < teams.Count; i++)
        {
            int swap = UnityEngine.Random.Range(i, teams.Count);
            (teams[i], teams[swap]) = (teams[swap], teams[i]);
        }

        var toSet = new Hashtable();
        for (int i = 0; i < players.Count; i++)
        {
            var actor = players[i].ActorNumber;
            toSet[$"{KEY_TEAM_PREFIX}{actor}"] = (byte)teams[i];
        }

        toSet[KEY_DONE] = true;
        room.SetCustomProperties(toSet);
    }

    /// <summary>
    /// Room.CustomProperties에서 내 팀 읽기
    /// </summary>
    public bool TryGetTeamFromRoomProps(int actor, out TeamId team)
    {
        team = TeamId.None;
        var room = _photonNet.CurrentRoom;
        if (room?.CustomProperties == null) return false;

        var key = $"{KEY_TEAM_PREFIX}{actor}";
        if (room.CustomProperties.TryGetValue(key, out var v) && v is byte b)
        {
            team = (TeamId)b;
            if (actor == _photonNet.LocalPlayer?.ActorNumber)
                _currentTeam = team;
            Debug.Log($"[TeamManager] Actor={actor} team={team} key={key}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 비밀방일 때만 OnTeamChanged 이벤트 사용
    /// </summary>
    public void HandleRoomPropertiesUpdated(Hashtable changed)
    {
        if (!IsPrivateRoom()) return;

        foreach (DictionaryEntry kv in changed)
        {
            if (kv.Key is string k && k.StartsWith(KEY_TEAM_PREFIX) && kv.Value is byte tb)
            {
                if (int.TryParse(k.Substring(KEY_TEAM_PREFIX.Length), out int actor))
                {
                    OnTeamChanged?.Invoke(actor, (TeamId)tb);
                }
            }
        }
    }
}