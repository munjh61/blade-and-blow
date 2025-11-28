using System;
using UnityEngine;

public static class DropSignals
{
    public static event Action<string, Vector3, Quaternion> OnRequested;
    public static event Action<int, string, Vector3, Quaternion> OnRequestedWithIndex;

    public static void Request(string weaponKey, Vector3 pos, Quaternion rot)
        => OnRequested?.Invoke(weaponKey, pos, rot);

    public static void Request(int equipId, string weaponKey, Vector3 pos, Quaternion rot)
        => OnRequestedWithIndex?.Invoke(equipId, weaponKey, pos, rot);
}
