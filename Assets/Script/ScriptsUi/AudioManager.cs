using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField]
    private AudioMixer audioMixer;

    [SerializeField]
    private AudioSource bgmSource;

    [SerializeField]
    private AudioSource seSource;

    private const string BGM_KEY = "BGMVolume";
    private const string SE_KEY = "SEVolume";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (SaveManager.Instance == null)
            return;
        SaveData data = SaveManager.Instance.CurrentData;
        SetBGMVolume(data.bgmVolume);
        SetSEVolume(data.seVolume);
    }

    public void SetBGMVolume(float value)
    {
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        audioMixer.SetFloat(BGM_KEY, db);
        SaveManager.Instance.CurrentData.bgmVolume = value;
    }

    public void SetSEVolume(float value)
    {
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        audioMixer.SetFloat(SE_KEY, db);
        SaveManager.Instance.CurrentData.seVolume = value;
    }

    public void PlaySE(AudioClip clip)
    {
        seSource.PlayOneShot(clip);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;
        bgmSource.clip = clip;
        bgmSource.Play();
        bgmSource.loop = true;
    }
}
