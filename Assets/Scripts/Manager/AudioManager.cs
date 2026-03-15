using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

/// <summary>
/// จัดการระบบเสียงทั้งหมดของเกม (BGM, SFX 2D, และการ Automation เชื่อมต่อ AudioSource)
/// รองรับการคุมระดับเสียงผ่าน Audio Mixer และระบบ Save/Load ผ่าน PlayerPrefs
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Control")]
    public AudioMixer mainMixer;
    public AudioMixerGroup sfxMixerGroup;

    [Header("BGM Clips")]
    [Tooltip("เพลงสำหรับหน้า Menu หลัก")]
    public AudioClip mainMenuBGM;
    [Tooltip("เพลงสำหรับหน้าเลือกตัวละคร (Solo และ Multi)")]
    public AudioClip characterSelectBGM; // ✅ เพิ่มตัวแปรสำหรับหน้านี้โดยเฉพาะ
    public AudioClip planningBGM;
    public AudioClip combatBGM;

    [Header("Win / Lose SFX")]
    public AudioClip winClip;
    public AudioClip loseClip;

    [Header("BGM Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public enum SoundType { None, Click, Buy, Place, Upgrade }

    [System.Serializable]
    public class SoundEntry
    {
        public SoundType soundType;
        public AudioClip clip;
    }

    [Header("SFX Library")]
    public SoundEntry[] sfxLibrary;
    private Dictionary<SoundType, AudioClip> sfxDict = new Dictionary<SoundType, AudioClip>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        SetupBGMSource(bgmSource);
        InitSfxDict();
    }

    void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneLoaded += AutoAssignSFXGroups;
    }

    void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded -= AutoAssignSFXGroups;
    }

    void Start()
    {
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
        PlayInitialBGM();
    }

    // ─────────────────────────────────────────
    //  Scene & Automation Handling
    // ─────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ✅ ตรวจสอบชื่อ Scene เพื่อเล่น BGM ที่ถูกต้อง
        if (scene.name == "MenuSceneTest")
        {
            PlayBGM(mainMenuBGM);
        }
        else if (scene.name == "CharacterSelectScene" || scene.name == "SoloCharactor")
        {
            PlayBGM(characterSelectBGM); // ✅ เล่นเพลงเลือกตัวละคร
        }
        else if (scene.name == "GameScene" || scene.name == "SoloGameScene")
        {
            PlayBGM(planningBGM);
        }
    }

    private void AutoAssignSFXGroups(Scene scene, LoadSceneMode mode)
    {
        if (sfxMixerGroup == null) return;

        AudioSource[] allSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in allSources)
        {
            if (source.outputAudioMixerGroup == null && source != bgmSource && source != sfxSource)
            {
                source.outputAudioMixerGroup = sfxMixerGroup;
            }
        }
    }

    private void PlayInitialBGM()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "MenuSceneTest")
            PlayBGM(mainMenuBGM);
        else if (currentScene == "CharacterSelectScene" || currentScene == "SoloCharactor")
            PlayBGM(characterSelectBGM); // ✅ รองรับตอนเริ่มเกม
        else if (currentScene == "GameScene" || currentScene == "SoloGameScene")
            PlayBGM(planningBGM);
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        AudioClip nextClip = phase == GamePhase.Planning ? planningBGM : combatBGM;
        PlayBGM(nextClip);
    }

    private void InitSfxDict()
    {
        sfxDict.Clear();
        foreach (var entry in sfxLibrary)
        {
            if (entry.clip != null && !sfxDict.ContainsKey(entry.soundType))
                sfxDict.Add(entry.soundType, entry.clip);
        }
    }

    // ─────────────────────────────────────────
    //  Public Controls
    // ─────────────────────────────────────────

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.isPlaying && bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlaySound(SoundType type, float volume = 1f)
    {
        if (sfxDict.TryGetValue(type, out AudioClip clip))
            PlaySFX2D(clip, volume);
    }

    public void PlayWin() { if (winClip != null) PlaySFX2D(winClip, sfxVolume); }
    public void PlayLose() { if (loseClip != null) PlaySFX2D(loseClip, sfxVolume); }

    public void SetBGMVolume(float v)
    {
        bgmVolume = v;
        if (mainMixer != null)
            mainMixer.SetFloat("BGMVol", LinearToDecibel(v));
        PlayerPrefs.SetFloat("BGMVolume", v);
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
        if (mainMixer != null)
            mainMixer.SetFloat("SFXVol", LinearToDecibel(v));
        PlayerPrefs.SetFloat("SFXVolume", v);
    }

    private float LinearToDecibel(float linear)
    {
        return linear <= 0 ? -80f : Mathf.Log10(Mathf.Max(0.0001f, linear)) * 20f;
    }

    private void SetupBGMSource(AudioSource src)
    {
        if (src == null) return;
        src.loop = true;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
    }
}