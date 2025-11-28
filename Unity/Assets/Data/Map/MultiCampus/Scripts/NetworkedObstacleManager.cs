using UnityEngine;
using System.Collections;
using Photon.Pun;

public class NetworkedObstacleManager : MonoBehaviourPun
{
    [Header("Obstacle Settings")]
    public GameObject obstaclePrefab; // 날아오는 장애물 프리팹
    public float spawnInterval = 5f; // 장애물 생성 간격
    public float obstacleSpeed = 15f; // 장애물 속도
    public float obstacleLifetime = 10f; // 장애물 수명
    
    [Header("Map Settings")]
    public float mapRadius = 20f; // 원형 맵 반지름
    public float spawnHeight = 2f; // 장애물 생성 높이
    public float spawnDistance = 30f; // 맵 가장자리에서 얼마나 멀리 생성할지
    
    private void Start()
    {
        // 마스터 클라이언트만 장애물 생성
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnObstacles());
        }
    }
    
    private IEnumerator SpawnObstacles()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnRandomObstacle();
            }
        }
    }
    
    private void SpawnRandomObstacle()
    {
        // 맵 바깥에서 맵 중심으로 향하는 방향으로 장애물 생성
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // 생성 위치 (맵 바깥)
        Vector3 spawnPosition = new Vector3(
            Mathf.Cos(randomAngle) * (mapRadius + spawnDistance),
            spawnHeight,
            Mathf.Sin(randomAngle) * (mapRadius + spawnDistance)
        );
        
        // 목표 위치 (맵 반대편)
        Vector3 targetPosition = new Vector3(
            -Mathf.Cos(randomAngle) * (mapRadius + spawnDistance),
            spawnHeight,
            -Mathf.Sin(randomAngle) * (mapRadius + spawnDistance)
        );
        
        // 네트워크 동기화된 장애물 생성
        photonView.RPC("CreateObstacle", RpcTarget.All, spawnPosition, targetPosition);
    }
    
    [PunRPC]
    void CreateObstacle(Vector3 spawnPos, Vector3 targetPos)
    {
        // 모든 클라이언트에서 장애물 생성
        GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
        
        // 장애물에 NetworkedObstacle 컴포넌트 추가
        NetworkedObstacle obstacleScript = obstacle.GetComponent<NetworkedObstacle>();
        if (obstacleScript == null)
        {
            obstacleScript = obstacle.AddComponent<NetworkedObstacle>();
        }
        
        obstacleScript.Initialize(targetPos, obstacleSpeed, obstacleLifetime);
    }
    
    // 마스터 클라이언트 변경 시 처리 - PUN2 콜백
    void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnObstacles());
        }
    }
    
    // IPunObservable 구현 (필요시)
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 필요한 경우 상태 동기화
    }
    
    // 기즈모로 맵 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        DrawWireCircle(transform.position, mapRadius);
        
        Gizmos.color = Color.yellow;
        DrawWireCircle(transform.position, mapRadius + spawnDistance);
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