using ExitGames.Client.Photon;
using Game.Domain;
using UnityEngine;

// 직렬화/역직렬화용 클래스
namespace Game.Net.Pun
{
    public class PunHashtableCodec : INetCodec
    {

        // 용량 최소화를 위해 짧은 문자열로 할당
        // ===============================
        // 플레이어 상태
        // ===============================
        private const string KEY_PLAYERS            = "ps";
        private const string KEY_USERID             = "id";
        private const string KEY_NAME               = "n";
        private const string KEY_TEAM               = "pt";
        private const string KEY_ACTOR              = "a";
        private const string KEY_HP                 = "h";
        private const string KEY_STAMINA            = "s";
        private const string KEY_STAMINA_TYPE       = "st";
        private const string KEY_STAMINA_PAYLOAD    = "sp";
        private const string KEY_ALIVE              = "al";
        // ===============================
        // 공격/피격
        // ===============================
        private const string KEY_ATTACKER           = "att";
        private const string KEY_KILLERID           = "aid";
        private const string KEY_KILLERNAME         = "kn";
        private const string KEY_KILLERTEAM         = "kt";
        private const string KEY_TARGET             = "tg";
        private const string KEY_VICTIMID           = "vid";
        private const string KEY_VICTIMNAME         = "vn";
        private const string KEY_VICTIMTEAM         = "vt";
        private const string KEY_DAMAGE             = "dm";
        private const string KEY_KILLS              = "kl";
        private const string KEY_DEATHS             = "dt";
        private const string KEY_TOT_DAMAGE         = "tdm";
        private const string KEY_CONSECUTIVE_KILL   = "ck";
        // ===============================
        // 장비/위치/키
        // ===============================
        private const string KEY_EQUIP              = "e";
        private const string KEY_WEAPON             = "w";
        private const string KEY_POS                = "p";
        private const string KEY_ROT                = "r";
        private const string KEY_TOKEN              = "t";
        private const string KEY_KEY                = "k";
        private const string KEY_CHANGE_DURATION    = "cd";
        // ===============================
        // Snapshot
        // Snapshot 정보를 HashTable (KEY_PLAYER: arr<HashTable>) 에 담아서 Encoding
        // Encoding된 Snapshot 정보를 Decode 한다.
        // ===============================

        public object EncodeSnapshot(GameSnapshotData s)
        {
            var arr = new object[s.players?.Length ?? 0];
            for (int i = 0; i < arr.Length; i++)
            {
                var p = s.players[i];
                arr[i] = new Hashtable
                { 
                    { KEY_ACTOR, p.actor },
                    { KEY_USERID, p.userId ?? "unknown" },
                    { KEY_NAME, p.name },
                    { KEY_TEAM, p.team },
                    { KEY_HP, p.hp },
                    { KEY_STAMINA, p.stamina },
                    { KEY_EQUIP, p.equipId },
                    { KEY_KILLS, p.kill},
                    { KEY_DEATHS, p.death },
                    { KEY_TOT_DAMAGE, p.totDamage },
                    { KEY_CONSECUTIVE_KILL, p.consecutiveKill }
                };
            }
            var ht = new Hashtable { { KEY_PLAYERS, arr } };
            if (s.alivePlayerActors != null)
            {
                ht[KEY_ALIVE] = s.alivePlayerActors;
            }
            return ht;
        }

        public GameSnapshotData DecodeSnapshot(object payload)
        {
            var h = (Hashtable)payload;
            var arr = (object[])h[KEY_PLAYERS];
            var players = new PlayerInfoData[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var ph = (Hashtable)arr[i];
                players[i] = new PlayerInfoData
                {
                    actor = (int)ph[KEY_ACTOR],
                    userId = ph.ContainsKey(KEY_USERID) ? (string)ph[KEY_USERID] : "unknown",
                    name = (string)ph[KEY_NAME],
                    team = ph.ContainsKey(KEY_TEAM) ? (TeamId)ph[KEY_TEAM] : TeamId.None,
                    hp = (int)ph[KEY_HP],
                    stamina = (int)ph[KEY_STAMINA],
                    equipId = (int)ph[KEY_EQUIP],
                    kill = (int) ph[KEY_KILLS],
                    death = (int)ph[KEY_DEATHS],
                    totDamage = (int)ph[KEY_TOT_DAMAGE],
                    consecutiveKill = (int)ph[KEY_CONSECUTIVE_KILL]
                };
            }

            int[] alive = h.ContainsKey(KEY_ALIVE) ? (int[])h[KEY_ALIVE] : null;

            return new GameSnapshotData { players = players, alivePlayerActors = alive };
        }

