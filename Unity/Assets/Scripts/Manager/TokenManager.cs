using DTO.Auth;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class TokenManager
{
    /* ===== Access Token ===== */
    private static string _accessToken;
    public static void SetAccess(string token) => _accessToken = token;
    public static string GetAccess() => _accessToken;
    public static void Clear() => _accessToken = null;

    /* ===== Refresh Token ===== */
    // 저장 위치: %AppData%\BladeAndBlow\roaming\refresh_token.bin
    private static readonly string RootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BladeAndBlow");
    private static readonly string FilePath = Path.Combine(RootDir, "refreshToken.bin");
    // 비밀값
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BnBzzang");

    public static void DeleteRefresh()
    {
        if(File.Exists(FilePath))
            File.Delete(FilePath);
    }
    public static void SaveRefresh(string refreshToken)
    {
        Directory.CreateDirectory(RootDir);
        if (string.IsNullOrEmpty(refreshToken)) { DeleteRefresh(); return; }
        // 암호화
        byte[] plain = Encoding.UTF8.GetBytes(refreshToken);
        var code = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, code);

        Array.Clear(plain, 0, plain.Length);
    }
    public static string LoadRefresh()
    {
        if (!File.Exists(FilePath)) return null;
        byte[] code = File.ReadAllBytes(FilePath);
        byte[] plain = ProtectedData.Unprotect(code, Entropy, DataProtectionScope.CurrentUser);
        string refreshToken = Encoding.UTF8.GetString(plain);
        Array.Clear (plain, 0, plain.Length);
        return refreshToken;
    }

    public static async Task<ResponseDTO<AccessTokenResp>> GetNewAccessToken()
    {
        var req = new RefreshTokenReq { refreshToken = LoadRefresh() };
        if(string.IsNullOrWhiteSpace(req.refreshToken)) return null;

        var resp = await Axios.Post<RefreshTokenReq, AccessTokenResp>("api/v1/auth/refresh", req, false);
        if (resp.status == 200)
        {
            SetAccess(resp.data.accessToken);
        }
        return resp;
    }
}
