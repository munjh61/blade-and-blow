using System.Collections.Generic;
using UnityEngine;

public static class DropRegistry
{
    private static readonly Dictionary<ulong, DropHandle> _byToken = new();

    public static void Register(DropHandle h)
    {
        if (h == null) return;
        _byToken[h.Token] = h;
    }

    public static void Unregister(DropHandle h)
    {
        if (h == null) return;
        if (_byToken.TryGetValue(h.Token, out var cur) && cur == h)
            _byToken.Remove(h.Token);
    }

    public static bool TryGet(ulong token, out GameObject go)
    {
        if (_byToken.TryGetValue(token, out var h) && h != null)
        {
            go = h.gameObject;
            return true;
        }
        go = null;
        return false;
    }

    public static bool RemoveAndDestroy(ulong token)
    {
        if (_byToken.TryGetValue(token, out var h) && h != null)
        {
            _byToken.Remove(token);
            if (h != null && h.gameObject != null)
            {
                Object.Destroy(h.gameObject);
            }
            return true;
        }
        return false;
    }
}
