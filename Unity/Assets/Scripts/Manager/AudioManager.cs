using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
        audioSource.volume = 1f;
    }
    public void OnClickPlay(AudioClip audioClip)
    {
        audioSource.PlayOneShot(audioClip);
    }

    public void OnPointerDown(AudioClip audioClip)
    {
        audioSource.PlayOneShot(audioClip);
    }
}
