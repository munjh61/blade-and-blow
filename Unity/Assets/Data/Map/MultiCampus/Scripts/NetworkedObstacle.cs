using UnityEngine;
using System.Collections;
using Photon.Pun;
using StarterAssets;

[RequireComponent(typeof(Collider))]
public class NetworkedObstacle : MonoBehaviour
{
    [Header("Obstacle Properties")]
    public float knockbackForce = 20f; // 넉백 힘
    public float knockbackDuration = 0.5f; // 넉백 지속시간
    
    private Vector3 targetPosition;
    private float speed;
    private float lifetime;
    private bool hasHitPlayer = false;
    
    public void Initialize(Vector3 target, float moveSpeed, float life)
    {
        targetPosition = target;
        speed = moveSpeed;
        lifetime = life;
        
        // 목표 방향으로 회전
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.LookAt(transform.position + direction);
        
        // 수명 후 자동 삭제
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        if (!hasHitPlayer)
        {
            MoveTowardsTarget();
        }
    }
    
    private void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        
        // 목표에 도달하면 삭제
        if (Vector3.Distance(transform.position, targetPosition) < 1f)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 충돌 확인 (로컬 플레이어만)
        PhotonView playerPV = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && !hasHitPlayer && playerPV && playerPV.IsMine)
        {
            hasHitPlayer = true;
            ApplyKnockback(other);
            
            // 장애물 제거
            Destroy(gameObject);
        }
    }
    
    private void ApplyKnockback(Collider playerCollider)
    {
        // ThirdPersonController 찾기 (StarterAssets 네임스페이스)
        var thirdPersonController = playerCollider.GetComponent<StarterAssets.ThirdPersonControllerReborn>();
        if (thirdPersonController != null)
        {
            // 플레이어 넉백 처리
            PlayerKnockback knockback = playerCollider.GetComponent<PlayerKnockback>();
            if (knockback == null)
            {
                knockback = playerCollider.gameObject.AddComponent<PlayerKnockback>();
            }
            
            // 넉백 방향 계산 (장애물 진행 방향)
            Vector3 knockbackDirection = (targetPosition - transform.position).normalized;
            knockback.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);
        }
    }
}