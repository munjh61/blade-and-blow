using UnityEngine;
using UnityEngine.Events;

public class CameraLook : MonoBehaviour
{
    [Min(0.01f)]
    [SerializeField] private float _sensitivity = 1.0f;
    public float Sensitivity => _sensitivity;

    [Header("Events")]
    public UnityEvent<float> onSensitivityChanged;

    private void OnEnable()
    {
        var store = SettingsStore.Instance;
        if (store != null)
        {
            store.OnApplied += HandleSettings;
            HandleSettings(store.Current); // 즉시 반영
        }
        else
        {
            // SettingsStore 없을 때도 현재값 알림
            onSensitivityChanged?.Invoke(_sensitivity);
        }
    }

    private void OnDisable()
    {
        var store = SettingsStore.Instance;
        if (store != null)
            store.OnApplied -= HandleSettings;
    }

    private void HandleSettings(UserSettings s)
    {
        SetSensitivity(s?.mouseSensitivity ?? 1f);
    }

    public void SetSensitivity(float value)
    {
        if (Mathf.Approximately(_sensitivity, value)) return;
        _sensitivity = value;
        onSensitivityChanged?.Invoke(_sensitivity);
    }
}
