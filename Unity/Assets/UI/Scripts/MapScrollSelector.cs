using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapScrollSelector : MonoBehaviour
{
    [Header("UI References")]
    public Transform mapContainer;
    public IconButton leftArrow;
    public IconButton rightArrow;

    [Header("Map Prefab")]
    public GameObject mapItemPrefab;

    [Header("Maps Data")]
    public List<MapData> maps;

    [Header("Settings")]
    public int visibleMaps = 3;
    public float spacing = 220f;

    private int currentIndex = 0;
    private readonly List<GameObject> mapItems = new List<GameObject>();

    void Start()
    {
        // null 체크 추가
        if (leftArrow != null)
            leftArrow.onClick.AddListener(ScrollLeft);
        else
            Debug.LogError("leftArrow is null!");

        if (rightArrow != null)
            rightArrow.onClick.AddListener(ScrollRight);
        else
            Debug.LogError("rightArrow is null!");

        GenerateMapItems();
        UpdateMapDisplay();
    }

    void GenerateMapItems()
    {
        if (maps == null || maps.Count == 0)
        {
            Debug.LogWarning("[MapScrollSelector] maps 가 비어있습니다.");
            return;
        }

        // 원본 목록 그대로 1회 생성
        for (int i = 0; i < maps.Count; i++)
        {
            var item = Instantiate(mapItemPrefab, mapContainer);
            var mapItem = item.GetComponent<MapItem>();
            mapItem.SetMapData(maps[i], i);
            mapItems.Add(item);
        }

        // 표시용 최소 개수(visibleMaps)까지 복제 (맵이 1~2개여도 3칸 채워서 무한순환)
        while (mapItems.Count < visibleMaps)
        {
            for (int i = 0; mapItems.Count <= visibleMaps; i++)
            {
                var item = Instantiate(mapItemPrefab, mapContainer);
                var mapItem = item.GetComponent<MapItem>();
                mapItem.SetMapData(maps[i], i); // 같은 데이터 재사용
                mapItems.Add(item);
            }
        }
    }

    void UpdateMapDisplay()
    {
        if (mapItems.Count == 0) return;

        // 전부 끄고
        for (int i = 0; i < mapItems.Count; i++)
            mapItems[i].SetActive(false);        

        for (int i = 0; i < visibleMaps; i++)
        {
            int idx = (currentIndex + i) % mapItems.Count;
            var go = mapItems[idx];
            go.SetActive(true);
            go.transform.localPosition = new Vector3(i * spacing, 0f, 0f);
        }
    }


    public void ScrollLeft()
    {
        if (mapItems.Count == 0) return;

        currentIndex = (currentIndex - 1 + mapItems.Count) % mapItems.Count;
        UpdateMapDisplay();
    }

    public void ScrollRight()
    {
        if (mapItems.Count == 0) return;
        // 오른쪽은 인덱스 증가
        currentIndex = (currentIndex + 1) % mapItems.Count;
        UpdateMapDisplay();
    }
}
