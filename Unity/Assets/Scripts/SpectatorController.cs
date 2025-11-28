using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using StarterAssets;
using Game.Domain; // PlayerInfoData를 사용하기 위해 추가

/// <summary>
/// 관전 모드를 전체적으로 관리하는 싱글톤 컨트롤러입니다.
/// 씬에 항상 존재하는 매니저 오브젝트에 추가해야 합니다.
/// </summary>
public class SpectatorController : MonoBehaviour
{
    public static SpectatorController Instance { get; private set; }

    // 플레이어 상태(HP)를 조회하기 위한 참조
    private PlayerManagerPunBehaviour _pm;

    // 사망한 플레이어로부터 제어권을 넘겨받은 CameraBinder
    private CameraBinder activeCameraBinder;

    private readonly List<Player> potentialTargets = new List<Player>();
    private int currentTargetIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // 씬에 있는 PlayerManagerPunBehaviour를 찾아서 참조를 저장합니다.
        _pm = FindObjectOfType<PlayerManagerPunBehaviour>();
        if (_pm == null)
        {
            Debug.LogError("SpectatorController: PlayerManagerPunBehaviour를 씬에서 찾을 수 없습니다!");
        }

        // 평소에는 비활성화 상태로 대기합니다.
        this.enabled = false;
    }

    /// <summary>
    /// 플레이어가 사망 시 호출하여 관전 모드를 시작하는 진입점입니다.
    /// </summary>
    /// <param name="binderFromDeadPlayer">사망한 플레이어의 CameraBinder</param>
    public void BeginSpectating(CameraBinder binderFromDeadPlayer)
    {
        if (binderFromDeadPlayer == null)
        {
            Debug.LogError("SpectatorController: CameraBinder가 null인 상태로 관전을 시작할 수 없습니다!");
            return;
        }

        this.activeCameraBinder = binderFromDeadPlayer;
        this.enabled = true;
    }

    void OnEnable()
    {
        if (activeCameraBinder == null)
        {
            Debug.LogWarning("SpectatorController가 활성화되었지만 제어할 CameraBinder가 없습니다. 다시 비활성화합니다.");
            this.enabled = false;
            return;
        }
        
        activeCameraBinder.Unbind();
        RefreshTargetList();
    }

    void OnDisable()
    {
        if (activeCameraBinder != null)
        {
            activeCameraBinder.Unbind();
        }
        activeCameraBinder = null; 
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 플레이어가 입장/퇴장하는 경우를 대비해 목록을 최신화합니다.
            RefreshTargetList();
            SwitchToNextValidTarget();
        }
    }

    public void RefreshTargetList()
    {
        int len = potentialTargets.Count;
        potentialTargets.Clear();

        if (_pm == null) 
        {
            Debug.LogError("PlayerManagerPunBehaviour 참조가 없어 대상을 찾을 수 없습니다.");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                // 자기 자신은 목록에서 제외
                if (player.IsLocal) continue;

                // PlayerManager를 통해 해당 플레이어의 상태(HP 정보 포함)를 가져옵니다.
                if (_pm.TryGetState(player.ActorNumber, out PlayerInfoData data))
                {
                    // HP가 0보다 큰, 즉 살아있는 플레이어만 관전 대상 목록에 추가합니다.
                    if (data.hp > 0)
                    {
                        potentialTargets.Add(player);
                    }
                }
            }
            if(len != potentialTargets.Count)
            {
                currentTargetIndex = -1; // 목록이 변경되었으므로 인덱스 초기화
            }
        }
        Debug.Log($"Spectator: {potentialTargets.Count}명의 살아있는 관전 대상을 찾았습니다.");
    }

    private void SwitchToNextValidTarget()
    {
        if (activeCameraBinder == null) return;

        if (potentialTargets.Count == 0)
        {
            activeCameraBinder.Unbind();
            return;
        }

        for (int i = 0; i < potentialTargets.Count; i++)
        {
            currentTargetIndex = (currentTargetIndex + 1) % potentialTargets.Count;
            Player nextPlayer = potentialTargets[currentTargetIndex];

            if (AvatarRegistry.TryGet(nextPlayer.ActorNumber, out var handle) && handle.go != null && handle.go.activeInHierarchy)
            {
                var tpc = handle.go.GetComponent<ThirdPersonControllerReborn>();
                if (tpc != null && tpc.CinemachineCameraTarget != null)
                {
                    Transform newTarget = tpc.CinemachineCameraTarget.transform;
                    activeCameraBinder.BindTo(newTarget);
                    return;
                }
            }
        }
        
        activeCameraBinder.Unbind();
    }
}
