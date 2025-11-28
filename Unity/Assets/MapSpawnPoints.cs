using UnityEngine;

public class MapSpawnPoints : MonoBehaviour
{
    public Transform[] spawnPoints;
    void Start()
    {
        var spawnGroup = GameObject.Find("SpawnGroup");
        if (spawnGroup != null)
        {
            // SpawnGroup 아래 자식 Transform 전부 가져오기
            var children = spawnGroup.GetComponentsInChildren<Transform>();

            // 자기 자신(SpawnGroup Transform)은 빼고 자식들만 남기기
            spawnPoints = System.Array.FindAll(children, t => t != spawnGroup.transform);

            Debug.Log($"[Init] Found {spawnPoints.Length} spawn points");
        }
        else
        {
            Debug.LogError("[Init] SpawnGroup not found!");
        }
    }
}
