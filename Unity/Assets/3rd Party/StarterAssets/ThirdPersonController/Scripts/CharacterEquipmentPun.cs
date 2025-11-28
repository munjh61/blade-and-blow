using Photon.Pun;
using UnityEngine;

//public enum Slot : byte
//{
//    Weapon = 0,
//    Count
//}

[RequireComponent(typeof(PhotonView))]
public class CharacterEquipmentPun : MonoBehaviourPun
{
    [Header("Weapon Settings")]
    public GameObject[] weapons;      // 플레이어가 들 수 있는 무기 오브젝트
    public GameObject[] dropWeapons;  // 드롭될 무기 프리팹
    public GameObject nearObject;     // 근처에서 상호작용 가능한 오브젝트
    public string equippedWeapon = null;
    public int equippedWeaponIndex = -1;
    public Weapon activatedWeapon;

    [Header("Slots")]
    public int[] equippedSlots = new int[(int)Slot.Count];

    private bool isDropping = false;

    // 플레이어 -> 마스터 클라이언트에게 아이템 줍기 요청
    public void Interaction()
    {
        Debug.Log("[CharacterEquipmentPun] Interaction");
        if (!photonView.IsMine) return; // 내 캐릭터만
        if (nearObject == null) return;
        if (equippedWeaponIndex >= 0) return; // 이미 무기 있음

        if (nearObject.CompareTag("Weapon"))
        {
            PhotonView itemView = nearObject.GetComponent<PhotonView>();
            if (itemView != null)
            {
                // 마스터 클라이언트에게 아이템 줍기 요청
                photonView.RPC(nameof(RPC_RequestPickupWeapon), RpcTarget.MasterClient, itemView.ViewID, photonView.ViewID);
            }
        }
    }
    // 마스터 클라이언트가 아이템 줍기 요청 처리
    [PunRPC]
    private void RPC_RequestPickupWeapon(int itemViewID, int playerVIewID)
    {
        Debug.Log("[CharacterEquipmentPun] RPC_RequestPickupWeapon");
        // 마스터 클라이언트만 처리
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonView itemView = PhotonView.Find(itemViewID);
        if (itemView == null) return; // 이미 없어졌으면 무시
        Weapon weapon = itemView.GetComponent<Weapon>();
        if (weapon == null) return;
        
        // 요청한 플레이어 PhotonView 찾기
        PhotonView playerView = PhotonView.Find(playerVIewID);
        Debug.Log("playerView : [" + playerView + "]");
        if (playerView == null) return;

        ItemManagerPun.Instance.PickupItem(itemView.gameObject);
        //PhotonNetwork.Destroy(itemView.gameObject);

        // 아이템 장착 전파
        playerView.RPC(nameof(RPC_SetEquip), RpcTarget.All, (int)Slot.Weapon, (int)weapon.type);
    }

    // 모든 클라이언트에게 아이템 장착 전파
    [PunRPC]
    void RPC_SetEquip(int slot, int itemIndex)
    {

        if (slot < 0 || slot >= equippedSlots.Length) return;

        equippedSlots[slot] = itemIndex;
        Debug.Log($"[CharacterEquipmentPun] slot[{(Slot)slot}] <- itemIndex {itemIndex}");

        switch ((Slot)slot)
        {
            case Slot.Weapon:
                EquipWeaponByIndex(itemIndex);
                break;

            default:
                Debug.Log($"슬롯 {(Slot)slot} 장착 로직 미구현");
                break;
        }
    }

    // 무기 인덱스로 실제 오브젝트 활성화
    private void EquipWeaponByIndex(int index)
    {
        Debug.Log($"[CharacterEquipmentPun] EquipWeaponByIndex {index}");
        if (weapons == null || weapons.Length == 0) return;
        if (index < 0 || index >= weapons.Length) return;

        // 모든 무기 비활성화 → 선택된 무기만 활성화
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].SetActive(i == index);
        }

        equippedWeapon = weapons[index] != null ? weapons[index].name : null;
        activatedWeapon = weapons[index] != null ? weapons[index].GetComponent<Weapon>() : null;
        equippedWeaponIndex = index;
    }

    // 무기 드롭
    public void Drop(Vector3 pos)
    {
        if (!photonView.IsMine) return;
        if (equippedWeaponIndex < 0) return;
        if (isDropping) return; // 이미 드롭 중이면 무시

        isDropping = true;
        equippedWeapon = null;
        activatedWeapon = null;

        // 클라이언트 -> 마스터 클라이언트에게 무기 드롭 요청
        photonView.RPC(nameof(RPC_RequestDropItem), RpcTarget.MasterClient, dropWeapons[equippedWeaponIndex].name, pos, photonView.ViewID);
    }

    [PunRPC]
    private void RPC_RequestDropItem(string prefabName, Vector3 pos, int playerViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab != null)
        {
            // 현재 무기 해제
            ItemManagerPun.Instance.DropItem(prefab, pos);

            // 요청한 플레이어 PhotonView 찾기
            PhotonView playerView = PhotonView.Find(playerViewID);
            if (playerView != null)
            {
                playerView.RPC(nameof(RPC_ClearEquip), RpcTarget.All, (int)Slot.Weapon);
            }
        }
    }

    [PunRPC]
    void RPC_ClearEquip(int slot)
    {
        if (slot < 0 || slot >= equippedSlots.Length) return;
        if (equippedWeaponIndex >= 0 && equippedWeaponIndex < weapons.Length)
            weapons[equippedWeaponIndex].SetActive(false);

        equippedSlots[slot] = -1;
        equippedWeapon = null;
        activatedWeapon = null;
        equippedWeaponIndex = -1;
        isDropping = false;
    }
}