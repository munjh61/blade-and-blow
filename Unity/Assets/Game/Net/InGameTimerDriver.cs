using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class InGameTimerDriver : MonoBehaviour
{
    private PhotonNetworkManager _mgr;
    private IMatchInfoWriter _writer;

    // 인게임 타이머
    private double _startAtSec = -1;
    private int _durMs = 0;
    private double _endAtSec = -1;
    private int _lastShownRemain = -1;

    // 서든데스 룰
    private bool _suddenArmed = false;
    private const int DEFAULT_DOT_INTERVAL_MS = 1000;
    private const int DEFAULT_DOT_DAMAGE = 5;

    private float _nextRebindAt = 0f;

    private void OnEnable()
    {
        _mgr = PhotonNetworkManager.Instance;

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.MatchInfoProviderChanged += OnGsmProviderChanged;

        RebindWriter("OnEnable");

        if (_mgr?.InRoom == true)
            PrimeFromRoom(_mgr.CurrentRoom.CustomProperties);

        if (_mgr != null)
            _mgr.RoomPropertiesUpdated += OnRoomPropsUpdated;
    }

    private void OnDisable()
    {
        if (_mgr != null)
            _mgr.RoomPropertiesUpdated -= OnRoomPropsUpdated;

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.MatchInfoProviderChanged -= OnGsmProviderChanged;

        _writer = null;
    }

    // ===== Writer 바인딩 =====
    private void OnGsmProviderChanged(IMatchInfoProvider _)
    {
        RebindWriter("GSM");
    }

    private void RebindWriter(string reason)
    {
        if (_writer is Object uo && uo == null)
            _writer = null;

        IMatchInfoWriter preferred = null;

        preferred = GameStateManager.Instance?.MatchInfoProvider as IMatchInfoWriter;

        if (preferred == null)
        {
            var prov = FindFirstObjectByType<PunMatchInfoProvider>();
            preferred = prov as IMatchInfoWriter;
        }

        if (!ReferenceEquals(preferred, _writer))
        {
            _writer = preferred;
            
            if (_writer != null && _lastShownRemain >= 0)
                _writer.SetMatchRemainSeconds(_lastShownRemain);

            if (_writer is Object obj)
                Debug.Log($"[InGameTimerDriver] writer bound via {reason}: {obj.name} (id={obj.GetInstanceID()})");
            else
                Debug.Log($"[InGameTimerDriver] writer {(preferred==null ? "cleared" : "bound")} via {reason}");
        }
    }

    // ===== 룸 프라임 & 업데이트 =====
    private void PrimeFromRoom(Hashtable props)
    {
        if (props == null) return;
        if (props.ContainsKey(MatchingCore.ROOM_PROP_START_AT))
            _startAtSec = ToDouble(props[MatchingCore.ROOM_PROP_START_AT]);

        if (props.ContainsKey(MatchingCore.ROOM_PROP_DURATION))
            _durMs = System.Convert.ToInt32(props[MatchingCore.ROOM_PROP_DURATION]);

        if (_startAtSec > 0 && _durMs > 0)
            _endAtSec = _startAtSec + (_durMs / 1000.0);
    }

    private void OnRoomPropsUpdated(Hashtable changed)
    {
        if (changed == null) return;

        bool startChanged = false, durChanged = false;

        if (changed.ContainsKey(MatchingCore.ROOM_PROP_START_AT))
        {
            _startAtSec = ToDouble(changed[MatchingCore.ROOM_PROP_START_AT]);
            startChanged = true;
        }
        if (changed.ContainsKey(MatchingCore.ROOM_PROP_DURATION))
        {
            _durMs = System.Convert.ToInt32(changed[MatchingCore.ROOM_PROP_DURATION]);
            durChanged = true;
        }

        if ((startChanged || durChanged) && _startAtSec > 0 && _durMs > 0)
            _endAtSec = _startAtSec + (_durMs / 1000.0);

        if (changed.ContainsKey(MatchingCore.ROOM_PROP_SUDDEN))
        {
            bool sd = System.Convert.ToBoolean(changed[MatchingCore.ROOM_PROP_SUDDEN]);
            if (sd) _suddenArmed = true;
        }
    }

    private void Update()
    {
        if (_mgr == null || !_mgr.InRoom) return;

        if (Time.unscaledTime >= _nextRebindAt)
        {
            _nextRebindAt = Time.unscaledTime + 0.4f;
            if (_writer == null || (_writer is Object uo && uo == null))
                RebindWriter("update");
        }

        double now = _mgr.Time;

        if (_startAtSec > 0 && _durMs > 0)
        {
            double end = _endAtSec > 0 ? _endAtSec : (_startAtSec + _durMs / 1000.0);
            double remain = end - now;
            int remainSec = Mathf.Max(0, Mathf.CeilToInt((float)remain));

            if (remainSec != _lastShownRemain)
            {
                _writer?.SetMatchRemainSeconds(remainSec);
                _lastShownRemain = remainSec;
            }

            // 타이머 종료시 1회 트리거
            if (!_suddenArmed && remain <= 0.0)
            {
                _suddenArmed = true;

                if (_mgr.IsMasterClient)
                {
                    _mgr.SetRoomProperties(new System.Collections.Generic.Dictionary<string, object>
                    {
                        { MatchingCore.ROOM_PROP_SUDDEN, true },
                        { MatchingCore.ROOM_PROP_DOT_AT, now },
                        { MatchingCore.ROOM_PROP_DOT_INT_MS, DEFAULT_DOT_INTERVAL_MS },
                        { MatchingCore.ROOM_PROP_DOT_DMG,    DEFAULT_DOT_DAMAGE      }
                    });
                }
            }
        }
    }

    private static double ToDouble(object v) => System.Convert.ToDouble(v);
}
