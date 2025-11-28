using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Button References")]
    public TMP2DButton gameStartButton;
    public TMP2DButton optionButton;
    public TMP2DButton recordButton;
    public TMP2DButton logoutButton;
    public TMP2DButton exitButton;

    [Header("BGM")]
    public AudioClip mainSceneBGM;
    public AudioClip playSceneBGM;

    private void Start()
    {
        InitializeBGMManager();
    }

    private void InitializeBGMManager()
    {
        Debug.Log($"[MainMenuController] InitializeBGMManager - mainSceneBGM: {(mainSceneBGM != null ? mainSceneBGM.name : "null")}, playSceneBGM: {(playSceneBGM != null ? playSceneBGM.name : "null")}");

        // BGMManager가 없으면 생성 (GreetingScene을 건너뛴 경우)
        if (BGMManager.Instance == null)
        {
            Debug.Log("[MainMenuController] Creating new BGMManager");
            GameObject bgmManagerGO = new GameObject("BGMManager");
            BGMManager bgmManager = bgmManagerGO.AddComponent<BGMManager>();

            // BGM 클립들 할당
            if (mainSceneBGM != null)
            {
                bgmManager.mainSceneBGM = mainSceneBGM;
                Debug.Log($"[MainMenuController] Assigned mainSceneBGM: {mainSceneBGM.name}");
            }
            if (playSceneBGM != null)
            {
                bgmManager.playSceneBGM = playSceneBGM;
                Debug.Log($"[MainMenuController] Assigned playSceneBGM: {playSceneBGM.name}");
            }

            Debug.Log("[MainMenuController] BGMManager initialized (GreetingScene was skipped)");

            // BGM 클립 할당 후 MainScene BGM 즉시 재생
            if (mainSceneBGM != null)
            {
                bgmManager.PlayBGM(mainSceneBGM);
                Debug.Log("[MainMenuController] Manually triggered MainScene BGM");
            }
        }
        else
        {
            Debug.Log("[MainMenuController] BGMManager already exists");
        }
    }

    private void OnEnable()
    {
        if (gameStartButton != null)
            gameStartButton.onClick.AddListener(OnGameStartClick);

        if (optionButton != null)
            optionButton.onClick.AddListener(OnOptionClick);

        if (recordButton != null)
            recordButton.onClick.AddListener(OnRecordClick);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClick);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClick);
    }

    private void OnDisable()
    {
        if (gameStartButton != null)
            gameStartButton.onClick.RemoveAllListeners();

        if (optionButton != null)
            optionButton.onClick.RemoveAllListeners();

        if (recordButton != null)
            recordButton.onClick.RemoveAllListeners();

        if (logoutButton != null)
            logoutButton.onClick.RemoveAllListeners();

        if (exitButton != null)
            exitButton.onClick.RemoveAllListeners();
    }

    private void OnGameStartClick()
    {
        UIManager.Instance.Open(MenuId.SelectMode);
    }

    private void OnOptionClick()
    {
        UIManager.Instance.Open(MenuId.Settings);
    }

    private void OnRecordClick()
    {
        if (UserSession.IsGuest)
        {
            UIManager.Instance.Open(MenuId.GuestSignup);
        }
        else
        {
            UIManager.Instance.Open(MenuId.Record);
        }
    }

    private void OnLogoutClick()
    {
        AccountManager.Instance.Logout();
        SceneManager.LoadScene("GreetingScene");
    }

    private void OnExitClick()
    {
        ExitGame();
    }

    private void ExitGame()
    {
        if (PhotonNetworkManager.Instance.IsConnected) PhotonNetworkManager.Instance.Disconnect();
        Application.Quit();
    }
}