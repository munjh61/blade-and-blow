// RelayMatchInfoWriter.cs
using UnityEngine;

public sealed class RelayMatchInfoWriter : IMatchInfoWriter
{
    private IMatchInfoWriter _target;

    // === 스냅샷 ===
    private enum Mode { None, Single, Team, Private }
    private Mode _mode = Mode.None;
    private string _roomCode = "";

    private int _cur = 0, _max = 0;

    private enum Status { None, Finding, Waiting, Starting }
    private Status _status = Status.None;
    private int _startingIn = -1;
    private int _remainSec  = -1;

    private bool _privateCleared = true;

    public IMatchInfoWriter Target
    {
        get => _target;
        set
        {
            var changed = !ReferenceEquals(_target, value);
            _target = value;
            if (changed) FlushToTarget();
        }
    }

    public void FlushToTarget()
    {
        if (_target == null) return;

        switch (_mode)
        {
            case Mode.Single:  _target.SetModeSingle();  break;
            case Mode.Team:    _target.SetModeTeam();    break;
            case Mode.Private: _target.SetModePrivate(_roomCode ?? ""); break;
        }

        if (_privateCleared || _mode != Mode.Private)
            _target.ClearPrivate();

        _target.SetPlayerCounts(_cur, _max);

        switch (_status)
        {
            case Status.Finding:  _target.SetStatusFinding();  break;
            case Status.Waiting:  _target.SetStatusWaiting();  break;
            case Status.Starting: _target.SetStatusStarting(); break;
        }

        if (_startingIn >= 0) _target.SetStatusStartingIn(_startingIn);
        if (_remainSec  >= 0) _target.SetMatchRemainSeconds(_remainSec);
    }

    // === IMatchInfoWriter 구현 ===
    public void SetModeSingle()
    {
        _mode = Mode.Single;
        _roomCode = "";
        _privateCleared = true;
        _target?.SetModeSingle();
        _target?.ClearPrivate();
    }

    public void SetModeTeam()
    {
        _mode = Mode.Team;
        _roomCode = "";
        _privateCleared = true;
        _target?.SetModeTeam();
        _target?.ClearPrivate();
    }

    public void SetModePrivate(string roomCodeOrEmpty)
    {
        _mode = Mode.Private;
        _roomCode = roomCodeOrEmpty ?? "";
        _privateCleared = false;
        _target?.SetModePrivate(_roomCode);
    }

    public void ClearPrivate()
    {
        _privateCleared = true;
        _roomCode = "";
        _target?.ClearPrivate();
    }

    public void SetPlayerCounts(int current, int max)
    {
        _cur = Mathf.Max(0, current);
        _max = Mathf.Max(0, max);
        _target?.SetPlayerCounts(_cur, _max);
    }

    public void SetStatusFinding()
    {
        _status = Status.Finding;
        _target?.SetStatusFinding();
    }

    public void SetStatusWaiting()
    {
        _status = Status.Waiting;
        _target?.SetStatusWaiting();
    }

    public void SetStatusStarting()
    {
        _status = Status.Starting;
        _target?.SetStatusStarting();
    }

    public void SetStatusStartingIn(int seconds)
    {
        _startingIn = Mathf.Max(0, seconds);
        _status = Status.Starting;
        _target?.SetStatusStartingIn(_startingIn);
    }

    public void SetMatchRemainSeconds(int seconds)
    {
        _remainSec = Mathf.Max(0, seconds);
        _target?.SetMatchRemainSeconds(_remainSec);
    }
}
