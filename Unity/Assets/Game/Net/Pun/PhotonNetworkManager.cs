using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-5000)]
public sealed class PhotonNetworkManager : MonoBehaviourPunCallbacks, IPhotonNetworkManager
{
    public static PhotonNetworkManager Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField] private bool autoSyncScene = true;
    [SerializeField] private bool autoJoinLobbyOnConnected = true;

    // IPhotonNetworkManager props
    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool InLobby => PhotonNetwork.NetworkingClient?.InLobby ?? false;
    public bool InRoom => PhotonNetwork.InRoom;
    public List<Player> PlayerList => new List<Player>(PhotonNetwork.PlayerList);
    public bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public double Time => PhotonNetwork.Time;
    public Room CurrentRoom => PhotonNetwork.CurrentRoom;
    public string GameVersion { get => PhotonNetwork.GameVersion; set => PhotonNetwork.GameVersion = value; }
    public string NickName { get => PhotonNetwork.NickName; set => PhotonNetwork.NickName = value; }
    public bool AutomaticallySyncScene { get => PhotonNetwork.AutomaticallySyncScene; set => PhotonNetwork.AutomaticallySyncScene = value; }
    public bool IsMessageQueueRunning { get => PhotonNetwork.IsMessageQueueRunning; set => PhotonNetwork.IsMessageQueueRunning = value; }

    public ClientState ClientState => PhotonNetwork.NetworkClientState;

    // ===== Master Client Accessors =====
    public Player MasterClient => PhotonNetwork.MasterClient;
    public int MasterActorNumber => PhotonNetwork.MasterClient?.ActorNumber ?? -1;
    public bool HasMasterClient => PhotonNetwork.MasterClient != null;

    // ===== Local Player Accessors =====
    public Player LocalPlayer => PhotonNetwork.LocalPlayer;
    public int LocalActorNumber => PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
    public bool HasLocalPlayer => PhotonNetwork.LocalPlayer != null;

    // Events
    public event Action ConnectedToMaster;
    public event Action JoinedLobby;
    public event Action LeftLobby;
    public event Action JoinedRoom;
    public event Action LeftRoom;
    public event Action<short, string> JoinRandomFailed;
    public event Action<short, string> JoinRoomFailed;
    public event Action<Hashtable> RoomPropertiesUpdated;
    public event Action<Player> PlayerEnteredRoomEvent;
    public event Action<Player> PlayerLeftRoomEvent;
    public event Action<Player> MasterClientSwitched;
    public event Action<DisconnectCause> Disconnected;
    public event Action<Player,int> LocalPlayerReady;
    public event Action<Player, Hashtable> PlayerPropertiesUpdated;
    public event Action<Hashtable> LocalPlayerPropertiesUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = autoSyncScene;
        if (string.IsNullOrEmpty(PhotonNetwork.GameVersion))
            PhotonNetwork.GameVersion = Application.version;
    }

    public void ConnectUsingSettings() => PhotonNetwork.ConnectUsingSettings();
    public void ConnectToRegion(string regionCode) => PhotonNetwork.ConnectToRegion(regionCode);
    public void Disconnect() => PhotonNetwork.Disconnect();
    public void JoinLobby() => PhotonNetwork.JoinLobby();
    public void LeaveLobby() => PhotonNetwork.LeaveLobby();
    public void JoinRoom(string roomName) => PhotonNetwork.JoinRoom(roomName);
    public void JoinRandomRoom(Hashtable expectedCustomProps, byte expectedMaxPlayers)
    {
        PhotonNetwork.JoinRandomRoom(expectedCustomProps, expectedMaxPlayers);
    }
    public void CreateRoom(string roomName, RoomOptions options)
        => PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public void SetRoomProperties(Dictionary<string, object> props)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        var ht = new Hashtable();
        foreach (var kv in props) ht[kv.Key] = kv.Value;
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
    }

    public void SetRoomOpenVisible(bool open, bool visible)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        PhotonNetwork.CurrentRoom.IsOpen = open;
        PhotonNetwork.CurrentRoom.IsVisible = visible;
    }

    public void LoadLevel(string sceneName)
    {
        if (SceneManager.GetActiveScene().name != sceneName)
            PhotonNetwork.LoadLevel(sceneName);
    }

    // Photon callbacks -> events
    public override void OnConnectedToMaster()
    {
        ConnectedToMaster?.Invoke();

        if (autoJoinLobbyOnConnected && !InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }
    public override void OnJoinedLobby() => JoinedLobby?.Invoke();
    public override void OnLeftLobby() => LeftLobby?.Invoke();
    public override void OnJoinedRoom()
    {
        JoinedRoom?.Invoke();

        var lp = PhotonNetwork.LocalPlayer;
        if (lp != null)
        {
            LocalPlayerReady?.Invoke(lp, lp.ActorNumber);
        }
    }
    public override void OnLeftRoom() => LeftRoom?.Invoke();
    public override void OnJoinRandomFailed(short c, string m) => JoinRandomFailed?.Invoke(c, m);
    public override void OnJoinRoomFailed(short c, string m) => JoinRoomFailed?.Invoke(c, m);
    public override void OnRoomPropertiesUpdate(Hashtable h) => RoomPropertiesUpdated?.Invoke(h);
    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        PlayerPropertiesUpdated?.Invoke(target, changedProps);

        if (target != null && target.IsLocal)
            LocalPlayerPropertiesUpdated?.Invoke(changedProps);
    }
    public override void OnPlayerEnteredRoom(Player p) => PlayerEnteredRoomEvent?.Invoke(p);
    public override void OnPlayerLeftRoom(Player p) => PlayerLeftRoomEvent?.Invoke(p);
    public override void OnMasterClientSwitched(Player newMaster) => MasterClientSwitched?.Invoke(newMaster);
    public override void OnDisconnected(DisconnectCause cause) => Disconnected?.Invoke(cause);
}
