using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class MakeRoomList : MonoBehaviour
{
    private RoomInfo _roomInfo;
    private TMP_Text roomInfoText;
    private PhotonManager photonManager;

    public RoomInfo RoomInfo
    {
        get
        {
            return _roomInfo;
        }
        set
        {
            _roomInfo = value;
            roomInfoText.text = $"{_roomInfo.Name}({_roomInfo.PlayerCount}/{_roomInfo.MaxPlayers})";
            //룸 버튼 클릭 이벤트에 함수 연결
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => onEnterRoom(_roomInfo.Name));
        }
    }
    private void Awake()
    {
        roomInfoText = GetComponentInChildren<TMP_Text>();
        photonManager = GameObject.Find("PhotonManager").GetComponent<PhotonManager>();
    }
    void onEnterRoom(string roomName)
    {
        photonManager.SetUserId();
        //특정 방 접속
        PhotonNetwork.JoinRoom(roomName);
    }
}
