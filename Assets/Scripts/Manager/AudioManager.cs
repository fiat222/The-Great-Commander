using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// จัดการ BGM ของทั้งเกม (Planning ↔ Combat) ด้วย Crossfade
/// ติด GameObject "AudioManager" ในซีน พร้อม AudioSource 2 ตัว
/// Subscribe OnPhaseChangedGlobal อัตโนมัติ — ไม่ต้องแตะ GameManager
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────

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

    // Properties สำหรับ AudioSettingsUI อ่านค่าตั้งต้น
    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    [Header("SFX 2D (Global)")]
    [Tooltip("AudioSource สำหรับเสียง 2D เช่น UI, Pickup")]
    [SerializeField] private AudioSource sfxSource;

    public enum SoundType 
    { 
        None,
        Click,
        Buy,
        Place,
        Upgrade
    }

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
    //  Private
    // ─────────────────────────────────────────

    [Header("BGM Source")]
    [Tooltip("ใส่ AudioSource ที่ใช้เล่น BGM (ตัวเดียวพอ)")]
    [SerializeField] private AudioSource bgmSource;

    // ─────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // BGM ไม่ขาดเมื่อ load scene

        // โหลดค่า Volume จาก PlayerPrefs (เพื่อให้เสียงที่เซฟไว้ทำงานตั้งแต่เปิดเกม)
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // ตั้งค่า BGM Source
        SetupBGMSource(bgmSource);

        // โหลด Library ลงมาพักไว้ให้ระบบดึงง่ายๆ
        foreach (var entry in sfxLibrary)
        {
            if (entry.clip != null)
            {
                if (!sfxDict.ContainsKey(entry.soundType))
                {
                    sfxDict.Add(entry.soundType, entry.clip);
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] มีเสียงประเภท '{entry.soundType}' ซ้ำกันในระบบ!");
                }
            }
        }
    }

    void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal     += HandlePhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal += HandlePhaseChanged; // ✅ Solo mode
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal     -= HandlePhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal -= HandlePhaseChanged; // ✅ Solo mode
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // จัดการเปิดเพลงสำหรับ Scene แรกสุดตอนเริ่มเกม
        string startScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[AudioManager] กำลังเริ่มเกมที่ Scene: {startScene} | BGM Source Assigned: {bgmSource != null}");

        if (startScene == "MenuSceneTest" || startScene == "CharacterSelectScene")
            PlayBGM(mainMenuBGM);
        else if (startScene == "GameScene")
            PlayBGM(planningBGM); // เริ่ม GameScene โหมด Planning
        else
            Debug.Log($"[AudioManager] ข้ามการเล่นเพลง เพราะชื่อ Scene '{startScene}' ไม่ตรงเงื่อนไข");
    }

    // ─────────────────────────────────────────
    //  Scene & Phase Handling
    // ─────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] โหลด Scene ใหม่: {scene.name}");
        if (scene.name == "MenuSceneTest" || scene.name == "CharacterSelectScene")
        {
            PlayBGM(mainMenuBGM);
        }
        else if (scene.name == "GameScene" || scene.name == "SoloGameScene")
        {
            PlayBGM(planningBGM); // เริ่ม Planning ก่อนเสมอ
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        AudioClip nextClip = phase == GamePhase.Planning ? planningBGM : combatBGM;
        PlayBGM(nextClip);
    }

    // ─────────────────────────────────────────
    //  BGM Control
    // ─────────────────────────────────────────

    /// <summary>เล่น BGM ทันที</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] เพลงที่จะเล่น (clip) เป็น null! ลืมใส่ใน Inspector หรือเปล่า?");
            return;
        }
        if (bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] bgmSource เป็น null! ลืมลาก AudioSource ใส่ช่อง bgmSource หรือเปล่า?");
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying) return; // ถ้าเล่นเพลงเดิมอยู่แล้ว ไม่ต้องเริ่มใหม่

        Debug.Log($"[AudioManager] เล่นเพลง: {clip.name} Volume={bgmVolume} Mute={bgmSource.mute}");
        bgmSource.clip   = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    // ─────────────────────────────────────────
    //  SFX 2D (Global — UI, Pickup ฯลฯ)
    // ─────────────────────────────────────────

    /// <summary>เล่นเสียง 2D SFX แบบ One Shot (เสียง UI, pickup ฯลฯ)</summary>
    public void PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>เล่นเสียงโดยเรียกผ่าน Enum SoundType</summary>
    public void PlaySound(SoundType type, float volume = 1f)
    {
        if (sfxDict.TryGetValue(type, out AudioClip clip))
        {
            PlaySFX2D(clip, volume);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] หาไฟล์เสียงประเภท '{type}' ไม่เจอ! ตรวจสอบการตั้งค่าใน AudioManager ด้วย");
        }
    }

    /// <summary>เล่นเสียงชนะ</summary>
    public void PlayWin()  { if (winClip  != null) PlaySFX2D(winClip,  sfxVolume); }
    /// <summary>เล่นเสียงแพ้</summary>
    public void PlayLose() { if (loseClip != null) PlaySFX2D(loseClip, sfxVolume); }

    // ─────────────────────────────────────────
    //  Volume Control (เรียกจาก AudioSettingsUI)
    // ─────────────────────────────────────────

    /// <summary>ปรับ Volume ของ BGM และ Source ที่กำลังเล่นทันที</summary>
    public void SetBGMVolume(float v)
    {
        bgmVolume = v;
        if (bgmSource != null) bgmSource.volume = v;
    }

    /// <summary>ปรับ Volume ของ SFX Source ทันที</summary>
    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
        if (sfxSource != null) sfxSource.volume = v;
    }

    // ─────────────────────────────────────────
    //  Utility
    // ─────────────────────────────────────────

    private void SetupBGMSource(AudioSource src)
    {
        if (src == null) return;
        src.loop         = true;
        src.spatialBlend = 0f;
        src.playOnAwake  = false;
        src.volume       = bgmVolume;
    }
}
