using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsApplier : MonoBehaviour
{
    public AudioMixer mixer; // Master, BGM, SFX exposed params 필요(예: "MasterVol","BGMVol","SFXVol")

    private void OnEnable()
    {
        SettingsStore.Instance.OnApplied += Apply;
        Apply(SettingsStore.Instance.Current);
    }

    private void OnDisable()
    {
        SettingsStore.Instance.OnApplied -= Apply;
    }

    private void Apply(UserSettings s)
    {
        // AudioMixer는 dB로 받으니, 0~1 → dB 변환 (예: -80dB ~ 0dB)
        mixer.SetFloat("MasterVol", LinearToDb(s.masterVolume));
        mixer.SetFloat("BGMVol", LinearToDb(s.bgmVolume));
        mixer.SetFloat("SFXVol", LinearToDb(s.sfxVolume));
    }

    private float LinearToDb(float v)
    {
        if (v <= 0.0001f) return -80f;
        return Mathf.Log10(v) * 20f;
    }
}
