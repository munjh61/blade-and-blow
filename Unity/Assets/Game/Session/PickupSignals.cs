using System;

public static class PickupSignals
{
    public static event Action<ulong, int> OnRequested;
    public static void Request(ulong token, int equipId) => OnRequested?.Invoke(token, equipId);
}