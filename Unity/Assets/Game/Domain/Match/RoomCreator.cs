using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace Game.Domain.Match
{
    /// <summary>
    /// 방 생성 전용 클래스 - 매칭 로직과 분리
    /// </summary>
    public class RoomCreator : MonoBehaviour
    {
        public static RoomCreator Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private PhotonNetworkManager networkManager;

        [Header("Room Settings")]
        [SerializeField] private string roomPrefix = "private_";
        [SerializeField] private int defaultMaxPlayers = 10;

        public event Action<string> OnRoomCreated;
        public event Action<short, string> OnRoomCreationFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!networkManager)
                networkManager = PhotonNetworkManager.Instance;
        }

        /// <summary>
        /// RoomConfiguration을 받아 새로운 Private 방을 생성합니다
        /// </summary>
        public void CreatePrivateRoom(RoomConfiguration config, string hostName = "Host")
        {
            if (config == null)
            {
                Debug.LogError("[RoomCreator] RoomConfiguration is null");
                OnRoomCreationFailed?.Invoke(-1, "Configuration is null");
                return;
            }

            if (!networkManager.IsConnected)
            {
                Debug.LogError("[RoomCreator] Not connected to Photon");
                OnRoomCreationFailed?.Invoke(-2, "Not connected to network");
                return;
            }

            try
            {
                string roomName = GenerateRoomName(config);
                RoomOptions options = CreateRoomOptions(config);

                Debug.Log($"[RoomCreator] Creating private room: {roomName}");
                networkManager.CreateRoom(roomName, options);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomCreator] Failed to create room: {e.Message}");
                OnRoomCreationFailed?.Invoke(-3, e.Message);
            }
        }

        /// <summary>
        /// RoomConfiguration에서 방 이름 생성
        /// </summary>
        private string GenerateRoomName(RoomConfiguration config)
        {
            if (!string.IsNullOrEmpty(config.roomCode))
            {
                return roomPrefix + config.roomCode;
            }

            // roomCode가 없으면 랜덤 생성
            string randomCode = GenerateRandomCode(8);
            config.roomCode = randomCode; // 생성된 코드를 설정에 저장
            return roomPrefix + randomCode;
        }

        /// <summary>
        /// RoomConfiguration에서 RoomOptions 생성
        /// </summary>
        private RoomOptions CreateRoomOptions(RoomConfiguration config)
        {
            var customProps = new Hashtable
            {
                { "gameMode", config.gameMode.ToString() },
                { "selectedMap", config.selectedMap.ToString() },
                { "allowAllWeapons", config.allowAllWeapons },
                { "isPrivate", config.isPrivate }
            };

            // 허용된 무기 목록을 문자열로 저장
            if (config.allowedWeapons != null && config.allowedWeapons.Count > 0)
            {
                string weaponsString = string.Join(",", config.allowedWeapons);
                customProps["allowedWeapons"] = weaponsString;
            }

            return new RoomOptions
            {
                MaxPlayers = (byte)Mathf.Clamp(config.maxPlayers, 1, 20),
                IsVisible = !config.isPrivate, // Private 방은 로비에서 보이지 않음
                IsOpen = true,
                CustomRoomProperties = customProps,
                CustomRoomPropertiesForLobby = new string[]
                {
                    "gameMode", "selectedMap", "isPrivate"
                }
            };
        }

        /// <summary>
        /// 랜덤 방 코드 생성 (영문 대문자 + 숫자)
        /// </summary>
        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }

        /// <summary>
        /// 방 생성 성공 시 호출 (외부에서 호출)
        /// </summary>
        public void NotifyRoomCreated(string roomName)
        {
            Debug.Log($"[RoomCreator] Room created successfully: {roomName}");
            OnRoomCreated?.Invoke(roomName);
        }

        /// <summary>
        /// 방 생성 실패 시 호출 (외부에서 호출)
        /// </summary>
        public void NotifyRoomCreationFailed(short returnCode, string message)
        {
            Debug.LogError($"[RoomCreator] Room creation failed: {returnCode} - {message}");
            OnRoomCreationFailed?.Invoke(returnCode, message);
        }
    }
}