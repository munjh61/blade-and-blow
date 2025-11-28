using Photon.Pun;
using UnityEngine;

public class ItemManagerPun : MonoBehaviour
{
    public static ItemManagerPun Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void PickupItem(GameObject item)
    {
        // 아이템 줍기 로직
        PhotonNetwork.Destroy(item);
    }

    public void DropItem(GameObject itemPrefab, Vector3 position)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터만 실행

        GameObject dropped = PhotonNetwork.Instantiate(itemPrefab.name, position, Quaternion.identity);

        string prefabName = itemPrefab.name;
        if (prefabName.StartsWith("drop_"))
        {
            prefabName = prefabName.Substring("drop_".Length);
        }
        dropped.name = prefabName;
    }
}
