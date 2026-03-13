using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio; // ✅ จำเป็นต้องมีเพื่อควบคุม Audio Mixer

/// <summary>
/// จัดการระบบเสียงทั้งหมดของเกม (BGM, SFX 2D, และการ Automation เชื่อมต่อ AudioSource)
/// รองรับการคุมระดับเสียงผ่าน Audio Mixer และระบบ Save/Load ผ่าน PlayerPrefs
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Control")]
    [Tooltip("ลากไฟล์ MainMixer มาใส่ที่นี่")]
    public AudioMixer mainMixer;
    [Tooltip("ลาก Mixer Group ชื่อ SFX มาใส่ที่นี่")]
    public AudioMixerGroup sfxMixerGroup;

    [Header("BGM Clips")]
    public AudioClip mainMenuBGM;
    public AudioClip planningBGM;
    public AudioClip combatBGM;

    [Header("Win / Lose SFX")]
    public AudioClip winClip;
    public AudioClip loseClip;

    [Header("BGM Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // Properties สำหรับให้ UI มาดึงค่าไปตั้งต้นที่ Slider
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

    // ─────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────

    void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 1. โหลดค่าระดับเสียงที่บันทึกไว้
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // 2. ตั้งค่าเบื้องต้นให้ Source
        SetupBGMSource(bgmSource);
        InitSfxDict();
    }

    void OnEnable()
    {
        // Subscribe เหตุการณ์ต่างๆ
        GameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // ✅ Automation: ทุกครั้งที่เปลี่ยน Scene ให้วิ่งหา AudioSource ที่ยังไม่ได้ต่อ Mixer
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
        // บังคับใช้ระดับเสียงที่โหลดมาเข้าสู่ Mixer ทันทีที่เริ่มเกม
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);

        PlayInitialBGM();
    }

    // ─────────────────────────────────────────
    //  Scene & Automation Handling
    // ─────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // จัดการเพลงตามชื่อ Scene
        if (scene.name == "MenuSceneTest" || scene.name == "CharacterSelectScene")
            PlayBGM(mainMenuBGM);
        else if (scene.name == "GameScene" || scene.name == "SoloGameScene")
            PlayBGM(planningBGM);
    }

    /// <summary>
    /// วิ่งกวาด AudioSource ทั้งหมดใน Scene (รวมถึงใน Player/Enemy) 
    /// เพื่อเชื่อมต่อสายสัญญาณเข้า SFX Group อัตโนมัติ
    /// </summary>
    private void AutoAssignSFXGroups(Scene scene, LoadSceneMode mode)
    {
        if (sfxMixerGroup == null) return;

        // หา AudioSource ทั้งหมดใน Scene (รวมตัวที่ปิดอยู่ด้วย)
        AudioSource[] allSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var source in allSources)
        {
            // ถ้ายังไม่ได้ต่อ Mixer และไม่ใช่ตัวหลักของ AudioManager
            if (source.outputAudioMixerGroup == null && source != bgmSource && source != sfxSource)
            {
                source.outputAudioMixerGroup = sfxMixerGroup;
            }
        }
    }

    private void PlayInitialBGM()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MenuSceneTest" || currentScene == "CharacterSelectScene") PlayBGM(mainMenuBGM);
        else if (currentScene == "GameScene" || currentScene == "SoloGameScene") PlayBGM(planningBGM);
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
    //  Public Controls (BGM & SFX)
    // ─────────────────────────────────────────

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;

        // กันเล่นซ้ำถ้าเป็นเพลงเดิมที่กำลังเล่นอยู่
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

    // ─────────────────────────────────────────
    //  Volume Control (Mixer Integration)
    // ─────────────────────────────────────────

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

    /// <summary>
    /// แปลงค่า Linear (0-1) เป็น Decibel (-80 ถึง 0) เพื่อใช้กับ Mixer
    /// </summary>
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
        // ต้องแน่ใจว่า bgmSource ต่อกับ BGM Mixer Group ใน Inspector ด้วยนะครับ
    }
}