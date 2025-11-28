using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    [Header("Buttons")]
    public Button loginButton;
    public Button signupButton;

    [Header("Modal")]
    public ModalController modal;

    [Header("SFX")]
    public AudioClip clickClip;

    public string Username => usernameInput ? usernameInput.text : string.Empty;
    public string Password => passwordInput ? passwordInput.text : string.Empty;

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (AudioManager.Instance && clickClip) AudioManager.Instance.OnPointerDown(clickClip);
            if (modal != null && modal.IsOpen)
            {
                modal.Hide();
                return;
            }
            UIManager.Instance.Open(MenuId.Greeting);
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnClickLogin();
        }
    }

    private void OnEnable()
    {
        if (loginButton)
        {
            loginButton.onClick.AddListener(
                () =>
                {
                    if (AudioManager.Instance && clickClip) AudioManager.Instance.OnPointerDown(clickClip);
                    OnClickLogin();
                });
        }
        if (signupButton)
        {
            signupButton.onClick.AddListener(
                 () =>
                 {
                     if (AudioManager.Instance && clickClip) AudioManager.Instance.OnPointerDown(clickClip);
                     OnClickSignUp();
                 });
        }
    }
    private void OnDisable()
    {
        if (loginButton)
            loginButton.onClick.RemoveAllListeners();
        if (signupButton)
            signupButton.onClick.RemoveAllListeners();
    }
    private async void OnClickLogin()
    {
        var resp = await AccountManager.Instance.Login(Username, Password);
        if (resp.status != 200)
        {
            modal.Show(resp.message);
        }
        else
        {
            if (!PhotonNetworkManager.Instance.IsConnected) PhotonNetworkManager.Instance.ConnectUsingSettings();
            if (!string.IsNullOrEmpty(UserSession.Nickname)) PhotonNetworkManager.Instance.NickName = UserSession.Nickname;
            //UIManager.Instance.Open(MenuId.MainMenu);
            SceneManager.LoadScene("MainScene");
        }
    }

    private void OnClickSignUp()
    {
        UIManager.Instance.Open(MenuId.Signup);
    }

}
