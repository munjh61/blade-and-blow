public interface IMatchInfoWriter
{
    // 모드: 반드시 이 3가지만
    void SetModeSingle();
    void SetModeTeam();
    void SetModePrivate(string roomCodeOrEmpty);

    // 인원: n / max
    void SetPlayerCounts(int current, int max);

    // 상태: Finding → Waiting → Starting
    void SetStatusFinding();
    void SetStatusWaiting();
    void SetStatusStarting();

    void SetStatusStartingIn(int seconds);

    void SetMatchRemainSeconds(int seconds);


    // Private off (코드 숨김)
    void ClearPrivate();
}
