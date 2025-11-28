using System;
using UnityEngine;


public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public IMatchInfoProvider MatchInfoProvider { get; private set; }
    public event Action<IMatchInfoProvider> MatchInfoProviderChanged;

    [Header("Current Game State")]
    public MatchMode currentMatchMode = MatchMode.None;
    public string roomCode = "";
    public bool isRoomHost = false;

    [Header("Match Info")]
    public int maxPlayers => MatchInfoProvider?.GetSnapshot().MaxPlayers ?? 0;
    public string mapName = "Map 1";

    // --- 재매치 요청 이벤트 ---
    public static event Action OnRematchRequested;

    public static void RaiseRematchRequested()
    {
        OnRematchRequested?.Invoke();
    }

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

    public string GetMatchModeDisplayText()
        => MatchInfoProvider?.GetSnapshot().Mode ?? "—";

    public string GetTimerDisplayText()
    {
        int s = MatchInfoProvider?.GetSnapshot().Timer ?? 0;
        return $"{s:00}";
    }

    public void SetMatchInfoProvider(IMatchInfoProvider provider)
    {
        MatchInfoProvider = provider;
        MatchInfoProviderChanged?.Invoke(provider);
    }

    public void SetMatchMode(MatchMode mode)
    {
        currentMatchMode = mode;
        Debug.Log($"[GameState] MatchMode = {mode}");
    }

    public void SetRoomInfo(string code, bool isHost = false)
    {
        roomCode = code;
        isRoomHost = isHost;
        Debug.Log($"[Game State] Room = {code}, Host = {isHost}");
    }
}