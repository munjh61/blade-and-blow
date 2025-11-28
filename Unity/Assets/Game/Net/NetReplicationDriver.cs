using Game.Domain;
using Game.Net;
using UnityEngine;

[DisallowMultipleComponent]
public class NetReplicationDriver : MonoBehaviour
{
    [Header("Interpolation")]
    public float netPosLerp = 12f;
    public float netRotLerp = 10f;

    private INetAdapter _net;
    private CharacterController _cc;

    private bool _writeEnabled;

    // 원격 보간 타깃
    private Vector3 _netPos;
    private Quaternion _netRot;
    private Vector3 _netVel;

    private bool _hasSnapshot;
    private const float POS_EPS = 0.0001f;

    private void Awake()
    {
        _net = GetComponent<INetAdapter>();
        _cc = GetComponent<CharacterController>();
        if (_net != null) _net.OnState += OnNetState;
    }

    private void OnDestroy()
    {
        if (_net != null) _net.OnState -= OnNetState;
    }

    public void SetWriteAuthority(bool enabled)
    {
        _writeEnabled = enabled;
    }

    private void FixedUpdate()
    {
        if (_net == null) return;

        if (_net.IsMine && _writeEnabled)
        {
            // 오너: 현재 상태 송신(컨트롤러는 입력/이동만 담당)
            _net.PublishState(new PlayerState
            {
                position = transform.position,
                rotation = transform.rotation,
                velocity = _cc != null ? _cc.velocity : Vector3.zero
            });
        }

        if (!_hasSnapshot) return;

        float posT = 1f - Mathf.Exp(-netPosLerp * Time.deltaTime);
        float rotT = 1f - Mathf.Exp(-netRotLerp * Time.deltaTime);


        var delta = _netPos - transform.position;
        if (delta.sqrMagnitude > POS_EPS)
        {
            if (_cc != null && _cc.enabled)
            {
                // CharacterController 사용 중이면 Move로 미세 이동
                _cc.Move(delta * posT);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, _netPos, posT);
            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, _netRot, rotT);
    }

    private void OnNetState(PlayerState s)
    {
        //if (_net != null && _net.IsMine && _writeEnabled) return;
        if (_net != null && _net.IsMine) return;

        _netPos = s.position;
        _netRot = s.rotation;
        _netVel = s.velocity;

        _hasSnapshot = true;

    }
}
