using DTO.Auth;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ResponseDTO<T>
{
    public int status;
    public string message;
    public T data;
    public long timestamp;
}
public class Axios
{
    private static string BASE_URL = "https://j13a405.p.ssafy.io/";
    private static T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);

    private static async Task<ResponseDTO<TResp>> Send<TResp>(
        string method, string path, bool withAuth, byte[] bodyBytes = null)
    {
        bool flag = true;
        while (flag)
        {
            using var req = new UnityWebRequest($"{BASE_URL}{path}", method);
            // 본문이 있으면 업로드 핸들러/Content-Type 설정
            if (bodyBytes != null)
            {
                req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            // 응답 본문 읽기
            req.downloadHandler = new DownloadHandlerBuffer();

            // 공통 헤더
            req.SetRequestHeader("Accept", "application/json");
            if (withAuth)
            {
                var at = TokenManager.GetAccess();
                if (!string.IsNullOrWhiteSpace(at)) req.SetRequestHeader("Authorization", $"Bearer {at}");
            }
            //Debug.Log($"Sending to {path}...");
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            var body = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (!string.IsNullOrWhiteSpace(body))
                return FromJson<ResponseDTO<TResp>>(body);
            else if (req.responseCode == 403 && flag && !path.Equals("api/v1/auth/refresh"))
            {
                flag = false;
                //Debug.Log("Getting New Access Token");
                ResponseDTO<AccessTokenResp> resp = await TokenManager.GetNewAccessToken();
                if (resp == null) {
                    //Debug.Log("No Refresh Token");
                    UIManager.Instance.Open(MenuId.Login);
                }
                else if (resp.status != 200)
                {
                    //Debug.Log(resp.message);
                    UIManager.Instance.Open(MenuId.Login);
                }
                //Debug.Log($"Got New Access Token {resp.data.accessToken}");
                continue;
            }
        }
        // 모든 경로에서 반환 또는 예외가 발생하도록 보장
        throw new Exception("Send<TResp> reached unexpected end of method.");
    }

    // ---- 얇은 래퍼들 ----
    public static Task<ResponseDTO<T>> Get<T>(string path, bool withAuth = false)
        => Send<T>(UnityWebRequest.kHttpVerbGET, path, withAuth);

    public static Task<ResponseDTO<Res>> Post<Req, Res>(string path, Req requestBody, bool withAuth = true)
        => Send<Res>(UnityWebRequest.kHttpVerbPOST, path, withAuth,
                     requestBody == null ? null : Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody)));

    public static Task<ResponseDTO<Res>> Put<Req, Res>(string path, Req requestBody, bool withAuth = true)
        => Send<Res>(UnityWebRequest.kHttpVerbPUT, path, withAuth,
                     requestBody == null ? null : Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody)));

    public static Task<ResponseDTO<Res>> Delete<Res>(string path, bool withAuth = true)
        => Send<Res>(UnityWebRequest.kHttpVerbDELETE, path, withAuth);

}
