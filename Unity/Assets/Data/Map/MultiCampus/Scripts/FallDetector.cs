using Photon.Pun;
using UnityEngine;

public class FallDetector : MonoBehaviourPun
{
    [Header("Fall Detection")]
    public float fallHeight = 0.0f; // 이 높이 이하로 떨어지면 낙사
    public Vector3 respawnPosition = Vector3.zero; // 리스폰 위치
    public float respawnHeight = 2f; // 리스폰 높이
    
    [Header("Map Settings")]
    public float mapRadius = 20f; // 맵 반지름
    public Transform mapCenter; // 맵 중심점
    
    private void Update()
    {
        CheckFallDeath();
        CheckOutOfBounds();
    }
    
    private void CheckFallDeath()
    {
        // 높이로 낙사 체크
        if (transform.position.y < fallHeight)
        {
            HandlePlayerDeath("낙사");
        }
    }
    
    private void CheckOutOfBounds()
    {
        // 맵 중심에서의 거리 체크
        Vector3 mapCenterPos = mapCenter ? mapCenter.position : Vector3.zero;
        float distanceFromCenter = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(mapCenterPos.x, 0, mapCenterPos.z)
        );
        
        if (distanceFromCenter > mapRadius + 5f) // 약간의 여유값
        {
            HandlePlayerDeath("맵 이탈");
        }
    }
    
    private void HandlePlayerDeath(string reason)
    {
        Debug.Log($"플레이어 사망: {reason}");
        
        // 멀티플레이어라면 PUN으로 처리
        if (photonView && photonView.IsMine)
        {
            // 사망 처리 로직
            // 예: HP 0으로 만들기, 리스폰, 게임 매니저에 알리기 등
            
            // 임시로 리스폰
            RespawnPlayer();
        }
        else if (!photonView) // 싱글플레이어
        {
            RespawnPlayer();
        }
    }
    
    private void RespawnPlayer()
    {
        // 맵 중심 근처의 랜덤한 위치로 리스폰
        Vector3 mapCenterPos = mapCenter ? mapCenter.position : Vector3.zero;
        
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(0f, mapRadius * 0.7f);
        
        Vector3 newPosition = new Vector3(
            mapCenterPos.x + Mathf.Cos(randomAngle) * randomDistance,
            respawnHeight,
            mapCenterPos.z + Mathf.Sin(randomAngle) * randomDistance
        );
        
        transform.position = newPosition;
        
        // 추가: 리스폰 효과, 무적 시간 등
    }
    
    // 기즈모로 범위 표시
    private void OnDrawGizmosSelected()
    {
        Vector3 center = mapCenter ? mapCenter.position : Vector3.zero;
        
        Gizmos.color = Color.green;
        DrawWireCircle(center, mapRadius);
        
        Gizmos.color = Color.red;
        DrawWireCircle(center, mapRadius + 5f);
    }
    
    // 원형 기즈모 그리기 헬퍼 함수
    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}