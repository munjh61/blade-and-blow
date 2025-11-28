// Assets/Scripts/Settings/SettingsStore.cs
using System;
using UnityEngine;

public class SettingsStore : MonoBehaviour
{
    public static SettingsStore Instance { get; private set; }

    [Header("Defaults")]
    public UserSettingsDefaults defaultsAsset;

    public UserSettings Current { get; private set; }

    public event Action<UserSettings> OnApplied;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (SettingsRepository.TryLoad(out var loaded))
        {
            Current = loaded;
        }
        else
        {
            Current = defaultsAsset ? UserSettings.Clone(defaultsAsset.defaults) : new UserSettings();
        }
    }

    public void Apply(UserSettings newData, bool save = true)
    {
        Current = UserSettings.Clone(newData);
        if (save) SettingsRepository.Save(Current);
        OnApplied?.Invoke(Current);
    }

    public void ResetToDefaults(bool save = false)
    {
        var baseData = defaultsAsset ? defaultsAsset.defaults : new UserSettings();
        Apply(baseData, save);
    }
}
