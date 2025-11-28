using System;

public enum MatchMode
{
    None,
    SingleMatch,
    TeamMatch,
    PrivateMatch
}

public readonly struct MatchInfoSnapshot
{
    public readonly string Mode;
    public readonly int MaxPlayers;
    public readonly int CurrentPlayers;
    public readonly bool IsPrivate;
    public readonly string RoomCode;
    public readonly string StatusText;
    public readonly int Timer;

    public MatchInfoSnapshot(string mode, int maxPlayers, int currentPlayers, bool isPrivate, string roomCode, string statusText, int timer)
    {
        Mode = mode;
        MaxPlayers = maxPlayers;
        CurrentPlayers = currentPlayers;
        IsPrivate = isPrivate;
        RoomCode = roomCode;
        StatusText = statusText;
        Timer = timer;
    }
}

public interface IMatchInfoProvider
{
    MatchInfoSnapshot GetSnapshot();

    event Action<MatchInfoSnapshot> OnChanged;

}
