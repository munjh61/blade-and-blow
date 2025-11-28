
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Scene BGMs")]
    public AudioClip mainSceneBGM;
    public AudioClip playSceneBGM;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    public bool fadeTransition = true;
    public float fadeTime = 1f;

    private AudioSource bgmAudioSource;
    private string currentSceneName;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSource();

        // 씬 변경 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 설정 변경 이벤트 구독
        if (SettingsStore.Instance != null)
        {
            SettingsStore.Instance.OnApplied += OnSettingsApplied;
            // 현재 설정 적용
            OnSettingsApplied(SettingsStore.Instance.Current);
        }

        Debug.Log("[BGMManager] BGMManager initialized");
    }

    private void Start()
    {
        // 설정 시스템이 늦게 초기화되는 경우를 위해 재시도
        if (SettingsStore.Instance != null)
        {
            SettingsStore.Instance.OnApplied -= OnSettingsApplied; // 중복 방지
            SettingsStore.Instance.OnApplied += OnSettingsApplied;
            OnSettingsApplied(SettingsStore.Instance.Current);
        }

        // BGMManager가 생성된 현재 씬의 BGM을 재생
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[BGMManager] Start() - Current scene: {currentScene}");
        currentSceneName = currentScene;
        PlayBGMForScene(currentScene);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (SettingsStore.Instance != null)
        {
            SettingsStore.Instance.OnApplied -= OnSettingsApplied;
        }
    }

    private void SetupAudioSource()
    {
        bgmAudioSource = GetComponent<AudioSource>();
        if (bgmAudioSource == null)
            bgmAudioSource = gameObject.AddComponent<AudioSource>();

        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.spatialBlend = 0f; // 2D 사운드
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = bgmVolume;

        Debug.Log("[BGMManager] AudioSource setup complete");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        Debug.Log($"[BGMManager] Scene loaded: {sceneName} (LoadMode: {mode})");
        Debug.Log($"[BGMManager] Previous scene: {currentSceneName}");

        // Additive 씬 로드는 무시 (UI나 기타 보조 씬들)
        if (mode == LoadSceneMode.Additive)
        {
            Debug.Log($"[BGMManager] Additive scene '{sceneName}' ignored for BGM");
            return;
        }

        // 같은 씬이면 BGM 변경하지 않음
        if (currentSceneName == sceneName)
        {
            Debug.Log($"[BGMManager] Same scene, no BGM change needed");
            return;
        }

        Debug.Log($"[BGMManager] Scene changed from '{currentSceneName}' to '{sceneName}'");
        currentSceneName = sceneName;
        PlayBGMForScene(sceneName);
    }

    private void PlayBGMForScene(string sceneName)
    {
        AudioClip targetBGM = null;

        Debug.Log($"[BGMManager] PlayBGMForScene called for: '{sceneName}'");
        Debug.Log($"[BGMManager] Available BGMs - mainSceneBGM: {(mainSceneBGM != null ? mainSceneBGM.name : "null")}, playSceneBGM: {(playSceneBGM != null ? playSceneBGM.name : "null")}");

        // 씬별 BGM 선택
        switch (sceneName)
        {
            case "PlayScene":
                // 플레이 씬만 별도 BGM 사용
                targetBGM = playSceneBGM;
                Debug.Log($"[BGMManager] PlayScene detected, using playSceneBGM: {(targetBGM != null ? targetBGM.name : "null")}");
                break;
            case "GreetingScene":
                // Greeting씬은 BGM 없음
                targetBGM = null;
                Debug.Log("[BGMManager] GreetingScene detected, no BGM");
                break;
            case "MenuScene":
                // MenuScene은 UI 전용 씬이므로 BGM 변경하지 않음
                Debug.Log("[BGMManager] MenuScene detected, ignoring BGM change (UI scene)");
                return;
            case "MainScene":
            default:
                // 메인씬, 로비, 매칭 등 모든 다른 씬들은 메인 BGM 사용
                targetBGM = mainSceneBGM;
                Debug.Log($"[BGMManager] MainScene or other scene detected, using mainSceneBGM: {(targetBGM != null ? targetBGM.name : "null")}");
                break;
        }

        if (targetBGM != null)
        {
            Debug.Log($"[BGMManager] Starting BGM: {targetBGM.name}");
            if (fadeTransition)
                StartFadeTransition(targetBGM);
            else
                PlayBGMImmediate(targetBGM);
        }
        else
        {
            Debug.Log("[BGMManager] No BGM to play, stopping current BGM");
            StopBGM();
        }
    }

    private void PlayBGMImmediate(AudioClip bgmClip)
    {
        Debug.Log($"[BGMManager] PlayBGMImmediate called with clip: {(bgmClip != null ? bgmClip.name : "null")}");
        Debug.Log($"[BGMManager] Current AudioSource clip: {(bgmAudioSource.clip != null ? bgmAudioSource.clip.name : "null")}, isPlaying: {bgmAudioSource.isPlaying}");

        if (bgmAudioSource.clip == bgmClip && bgmAudioSource.isPlaying)
        {
            Debug.Log("[BGMManager] Same BGM already playing, skipping");
            return; // 같은 BGM이 이미 재생 중
        }

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.Play();

        Debug.Log($"[BGMManager] Playing BGM: {bgmClip.name} for scene: {currentSceneName}");
        Debug.Log($"[BGMManager] AudioSource state after play - clip: {bgmAudioSource.clip.name}, isPlaying: {bgmAudioSource.isPlaying}, volume: {bgmAudioSource.volume}");
    }

    private void StartFadeTransition(AudioClip newBGM)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeTransitionCoroutine(newBGM));
    }

    private System.Collections.IEnumerator FadeTransitionCoroutine(AudioClip newBGM)
    {
        Debug.Log($"[BGMManager] Starting fade transition to: {(newBGM != null ? newBGM.name : "null")}");

        // 현재 BGM이 있으면 페이드 아웃
        if (bgmAudioSource.isPlaying)
        {
            Debug.Log($"[BGMManager] Fading out current BGM: {bgmAudioSource.clip.name}");
            float startVolume = bgmAudioSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
                yield return null;
            }
            bgmAudioSource.volume = 0f;
            bgmAudioSource.Stop();
            Debug.Log("[BGMManager] Fade out complete");
        }

        // 새 BGM이 있으면 페이드 인
        if (newBGM != null)
        {
            Debug.Log($"[BGMManager] Fading in new BGM: {newBGM.name}");
            bgmAudioSource.clip = newBGM;
            bgmAudioSource.volume = 0f;
            bgmAudioSource.Play();

            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                bgmAudioSource.volume = Mathf.Lerp(0f, bgmVolume, t / fadeTime);
                yield return null;
            }
            bgmAudioSource.volume = bgmVolume;

            Debug.Log($"[BGMManager] Fade transition complete: {newBGM.name} for scene: {currentSceneName}");
        }

        fadeCoroutine = null;
    }

    // 공개 메서드들
    public void PlayBGM(AudioClip bgmClip)
    {
        if (fadeTransition)
            StartFadeTransition(bgmClip);
        else
            PlayBGMImmediate(bgmClip);
    }

    public void StopBGM()
    {
        if (fadeTransition && bgmAudioSource.isPlaying)
        {
            StartFadeTransition(null);
        }
        else
        {
            bgmAudioSource.Stop();
            Debug.Log("[BGMManager] BGM stopped");
        }
    }

    public void PauseBGM()
    {
        if (bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Pause();
            Debug.Log("[BGMManager] BGM paused");
        }
    }

    public void ResumeBGM()
    {
        if (!bgmAudioSource.isPlaying && bgmAudioSource.clip != null)
        {
            bgmAudioSource.UnPause();
            Debug.Log("[BGMManager] BGM resumed");
        }
    }

    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null && fadeCoroutine == null)
        {
            bgmAudioSource.volume = bgmVolume;
            Debug.Log($"[BGMManager] Manual volume set to: {bgmVolume:F2}");
        }
    }

    public void SetFadeTransition(bool enabled)
    {
        fadeTransition = enabled;
    }

    public void SetFadeTime(float time)
    {
        fadeTime = Mathf.Max(0.1f, time);
    }

    // 설정 적용 메서드
    private void OnSettingsApplied(UserSettings settings)
    {
        if (settings == null) return;

        // UserSettings의 볼륨은 0~10 범위이므로 0~1로 변환
        float masterVol = Mathf.Clamp01(settings.masterVolume / 10f);
        float bgmVol = Mathf.Clamp01(settings.bgmVolume / 10f);

        // BGM 볼륨 = 마스터 볼륨 * BGM 볼륨
        float newVolume = masterVol * bgmVol;

        Debug.Log($"[BGMManager] Settings applied - Master: {settings.masterVolume:F2}({masterVol:F2}), BGM: {settings.bgmVolume:F2}({bgmVol:F2}), Final: {newVolume:F2}");

        // bgmVolume 필드 업데이트
        bgmVolume = newVolume;

        // 현재 재생 중인 오디오에 즉시 적용
        if (bgmAudioSource != null && fadeCoroutine == null)
        {
            bgmAudioSource.volume = bgmVolume;
            Debug.Log($"[BGMManager] Volume applied to AudioSource: {bgmAudioSource.volume:F2}");
        }
    }
}