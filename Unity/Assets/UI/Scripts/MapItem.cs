using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapItem : MonoBehaviour
{
    [Header("UI References")]
    public Image mapImage;
    public TextMeshProUGUI mapName;
    // public Button selectButton;
    
    private MapData mapData;
    private int mapIndex;

    void Start()
    {
        // selectButton.onClick.AddListener(OnSelectMap);
    }

    public void SetMapData(MapData data, int index)
    {
        mapData = data;
        mapIndex = index;
        
        mapImage.sprite = data.mapSprite;
        mapName.text = data.mapName;
    }

    void OnSelectMap()
    {
        // 맵 선택 이벤트
        Debug.Log($"Selected Map: {mapData.mapName}");
        
        // 이벤트 발생 또는 Manager에게 알림
        // FindObjectOfType<MapScrollSelector>().OnMapSelected(mapIndex);
    }
}