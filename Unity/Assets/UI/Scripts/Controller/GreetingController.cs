using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GreetingController : MonoBehaviour
{
    [Header("Buttons")]
    public Button loginButton;
    public Button guestLoginButton;
    public Button exitButton;

    [Header("SFX")]
    public AudioClip clickClip;

    [Header("BGM")]
    public AudioClip mainSceneBGM;
    public AudioClip playSceneBGM;

    private void Start()
    {
        InitializeBGMManager();
    }

    private void InitializeBGMManager()
    {
        // BGMManager가 없으면 생성
        if (BGMManager.Instance == null)
        {
            GameObject bgmManagerGO = new GameObject("BGMManager");
            BGMManager bgmManager = bgmManagerGO.AddComponent<BGMManager>();

            // BGM 클립들 할당
            if (mainSceneBGM != null) bgmManager.mainSceneBGM = mainSceneBGM;
            if (playSceneBGM != null) bgmManager.playSceneBGM = playSceneBGM;

            Debug.Log("[GreetingController] BGMManager initialized with BGM clips");
        }
    }

    private void OnEnable()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(
            () =>
            {
                if (AudioManager.Instance && clickClip) AudioManager.Instance.OnPointerDown(clickClip);
                OnLoginClick();
            });
        }

        if (guestLoginButton != null)
        {
            guestLoginButton.onClick.AddListener(
                () =>
                {
                    if (AudioManager.Instance && clickClip) AudioManager.Instance.OnPointerDown(clickClip);
                    OnGuestLoginClick();
                });
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(
                () =>
                {
                    if (AudioManager.Instance && clickClip) AudioManager.Instance.OnPointerDown(clickClip);
                    OnExitClick();
                });
        }

    }

    private void OnDisable()
    {
        if (loginButton != null)
            loginButton.onClick.RemoveAllListeners();

        if (guestLoginButton != null)
            guestLoginButton.onClick.RemoveAllListeners();

        if (exitButton != null)
            exitButton.onClick.RemoveAllListeners();
    }

    private void OnLoginClick()
    {
        UIManager.Instance.Open(MenuId.Login);
    }

    private async void OnGuestLoginClick()
    {
        if (!PhotonNetworkManager.Instance.IsConnected) PhotonNetworkManager.Instance.ConnectUsingSettings();
        //UIManager.Instance.Open(MenuId.MainMenu);
        var resp = await AccountManager.Instance.GuestLogin();
        Debug.Log(resp.message);
        SceneManager.LoadScene("MainScene");
    }

    private void OnExitClick()
    {
        var appController = FindFirstObjectByType<AppController>();
        if (appController != null)
        {
            appController.ExitGame();
        }
    }
}