using ExitGames.Client.Photon;
using Game.Domain;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

namespace Game.Net.Pun
{
    /// <summary>
    /// Photon PUN을 기반으로 한 방 단위(Event Bus) 통신 어댑터.
    /// 
    /// - <see cref="IRoomBus"/> 인터페이스 구현.
    /// - Photon의 RaiseEvent/OnEvent 메커니즘을 사용하여 커스텀 이벤트를 송수신.
    /// - EventReceived 이벤트를 통해 상위 계층이 특정 NetEvt를 구독/처리할 수 있음.
    /// </summary>
    public class PunRoomBus : MonoBehaviour, IRoomBus, IOnEventCallback
    {
        /// <summary>
        /// 외부로 노출되는 이벤트 수신 콜백.
        /// evt = 이벤트 코드, payload = 데이터, sender = 발신자
        /// </summary>
        public event Action<NetEvt, object, PlayerId> EventReceived;

        private static byte ToCode(NetEvt e) => (byte)e;
        private static NetEvt ToEvt(byte code) => (NetEvt)code;

        void OnEnable() => PhotonNetwork.AddCallbackTarget(this);
        void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

        /// <summary>
        /// 특정 대상 플레이어에게 이벤트를 전송.
        /// </summary>
        /// <param name="target">대상 PlayerId</param>
        /// <param name="evt">전송할 이벤트 코드</param>
        /// <param name="payload">부가 데이터</param>
        /// <param name="reliable">신뢰성 여부 (기본: true)</param>
        public void SendTo(PlayerId target, NetEvt evt, object payload, bool reliable = true)
        {
            var code = ToCode(evt);

            if (!PhotonNetwork.InRoom) return;

            var opts = new RaiseEventOptions 
            {
                Receivers = ReceiverGroup.Others,
                TargetActors = new[] { target.Value },
                CachingOption = EventCaching.DoNotCache
            };

            Debug.Log($"[Bus] SendTo -> code={code} evt={evt} to={target.Value} payloadType={DescribePayload(payload)}");
            PhotonNetwork.RaiseEvent((byte)evt, payload, opts, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
        }

        /// <summary>
        /// 방의 다른 모든 플레이어에게 이벤트 브로드캐스트.
        /// (자신은 제외)
        /// </summary>
        public void Broadcast(NetEvt evt, object payload, bool reliable = true)
        {
            var code = ToCode(evt);
            if (!PhotonNetwork.InRoom) return;

            var opts = new RaiseEventOptions
            {
                Receivers = NeedsMasterOnly(evt) ? ReceiverGroup.MasterClient : ReceiverGroup.All,
                CachingOption = EventCaching.DoNotCache
            };

            Debug.Log($"[Bus] Broadcast code={code} evt={evt} payload={DescribePayload(payload)}");
            PhotonNetwork.RaiseEvent((byte)evt, payload, opts, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
        }

        /// <summary>
        /// Photon의 이벤트 수신 콜백.
        /// - 사용자 정의 NetEvt 코드만 처리.
        /// - Photon 내부 예약 코드(200+)는 무시.
        /// </summary>
        void IOnEventCallback.OnEvent(EventData e)
        {
            try
            {
                byte code = e.Code;
                
                if (code < 1 || code > 200)
                {
                    // Photon 내부 이벤트(200+)는 무시
                    return;
                }

                var evt = (NetEvt)code;
                var sender = new PlayerId(e.Sender);
                var payload = e.CustomData;
                Debug.Log($"[Bus] OnEvent recv code={code} evt={evt} from={sender.Value} payloadType={payload?.GetType().Name}");
                EventReceived?.Invoke(evt, payload, sender);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PunRoomBus] OnEvent error: {ex.Message}");
            }
        }

        private static bool NeedsMasterOnly(NetEvt evt)
        {
            switch (evt)
            {
                case NetEvt.StaminaIntent:
                case NetEvt.EquipRequest:
                case NetEvt.PickupRequest:
                case NetEvt.ShootRequest:
                case NetEvt.HitRequestDelta:
                case NetEvt.PlayerIntent:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// payload 객체에 대한 간단한 설명 문자열 생성.
        /// (디버그 로그용)
        /// </summary>
        private static string DescribePayload(object p)
        {
            if (p == null) return "null";
            if (p is Hashtable h) return $"Hashtable({h.Count})";
            if (p is object[] arr) return $"object[{arr.Length}]";
            return p.GetType().Name;
        }
    }
}
