using System.Collections;
using UnityEngine;

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
    public AudioClip planningBGM;
    public AudioClip combatBGM;

    [Header("BGM Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Min(0.1f)]     public float crossfadeDuration = 1.5f;

    // Properties สำหรับ AudioSettingsUI อ่านค่าตั้งต้น
    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    [Header("SFX 2D (Global)")]
    [Tooltip("AudioSource สำหรับเสียง 2D เช่น UI, Pickup")]
    [SerializeField] private AudioSource sfxSource;

    // ─────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────

    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;

    private AudioSource activeBGMSource;   // ตัวที่กำลังเล่นอยู่
    private Coroutine   crossfadeCoroutine;

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

        // ตั้งค่า BGM Sources
        SetupBGMSource(bgmSourceA);
        SetupBGMSource(bgmSourceB);

        activeBGMSource = bgmSourceA;
    }

    void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
    }

    void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
    }

    void Start()
    {
        // เล่น BGM เริ่มต้น (Planning Phase)
        PlayBGMImmediate(planningBGM);
    }

    // ─────────────────────────────────────────
    //  Phase Handling
    // ─────────────────────────────────────────

    private void HandlePhaseChanged(GamePhase phase)
    {
        AudioClip nextClip = phase == GamePhase.Planning ? planningBGM : combatBGM;
        CrossfadeTo(nextClip);
    }

    // ─────────────────────────────────────────
    //  BGM Control
    // ─────────────────────────────────────────

    /// <summary>เปลี่ยน BGM แบบ Crossfade (วิธีหลัก)</summary>
    public void CrossfadeTo(AudioClip newClip)
    {
        if (newClip == null) return;
        if (activeBGMSource.clip == newClip && activeBGMSource.isPlaying) return; // ไม่เล่นซ้ำ

        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        crossfadeCoroutine = StartCoroutine(DoCrossfade(newClip));
    }

    /// <summary>เล่น BGM ทันทีโดยไม่ Fade (ใช้ตอน Start)</summary>
    private void PlayBGMImmediate(AudioClip clip)
    {
        if (clip == null) return;
        activeBGMSource.clip   = clip;
        activeBGMSource.volume = bgmVolume;
        activeBGMSource.Play();
    }

    private IEnumerator DoCrossfade(AudioClip newClip)
    {
        // เลือก Source ที่ไม่ Active มาเป็นตัวใหม่
        AudioSource incoming = activeBGMSource == bgmSourceA ? bgmSourceB : bgmSourceA;

        incoming.clip   = newClip;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed = 0f;
        float startVolume = activeBGMSource.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / crossfadeDuration;

            activeBGMSource.volume = Mathf.Lerp(startVolume, 0f,         t);
            incoming.volume        = Mathf.Lerp(0f,          bgmVolume,  t);

            yield return null;
        }

        activeBGMSource.Stop();
        activeBGMSource.volume = bgmVolume;

        activeBGMSource = incoming;
        crossfadeCoroutine = null;
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

    // ─────────────────────────────────────────
    //  Volume Control (เรียกจาก AudioSettingsUI)
    // ─────────────────────────────────────────

    /// <summary>ปรับ Volume ของ BGM และ Source ที่กำลังเล่นทันที</summary>
    public void SetBGMVolume(float v)
    {
        bgmVolume = v;
        if (activeBGMSource != null) activeBGMSource.volume = v;
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
        src.spatialBlend = 0f;  // 2D เสียง BGM ได้ยินเหมือนกันทุกจุด
        src.playOnAwake  = false;
        src.volume       = bgmVolume;
    }
}