        // ===============================
        // Player Delta
        // 직렬화된 정보를 Byte, Variable 형식으로 Hashtable에 담아서 Encoding.
        // ===============================
        public object EncodeDelta(PlayerInfoData d)
        {
            return new Hashtable
            { 
                { KEY_ACTOR, d.actor },
                { KEY_USERID, d.userId ?? "unknown" },
                { KEY_NAME, d.name },
                { KEY_TEAM, d.team },
                { KEY_HP, d.hp },
                { KEY_STAMINA, d.stamina },
                { KEY_EQUIP, d.equipId } ,
                { KEY_KILLS, d.kill },
                { KEY_DEATHS, d.death },
                { KEY_TOT_DAMAGE, d.totDamage },
                { KEY_CONSECUTIVE_KILL, d.consecutiveKill }
            };
        }

        // 받은 데이터를 PlayerInfoData에 대입.
        public PlayerInfoData DecodeDelta(object payload)
        {
            var h = (Hashtable)payload;
            return new PlayerInfoData
            {
                actor = (int)h[KEY_ACTOR],
                userId = h.ContainsKey(KEY_USERID) ? (string)h[KEY_USERID] : "unknown",
                name = (string)h[KEY_NAME],
                team = h.ContainsKey(KEY_TEAM) ? (TeamId)h[KEY_TEAM] : TeamId.None,
                hp = (int)h[KEY_HP],
                stamina = (int)h[KEY_STAMINA],
                equipId = (int)h[KEY_EQUIP],
                kill = h.ContainsKey(KEY_KILLS) ? (int)h[KEY_KILLS] : 0,
                death = h.ContainsKey(KEY_DEATHS) ? (int)h[KEY_DEATHS] : 0,
                totDamage = h.ContainsKey(KEY_TOT_DAMAGE) ? (int)h[KEY_TOT_DAMAGE] : 0,
                consecutiveKill = h.ContainsKey(KEY_CONSECUTIVE_KILL) ? (int)h[KEY_CONSECUTIVE_KILL] : 0
            };
        }

        // ===============================
        // Kill Event
        // ===============================
        public object EncodeKillEvent(Kill kill)
        {
            return new Hashtable
            {
                { KEY_ATTACKER, kill.killerActor },
                { KEY_KILLERID, kill.killerId },
                { KEY_KILLERNAME, kill.killerName },
                { KEY_KILLERTEAM, kill.killerTeam },
                { KEY_TARGET, kill.victimActor },
                { KEY_VICTIMID, kill.victimId },
                { KEY_VICTIMNAME, kill.victimName },
                { KEY_VICTIMTEAM, kill.victimTeam },
                { KEY_EQUIP, kill.weaponId },
                { KEY_WEAPON, kill.weapon }
            };
        }

        public Kill DecodeKillEvent(object payload)
        {
            var h = (Hashtable)payload;
            return new Kill
            {
                killerActor = (int)h[KEY_ATTACKER],
                killerId = (string)h[KEY_KILLERID],
                killerName = (string)h[KEY_KILLERNAME],
                killerTeam = h.ContainsKey(KEY_KILLERTEAM) ? (TeamId)h[KEY_KILLERTEAM] : TeamId.None,
                victimActor = (int)h[KEY_TARGET],
                victimId = (string)h[KEY_VICTIMID],
                victimName = (string)h[KEY_VICTIMNAME],
                victimTeam = h.ContainsKey(KEY_VICTIMTEAM) ? (TeamId)h[KEY_VICTIMTEAM] : TeamId.None,
                weaponId = (int)h[KEY_EQUIP],
                weapon = (string)h[KEY_WEAPON]
            };
        }

