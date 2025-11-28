using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
public class PhotonManager : MonoBehaviourPunCallbacks
{
    //게임의 버전
    private readonly string version = "1.0";
    private string userId = "Nickname";
    public TMP_InputField userIF;
    public TMP_InputField roomNameIF;

    //룸 목록 데이터
    private Dictionary<string, GameObject> rooms = new Dictionary<string, GameObject>();
    private GameObject roomItemPrefab;
    public Transform scrollContent;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = version;

        //포톤 서버 접속
        if(PhotonNetwork.IsConnected == false)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        userId = PlayerPrefs.GetString("USER_ID", $"USER_{Random.Range(1, 21):00}");
        userIF.text = userId;
        PhotonNetwork.NickName = userId;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master!");
        PhotonNetwork.JoinLobby();
    }

    //포톤 로비 접속 시 호출 콜백 함수
    public override void OnJoinedLobby()
    {
        Debug.Log($"PhotonNetwork.InLobby = {PhotonNetwork.InLobby}");
        //PhotonNetwork.JoinRandomRoom();
    }

    //랜덤 룸 입장이 실패하면 호출되는 콜백
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"JoinRandom Failed {returnCode}:{message}");
        OnMakeRoomClick();
    }

    //룸 입장 시 호출 콜백
    public override void OnJoinedRoom()
    {
        Debug.Log($"PhotonNetwork.InRoom = {PhotonNetwork.InRoom}");
        Debug.Log($"Player Count = {PhotonNetwork.CurrentRoom.PlayerCount}");

        foreach(var player in PhotonNetwork.CurrentRoom.Players)
        {
            Debug.Log($"{player.Value.NickName}, {player.Value.ActorNumber}");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            //마스터 클라이언트 -> 룸 입장 후 메인 씬 로딩
            PhotonNetwork.LoadLevel("MainScene");
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        GameObject tempRoom = null;

        foreach(var roomInfo in roomList)
        {
            if(roomInfo.RemovedFromList == true)    //삭제 룸 존재
            {
                rooms.TryGetValue(roomInfo.Name, out tempRoom);
                Destroy(tempRoom);
                rooms.Remove(roomInfo.Name);
            }
            else
            {
                if(rooms.ContainsKey(roomInfo.Name) == false)   //룸 추가
                {
                    GameObject roomPrefab = Instantiate(roomItemPrefab, scrollContent);
                    roomPrefab.GetComponent<MakeRoomList>().RoomInfo = roomInfo;
                    rooms.Add(roomInfo.Name, roomPrefab);
                }
                else
                {
                    rooms.TryGetValue(roomInfo.Name, out tempRoom);
                    tempRoom.GetComponent<MakeRoomList>().RoomInfo = roomInfo;
                }
            }
        }
    }
    public void SetUserId()
    {
        if (string.IsNullOrEmpty(userIF.text))
        {
            return;
        }
        else
        {
            //입력한 값으로 userid 설정, playerprefs에 저장
            PlayerPrefs.SetString("USER_ID", userIF.text);
            PhotonNetwork.NickName = userIF.text;
        }
        
    }
    public void OnLoginClick()
    {
        SetUserId();
        PhotonNetwork.JoinRandomRoom();
    }

    string SetRoomName()
    {
        if (string.IsNullOrEmpty(roomNameIF.text))
        {
            roomNameIF.text = $"Room_{Random.Range(1, 101):000}";
        }
        return roomNameIF.text;
    }
    public void OnMakeRoomClick()
    {
        SetUserId();
        RoomOptions ro = new RoomOptions();
        ro.MaxPlayers = 20;
        ro.IsOpen = true;
        ro.IsVisible = true;

        PhotonNetwork.CreateRoom(SetRoomName(), ro);
    }
}
