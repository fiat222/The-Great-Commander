using UnityEngine;

/// <summary>
/// Component ติดบน Enemy Prefab — ดึงข้อมูลเสียงจาก EnemyStatsSO ที่มีอยู่แล้ว
/// ไม่ต้อง drag อะไรเพิ่ม: อ้างอิง stats.attackSounds / roarSounds / deathSounds โดยตรง
/// เสียงเป็น 3D (ดังน้อยลงเมื่ออยู่ไกล) ตามค่า minHearDistance / maxHearDistance ใน SO
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CharacterAudio : MonoBehaviour
{
    private AudioSource sfxSource;   // สำหรับ Attack / Death (PlayOneShot)
    private AudioSource roarSource;  // สำหรับ Roar (รองรับ Loop)
    private EnemyStatsSO stats;

    private int lastAttackIndex = -1;
    private int lastRoarIndex   = -1;
    private int lastDeathIndex  = -1;

    void Awake()
    {
        // AudioSource แรก (auto-added โดย RequireComponent) → SFX ทั่วไป
        sfxSource = GetComponent<AudioSource>();

        // AudioSource ที่สอง → สำหรับ Roar โดยเฉพาะ (รองรับ Loop)
        roarSource = gameObject.AddComponent<AudioSource>();

        // ดึง stats จาก EnemyAI บน GameObject เดียวกัน
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) stats = ai.stats;

        // ตั้งค่า 3D Audio ให้ทั้งสอง Source
        ConfigureSource(sfxSource,  loop: false);
        ConfigureSource(roarSource, loop: false); // จะเซ็ต loop จริงตอน PlayRoar()
    }

    // ─────────────────────────────────────────
    //  Public Methods
    // ─────────────────────────────────────────

    /// <summary>เรียกจาก Animation Event ตอน Frame ที่อาวุธปะทะ</summary>
    public void PlayAttack()
    {
        if (stats == null || stats.attackSounds == null || stats.attackSounds.Length == 0) return;
        AudioClip clip = PickRandom(stats.attackSounds, ref lastAttackIndex);
        sfxSource.PlayOneShot(clip, stats.attackVolume);
    }

    /// <summary>เรียกจาก EnemyAI.Start() ตอน Spawn</summary>
    public void PlayRoar()
    {
        if (stats == null || stats.roarSounds == null || stats.roarSounds.Length == 0) return;
        AudioClip clip = PickRandom(stats.roarSounds, ref lastRoarIndex);

        if (stats.roarLoop)
        {
            // วนลูปตลอดชีวิต หยุดอัตโนมัติตอน Die
            roarSource.clip   = clip;
            roarSource.volume = stats.roarVolume;
            roarSource.loop   = true;
            roarSource.Play();
        }
        else
        {
            // เล่นครั้งเดียว
            sfxSource.PlayOneShot(clip, stats.roarVolume);
        }
    }

    /// <summary>เรียกจาก EnemyAI.Die()</summary>
    public void PlayDeath()
    {
        // หยุด Roar Loop (ถ้ามี)
        if (roarSource.isPlaying)
        {
            roarSource.loop = false;
            roarSource.Stop();
        }

        if (stats == null || stats.deathSounds == null || stats.deathSounds.Length == 0) return;
        AudioClip clip = PickRandom(stats.deathSounds, ref lastDeathIndex);
        sfxSource.PlayOneShot(clip, stats.deathVolume);
    }

    // ─────────────────────────────────────────
    //  Utility
    // ─────────────────────────────────────────

    private void ConfigureSource(AudioSource src, bool loop)
    {
        src.spatialBlend = 1f;
        src.rolloffMode  = AudioRolloffMode.Linear;
        src.loop         = loop;
        src.playOnAwake  = false;

        if (stats != null)
        {
            src.minDistance = stats.minHearDistance;
            src.maxDistance = stats.maxHearDistance;
        }
    }

    /// <summary>สุ่มโดยไม่ซ้ำตัวเดิมติดกัน</summary>
    private AudioClip PickRandom(AudioClip[] clips, ref int lastIndex)
    {
        if (clips.Length == 1) return clips[0];

        int index;
        do { index = Random.Range(0, clips.Length); }
        while (index == lastIndex);

        lastIndex = index;
        return clips[index];
    }
}