        // Client -> Master
        // HitRequest Encoding용 (Client 송신용)
        public object EncodeHitRequest(int attackerId, int targetId, int damage)
        {
            return new Hashtable {
                {KEY_ATTACKER, attackerId },
                {KEY_TARGET, targetId },
                {KEY_DAMAGE, damage }
            };
        }

        // Hashtable로 받은 데이터를 Decoding한다.(Master 수신용)
        public (int attackerId, int targetId, int damage) DecodeHitRequest(object payload) {
            var h = (Hashtable)payload;

            int attackerId = (int)h[KEY_ATTACKER];
            int targetId = (int)h[KEY_TARGET];
            int damage = (int)h[KEY_DAMAGE];

            return (attackerId, targetId, damage);
        }


        // ===============================
        // EquipId (request)
        // ===============================
        public object EncodeEquipId(int equipId) => equipId;
        public int DecodeEquipId(object payload)
        {
            if (payload is int i) return i;

            if (payload is Hashtable h)
            {
                if (h.ContainsKey(KEY_EQUIP)) return (int)h[KEY_EQUIP];
            }
            return -1;
        }

        // ===============================
        // Equip Delta (broadcast)
        // ===============================
        public object EncodeEquipDelta(int actor, int equipId)
        {
            return new Hashtable
        {
            { KEY_ACTOR, actor },
            { KEY_EQUIP, equipId }
        };
        }

        public (int actor, int equipId) DecodeEquipDelta(object payload)
        {
            var h = (Hashtable)payload;
            return ((int)h[KEY_ACTOR], (int)h[KEY_EQUIP]);
        }

        // Helper
        public object EncodeEquipIdWithPlayerPos(int equipId, Vector3 playerPosition)
        {
            return new Hashtable
            {
                {KEY_EQUIP, equipId },
                {KEY_POS, playerPosition }
            };
        }

        // ===============================
        // Drop: Spawned (master -> all)
        // ===============================
        public object EncodeDropSpawned(ulong token, string weaponKey, Vector3 pos, Quaternion rot)
        {
            // Photon RaiseEvent payload에선 ulong 미보장 ⇒ long으로 변환 저장
            long t = unchecked((long)token);
            return new Hashtable
            {
                { KEY_TOKEN, t         },
                { KEY_KEY,   weaponKey },
                { KEY_POS,   pos       },
                { KEY_ROT,   rot       }
            };
        }

        public (ulong token, string weaponKey, Vector3 pos, Quaternion rot) DecodeDropSpawned(object payload)
        {
            var h = (Hashtable)payload;

            // token은 long/int로 들어올 수 있으니 안전 변환
            ulong token = 0;
            if (h[KEY_TOKEN] is long tl) token = unchecked((ulong)tl);
            else if (h[KEY_TOKEN] is int ti) token = unchecked((ulong)ti);

            string key = (string)h[KEY_KEY];
            Vector3 pos = (Vector3)h[KEY_POS];
            Quaternion rot = (Quaternion)h[KEY_ROT];

            return (token, key, pos, rot);
        }

        // ===============================
        // Drop: Client -> Master (pickup request)
        // ===============================
        public object EncodePickupRequest(int actor, ulong token, int equipId)
        {
            long t = unchecked((long)token);
            return new Hashtable
            {
                { KEY_ACTOR, actor   },
                { KEY_TOKEN, t       },
                { KEY_EQUIP, equipId }
            };
        }

        public (int actor, ulong token, int equipId) DecodePickupRequest(object payload)
        {
            var h = (Hashtable)payload;

            int actor = (int)h[KEY_ACTOR];

            ulong token = 0;
            if (h[KEY_TOKEN] is long tl) token = unchecked((ulong)tl);
            else if (h[KEY_TOKEN] is int ti) token = unchecked((ulong)ti);

            int equipId = (int)h[KEY_EQUIP];

            return (actor, token, equipId);
        }

