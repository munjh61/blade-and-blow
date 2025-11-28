using DTO.Auth;
using System;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }

    private static readonly string[] GuestRoles =
    {
        "BraveWarrior",
        "KoreaSceretWeapon",
        "GreatWizard",
    };

    private static string GenerateGuestNickname()
    {
        string role = GuestRoles[RandomNumberGenerator.GetInt32(GuestRoles.Length)];
        int number = RandomNumberGenerator.GetInt32(1000, 10000); // 1000~9999
        return $"{role}_{number}";
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Task<ResponseDTO<Nothing>> Signup(string username, string password, string nickname, bool isGuestLogin = false, bool guestAsUser = false)
    {
        if (username.StartsWith("Guest") && !isGuestLogin)
        {
            ResponseDTO<Nothing> res = new();
            res.status = 400;
            res.message = "ID cannot start with Guest!!!!";
            return Task.FromResult(res);
        }
        var req = new SignupReq { username = username, password = password, nickname = nickname };
        if (guestAsUser)
        {
            return Axios.Post<SignupReq, Nothing>("api/v1/user/guestasuser", req);
        }
        return Axios.Post<SignupReq, Nothing>("api/v1/auth/signup", req);
    }
    public async Task<ResponseDTO<LoginResp>> Login(string username, string password)
    {
        var req = new LoginReq { username = username, password = password };
        var resp = await Axios.Post<LoginReq, LoginResp>("api/v1/auth/login", req, false);
        if (resp.status == 200)
        {
            TokenManager.SetAccess(resp.data.accessToken);
            TokenManager.SaveRefresh(resp.data.refreshToken);
            UserSession.Username = resp.data.username;
            UserSession.Nickname = resp.data.nickname;
            UserSession.IsGuest = UserSession.Username.StartsWith("Guest");
        }
        return resp;
    }
    public async Task<ResponseDTO<LoginResp>> GuestLogin()
    {
        string mac = "ID";

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            var addr = ni.GetPhysicalAddress();
            if (addr != null && addr.GetAddressBytes().Length > 0)
            {
                mac = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace("-", "");
                break;
            }
        }
        string username = $"Guest_{mac}";
        string password = "bladeandblow";
        string nickname = GenerateGuestNickname();
        UserSession.IsGuest = true;
        await Signup(username, password, nickname, true);
        return await Login(username, password);
    }
    public void Logout()
    {
        TokenManager.Clear();
        TokenManager.DeleteRefresh();
        UserSession.Username = null;
        UserSession.Nickname = null;
        UserSession.IsGuest = true;
    }

    public async Task<ResponseDTO<MeResp>> GetMe()
    {
        var resp = await Axios.Get<MeResp>("api/v1/user/me", true);
        if (resp.status == 200)
        {
            UserSession.Username = resp.data.username;
            UserSession.Nickname = resp.data.nickname;
            UserSession.IsGuest = UserSession.Username.StartsWith("Guest");
        }
        return resp;
    }

    public async Task<ResponseDTO<Nothing>> ChangeNickname(string nickname)
    {
        var req = new NicknameReq { nickname = nickname };
        var resp = await Axios.Put<NicknameReq, Nothing>("api/v1/user/nickname", req);
        if (resp.status == 200) { UserSession.Nickname = nickname; }
        return resp;
    }

    public async Task<string> AutoLogin()
    {
        ResponseDTO<AccessTokenResp> resp = await TokenManager.GetNewAccessToken();
        if (resp == null) return "No RefreshToken";
        if (resp.status != 200) return resp.message;
        var me = await GetMe();
        if (me.status != 200) return "Failed to Get Me";
        return "OK";
    }

    public async Task<ResponseDTO<PingResp>> TestPing()
    {
        var resp = await Axios.Get<PingResp>("api/v1/test/ping", true);
        return resp;
    }

}
