using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    [Header("Display")]
    public ResolutionController resolutionController;

    [Header("Audio")]
    public Slider masterVolumeSlider;   // 0~1
    public Slider bgmVolumeSlider;      // 0~1
    public Slider sfxVolumeSlider;      // 0~1

    [Header("Control")]
    public Slider mouseSensitivitySlider; // 0.1~10 같은 범위

    [Header("Buttons")]
    public Button applyButton;
    //public Button revertButton;
    public Button defaultButton;
    public TMP2DButton ExitButton;

    private UserSettings _working; // UI에서 편집 중인 사본

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.ToFrontViewCam();
            }
            Invoke("DelayedOpenMainMenu", 2.0f);
        }
    }

    public void ResetUIFromSaved()
    {
        _working = UserSettings.Clone(SettingsStore.Instance.Current);
        SyncUIFromData(_working);

        if (resolutionController != null)
            resolutionController.SelectBySaved(_working.displayWidth, _working.displayHeight, _working.displayHz, _working.displayMode);
    }

    private void OnEnable()
    {
        // 현재값으로 UI 초기화
        ResetUIFromSaved();

        // 리스너 등록
        masterVolumeSlider.onValueChanged.AddListener(v => _working.masterVolume = v);
        bgmVolumeSlider.onValueChanged.AddListener(v => _working.bgmVolume = v);
        sfxVolumeSlider.onValueChanged.AddListener(v => _working.sfxVolume = v);
        mouseSensitivitySlider.onValueChanged.AddListener(v => _working.mouseSensitivity = v);
        //fullscreenToggle.onValueChanged.AddListener(v => _working.fullscreen = v);

        applyButton.onClick.AddListener(OnApply);
        //revertButton.onClick.AddListener(OnRevert);
        defaultButton.onClick.AddListener(OnDefault);
        ExitButton.onClick.AddListener(OnExit);
    }

    private void OnDisable()
    {
        // 리스너 해제 (메모리/중복 호출 방지)
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        bgmVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        mouseSensitivitySlider.onValueChanged.RemoveAllListeners();
        //fullscreenToggle.onValueChanged.RemoveAllListeners();

        applyButton.onClick.RemoveAllListeners();
        //revertButton.onClick.RemoveAllListeners();
        defaultButton.onClick.RemoveAllListeners();
        ExitButton.onClick.RemoveAllListeners();
    }

    private void SyncUIFromData(UserSettings data)
    {
        if (masterVolumeSlider) masterVolumeSlider.SetValueWithoutNotify(data.masterVolume);
        if (bgmVolumeSlider) bgmVolumeSlider.SetValueWithoutNotify(data.bgmVolume);
        if (sfxVolumeSlider) sfxVolumeSlider.SetValueWithoutNotify(data.sfxVolume);

        if (mouseSensitivitySlider) mouseSensitivitySlider.SetValueWithoutNotify(data.mouseSensitivity);

        if (resolutionController != null)
            resolutionController.RebuildAndSync();

        if (resolutionController != null)
            resolutionController.SelectBySaved(data.displayWidth, data.displayHeight, data.displayHz, data.displayMode);
    }

    private void OnApply()
    {
        int w, h, hz; FullScreenMode mode;
        if (resolutionController != null &&
            resolutionController.TryGetSelected(out w, out h, out hz, out mode))
        {
            _working.displayWidth = w;
            _working.displayHeight = h;
            _working.displayHz = hz;
            _working.displayMode = mode;

            // 스토어에 적용 + 저장 + 이벤트 발행
            SettingsStore.Instance.Apply(_working, save: true);

            if (mode == FullScreenMode.FullScreenWindow)
                Screen.SetResolution(w, h, mode);
            else
                Screen.SetResolution(w, h, mode, new RefreshRate { numerator = (uint)hz, denominator = 1 });
        }
    }

    private void OnRevert()
    {
        // 저장된 현재값으로 되돌려 UI 갱신
        _working = UserSettings.Clone(SettingsStore.Instance.Current);
        SyncUIFromData(_working);
    }

    private void OnDefault()
    {
        // 기본값으로 UI 갱신 (저장 X, 적용은 Apply 누를 때)
        var def = SettingsStore.Instance.GetComponent<SettingsStore>().defaultsAsset;
        _working = UserSettings.Clone(def ? def.defaults : new UserSettings());
        SyncUIFromData(_working);
    }

    public void OnExit()
    {
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            cameraController.ToFrontViewCam();
            if (cameraController.IsPlayScene())
                UIManager.Instance.Open(MenuId.EscapeMenu);
            else
                Invoke("DelayedOpenMainMenu", 2.0f);
        }
    }

    private void DelayedOpenMainMenu()
    {
        UIManager.Instance.Open(MenuId.MainMenu);
    }
}