        // ===============================
        // Drop: Removed (master -> all)
        // ===============================
        public object EncodeDropRemoved(ulong token)
        {
            long t = unchecked((long)token);
            return new Hashtable
            {
                { KEY_TOKEN, t }
            };
        }

        public ulong DecodeDropRemoved(object payload)
        {
            if (payload is Hashtable h)
            {
                if (h[KEY_TOKEN] is long tl) return unchecked((ulong)tl);
                if (h[KEY_TOKEN] is int ti) return unchecked((ulong)ti);
            }
            // fallback: 직접 숫자가 올 수도 있음
            if (payload is long l) return unchecked((ulong)l);
            if (payload is int i) return unchecked((ulong)i);
            return 0UL;
        }

        public object EncodeDropRequest(int actor) => actor;

        public int DecodeDropRequest(object payload)
        {
            if (payload is int i) return i;

            if (payload is Hashtable h)
            {
                if (h.ContainsKey(KEY_ACTOR)) return (int)h[KEY_ACTOR];
            }
            return -1;
        }

        public object EncodeActorWeaponContext(int actor, Vector3 aimOriginPosition, Quaternion aimOriginRotation, float changeDuration)
        {
            return new Hashtable
            {
                {KEY_ACTOR, actor },
                {KEY_CHANGE_DURATION, changeDuration },
                {KEY_POS, aimOriginPosition},
                {KEY_ROT, aimOriginRotation }
            };
        }

        public (int actor, float changeDuration, Vector3 aimOriginPosition, Quaternion aimOriginRotation) DecodeActorWeaponContext(object payload)
        {
            var h = (Hashtable)payload;
            return ((int)h[KEY_ACTOR], (float)h[KEY_CHANGE_DURATION], (Vector3)h[KEY_POS], (Quaternion)h[KEY_ROT]);
        }

        public object EncodeStaminaIntent(byte intentType, int payload)
        {
            return new Hashtable
            {
                { KEY_STAMINA_TYPE, intentType },
                { KEY_STAMINA_PAYLOAD, payload }
            };
        }

        public (byte intentType, int payload) DecodeStaminaIntent(object payload)
        {
            var h = (Hashtable)payload;

            byte type = (byte)h[KEY_STAMINA_TYPE];
            int cost = (int)h[KEY_STAMINA_PAYLOAD];

            return (type, cost);
        }

        public object EncodeStaminaDelta(int actor, int stamina)
        {
            return new Hashtable
            {
                { KEY_ACTOR, actor      },
                { KEY_STAMINA, stamina  }
            };
        }

        public (int actor, int stamina) DecodeStaminaDelta(object payload)
        {
            var h = (Hashtable)payload;

            int actor = (int)h[KEY_ACTOR];
            int stamina = (int)h[KEY_STAMINA];

            return (actor, stamina);
        }

        public object EncodeHealthDelta(int actor, int hp)
        {
            return new Hashtable
            {
                { KEY_ACTOR, actor },
                { KEY_HP, hp }
            };
        }

        public (int actor, int hp) DecodeHealthDelta(object payload)
        {
            var h = (Hashtable)payload;

            int actor = (int)h[KEY_ACTOR];
            int health = (int)h[KEY_HP];

            return (actor, health);
        }

        public object EncodeAimWeaponContext(int actor)
        {
            return new Hashtable
            {
                {KEY_ACTOR, actor },
            };
        }

        public int DecodeAimWeaponContext(object payload)
        {
            var h = (Hashtable)payload;
            return ((int)h[KEY_ACTOR]);
        }

        public object EncodeAlivePlayers(int[] alivePlayerActors)
        {
            return new Hashtable { { KEY_ALIVE, alivePlayerActors } };
        }

        public int[] DecodeAlivePlayers(object payload)
        {
            var h = (Hashtable)payload;
            if (h != null && h.ContainsKey(KEY_ALIVE))
            {
                return (int[])h[KEY_ALIVE];
            }
            return System.Array.Empty<int>();
        }

    }
}
