using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MatchingCore
{
    // ===== Config =====
    public string GameSceneName { get; set; } = "PlayScene";
    public byte RequiredPlayers { get; set; } = 10;
    public byte MinPlayersToStart { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 5;
    public int MaxTimeoutSeconds { get; set; } = 30;
    public int StartDelaySeconds { get; set; } = 5;
    public float DurationSeconds { get; set; } = 60f;
    public string AppVersion { get; set; } = "1.0.0";
    public MatchMode CurrentMatchMode { get; set; } = MatchMode.None;

    // ===== Room Prop Keys =====
    public const string ROOM_PROP_DEADLINE      = "deadline";
    public const string ROOM_PROP_MAXDEADLINE   = "maxdl";
    public const string ROOM_PROP_START         = "start";
    public const string ROOM_PROP_START_AT      = "startAt";
    public const string ROOM_PROP_END_AT        = "endAt";
    public const string ROOM_PROP_VERSION       = "ver";
    public const string ROOM_PROP_DURATION      = "durMS";
    public const string ROOM_PROP_SUDDEN        = "sd";
    public const string ROOM_PROP_DOT_AT        = "dotAt";
    public const string ROOM_PROP_DOT_INT_MS    = "dotInt";
    public const string ROOM_PROP_DOT_DMG       = "dotDmg";

    // ===== State =====
    public bool InMatching { get; private set; }
    public bool SceneLoadIssued { get; private set; }
    public string PendingPrivateRoomId { get; private set; }
    public double Deadline { get; private set; } = -1;
    public double MaxDeadline { get; private set; } = -1;
    public double StartAt { get; private set; } = -1;
    public double EndAt { get; private set; } = -1;
    public int DurationMs { get; private set; } = 0;
    public int LastCountdownSec { get; private set; } = -1;
    public bool CancelRequested { get; private set; }

    // ===== UI Writer =====
    private readonly IMatchInfoWriter _writer;
    public MatchingCore(IMatchInfoWriter writer) { _writer = writer; }

    // ===== Adapter Hooks =====
    public event Action RequestJoinLobby;
    public event Action<Dictionary<string, object>> RequestJoinRandom;
    public event Action<string> RequestJoinRoomByName;
    public event Action<string, byte, bool, Dictionary<string, object>> RequestCreateRoom;
    public event Action<Dictionary<string, object>> RequestSetRoomProperties;
    public event Action<bool, bool> RequestSetRoomOpenVisible;
    public event Action RequestLeaveRoom;
    public event Action<string> RequestLoadScene;
    public event Action<short, string, string> PrivateJoinFailed;

    // ===== Public API =====
    public void StartSingle()
    {
        UnityEngine.Debug.Log("[MatchignCore] Start Single ");
        CurrentMatchMode = MatchMode.SingleMatch;
        _writer?.SetModeSingle();
        _writer?.ClearPrivate();
        _writer?.SetStatusFinding();
        BeginMatching();
    }

    public void StartTeam()
    {
        CurrentMatchMode = MatchMode.TeamMatch;
        _writer?.SetModeTeam();
        _writer?.ClearPrivate();
        _writer?.SetStatusFinding();
        BeginMatching();
    }

    public void StartPrivate(string roomIdUpperOrFull)
    {
        CurrentMatchMode = MatchMode.PrivateMatch;
        CancelRequested = false;
        InMatching = true;
        PendingPrivateRoomId = ComposePrivateRoomName(roomIdUpperOrFull);
        string codeForUi = ExtractPrivateCode(PendingPrivateRoomId);
        _writer?.SetModePrivate(codeForUi);
        _writer?.SetStatusFinding();
        RequestJoinLobby?.Invoke();
    }


    public void Cancel()
    {
        ResetState(resetUi: true, clearMode: true);
        CancelRequested = true;
        RequestLeaveRoom?.Invoke();
    }

    // ===== Call-ins from Adapter =====
    public void OnConnectedToMaster()
    {
        UnityEngine.Debug.Log("[MatcingCore] OnConnectedToMaster");
        if (CancelRequested || !InMatching) return;
        RequestJoinLobby?.Invoke();
        _writer?.SetStatusFinding();
    }

    public void OnJoinedLobby()
    {
        UnityEngine.Debug.Log("[MatcingCore] OnJoinedLobby");
        Debug.Log("[MatcingCore] CancelRequested=" + CancelRequested + ", InMatching=" + InMatching);
        if (CancelRequested || !InMatching) return;

        _writer?.SetStatusFinding();

        if (CurrentMatchMode == MatchMode.PrivateMatch && !string.IsNullOrEmpty(PendingPrivateRoomId))
        {
            RequestJoinRoomByName?.Invoke(PendingPrivateRoomId);
            return;
        }

        var expected = new Dictionary<string, object> { { ROOM_PROP_VERSION, AppVersion } };
        RequestJoinRandom?.Invoke(expected);
    }

    public void OnJoinRoomFailed(short code, string message)
    {
        UnityEngine.Debug.Log("[MatchingCore] OnJoinRoomFailed");
        // Private 모드일 때만 실패 알림(생성 안 함)
        if (CurrentMatchMode == MatchMode.PrivateMatch && !string.IsNullOrEmpty(PendingPrivateRoomId))
        {
            UnityEngine.Debug.Log($"[MatchingCore] PendingPrivateRoomId={PendingPrivateRoomId}");
            var attempted = PendingPrivateRoomId;
            PendingPrivateRoomId = null;
            _writer?.SetStatusFinding();
            PrivateJoinFailed?.Invoke(code, message, attempted);
        }
    }

    public void OnJoinRandomFailed(short code, string message)
    {
        UnityEngine.Debug.Log("[MatcingCore] OnJoinRandomFailed!!!!!!!!!!!!!!!!!!");
        if (CancelRequested || !InMatching) return;
        // Single/Team만 랜덤 실패 시 방 생성
        var prefix = (CurrentMatchMode == MatchMode.TeamMatch) ? "team_" : "single_";
        var roomName = prefix + GenerateRandomString(8);

        var props = new Dictionary<string, object> { { ROOM_PROP_VERSION, AppVersion } };
        // Single/Team은 공개방으로 생성(필요시 visible 정책 조정 가능)
        RequestCreateRoom?.Invoke(roomName, RequiredPlayers, true, props);
    }

    public void OnJoinedRoom(int playerCount, int maxPlayers, IReadOnlyDictionary<string, object> roomProps, bool isMaster, double now)
    {
        UnityEngine.Debug.Log("[MatcingCore] OnJoinedRoom");
        if (CancelRequested) { RequestLeaveRoom?.Invoke(); return; }

        _writer?.SetStatusWaiting();
        _writer?.SetPlayerCounts(playerCount, maxPlayers);

        if (isMaster)
        {
            if (!roomProps.ContainsKey(ROOM_PROP_DEADLINE) || !roomProps.ContainsKey(ROOM_PROP_MAXDEADLINE))
            {
                Deadline = now + TimeoutSeconds;
                MaxDeadline = now + MaxTimeoutSeconds;
                RequestSetRoomProperties?.Invoke(new Dictionary<string, object>{
                    { ROOM_PROP_DEADLINE,    Deadline },
                    { ROOM_PROP_MAXDEADLINE, MaxDeadline }
                });
            }
            else
            {
                Deadline = ToDouble(roomProps[ROOM_PROP_DEADLINE]);
                MaxDeadline = ToDouble(roomProps[ROOM_PROP_MAXDEADLINE]);
            }
        }
        else
        {
            if (roomProps.TryGetValue(ROOM_PROP_DEADLINE, out var dl)) Deadline = ToDouble(dl);
            if (roomProps.TryGetValue(ROOM_PROP_MAXDEADLINE, out var md)) MaxDeadline = ToDouble(md);
        }

        // 방 입장 시점에 게임이 이미 시작되었는지 확인하고 상태를 동기화합니다.
        if (roomProps.TryGetValue(ROOM_PROP_START, out var st) && st is bool started && started)
        {
            SceneLoadIssued = true;
        }

        LastCountdownSec = -1;
        StartAt = -1;
        EndAt = -1;
        DurationMs = 0;
    }

    public void OnLeftRoom()
    {
        ResetAfterLeave(resetUi: true);
    }

    public void OnLeftLobby() { /* optional log */ }

    public void OnRoomPropertiesUpdate(IReadOnlyDictionary<string, object> changed)
    {
        if (CancelRequested || !InMatching) return;

        if (changed.TryGetValue(ROOM_PROP_DEADLINE, out var dl)) Deadline = ToDouble(dl);
        if (changed.TryGetValue(ROOM_PROP_MAXDEADLINE, out var mdl)) MaxDeadline = ToDouble(mdl);

        bool durChanged = false;
        bool startAtChanged = false;

        if (changed.TryGetValue(ROOM_PROP_DURATION, out var dm))
        {
            DurationMs = Convert.ToInt32(dm);
            durChanged = true;
        }

        if (changed.TryGetValue(ROOM_PROP_START_AT, out var sa))
        {
            StartAt = ToDouble(sa);
            _writer?.SetStatusStarting();
            LastCountdownSec = -1;
            startAtChanged = true;
        }

        if (changed.TryGetValue(ROOM_PROP_START, out var st) && st is bool started && started)
        {
            _writer?.SetStatusStarting();
            SceneLoadIssued = true;
        }

        if ((startAtChanged || durChanged) && StartAt > 0 && DurationMs > 0)
            EndAt = StartAt + (DurationMs / 1000.0);
    }

    public void OnPlayerCountsChanged(int playerCount, int maxPlayers, bool isMaster, double now)
    {
        _writer?.SetPlayerCounts(playerCount, maxPlayers);
        if (isMaster) TryStartMatch(playerCount, now);
    }

    public void Tick(double now, int playerCount, bool isMaster)
    {
        if (CancelRequested || !InMatching) return;

        if (StartAt > 0)
        {
            var remain = StartAt - now;
            if (remain > 0 && remain <= 10.0)
            {
                var sec = (int)Math.Ceiling(remain);
                if (sec != LastCountdownSec)
                {
                    _writer?.SetStatusStartingIn(sec);
                    LastCountdownSec = sec;
                }
            }
        }

        if (isMaster) TryStartMatch(playerCount, now);
    }

    // ===== Public Reset APIs =====
    /// <summary>
    /// 매칭 흐름을 완전히 초기화. Exit/Disconnect 시점에 사용.
    /// </summary>
    public void ResetForExit(bool resetUi = true)
        => ResetState(resetUi, clearMode: true);

    /// <summary>
    /// 방을 나간 뒤(LeftRoom) 또는 Cancel 이후의 일반 초기화.
    /// </summary>
    public void ResetAfterLeave(bool resetUi = true)
        => ResetState(resetUi, clearMode: false);

    // ===== Internals =====
    private void ResetState(bool resetUi, bool clearMode)
    {
        InMatching = false;
        SceneLoadIssued = false;
        CancelRequested = false;

        PendingPrivateRoomId = null;

        StartAt = -1;
        EndAt = -1;
        DurationMs = 0;
        Deadline = -1;
        MaxDeadline = -1;
        LastCountdownSec = -1;

        if (clearMode) CurrentMatchMode = MatchMode.None;

        if (resetUi)
        {
            _writer?.SetPlayerCounts(0, 0);
            _writer?.ClearPrivate();
            _writer?.SetStatusFinding();
        }
    }
    private void BeginMatching()
    {
        UnityEngine.Debug.Log($"[MatchignCore] Begin Match {CurrentMatchMode} ");
        CancelRequested = false;
        InMatching = true;
        PendingPrivateRoomId = null;
        _writer?.SetStatusFinding();
    }

    private void TryStartMatch(int playerCount, double now)
    {
        bool timeout = (Deadline > 0) && (now >= Deadline);
        bool maxTimeout = (MaxDeadline > 0) && (now >= MaxDeadline);
        bool minEnough = playerCount >= MinPlayersToStart;

        if (MeetsStartRule(playerCount, timeout) && StartAt <= 0)
        {
            if (!minEnough) 
            { 
                _writer?.SetStatusWaiting();
                Deadline = now + TimeoutSeconds;
                return; 
            }
            StartAt = now + StartDelaySeconds;
            DurationMs = (int)Math.Round(DurationSeconds * 1000); // ms
            EndAt = StartAt + (DurationMs / 1000.0);

            var props = new Dictionary<string, object>
            {
                { ROOM_PROP_START_AT, StartAt },
                { ROOM_PROP_DURATION,  DurationMs }
            };

            RequestSetRoomProperties?.Invoke(props);
            RequestSetRoomOpenVisible?.Invoke(false, false);
            return;
        }

        if (StartAt > 0 && now >= StartAt)
        {
            if (minEnough)
            {
                StartMatch();
                return;
            }
            else
            {
                _writer?.SetStatusWaiting();
                StartAt = -1;
                EndAt = -1;
                DurationMs = 0;
                Deadline = now + TimeoutSeconds;
                return;
            }
        }

        if (maxTimeout)
        {
            RequestLeaveRoom?.Invoke();
        }
    }

    private void StartMatch()
    {
        if (SceneLoadIssued) return;
        SceneLoadIssued = true;

        RequestSetRoomProperties?.Invoke(new Dictionary<string, object> { { ROOM_PROP_START, true } });
        RequestSetRoomOpenVisible?.Invoke(false, false);
        _writer?.SetStatusStarting();
        RequestLoadScene?.Invoke(GameSceneName);
    }

    private bool MeetsStartRule(int playerCount, bool timeout)
    {
        var baseReady = (playerCount >= RequiredPlayers) || timeout;
        if (!baseReady) return false;
        if (CurrentMatchMode == MatchMode.TeamMatch) return (playerCount % 2 == 0);
        return true;
    }

    private static double ToDouble(object v) => Convert.ToDouble(v);
    private static string ComposePrivateRoomName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (input.StartsWith("private_") || input.StartsWith("public_")) return input;
        if (System.Text.RegularExpressions.Regex.IsMatch(input, "^[A-Z0-9]{8}$")) return "private_" + input;
        return input;
    }

    private static string ExtractPrivateCode(string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return "";
        const string p = "private_";
        return roomName.StartsWith(p) && roomName.Length > p.Length
            ? roomName.Substring(p.Length)
            : roomName;
    }

    private static string GenerateRandomString(int n)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var r = new System.Random(); var buf = new char[n];
        for (int i = 0; i < n; i++) buf[i] = chars[r.Next(chars.Length)];
        return new string(buf);
    }
}
