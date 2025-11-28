using System;
using Game.Domain;
using Photon.Pun;
using UnityEngine;

namespace Game.Net.Pun
{
    /// <summary>
    /// Photon PUN 기반의 네트워크 어댑터.
    /// 
    /// - INetAdapter를 구현하여 프로젝트 내부의 네트워크 추상화 계층에 연결됨.
    /// - IPunObservable을 구현하여 Photon의 직렬화 콜백을 통해 상태 동기화를 수행.
    /// - PlayerState의 position, velocity, rotation을 네트워크로 송수신함.
    /// - RPC 호출 기능을 제공하여 모든 클라이언트에 메서드 실행을 요청할 수 있음.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunNetAdapter : MonoBehaviour, INetAdapter, IPunObservable
    {
        /// <summary>
        /// 원격 플레이어의 상태가 수신되었을 때 발생하는 이벤트.
        /// 구독자는 보간/적용 로직을 여기서 처리할 수 있음.
        /// </summary>
        public event Action<PlayerState> OnState;

        PhotonView _view;

        PlayerState _lastSent, _lastRecv;

        public bool IsMine => _view.IsMine;
        public int OwnerId => _view.OwnerActorNr;

        void Awake() => _view = GetComponent<PhotonView>();

        /// <summary>
        /// 네트워크로 전송할 상태를 Publish.
        /// 실제 전송은 Photon의 직렬화 콜백에서 이루어짐.
        /// </summary>
        public void PublishState(PlayerState s) => _lastSent = s;

        public void RpcAll(string method, params object[] args) => _view.RPC(method, RpcTarget.All, args);

        /// <summary>
        /// Photon 직렬화 콜백.
        /// - stream.IsWriting == true → 로컬 상태를 네트워크에 송신.
        /// - stream.IsWriting == false → 네트워크에서 상태를 수신하고 이벤트 발생.
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(_lastSent.position);
                stream.SendNext(_lastSent.velocity);
                stream.SendNext(_lastSent.rotation);
            }
            else
            {
                _lastRecv.position = (Vector3)stream.ReceiveNext();
                _lastRecv.velocity = (Vector3)stream.ReceiveNext();
                _lastRecv.rotation = (Quaternion)stream.ReceiveNext();

                OnState?.Invoke(_lastRecv);
            }
        }
    }
}
