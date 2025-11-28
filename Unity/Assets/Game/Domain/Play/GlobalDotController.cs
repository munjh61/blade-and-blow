using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GlobalDotController : MonoBehaviour
{
    private PhotonNetworkManager _mgr;

    private bool _enabledSD = false;
    private double _dotStartAtSec = -1;
    private int _dotIntervalMs = 1000;
    private int _dotDamage = 5;

    private void OnEnable()
    {
        _mgr = PhotonNetworkManager.Instance;
        if (_mgr == null) { enabled = false; return; }

        // 스냅샷에서 서든데스 이미 켜졌는지 확인
        var props = _mgr.CurrentRoom?.CustomProperties;
        if (props != null) PrimeFromRoom(props);

        _mgr.JoinedRoom += OnJoinedRoom;
        _mgr.RoomPropertiesUpdated += OnRoomPropsUpdated;
        _mgr.LeftRoom += OnLeftRoom; // 방 나가면 리셋
    }

    private void OnDisable()
    {
        if (_mgr != null)
        {
            _mgr.JoinedRoom -= OnJoinedRoom;
            _mgr.RoomPropertiesUpdated -= OnRoomPropsUpdated;
            _mgr.LeftRoom -= OnLeftRoom;
        }
    }

    private void OnJoinedRoom()
    {
        var props = _mgr.CurrentRoom?.CustomProperties;
        if (props != null) PrimeFromRoom(props);
    }

    private void OnLeftRoom() => ResetState();

    private void ResetState()
    {
        _enabledSD = false;
        _dotStartAtSec = -1;
        _dotIntervalMs = 1000;
        _dotDamage = 5;
    }

    private void PrimeFromRoom(Hashtable props)
    {
        if (props.ContainsKey(MatchingCore.ROOM_PROP_SUDDEN) &&
            System.Convert.ToBoolean(props[MatchingCore.ROOM_PROP_SUDDEN]))
        {
            _enabledSD = true;
            if (props.ContainsKey(MatchingCore.ROOM_PROP_DOT_AT))
                _dotStartAtSec = System.Convert.ToDouble(props[MatchingCore.ROOM_PROP_DOT_AT]);
            if (props.ContainsKey(MatchingCore.ROOM_PROP_DOT_INT_MS))
                _dotIntervalMs = System.Convert.ToInt32(props[MatchingCore.ROOM_PROP_DOT_INT_MS]);
            if (props.ContainsKey(MatchingCore.ROOM_PROP_DOT_DMG))
                _dotDamage = System.Convert.ToInt32(props[MatchingCore.ROOM_PROP_DOT_DMG]);

            if (_mgr.IsMasterClient)
                PlayerManagerPunBehaviour.Instance?.Master_ArmSuddenDeath(
                    _dotStartAtSec, _dotIntervalMs, _dotDamage, _mgr.Time);
        }
    }

    private void OnRoomPropsUpdated(Hashtable changed)
    {
        if (changed == null) return;

        if (changed.ContainsKey(MatchingCore.ROOM_PROP_SUDDEN))
        {
            bool sd = System.Convert.ToBoolean(changed[MatchingCore.ROOM_PROP_SUDDEN]);
            _enabledSD = sd;
            if (!sd) return; // 꺼짐 처리도 가능
        }

        if (!_enabledSD) return;

        if (changed.ContainsKey(MatchingCore.ROOM_PROP_DOT_AT))
            _dotStartAtSec = System.Convert.ToDouble(changed[MatchingCore.ROOM_PROP_DOT_AT]);
        if (changed.ContainsKey(MatchingCore.ROOM_PROP_DOT_INT_MS))
            _dotIntervalMs = System.Convert.ToInt32(changed[MatchingCore.ROOM_PROP_DOT_INT_MS]);
        if (changed.ContainsKey(MatchingCore.ROOM_PROP_DOT_DMG))
            _dotDamage = System.Convert.ToInt32(changed[MatchingCore.ROOM_PROP_DOT_DMG]);

        if (_mgr.IsMasterClient)
            PlayerManagerPunBehaviour.Instance?.Master_ArmSuddenDeath(
                _dotStartAtSec, _dotIntervalMs, _dotDamage, _mgr.Time);
    }

    private void Update()
    {
        if (!_enabledSD || _mgr == null || !_mgr.InRoom) return;

        if (!_mgr.IsMasterClient) return;

        PlayerManagerPunBehaviour.Instance?.Master_TickSuddenDeath(_mgr.Time);
    }
}
