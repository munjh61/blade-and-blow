using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameExitManager : MonoBehaviour
{
    public static GameExitManager Instance { get; private set; }
    private PhotonNetworkManager _mgr;

    [Header("Scene Settings")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private float exitDelay = 0.5f;

    // 이벤트 시스템
    public static event Action OnExitStarted;
    public static event Action OnExitCompleted;
    public static event Action<string> OnExitFailed;

    private bool _isExiting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        _mgr = PhotonNetworkManager.Instance;
        if (_mgr == null) { enabled = false; return; }

        _mgr.Disconnected += OnDisconnected;
        _mgr.LeftRoom += OnLeftRoom;
    }

    private void OnDisable()
    {
        if (_mgr != null)
        {
            _mgr.Disconnected -= OnDisconnected;
            _mgr.LeftRoom -= OnLeftRoom;
        }
    }

    /// <summary>
    /// 재매치 프로세스를 시작합니다. 게임 나가기 완료 후 재매치가 시작되도록 이벤트를 체이닝합니다.
    /// </summary>
    public void RequestRematch()
    {
        OnExitCompleted += TriggerRematchCoroutine;
        LeaveGame();
    }

    /// <summary>
    /// 게임에서 나갑니다. 완료 시 OnExitCompleted 이벤트가 발생합니다.
    /// </summary>
    public void LeaveGame()
    {
        if (_isExiting) return;
        StartCoroutine(ExitCoroutine());
    }
    
    /// <summary>
    /// OnExitCompleted 이벤트에 의해 호출되어, 재매치 시작 코루틴을 실행합니다.
    /// </summary>
    private void TriggerRematchCoroutine()
    {
        OnExitCompleted -= TriggerRematchCoroutine;
        StartCoroutine(StartRematchAfterExitCoroutine());
    }

    /// <summary>
    /// UI를 열고 재매치 요청 이벤트를 발생시키는 코루틴.
    /// </summary>
    private IEnumerator StartRematchAfterExitCoroutine()
    {
        Debug.Log("[GameExitManager] Exit completed. Opening SelectMode UI...");

        UIManager.Instance.Open(MenuId.SelectMode);

        yield return new WaitForSeconds(1f); // 1초 대기

        var gsm = GameStateManager.Instance;
        if (gsm == null) 
        {
            Debug.LogError("[GameExitManager] GameStateManager not found! Cannot raise rematch event.");
            yield break;
        }

        // 4. 이제 구독자가 준비되었으므로 이벤트를 발생시킵니다.
        GameStateManager.RaiseRematchRequested();
    }

    private void OnExitRoom()
    {
        PhotonMatchingAgent.Instance?.ResetForExit();
        GameSceneInitializer.ResetStatics();

        if (!_mgr.InRoom)
        {
            LoadMainScene();
            return;
        }
        _mgr.LeaveRoom();
    }

    private IEnumerator ExitCoroutine()
    {
        if (_isExiting) yield break;
        _isExiting = true;

        OnExitStarted?.Invoke();

        try
        {
            UIManager.Instance?.CloseAll();

            // Photon 방 나가기
            OnExitRoom();

            // OnLeftRoom 콜백에서 LoadMainScene이 호출될 것임
        }
        catch (Exception ex)
        {
            Debug.LogError("[GameExitManager] Exception during exit: " + ex.Message);
            OnExitFailed?.Invoke(ex.Message);
            _isExiting = false;
            yield break;
        }

        yield return new WaitForSeconds(exitDelay);

        _isExiting = false;
    }

    private IEnumerator PostMainSceneStabilize()
    {
        // 프레임 하나 쉬고 보정(씬 객체 준비 대기)
        yield return null;

        if(_mgr != null) _mgr.IsMessageQueueRunning = true;

        OnExitCompleted?.Invoke();
        
        _isExiting = false;
    }

    private void CleanupNetworkSettings()
    {
        if (_mgr.IsConnected)
        {
            _mgr.AutomaticallySyncScene = false;
        }
    }

    private void OnDisconnected(DisconnectCause cause)
    {
        GameSceneInitializer.ResetStatics();
        _isExiting = false;
    }

    private void OnLeftRoom()
    {
        GameSceneInitializer.ResetStatics();

        LoadMainScene();
    }

    private void LoadMainScene()
    {
        GameSceneInitializer.ResetStatics();

        CleanupNetworkSettings();

        SceneManager.LoadScene(mainSceneName);
        StartCoroutine(PostMainSceneStabilize());
    }

    public bool IsExiting => _isExiting;
    
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}