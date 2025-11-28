using System.IO;
using UnityEngine;

public static class SettingsRepository
{
    private static readonly string Path = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");

    public static void Save(UserSettings data)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path, json);
#if UNITY_EDITOR
        Debug.Log($"[SettingsRepository] Saved: {Path}");
#endif
    }

    public static bool TryLoad(out UserSettings data)
    {
        if (File.Exists(Path))
        {
            var json = File.ReadAllText(Path);
            data = JsonUtility.FromJson<UserSettings>(json);
            return data != null;
        }
        data = null;
        return false;
    }
}
