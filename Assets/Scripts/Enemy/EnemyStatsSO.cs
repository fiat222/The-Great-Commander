using UnityEngine;

/// <summary>
/// SO สำหรับ Enemy แต่ละชนิด
/// Stat สเกลอัตโนมัติตาม Wave — GameManager เรียก SetWave() ทุกครั้งที่เปลี่ยน Phase
///
/// สูตร Scaling: stat = baseStat × (waveMultiplier ^ (wave - 1))
/// Wave 1 = base stats เต็ม (×1.0)
///
/// ตัวอย่าง Goblin (hp=40, waveHp=1.15):
///   Wave1=40 | Wave2=46 | Wave3=53 | Wave4=61 | Wave5=70
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Stats SO")]
public class EnemyStatsSO : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public GameObject prefab;
    public Sprite icon;
    public int cost;
    public float orbDrop;
    public float attackRange;

    [Header("VFX")]
    [Tooltip("Particle/Effect ที่เล่นเมื่อ Enemy โดนตี")]
    public GameObject hitVFX;
    [Tooltip("Particle/Effect ที่เล่นเมื่อ Enemy ตาย")]
    public GameObject deathVFX;
    [Tooltip("Particle/Effect ที่เล่นเมื่อ Enemy ถูกลบออกจากแมพ")]
    public GameObject removalVFX;

    [Header("SFX — Attack")]
    [Tooltip("สุ่มเล่น 1 ตัวทุกครั้งที่โจมตี (เรียกจาก Animation Event)")]
    public AudioClip[] attackSounds;
    [Range(0f, 1f)] public float attackVolume = 0.8f;

    [Header("SFX — Roar")]
    [Tooltip("สุ่มเล่น 1 ตัวตอน Spawn")]
    public AudioClip[] roarSounds;
    [Range(0f, 1f)] public float roarVolume = 1f;
    [Tooltip("ติ๊กเพื่อให้เสียงคำรามวนลูปตลอดชีวิต หยุดอัตโนมัติเมื่อตาย")]
    public bool roarLoop = false;

    [Header("SFX — Death")]
    [Tooltip("สุ่มเล่น 1 ตัวตอนตาย")]
    public AudioClip[] deathSounds;
    [Range(0f, 1f)] public float deathVolume = 1f;

    [Header("SFX — 3D Audio Settings")]
    [Tooltip("ระยะที่ดังเต็ม 100%")]
    public float minHearDistance = 3f;
    [Tooltip("ระยะที่ไม่ได้ยินแล้ว")]
    public float maxHearDistance = 30f;

    [Header("Base Stats (Wave 1)")]
    public float baseDamage = 15f;
    public float baseDefense = 2.5f;
    public float baseSpeed = 5f;
    public float baseHP = 40f;

    [Header("Wave Scaling Multipliers (คูณต่อ Wave)")]
    [Range(1f, 2f)] public float waveHpMultiplier = 1.15f;
    [Range(1f, 2f)] public float waveAttackMultiplier = 1.12f;
    [Range(1f, 2f)] public float waveDefenseMultiplier = 1.10f;
    [Range(1f, 2f)] public float waveSpeedMultiplier = 1.05f;

    // ==================== Runtime ====================
    [System.NonSerialized] private int _wave = 1;
    public int CurrentWave => _wave;

    // ==================== Computed Stats ====================
    public float GetHP() => baseHP * Mathf.Pow(waveHpMultiplier, _wave - 1);
    public float GetDamage() => baseDamage * Mathf.Pow(waveAttackMultiplier, _wave - 1);
    public float GetDefense() => baseDefense * Mathf.Pow(waveDefenseMultiplier, _wave - 1);
    public float GetSpeed() => baseSpeed * Mathf.Pow(waveSpeedMultiplier, _wave - 1);

    // ==================== Wave Control ====================

    /// <summary>
    /// เรียกจาก GameManager ทุกครั้งที่เปลี่ยน Wave
    /// จะ fire OnEnemyWaveScaled เพื่อให้ EnemyAI ที่อยู่ในซีน refresh stat ด้วย
    /// </summary>
    public void SetWave(int wave)
    {
        _wave = Mathf.Max(1, wave);
        OnEnemyWaveScaled?.Invoke(this);
        Debug.Log($"[EnemyStatsSO:{enemyName}] Wave{_wave} | HP:{GetHP():F0} Atk:{GetDamage():F1} Def:{GetDefense():F1} Spd:{GetSpeed():F2}");
    }

    /// <summary>Event แจ้ง EnemyAI ให้ refresh stat เมื่อขึ้น Wave ใหม่</summary>
    public static event System.Action<EnemyStatsSO> OnEnemyWaveScaled;

    public void ResetWave() => _wave = 1;

#if UNITY_EDITOR
    [ContextMenu("Preview Wave 1-10")]
    private void Preview()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {enemyName} ===");
        sb.AppendLine($"{"Wave",-6}{"HP",-8}{"Atk",-8}{"Def",-8}{"Spd",-8}");
        sb.AppendLine(new string('-', 38));
        for (int w = 1; w <= 10; w++)
        {
            float h = baseHP * Mathf.Pow(waveHpMultiplier, w - 1);
            float a = baseDamage * Mathf.Pow(waveAttackMultiplier, w - 1);
            float d = baseDefense * Mathf.Pow(waveDefenseMultiplier, w - 1);
            float s = baseSpeed * Mathf.Pow(waveSpeedMultiplier, w - 1);
            sb.AppendLine($"{w,-6}{h,-8:F0}{a,-8:F1}{d,-8:F1}{s,-8:F2}");
        }
        Debug.Log(sb.ToString());
    }
#endif
}