using UnityEngine;
using UnityEngine.SceneManagement;

public class AppController : MonoBehaviour
{
    void Start()
    {
        if (!string.IsNullOrEmpty(UserSession.Nickname)) PhotonNetworkManager.Instance.NickName = UserSession.Nickname;
        if (!PhotonNetworkManager.Instance.IsConnected) PhotonNetworkManager.Instance.ConnectUsingSettings();
        Init();
    }

    public void ExitGame()
    {
        if (PhotonNetworkManager.Instance.IsConnected) PhotonNetworkManager.Instance.Disconnect();
        Application.Quit();
    }

    private async void Init()
    {
        string autologin = await AccountManager.Instance.AutoLogin();
        if (autologin == "OK")
        {
            if (!string.IsNullOrEmpty(UserSession.Nickname)) PhotonNetworkManager.Instance.NickName = UserSession.Nickname;
            //UIManager.Instance.Open(MenuId.MainMenu);
            SceneManager.LoadScene("MainScene");
            return;
        }
        UIManager.Instance.Open(MenuId.Greeting);
        return;
    }
}
