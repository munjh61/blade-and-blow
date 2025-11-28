using System;
using System.Collections;
using UnityEngine;

public class PunMatchInfoProvider : MonoBehaviour, IMatchInfoProvider, IMatchInfoWriter
{
    public static PunMatchInfoProvider Instance { get; private set; }

    public event Action<MatchInfoSnapshot> OnChanged;

    private MatchInfoSnapshot _snap;

    private string _mode = "Single Play";
    private int _maxPlayers = 10;
    private int _currentPlayers = 1;
    private bool _isPrivate = false;
    private string _roomCode = "";
    private string _status = "Finding Rooms...";
    private int _timer = 120;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _snap = Build();
        TryRegisterToGSM();
        Publish();
    }

    private void OnEnable()
    {
        GameExitManager.OnExitCompleted += ResetSnapshotForMain;
        if (GameStateManager.Instance?.MatchInfoProvider != this)
            TryRegisterToGSM();
    }

    private void OnDisable()
    {
        GameExitManager.OnExitCompleted -= ResetSnapshotForMain;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameStateManager.Instance?.MatchInfoProvider == this)
            GameStateManager.Instance.SetMatchInfoProvider(null);
    }

    public MatchInfoSnapshot GetSnapshot() => _snap;

    // ===== IMatchInfoWriter 구현(표기 규칙 강제) =====
    public void SetModeSingle() { _mode = "Single Mode"; Publish(); }
    public void SetModeTeam() { _mode = "Team Mode"; Publish(); }
    public void SetModePrivate(string roomCodeOrEmpty)
    {
        _mode = "Private Mode"; _isPrivate = true; _roomCode = roomCodeOrEmpty ?? "";
        Publish();
    }

    public void ClearPrivate()
    {
        _isPrivate = false; _roomCode = ""; Publish();
    }

    public void SetPlayerCounts(int current, int max)
    {
        _currentPlayers = Mathf.Max(0, current);
        _maxPlayers = Mathf.Max(0, max);
        Publish();
    }

    public void SetPrivate(bool isPrivate, string roomCode)
    {
        _isPrivate = isPrivate;
        _roomCode = roomCode ?? "";
        Publish();
    }

    public void SetStatusFinding() { _status = "Finding Match..."; Publish(); }
    public void SetStatusWaiting() { _status = "Waiting for Players..."; Publish(); }
    public void SetStatusStarting() { _status = "Starting Games..."; Publish(); }

    public void SetStatusStartingIn(int seconds)
    {
        int s = Mathf.Max(0, seconds);
        _status = $"Starting Games in {s} s...";
        Publish();
    }

    public void SetMatchRemainSeconds(int seconds)
    {
        int s = Mathf.Max(0, seconds);
        _timer = s;
        Publish();
    }

    // ===== 내부 유틸 =====
    private void TryRegisterToGSM()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetMatchInfoProvider(this);
        else
            StartCoroutine(RegisterNextFrame());
    }

    private IEnumerator RegisterNextFrame()
    {
        yield return null;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetMatchInfoProvider(this);
    }

    private MatchInfoSnapshot Build()
        => new MatchInfoSnapshot(_mode, _maxPlayers, _currentPlayers, _isPrivate, _roomCode, _status, _timer);

    private void Publish()
    {
        _snap = Build();
        OnChanged?.Invoke(_snap);
    }
    
    private void ResetSnapshotForMain()
    {
        _mode = "—";
        _currentPlayers = 0;
        _maxPlayers = 0;
        _isPrivate = false;
        _roomCode = "";
        _status = "Finding Match...";
        _timer = 0;
        Publish();
    }
}
