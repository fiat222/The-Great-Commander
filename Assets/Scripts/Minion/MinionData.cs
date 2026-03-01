using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// SO สำหรับ Minion แต่ละชนิด
/// อัพเกรดแยกต่อชนิด → Minion ชนิดนั้นทุกตัวในซีนได้รับ stat ใหม่ทันที
/// ใช้ Orb เป็นสกุลเงิน
///
/// ราคา Upgrade = cost × (costEscalation ^ currentLevel)
/// ตัวอย่าง Balancer (cost=20, escalation=1.5):
///   0→1=20 Orb | 1→2=30 | 2→3=45 | 3→4=68 | 4→5=101
/// </summary>
[CreateAssetMenu(fileName = "NewMinion", menuName = "Game/Minion Data")]
public class MinionData : ScriptableObject
{
    [Header("Identity")]
    public string minionName;
    public GameObject prefab;

    [FormerlySerializedAs("icon")]
    public Sprite picture;
    public Sprite icon;

    [Header("VFX")]
    [Tooltip("Particle/Effect ที่เล่นตอนโจมตี (เช่น ฝุ่นกระทืบเท้า)")]
    public GameObject attackVFX;

    [Header("Base Stats (Level 0)")]
    public int cost;
    public float damage;
    public float defense;
    public float speed;
    public float hp;
    public float attackrange;

    [Header("Upgrade Cost")]
    [Tooltip("ราคาอัพแพงขึ้นเท่าไหร่ต่อขั้น (แนะนำ 1.5)")]
    [Range(1f, 3f)] public float costEscalation = 1.5f;

    [Header("Upgrade Multipliers")]
    [Range(1f, 2f)] public float hpMultiplier = 1.30f;
    [Range(1f, 2f)] public float attackMultiplier = 1.20f;
    [Range(1f, 2f)] public float defenseMultiplier = 1.30f;
    // Speed คงที่ตามดีไซน์

    public int maxLevel = 5;

    // ==================== Runtime ====================
    [System.NonSerialized] private int _level = 0;
    public int CurrentLevel => _level;
    public bool IsMaxLevel => _level >= maxLevel;

    // ==================== Computed Stats ====================
    public float GetHP() => hp * Mathf.Pow(hpMultiplier, _level);
    public float GetDamage() => damage * Mathf.Pow(attackMultiplier, _level);
    public float GetDefense() => defense * Mathf.Pow(defenseMultiplier, _level);
    public float GetSpeed() => speed;

    /// <summary>ราคา Orb สำหรับอัพเกรดขั้นถัดไป</summary>
    public int GetUpgradeCost()
    {
        if (IsMaxLevel) return 0;
        return Mathf.RoundToInt(cost * Mathf.Pow(costEscalation, _level));
    }

    // ==================== Upgrade ====================
    /// <summary>
    /// อัพเกรด 1 ขั้น — fire OnMinionUpgraded เพื่อแจ้ง MinionAI ทุกตัวที่ใช้ SO นี้
    /// ตัวเรียกต้องหักเงิน Orb เองก่อนครับ
    /// </summary>
    public bool Upgrade()
    {
        if (IsMaxLevel) return false;
        _level++;
        OnMinionUpgraded?.Invoke(this);
        Debug.Log($"[MinionData:{minionName}] Lv{_level} | HP:{GetHP():F0} Atk:{GetDamage():F1} Def:{GetDefense():F1}");
        return true;
    }

    /// <summary>Event แจ้ง MinionAI ทุกตัวที่ใช้ SO นี้ให้ refresh stat ทันที</summary>
    public static event System.Action<MinionData> OnMinionUpgraded;

    public void SetLevel(int lv) => _level = Mathf.Clamp(lv, 0, maxLevel);
    public void ResetLevel() => _level = 0;

#if UNITY_EDITOR
    [ContextMenu("Preview All Levels")]
    private void Preview()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {minionName} ===");
        sb.AppendLine($"{"Lv",-4}{"HP",-8}{"Atk",-8}{"Def",-8}{"Spd",-6}{"Cost",-8}");
        sb.AppendLine(new string('-', 42));
        for (int i = 0; i <= maxLevel; i++)
        {
            float h = hp * Mathf.Pow(hpMultiplier, i);
            float a = damage * Mathf.Pow(attackMultiplier, i);
            float d = defense * Mathf.Pow(defenseMultiplier, i);
            int c = i < maxLevel ? Mathf.RoundToInt(cost * Mathf.Pow(costEscalation, i)) : 0;
            sb.AppendLine($"{i,-4}{h,-8:F0}{a,-8:F1}{d,-8:F1}{speed,-6:F1}{(i < maxLevel ? c.ToString() : "MAX"),-8}");
        }
        Debug.Log(sb.ToString());
    }
#endif
}