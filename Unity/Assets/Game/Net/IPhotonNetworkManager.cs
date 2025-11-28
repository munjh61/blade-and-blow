using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Realtime;

public interface IPhotonNetworkManager
{
    // IPhotonNetworkManager props
    bool IsConnected { get; }
    bool InLobby { get; }
    bool InRoom { get; }
    bool IsMasterClient { get; }
    double Time { get; }
    Room CurrentRoom { get; }
    string GameVersion { get; set; }
    string NickName { get; set; }
    bool AutomaticallySyncScene { get; set; }
    bool IsMessageQueueRunning { get; set; }

    // ===== Master Client Accessors =====
    Player MasterClient { get; }
    int MasterActorNumber { get; }
    bool HasMasterClient { get; }

    // ===== Local Player Accessors =====
    Player LocalPlayer { get; }
    int LocalActorNumber { get; }
    bool HasLocalPlayer { get; }

    void ConnectUsingSettings();
    void ConnectToRegion(string regionCode);
    void Disconnect();

    void JoinLobby();
    void LeaveLobby();
    void JoinRoom(string roomName);
    void JoinRandomRoom(Hashtable expectedCustomProps, byte expectedMaxPlayers);
    void CreateRoom(string roomName, RoomOptions options);
    void LeaveRoom();

    void SetRoomProperties(Dictionary<string, object> props);
    void SetRoomOpenVisible(bool open, bool visible);
    void LoadLevel(string sceneName);

    event Action ConnectedToMaster;
    event Action JoinedLobby;
    event Action LeftLobby;
    event Action JoinedRoom;
    event Action LeftRoom;
    event Action<short, string> JoinRandomFailed;
    event Action<short, string> JoinRoomFailed;
    event Action<Hashtable> RoomPropertiesUpdated;
    event Action<Player> PlayerEnteredRoomEvent;
    event Action<Player> PlayerLeftRoomEvent;
    event Action<Player> MasterClientSwitched;
    event Action<Player,int> LocalPlayerReady;
    event Action<Player, Hashtable> PlayerPropertiesUpdated;
    event Action<DisconnectCause> Disconnected;
}