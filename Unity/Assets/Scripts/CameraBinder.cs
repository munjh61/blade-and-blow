using Cinemachine;
using Photon.Pun;
using System.Linq;
using UnityEngine;
public class CameraBinder : MonoBehaviour
{
    public enum Mode { Disabled, OwnerOnly, Always }

    [SerializeField] private CinemachineVirtualCamera vcam;

    [Header("Vcam Lookup")]
    [SerializeField, Tooltip("찾을 가상카메라 오브젝트 이름(정확 일치)")]
    private string vcamObjectName = "PlayerFollowCamera";

    [SerializeField, Tooltip("자동으로 타깃을 찾습니다 (없으면 this.transform)")]
    public bool autoFindTargets = true;

    [Header("Targets (optional)")]
    public Transform follow;
    public Transform lookAt;

    [Header("Lens/Body Defaults")]
    [SerializeField] private float defaultFov = 45f;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;

    [SerializeField, Tooltip("현재 Follow/LookAt 타깃 (디버깅용)")]
    private Transform currentTarget;

    private Cinemachine3rdPersonFollow third;
    private Mode _mode = Mode.OwnerOnly;

    private StarterAssets.ThirdPersonControllerReborn _tpc;
    private Transform _fallbackTargetCreated;

    private void Awake()
    {
        EnsureVcam();

        _tpc = GetComponent<StarterAssets.ThirdPersonControllerReborn>()
           ?? GetComponentInParent<StarterAssets.ThirdPersonControllerReborn>();

        if (autoFindTargets) ResolveOrCreateTargetsOnce();
    }

    private void EnsureVcam()
    {
        if (vcam != null)
        {
            if (third == null)
                third = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            return;
        }

        var all = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
        CinemachineVirtualCamera pick = null;

        pick = all.FirstOrDefault(c =>
            c.gameObject.scene == gameObject.scene &&
            c.gameObject.name == vcamObjectName);

        if (pick == null)
            pick = all.FirstOrDefault(c => c.gameObject.name == vcamObjectName);

        if (pick == null)
            pick = all.FirstOrDefault(c => c.gameObject.scene == gameObject.scene);

        if (pick == null && all.Length > 0)
            pick = all[0];

        vcam = pick;

        if (vcam != null && third == null)
            third = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    }

    public void SetMode(Mode mode) => _mode = mode;

    /// <summary>
    /// 현재 모드에 따라 카메라 바인딩을 적용/해제한다.
    /// OwnerOnly일 때는 pv.IsMine일 때만 바인딩.
    /// </summary>
    public void ApplyOwnerGate(bool isOwner)
    {
        EnsureVcam();
        if (!vcam) return;

        if (autoFindTargets) ResolveOrCreateTargetsOnce();

        bool active = _mode switch
        {
            Mode.Disabled   => false,
            Mode.Always     => true,
            Mode.OwnerOnly  => isOwner,
            _               => false
        };

        if (active)
        {
            var f = follow ? follow : transform;
            var l = lookAt ? lookAt : f;
            BindTo(f, l);
            _tpc.SetAimOrigin(vcam.transform);
        }
        else
        {
            UnbindActive();
        }
    }

    public void BindTo(Transform followTarget) => BindTo(followTarget, followTarget);

    public void BindTo(Transform followTarget, Transform lookAtTarget)
    {
        EnsureVcam();
        if (!vcam || !followTarget) return;

        if (currentTarget == followTarget && vcam.Follow == followTarget && vcam.LookAt == lookAtTarget) return;

        // 기본 렌즈 및 3rdPersonFollow 파라미터(원래 코드 유지)
        var lens = vcam.m_Lens;
        lens.FieldOfView = defaultFov;
        vcam.m_Lens = lens;

        vcam.Follow = followTarget;
        vcam.LookAt = lookAtTarget;

        if (third)
        {
            third.CameraDistance = 2.0f;
            third.ShoulderOffset = new Vector3(0.3f, 0.3f, 0f);
            third.VerticalArmLength = 0.0f;
            third.CameraSide = 1.0f;
            third.Damping = new Vector3(0.3f, 0.2f, 1.0f);
        }

        vcam.Priority = Mathf.Max(vcam.Priority, activePriority);
        vcam.gameObject.SetActive(true);

        currentTarget = followTarget;
    }

    /// <summary>
    /// 현재 내가 묶어둔 타깃과 일치할 때만 언바인드한다.
    /// (남의 바인딩을 풀지 않기 위함)
    /// </summary>
    public void UnbindIf(Transform target)
    {
        EnsureVcam();
        if (!vcam) return;

        if (currentTarget == target)
        {
            // 내가 묶은 것만 해제
            if (vcam.Follow == target) vcam.Follow = null;
            if (vcam.LookAt == target) vcam.LookAt = null;

            // 필요 시 비활성/우선순위 낮추기
            // vcam.gameObject.SetActive(false);
            if (vcam.Priority > 0) vcam.Priority = 0;

            currentTarget = null;
        }
    }

    /// <summary>
    /// 강제 언바인드(현재 타깃과 무관, 주의해서 사용)
    /// </summary>
    public void Unbind()
    {
        EnsureVcam();
        if (!vcam) return;

        vcam.Follow = null;
        vcam.LookAt = null;
        if (vcam.Priority > 10) vcam.Priority = 10;

        currentTarget = null;
    }

    private void UnbindActive()
    {
        if (!vcam) return;
        if (currentTarget)
        {
            if (vcam.Follow == currentTarget) vcam.Follow = null;
            if (vcam.LookAt == currentTarget) vcam.LookAt = null;
            vcam.Priority = Mathf.Min(vcam.Priority, inactivePriority);
            currentTarget = null;
        }
    }

    /// <summary>
    /// 이름 검색 없이 타깃 확보:
    /// 1) TPC.CinemachineCameraTarget 사용
    /// 2) 없으면 로컬 자식으로 새 Target 생성(한 번만)
    /// </summary>
    private void ResolveOrCreateTargetsOnce()
    {
        // 이미 수동 지정돼 있으면 존중
        if (follow && lookAt) return;

        Transform target = null;

        if (_tpc != null && _tpc.CinemachineCameraTarget != null)
            target = _tpc.CinemachineCameraTarget.transform;

        if (!target)
            target = _fallbackTargetCreated ?? (_fallbackTargetCreated = CreateLocalCameraTarget());

        if (!follow) follow = target;
        if (!lookAt) lookAt = follow;
    }

    private Transform CreateLocalCameraTarget()
    {
        var go = new GameObject("CinemachineCameraTarget"); // 이름은 디버그용, 논리적 식별은 참조로만
        var t = go.transform;
        t.SetParent(transform, false);
        t.localPosition = new Vector3(0f, 1.4f, 0f);
        t.localRotation = Quaternion.identity;
        return t;
    }

    /// <summary>
    /// 현재 이 바인더가 타깃에 바인드돼 있는지 확인.
    /// </summary>
    public bool IsBoundTo(Transform target)
    {
        return currentTarget == target && vcam && vcam.Follow == target && vcam.LookAt == target;
    }
}