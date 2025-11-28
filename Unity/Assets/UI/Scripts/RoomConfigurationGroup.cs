using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class RoomConfigurationGroup : MonoBehaviour
{
    private readonly List<RoomConfigurable> _configurables = new();
    private RoomConfiguration _currentConfig = new();

    [Header("Events")]
    public UnityEvent<RoomConfiguration> onConfigurationChanged;

    public void Register(RoomConfigurable configurable)
    {
        if (configurable == null || _configurables.Contains(configurable)) return;
        
        _configurables.Add(configurable);
        configurable.group = this;
        configurable.LoadFromConfiguration(_currentConfig);
    }

    public void Unregister(RoomConfigurable configurable)
    {
        _configurables.Remove(configurable);
    }

    public RoomConfiguration GetConfiguration()
    {
        // 모든 configurable에서 설정 수집
        foreach (var config in _configurables)
            config.ApplyToConfiguration(_currentConfig);

        return _currentConfig;
    }

    public void SetConfiguration(RoomConfiguration config)
    {
        _currentConfig = config;
        foreach (var configurable in _configurables)
            configurable.LoadFromConfiguration(config);

        onConfigurationChanged?.Invoke(config);
    }
}