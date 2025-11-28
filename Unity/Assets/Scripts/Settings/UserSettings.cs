using System;
using UnityEngine;

[Serializable]
public class UserSettings
{
    // 오디오
    public float masterVolume = 1.0f;       // 0~10
    public float bgmVolume = 1.0f;          // 0~10
    public float sfxVolume = 1.0f;          // 0~10

    // 조작
    public float mouseSensitivity = 1.0f;   // 1~10

    // 화면
    public int displayWidth = 1920;
    public int displayHeight = 1080;
    public int displayHz = 60;
    public FullScreenMode displayMode = FullScreenMode.FullScreenWindow;

    // 필요 시 확장: 해상도, 언어, 색약모드 등
    // public int resolutionIndex = 0;
    // public string language = "ko";

    public static UserSettings Clone(UserSettings src)
    {
        return new UserSettings
        {
            masterVolume = src.masterVolume,
            bgmVolume = src.bgmVolume,
            sfxVolume = src.sfxVolume,
            mouseSensitivity = src.mouseSensitivity,
            displayMode = src.displayMode
        };
    }
}
