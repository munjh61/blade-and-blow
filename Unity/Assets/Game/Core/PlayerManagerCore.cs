using Game.Domain;
using Game.Net;
using System;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public sealed class PlayerManagerCore : IPlayerStateView
{
    // 데이터 조작을 위한 접근 참조
    // _cache: 포톤 네트워크를 통해 업데이트 되는 플레이어 정보
    // _mine: 포톤 네트워크를 통해 업데이트 되는 권한 정보
    // _bus: 포톤 네트워크를 사용하여 전송/수신용 터널
    // _codec: 인코딩/디코딩 용 참조 변수 
    private readonly Dictionary<int, PlayerInfoData> _cache = new();
    private readonly Dictionary<int, bool> _mine = new();
    private readonly List<int> _alivePlayers = new();

    private readonly IRoomBus _bus;
    private readonly INetCodec _codec;

    // _accessor: 플레이어블 오브젝트 제어를 위한 참조 변수
    private IPlayerAccessor _accessor;

    // 내부 참조와 외부 접근용 현재 Master 여부
    private bool _isMaster;
    public bool IsMaster => _isMaster;


    // 스태미나 시뮬레이션 초기화
    private sealed class StaminaSim
    {
        public int stamina;
        public bool sprinting;
        public float lastT;
        public int lastBroadcast;
    }
    private readonly Dictionary<int, StaminaSim> _stims = new();
    private readonly List<int> _tickActors = new(32);

    private int _healthMax = 100;
    private int _staminaMax = 100;
    private float _regenPerSec = 30f;
    private float _drainPerSec = 32f;
    private int _sprintMinToStart = 10;
    private float _broadcastHz = 8f;

    private float _nextBroadcastAt;

    // 서든데스 상태
    private bool _dotEnabled = false;
    private double _dotStartAtSec = -1;
    private int _dotIntervalMs = 1000;
    private int _dotDamage = 5;
    private long _lastDotTick = -1;

    public void ConfigureStamina(int max, float regen, float drain, int minToStart, float bps)
    {
        _staminaMax = Mathf.Max(1, max);
        _regenPerSec = Mathf.Max(0f, regen);
        _drainPerSec = Mathf.Max(0f, drain);
        _sprintMinToStart = Mathf.Max(0, minToStart);
        _broadcastHz = Mathf.Max(1f, bps);
    }

    public event Action<PlayerId, int> OnEquipApplied;
    public event Action<PlayerId, int> OnStaminaApplied;
    public event Action<PlayerId, int> OnHealthApplied;
    public event Action<PlayerId, bool> OnAuthorityChanged;
    public event Action<Kill> OnPlayerDied;
    public event Action OnGameOver;
    private event Action<PlayerId> OnPlayerRemoved;

    // 초기화
    public PlayerManagerCore(IRoomBus bus, INetCodec codec, IPlayerAccessor accessor)
    {
        _bus = bus; _codec = codec; _accessor = accessor;
        bus.EventReceived += OnRoomEvent;
        OnPlayerRemoved += HandlePlayerRemoval;
        OnGameOver += Master_NotifyGameOver;
    }

    public void Dispose() {
        _bus.EventReceived -= OnRoomEvent;
        OnPlayerRemoved -= HandlePlayerRemoval;
        OnGameOver -= Master_NotifyGameOver;
    }

    private readonly List<int> _aliveScratch = new(32);
    private IEnumerable<int> AliveActors()
    {
        _aliveScratch.Clear();
        foreach (var kv in _cache)
            if (kv.Value.hp > 0) _aliveScratch.Add(kv.Key);
        return _aliveScratch;
    }

    public void EnsureDefaults(int actor)
    {
        if (actor <= 0) return;

        if (!_cache.TryGetValue(actor, out var cur))
        {
            var name = PhotonNetworkManager.Instance.NickName ?? $"Player{actor}";
            var userId = UserSession.Username ?? "Unknown";
            var initial = new PlayerInfoData { actor = actor, name = name, userId = userId, hp = _healthMax, stamina = _staminaMax, equipId = -1 };
            Local_Register(new PlayerId(actor), initial);
        }
        else
        {
            var s = EnsureStim(actor);
            if (s.stamina <= 0)
            {
                var baseSt = cur.stamina <= 0 ? _staminaMax : cur.stamina;
                s.stamina = Mathf.Clamp(baseSt, 0, _staminaMax);
                s.lastBroadcast = s.stamina;
                s.lastT = Time.realtimeSinceStartup;
            }
        }
    }

    public bool TryGetState(int actor, out PlayerInfoData d)
    {
        if (_cache.TryGetValue(actor, out d))
        {
            if (_stims.TryGetValue(actor, out var s))
                d.stamina = Mathf.RoundToInt(s.stamina);
            // 캐시에 있는데도 0이면 방어
            if (d.stamina <= 0) d.stamina = _staminaMax;
            return true;
        }
        d = default;
        return false;
    }

    private void Master_NotifyGameOver()
    {
        if (!_isMaster) return;

        _bus.Broadcast(NetEvt.GameOver, null);
    }

    // 플레이어 제거 시 호출되어 게임 종료 조건을 확인하는 이벤트 핸들러
    private void HandlePlayerRemoval(PlayerId id)
    {
        if (!_isMaster) return;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PlayScene") return;

        // 살아남은 플레이어가 1명 이하일 경우 게임오버 이벤트 호출
        if (_alivePlayers.Count <= 1)
        {
            OnGameOver?.Invoke();
        }
    }

    public void SetMaster(bool isMaster) => _isMaster = isMaster;
    public void SetAccessor(IPlayerAccessor accessor) => _accessor = accessor;

    public void SetMine(PlayerId id, bool isMine)
    {
        if (_mine.TryGetValue(id.Value, out var prev) && prev == isMine) return;
        _mine[id.Value] = isMine;
        OnAuthorityChanged?.Invoke(id, isMine);
    }

    // ActorId/ TryGet -> Id를 이용한 Cache 값 가져오기
    public bool IsMine(PlayerId id) => _mine.TryGetValue(id.Value, out var v) && v;
    public void ClearAuthorities() => _mine.Clear();

    public bool TryGet(int actor, out PlayerInfoData d) => _cache.TryGetValue(actor, out d);

    //
    // 장비 착용
    //
    public void Master_SetEquip(PlayerId id, int equipId)
    {
        // Master만 실행 가능하도록 구성
        if (!_isMaster) return;
        // 만약 캐시에 일치하는 플레이어 id가 없다면, 해당 id값으로 캐시 기본값 구성
        if (!_cache.TryGetValue(id.Value, out var d))
        {
            d = new PlayerInfoData { actor = id.Value, hp = _healthMax, stamina = _staminaMax, equipId = -1 };
            _cache[id.Value] = d;
        }
        // 만약 캐시에 저장된 착용 장비 인덱스와 수신된 인덱스가 일치하면 조기 리턴
        if (d.equipId == equipId) return;

        // 플레이어 정보 갱신
        d.equipId = equipId;
        _cache[id.Value] = d;

        // 로컬 선제 적용, 없어도 될듯
        _accessor.ApplyImmediate(id, d);

        OnEquipApplied?.Invoke(id, equipId);

        // 브로드캐스트로 모든 클라이언트에게 장비 인덱스만 갱신되도록 전달
        _bus.Broadcast(NetEvt.EquipDelta, _codec.EncodeEquipDelta(id.Value, equipId));
    }
    public void Master_SetAlive(PlayerId id)
    {
        if (!_isMaster) return;
        if (!_alivePlayers.Contains(id.Value))
        {
            _alivePlayers.Add(id.Value);
        }
    }

    public void Master_SyncCacheAndAlive() {
        if (!_isMaster) return;
        var snap = new GameSnapshotData
        {
            players = new List<PlayerInfoData>(_cache.Values).ToArray(),
            alivePlayerActors = new List<int>(_alivePlayers).ToArray()
        };
        _bus.Broadcast(NetEvt.FullSnapshot, _codec.EncodeSnapshot(snap));

    }

    // 서든데스 활성화
    public void Master_ArmSuddenDeath(double startAtSec, int intervalMs, int damage, double nowSec)
    {
        _dotEnabled = true;
        _dotStartAtSec = startAtSec;
        _dotIntervalMs = Math.Max(1, intervalMs);
        _dotDamage = Math.Max(1, damage);

        // 마스터 스위치 대비: 과거 틱은 모두 소비 처리
        _lastDotTick = (long)Math.Floor(((nowSec - _dotStartAtSec) * 1000.0) / _dotIntervalMs);
    }

    public void Master_TryApplyGlobalDot(double nowSec)
    {
        if (!_dotEnabled || _dotStartAtSec <= 0 || _dotIntervalMs <= 0 || _dotDamage <= 0) return;

        long tick = (long)Math.Floor(((nowSec - _dotStartAtSec) * 1000.0) / _dotIntervalMs);
        if (tick <= _lastDotTick) return;

        // 누락된 틱 보정
        for (long i = _lastDotTick + 1; i <= tick; i++)
        {
            // 1) 실제 데미지 적용 (권위 서버: 마스터만)
            foreach (var actor in AliveActors())
            {
                Master_ApplyDamageToActor(actor, _dotDamage);
            }

            // 2) 모든 클라에 틱 알림(효과/사운드 동기화용). HP는 별도 동기화 경로로 반영됨.
            _bus?.Broadcast(NetEvt.GlobalDotTick, MakeDotPayload(i, _dotDamage), reliable: false);
        }

        _lastDotTick = tick;
    }

    private static object MakeDotPayload(long tick, int dmg)
    {
        return new Hashtable { { "i", (int)tick }, { "amt", dmg } };
    }

    private void Master_ApplyDamageToActor(int targetActor, int dmg)
    {
        if (!_isMaster) return;

        if (!_cache.TryGetValue(targetActor, out var pdTarget))
            pdTarget = new PlayerInfoData { actor = targetActor, userId = "unknown", hp = _healthMax, stamina = _staminaMax, equipId = -1 };

        int prevHp = pdTarget.hp;
        int newHp = Mathf.Max(0, prevHp - Mathf.Max(0, dmg));
        if (newHp == prevHp) return;

        bool diedNow = (prevHp > 0 && newHp == 0);

        pdTarget.hp = newHp;
        _cache[targetActor] = pdTarget;

        //_accessor?.ApplyImmediate(new PlayerId(targetActor), pdTarget);

        // 전파
        _bus.Broadcast(NetEvt.PlayerStateUpdate, _codec.EncodeDelta(pdTarget));

        if (diedNow)
        {
            var killEvent = new Kill
            {
                killerActor = -1,
                killerName = "SuddenDeath",
                killerId = "system",
                killerTeam = TeamId.None,
                victimActor = pdTarget.actor,
                victimName = pdTarget.name ?? $"Player{pdTarget.actor}",
                victimId = pdTarget.userId ?? "unknown",
                victimTeam = pdTarget.team,
                weaponId = 3,
                weapon = "SuddenDeath",
            };
            Master_OnPlayerDied(new PlayerId(targetActor), killEvent, _dotDamage);
        }
    }
    
    public void Master_InstantKill(PlayerId targetId, string causeOfDeath)
    {
        if (!_isMaster) return;

        if (!_cache.TryGetValue(targetId.Value, out var pdTarget))
            pdTarget = new PlayerInfoData { actor = targetId.Value, hp = _healthMax, stamina = _staminaMax };

        if (pdTarget.hp <= 0) return;

        pdTarget.hp = 0;
        pdTarget.death += 1;

        var killEvent = new Kill
        {
            killerActor = -1,
            killerName = causeOfDeath,
            killerId = "system",
            killerTeam = TeamId.None,
            victimActor = pdTarget.actor,
            victimName = pdTarget.name ?? $"Player{pdTarget.actor}",
            victimId = pdTarget.userId ?? "unknown",
            victimTeam = pdTarget.team,
            weaponId = 3,
            weapon = causeOfDeath,
        };
        Master_OnPlayerDied(targetId, killEvent);

        _cache[targetId.Value] = pdTarget;
        _accessor?.ApplyDeath(targetId, pdTarget); // 로컬에서 즉시 사망 연출 적용
        _bus.Broadcast(NetEvt.PlayerStateUpdate, _codec.EncodeDelta(pdTarget));
    }

    public void Local_RequestInstantKill(PlayerId requesterId, string causeOfDeath)
    {
        if (_isMaster)
        {
            Master_InstantKill(requesterId, causeOfDeath);
        }
        else
        {
            // 마스터에게 처리를 요청하는 이벤트 전송
            var master = Photon.Pun.PhotonNetwork.MasterClient;
            _bus.SendTo(new PlayerId(master.ActorNumber), NetEvt.InstantKillRequest, causeOfDeath);
        }
    }

    // 플레이어 공격 요청 처리
    public void Master_RequestHit(PlayerId targetId, int damage, int attackerId)
    {
        // Master가 아니면, Master에게 Damage요청을 해준다.
        if (!_isMaster)
        {
            var payload = _codec.EncodeHitRequest(attackerId, targetId.Value, damage);
            var master = PhotonNetworkManager.Instance.MasterClient;
            _bus.SendTo(new PlayerId(master.ActorNumber), NetEvt.HitRequestDelta, payload);
        }
        // Master라면 바로 Broadcast 호출
        else
        {
            Master_BroadcastHit(targetId, damage, attackerId);
            return;
        }


    }
    // Master -> All Client  (로직 검증 후 송신)
    public void Master_BroadcastHit(PlayerId targetId, int damage, int attackerId)
    {
        // Master만 Broadcasting 가능
        if (!_isMaster) return;
        if (attackerId == targetId.Value) return;
        if(!_cache.TryGetValue(attackerId, out var pdAtt)) return; // ★ 공격자 엔트리 없으면 브로드캐스트하지 않음

        // 오직 Cache에 있는 정보만을 신뢰한다.
        // Cache(Master Client Local 정보)에 정보가 없으면 새로 생성.

        // 1) Master Cache에서 targetId, attackerId 정보 가져오기
        if (!_cache.TryGetValue(targetId.Value, out var pdTarget))
            pdTarget = new PlayerInfoData { actor = targetId.Value, hp = _healthMax, stamina = _staminaMax, equipId = -1, kill = 0, death = 0, totDamage = 0, consecutiveKill = 0, lastKillInterval=0 };

        // 2) 판정
        // damage 검증 후, Master cache에서 값 가져와서 targetId HP 갱신 
        damage = Mathf.Clamp(damage, 0, 150);
        int prevHp = pdTarget.hp;
        int newHp = Mathf.Max(0, prevHp - damage);

        if (newHp == prevHp)
            return;

        // Attacker totDamage | Target player: Death && Attacker player: kill 처리
        pdTarget.hp = newHp;
        pdAtt.totDamage += damage;

        bool diedNow = (prevHp > 0 && newHp == 0);
        if (diedNow)
        {
            pdTarget.death += 1;
            pdAtt.kill += 1;
            if (pdAtt.kill >=1 && pdAtt.lastKillInterval <= 10) {
                pdAtt.consecutiveKill += 1;
            }
            
            var killEvent = new Kill
            {
                killerActor = pdAtt.actor,
                killerName  = pdAtt.name ?? $"Player{pdAtt.actor}",
                killerId    = pdAtt.userId ?? "unknown",
                killerTeam  = pdAtt.team,
                victimActor = pdTarget.actor,
                victimName  = pdTarget.name ?? $"Player{pdTarget.actor}",
                victimId    = pdTarget.userId ?? "unknown",
                victimTeam  = pdTarget.team,
                weaponId    = pdAtt.equipId,
                weapon      = MapWeaponName(pdAtt.equipId)
            };
            // 플레이어가 사망했으므로, 생존자 목록에서 제거하고 게임 종료 조건을 확인
            Master_OnPlayerDied(targetId, killEvent, damage);
        }

        // 3) Master 캐시 값 갱신
        _cache[targetId.Value] = pdTarget;
        _cache[attackerId] = pdAtt;

        // 4) Master 로컬 적용
        _accessor?.ApplyImmediate(targetId, pdTarget);
        _accessor?.ApplyImmediate(new PlayerId(attackerId), pdAtt);

        // 5) 새로운 PlayerStateUpdate 이벤트로 전체 데이터 브로드캐스트
        _bus.Broadcast(NetEvt.PlayerStateUpdate, _codec.EncodeDelta(pdTarget));
        _bus.Broadcast(NetEvt.PlayerStateUpdate, _codec.EncodeDelta(pdAtt));
    }

    // 마스터 클라이언트에서 플레이어 사망 시 호출되는 메서드
    public void Master_OnPlayerDied(PlayerId id, Kill killEvent, int damage = 0)
    {
        if (!_isMaster) return;

        // 살아있는 플레이어 목록에서 사망한 플레이어 제거
        if (_alivePlayers.Contains(id.Value))
        {
            _alivePlayers.Remove(id.Value);
            // 플레이어 제거 이벤트를 호출하여 게임 종료 조건 검사
            OnPlayerRemoved?.Invoke(id);
            // [수정] 최신 생존자 목록을 모든 클라이언트에게 전파
            _bus.Broadcast(NetEvt.AlivePlayersUpdate, _codec.EncodeAlivePlayers(_alivePlayers.ToArray()));
        }

        bool isTeamKill = killEvent.killerTeam != TeamId.None && killEvent.killerTeam == killEvent.victimTeam;
        KillReporter.Enqueue(killEvent, damage, isTeamKill);
        _bus.Broadcast(NetEvt.KillEvent, _codec.EncodeKillEvent(killEvent));
    }

    // 스테미너 시뮬 계산
    private StaminaSim EnsureStim(int actor)
    {
        if (!_stims.TryGetValue(actor, out var s))
            _stims[actor] = s = new StaminaSim { stamina = _staminaMax, lastT = Time.realtimeSinceStartup, lastBroadcast = _staminaMax };
        return s;
    }

    public void Local_SendStaminaIntent(PlayerId id, StaminaIntentType type, int payload = 0)
    {
        if (_isMaster) { Master_ApplyStaminaIntent(id, type, payload); return; }
        _bus.Broadcast(NetEvt.StaminaIntent, _codec.EncodeStaminaIntent((byte)type, payload));
    }

    private void Master_ApplyStaminaIntent(PlayerId sender, StaminaIntentType type, int payload)
    {
        if (!_isMaster) return;

        var s = EnsureStim(sender.Value);
        IntegrateOne(s);
        switch (type)
        {
            case StaminaIntentType.StartSprint:
                if (s.stamina >= _sprintMinToStart) s.sprinting = true;
                break;

            case StaminaIntentType.StopSprint:
                s.sprinting = false;
                break;

            case StaminaIntentType.TryAction:
                if (payload <= 0) break;
                if (s.stamina >= payload) s.stamina -= payload;
                break;
        }

        // 캐시 반영 + 전파
        ApplyStaminaToCacheAndMaybeBroadcast(sender.Value, s, forceImmediate: true);
    }


    public void Master_Tick(float nowRealtime)
    {
        if (!_isMaster) return;

        // 전파 레이트 제한(글로벌)
        bool canBroadcastNow = (Time.time >= _nextBroadcastAt);
        if (canBroadcastNow) _nextBroadcastAt = Time.time + 1f / _broadcastHz;

        _tickActors.Clear();
        foreach (var k in _cache.Keys)
            _tickActors.Add(k);

        for (int i = 0; i < _tickActors.Count; i++)
        {
            int actor = _tickActors[i];

            // 키가 그 사이에 제거되었을 수 있으니 확인
            if (!_cache.ContainsKey(actor))
                continue;

            var s = EnsureStim(actor);    // 시뮬 상태 확보
            IntegrateOne(s);              // 프레임 적분

            // 캐시 갱신 + 필요 시 델타 브로드캐스트
            ApplyStaminaToCacheAndMaybeBroadcast(actor, s, forceImmediate: false, allowSend: canBroadcastNow);
        }
    }

    private void IntegrateOne(StaminaSim s)
    {
        float now = Time.realtimeSinceStartup;
        float dt = Mathf.Max(0f, now - s.lastT);
        s.lastT = now;

        float rate = s.sprinting ? -_drainPerSec : _regenPerSec;
        if (Mathf.Abs(rate) < 0.0001f || dt <= 0f) return;

        float v = s.stamina + rate * dt;
        s.stamina = Mathf.Clamp(Mathf.RoundToInt(v), 0, _staminaMax);

        if (s.stamina == 0) s.sprinting = false; // 바닥나면 스프린트 자동 해제
    }

    private void ApplyStaminaToCacheAndMaybeBroadcast(int actor, StaminaSim s, bool forceImmediate, bool allowSend = true)
    {
        // 캐시 갱신
        if (!_cache.TryGetValue(actor, out var d))
            d = new PlayerInfoData { actor = actor, hp = _healthMax, stamina = _staminaMax, equipId = -1 };

        if (d.stamina != s.stamina)
        {
            d.stamina = s.stamina;
            _cache[actor] = d;

            _accessor?.ApplyImmediate(new PlayerId(actor), d);
            OnStaminaApplied?.Invoke(new PlayerId(actor), d.stamina);
        }

        // 델타 전파 (변화가 없으면 전파하지 않음)
        if ((forceImmediate || allowSend) && s.stamina != s.lastBroadcast)
        {
            s.lastBroadcast = s.stamina;
            _bus.Broadcast(NetEvt.StaminaDelta, _codec.EncodeStaminaDelta(actor, s.stamina));
        }
    }

    // 플레이어가 방에 참여할 때 현재 _cache 정보 전체 스냅샷 적용해서 전달
    public void Master_OnPlayerJoined(PlayerId target)
    {
        if (!_alivePlayers.Contains(target.Value))
        {
            _alivePlayers.Add(target.Value);
        }
        var snap = new GameSnapshotData {
            players = new List<PlayerInfoData>(_cache.Values).ToArray(),
            alivePlayerActors = new List<int>(_alivePlayers).ToArray() 
        };
        _bus.SendTo(target, NetEvt.FullSnapshot, _codec.EncodeSnapshot(snap));
    }

    // 로컬 캐시 등록
    // 최초 등록을 가정한 것으로 전체 PlayerDelta가 아닌 장비 선택 값만 갱신되도록 구성
    public void Local_Register(PlayerId id, PlayerInfoData initial)
    {
        var had = _cache.TryGetValue(id.Value, out var cur);

        if (initial.stamina <= 0 && !had) initial.stamina = _staminaMax;
        var s = EnsureStim(id.Value);
        s.stamina = Mathf.Clamp(initial.stamina, 0, _staminaMax);
        s.lastBroadcast = s.stamina;
        s.lastT = Time.realtimeSinceStartup;

        _cache[id.Value] = initial;

        if (_isMaster)
        {
            _bus.Broadcast(NetEvt.PlayerDelta, _codec.EncodeDelta(initial));
        }
        else
        {
            _bus.Broadcast(NetEvt.PlayerIntent, _codec.EncodeDelta(initial));
        }
    }

    public void Master_RegistPlayer(int actor, PlayerInfoData d)
    {
        if (!_isMaster) return;
        if (!_cache.TryGetValue(actor, out var cur))
            cur = new PlayerInfoData { actor = actor, hp = _healthMax, stamina = _staminaMax, equipId = -1 };

        d.actor = actor;
        bool nameChanged  = cur.name    != d.name;
        bool equipChanged = cur.equipId != d.equipId;
        bool teamChanged  = cur.team    != d.team;
        bool userIdChanged = cur.userId  != d.userId;

        if (!(nameChanged || equipChanged || userIdChanged || teamChanged)) return;

        cur.name    = nameChanged  ? d.name    : cur.name;
        cur.equipId = equipChanged ? d.equipId : cur.equipId;
        cur.userId  = userIdChanged ? d.userId : cur.userId;
        cur.team    = teamChanged  ? d.team    : cur.team;
        _cache[actor] = cur;
        _accessor?.ApplyImmediate(new PlayerId(actor), cur);
        var delta = new PlayerInfoData { actor = actor, name = cur.name, team = cur.team, userId = cur.userId, equipId = cur.equipId };
        _bus.Broadcast(NetEvt.PlayerDelta, _codec.EncodeDelta(delta));
    }

    public void ClearCache() => _cache.Clear();
    public void ApplyPlayerDelta(int actor, PlayerInfoData d)
    {
        if (!_cache.TryGetValue(actor, out var cur))
            cur = new PlayerInfoData { actor = actor, hp = _healthMax, stamina = _staminaMax, equipId = -1 };

        bool nameChanged = cur.name != d.name;
        bool equipChanged = cur.equipId != d.equipId;
        bool teamChanged = cur.team != d.team;
        bool userIdChanged = cur.userId  != d.userId;

        if (!(nameChanged || equipChanged || userIdChanged || teamChanged)) return;

        cur.name = nameChanged ? d.name : cur.name;
        cur.equipId = equipChanged ? d.equipId : cur.equipId;
        cur.userId  = userIdChanged ? d.userId  : cur.userId;
        cur.team = teamChanged ? d.team : cur.team;

        _cache[actor] = cur;
        _accessor?.ApplyImmediate(new PlayerId(actor), cur);
    }

    // 캐시만 제거, Unregister 발생시 포톤 네트워크가 PhotonView 오브젝트를 관리하므로 자동 제거됨
    public void Unregister(PlayerId id)
    {
        //_cache.Remove(id.Value);
        _stims.Remove(id.Value);
        _alivePlayers.Remove(id.Value);
        OnPlayerRemoved?.Invoke(id);
    }

    public void Local_RequestAim(PlayerId id)
    {
        if (_isMaster)
        {
            Master_Aim(id);
        } else
        {
            _bus.Broadcast(NetEvt.AimRequest, _codec.EncodeAimWeaponContext(id.Value));
        }
    }

    public void Master_Aim(PlayerId id)
    {
        if (!_isMaster) return;

        _bus.Broadcast(NetEvt.AimDelta, _codec.EncodeAimWeaponContext(id.Value));
    }
    public void Local_RequestShoot(PlayerId id, Vector3 aimOriginPosition, Quaternion aimOriginRotation, float changeDuration)
    {
        if (_isMaster)
        {
            Master_Shoot(id, aimOriginPosition, aimOriginRotation, changeDuration);
        } else
        {
            _bus.Broadcast(NetEvt.ShootRequest, _codec.EncodeActorWeaponContext(id.Value, aimOriginPosition, aimOriginRotation, changeDuration));
        }
    }
    //투사체 발사 이벤트
    public void Master_Shoot(PlayerId id, Vector3 aimOriginPosition, Quaternion aimOriginRotation, float changeDuration)
    {
        if (!_isMaster) return;

        _bus.Broadcast(NetEvt.ShootDelta, _codec.EncodeActorWeaponContext(id.Value, aimOriginPosition, aimOriginRotation, changeDuration));
    }
    
    private static string MapWeaponName(int id) => id switch
    {
        0 => "Knife",
        1 => "Bow",
        2 => "Wand",
        _ => "Unknown"
    };

    //
    // 이벤트 핸들러
    // RaiseEvent가 되면 _bus에서 EventReceived 이벤트로 Invoke
    // 해당 이벤트에 대한 구독 구현
    //
    private void OnRoomEvent(NetEvt evt, object payload, PlayerId sender)
    {
        switch (evt)
        {
            case NetEvt.FullSnapshot:
                {
                    var snap = _codec.DecodeSnapshot(payload);
                    _cache.Clear();
                    _alivePlayers.Clear();
                    foreach (var p in snap.players)
                    {
                        _cache[p.actor] = p;

                        var s = EnsureStim(p.actor);
                        s.stamina = Mathf.Clamp(p.stamina, 0, _staminaMax);
                        s.lastBroadcast = s.stamina;
                        s.lastT = Time.realtimeSinceStartup;

                        _accessor?.ApplyImmediate(new PlayerId(p.actor), p);
                    }

                    if (snap.alivePlayerActors != null)
                    {
                        _alivePlayers.AddRange(snap.alivePlayerActors);
                    }
                    break;
                }
            case NetEvt.PlayerIntent:
                {
                    var pd = _codec.DecodeDelta(payload);
                    Master_RegistPlayer(sender.Value, pd);
                    break;
                }
            case NetEvt.PlayerDelta:
                {
                    var pd = _codec.DecodeDelta(payload);
                    ApplyPlayerDelta(pd.actor, pd);
                    break;
                }
            case NetEvt.PlayerStateUpdate:
                {
                    var pd = _codec.DecodeDelta(payload);
                    if (!_cache.TryGetValue(pd.actor, out var cur))
                        cur = new PlayerInfoData { actor = pd.actor };

                    // 모든 정보 갱신
                    cur.name = pd.name;
                    cur.userId = pd.userId;
                    cur.team = pd.team;
                    cur.equipId = pd.equipId;
                    cur.hp = pd.hp;
                    cur.stamina = pd.stamina;
                    cur.death = pd.death;
                    cur.kill = pd.kill;
                    cur.totDamage = pd.totDamage;
                    cur.consecutiveKill = pd.consecutiveKill;

                    _cache[pd.actor] = cur;

                    // HP 변경 이벤트 호출
                    OnHealthApplied?.Invoke(new PlayerId(pd.actor), cur.hp);
                    // 스태미나 변경 이벤트 호출
                    OnStaminaApplied?.Invoke(new PlayerId(pd.actor), cur.stamina);

                    // 사망 처리
                    if (pd.hp <= 0) _accessor?.ApplyDeath(new PlayerId(pd.actor), cur);

                    // 장비 등 즉시 적용
                    _accessor?.ApplyImmediate(new PlayerId(pd.actor), cur);
                    break;
                }
            case NetEvt.EquipRequest:
                {
                    if (!_isMaster) break;

                    int equipId = _codec.DecodeEquipId(payload);

                    Master_SetEquip(sender, equipId);
                    break;
                }
            case NetEvt.EquipDelta:
                {
                    var (actor, eq) = _codec.DecodeEquipDelta(payload);

                    if (_cache.TryGetValue(actor, out var cur) && cur.equipId == eq) break;
                    if (!_cache.TryGetValue(actor, out var d)) d = new PlayerInfoData { actor = actor };
                    d.equipId = eq;
                    _cache[actor] = d;
                    _accessor?.ApplyImmediate(new PlayerId(actor), d);

                    OnEquipApplied?.Invoke(new PlayerId(actor), eq);
                    break;
                }
            case NetEvt.KillEvent:
                {
                    var killEvent = _codec.DecodeKillEvent(payload);
                    OnPlayerDied?.Invoke(killEvent);
                    break;
                }
            case NetEvt.PickupRequest:
                {
                    if (!_isMaster) break;
                    var (actor, token, equipId) = _codec.DecodePickupRequest(payload);
                    Master_SetEquip(sender, equipId);
                    break;
                }
            case NetEvt.ShootRequest:
                {
                    if (!_isMaster) break;
                    var (actor, changeDuration, aimOriginPosition, aimOriginRotation) = _codec.DecodeActorWeaponContext(payload);
                    Master_Shoot(sender, aimOriginPosition, aimOriginRotation, changeDuration);
                    break;
                }
            case NetEvt.ShootDelta:
                {
                    var (actor, changeDuration, aimOriginPosition, aimOriginRotation) = _codec.DecodeActorWeaponContext(payload);
                    _accessor?.ShootImmediate(new PlayerId(actor), aimOriginPosition, aimOriginRotation, changeDuration);
                    break;
                }
            case NetEvt.StaminaIntent:
                {
                    if (!_isMaster) break;
                    var (type, payload2) = _codec.DecodeStaminaIntent(payload);
                    Master_ApplyStaminaIntent(sender, (StaminaIntentType)type, payload2);
                    break;
                }
            case NetEvt.StaminaDelta:
                {
                    var (actor, st) = _codec.DecodeStaminaDelta(payload);
                    if (!_cache.TryGetValue(actor, out var d)) d = new PlayerInfoData { actor = actor };
                    if (d.stamina == st) break;

                    d.stamina = st;
                    _cache[actor] = d;

                    // 시뮬 상태도 동기화(마스터 외 클라)
                    var s = EnsureStim(actor);
                    s.stamina = st;
                    s.lastBroadcast = st;
                    s.lastT = Time.realtimeSinceStartup;

                    OnStaminaApplied?.Invoke(new PlayerId(actor), st);
                    break;
                }
            case NetEvt.HealthDelta:
                {
                    var (actor, hp) = _codec.DecodeHealthDelta(payload);
                    if (!_cache.TryGetValue(actor, out var d)) d = new PlayerInfoData { actor = actor };
                    if (d.hp == hp) break;

                    d.hp = hp;
                    _cache[actor] = d;

                    break;
                }
            case NetEvt.GlobalDotTick:
                {
                    var h = payload as Hashtable;
                    int tick = h != null && h.ContainsKey("i") ? Convert.ToInt32(h["i"]) : -1;
                    int amt = h != null && h.ContainsKey("amt") ? Convert.ToInt32(h["amt"]) : 0;

                    // TODO: 글로벌 펄스/화면 이펙트/사운드 등 연출만. HP 재적용 금지!

                    break;
                }

            // Client->Master: Hit Request 수신
            case NetEvt.HitRequestDelta:
                {

                    var (attackerId, targetId, damage) = _codec.DecodeHitRequest(payload);
                    Master_BroadcastHit(new PlayerId(targetId), damage, attackerId);
                    break;
                }

            // 즉사 요청 처리
            case NetEvt.InstantKillRequest:
            {
                if (!_isMaster) break;

                if (payload is string cause)
                {
                    Master_InstantKill(sender, cause);
                }
                break;
            }

            case NetEvt.GameOver:
                {
                    // TODO : 게임 오버 UI 띄우기 전 필요한 작업 -> 전적 api 전송
                    UIManager.Instance.Open(MenuId.HUD, MenuId.GameOver);
                    break;
                }

            case NetEvt.AimRequest:
                {
                    if (!_isMaster) break;
                    int actor = _codec.DecodeAimWeaponContext(payload);
                    _accessor.AimImmediate(new PlayerId(actor));
                    Master_Aim(sender);
                    break;
                }
            case NetEvt.AimDelta:
                {
                    int actor = _codec.DecodeAimWeaponContext(payload);
                    _accessor.AimImmediate(new PlayerId(actor));
                    break;
                }
            case NetEvt.AlivePlayersUpdate:
                {
                    var aliveActors = _codec.DecodeAlivePlayers(payload);
                    _alivePlayers.Clear();
                    _alivePlayers.AddRange(aliveActors);
                    break;
                }
        }
    }
}