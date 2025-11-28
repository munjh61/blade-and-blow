using System.Collections;
using System.Diagnostics;
using UnityEngine;

public sealed class KillReporterDriver : MonoBehaviour
{
    private static KillReporterDriver _inst;
    private bool _sending;

    public static void Ensure()
    {
        if (_inst != null) return;
        var go = new GameObject("[KillReporterDriver]");
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<KillReporterDriver>();
    }

    private void Update()
    {
        if (_sending) return;

        if (KillReporter.TryDequeue(out var req))
            StartCoroutine(SendOne(req));
    }

    private IEnumerator SendOne(KillReportReq body)
    {
        _sending = true;
        
        if (string.IsNullOrWhiteSpace(body.matchId) || body.matchId == "unknown")
        {
            var room = PhotonNetworkManager.Instance?.CurrentRoom;
            body.matchId = room?.Name ?? "unknown";
        }

        var sw = Stopwatch.StartNew();

        var task = Axios.Post<KillReportReq, KillReportResp>("api/ingame/killSave", body, withAuth: true);

        while (!task.IsCompleted) yield return null;
        sw.Stop();

        if (task.IsFaulted)
        {
            var msg = task.Exception?.GetBaseException()?.Message ?? "unknown error";
            UnityEngine.Debug.LogWarning($"[KillReporter] drop: {msg}");
        }
        else
        {
            var dto = task.Result;
            if (dto == null)
            {
                UnityEngine.Debug.LogWarning($"[KillReporter] ◀ null response (no body), elapsed={sw.ElapsedMilliseconds}ms");
            }
            else
            {
                UnityEngine.Debug.Log($"[KillReporter] ◀ ok: http(statusField)={dto.status}, msg={dto.message ?? "(null)"}, ts={dto.timestamp}, elapsed={sw.ElapsedMilliseconds}ms");
            }
        }

        _sending = false;
    }
}
