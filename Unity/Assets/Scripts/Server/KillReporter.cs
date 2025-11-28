using Game.Domain;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

[Serializable]
public struct KillReportReq
{
    public string matchId;

    public string attackerId;
    public string hitId;

    public int damage;
    public string weapon;
}

[Serializable]
public struct KillReportResp
{
}

public static class KillReporter
{
    private static readonly ConcurrentQueue<KillReportReq> _queue = new();

    public static bool TryDequeue(out KillReportReq req) => _queue.TryDequeue(out req);

    public static void Enqueue(Kill e, int damage, bool teamKill = false)
    {
        var body = BuildRequest(e, damage, teamKill);
        
        _queue.Enqueue(body);
        KillReporterDriver.Ensure();
    }

    private static KillReportReq BuildRequest(Kill e, int damage, bool teamKill)
    {
        return new KillReportReq
        {
            matchId = "unknown",
            attackerId = e.killerId ?? "unknown",
            hitId = e.victimId ?? "unknown",
            damage = damage,
            weapon = e.weapon ?? "unknown"
        };
    }
}