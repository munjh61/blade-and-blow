using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignupController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField nicknameInput;

    [Header("Buttons")]
    public Button signupButton;

    [Header("Modal")]
    public ModalController modal;

    [Header("SFX")]
    public AudioClip clickClip;

    public string Username => usernameInput ? usernameInput.text : string.Empty;
    public string Password => passwordInput ? passwordInput.text : string.Empty;
    public string Nickname => nicknameInput ? nicknameInput.text : string.Empty;

    public bool SignupFlag;

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
                if (SignupFlag)
                {
                    UIManager.Instance.Open(MenuId.Login);
                }
                return;
            }
            UIManager.Instance.Open(MenuId.Login);
        }
    }

    private void OnEnable()
    {
        SignupFlag = false;
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
        if (signupButton)
            signupButton.onClick.RemoveAllListeners();
        SignupFlag = false;
    }

    private async void OnClickSignUp()
    {
        var resp = await AccountManager.Instance.Signup(Username, Password, Nickname);
        modal.Show(resp.message);
        if(resp.status == 200) SignupFlag = true;
    }

}
