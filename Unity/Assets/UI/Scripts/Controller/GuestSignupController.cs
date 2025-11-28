using TMPro;
using UnityEngine;

public class GuestSignup : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField nicknameInput;

    [Header("Buttons")]
    public TMP2DButton signupButton;

    [Header("Text")]
    public TMP_Text message;

    public string Username => usernameInput ? usernameInput.text : string.Empty;
    public string Password => passwordInput ? passwordInput.text : string.Empty;
    public string Nickname => nicknameInput ? nicknameInput.text : string.Empty;

    private void OnEnable()
    {
        if (signupButton != null)
        {
            signupButton.onClick.AddListener(OnClickSignUp);
        }
    }

    private void OnDisable()
    {
        if (signupButton != null)
        {
            signupButton.onClick.RemoveAllListeners();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.ToFrontViewCam();
            }
            UIManager.Instance.Open(MenuId.MainMenu);
        }
    }

    private async void OnClickSignUp()
    {
        string u = Username;
        string p = Password;
        string n = Nickname;
        var resp = await AccountManager.Instance.Signup(u, p, n, false, true);
        if (resp.status == 200)
        {
            await AccountManager.Instance.Login(u, p);
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.ToFrontViewCam();
            }
            UIManager.Instance.Open(MenuId.MainMenu);
        }
        message.text = resp.message;
    }
}
