using UnityEngine;

/// <summary>
/// SO สำหรับ Player
/// กดอัพ 1 ครั้ง = ทุก stat เพิ่มพร้อมกันเลย ใช้ Orb เป็นสกุลเงิน
///
/// Multiplier ต่อขั้น:
///   HP       × 1.30
///   Speed    × 1.15
///   Defense  × 1.30
///   Damage   × 1.20
///
/// ราคา Upgrade = baseUpgradeCost × (costEscalation ^ currentLevel)
/// ตัวอย่าง baseUpgradeCost=20, escalation=1.5:
///   0→1=20 | 1→2=30 | 2→3=45 | 3→4=68 | 4→5=101
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Player Stats SO")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Character Info")]
    public string characterName = "Character";

    [Header("Icons")]
    public Sprite icon; // ไอคอนหลักของ Player (ใช้ใน UpgradePlayerCard)
    [Header("Skill Icons")]
    [Tooltip("Icon สำหรับ Skill — ใช้กับ Image ที่ Tag 'SkillIcon'")]
    public Sprite skillIcon;
    [Tooltip("Icon สำหรับ Normal Attack — ใช้กับ Image ที่ Tag 'SkillNormal'")]
    public Sprite normalAttackIcon;
    [Tooltip("Icon สำหรับ Special Attack — ใช้กับ Image ที่ Tag 'SkillSpecial'")]
    public Sprite specialAttackIcon;

    [Header("Base Stats (Level 0)")]
    public int baseHP = 100;
    public float baseSpeed = 5f;
    public float baseDefense = 5f;
    public float baseDamage = 15f;

    [Header("Upgrade Cost")]
    [Tooltip("ต้นทุนฐาน Orb สำหรับอัพเกรดครั้งแรก")]
    public int baseUpgradeCost = 20;

    [Header("Upgrade Multipliers")]
    [Range(1f, 2f)] public float hpMultiplier = 1.30f;
    [Range(1f, 2f)] public float speedMultiplier = 1.15f;
    [Range(1f, 2f)] public float defenseMultiplier = 1.30f;
    [Range(1f, 2f)] public float damageMultiplier = 1.20f;

    [Header("Cost Escalation")]
    [Range(1f, 3f)] public float costEscalation = 1.5f;

    public int maxLevel = 5;

    // ==================== Runtime ====================
    [System.NonSerialized] private int _level = 0;
    public int CurrentLevel => _level;
    public bool IsMaxLevel => _level >= maxLevel;

    // ==================== Computed Stats ====================
    public int GetHP() => Mathf.RoundToInt(baseHP * Mathf.Pow(hpMultiplier, _level));
    public float GetSpeed() => baseSpeed * Mathf.Pow(speedMultiplier, _level);
    public float GetDefense() => baseDefense * Mathf.Pow(defenseMultiplier, _level);
    public float GetDamage() => baseDamage * Mathf.Pow(damageMultiplier, _level);

    /// <summary>ราคา Orb สำหรับอัพเกรดขั้นถัดไป</summary>
    public int GetUpgradeCost()
    {
        if (IsMaxLevel) return 0;
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(costEscalation, _level));
    }

    // ==================== Upgrade ====================
    /// <summary>อัพเกรด 1 ขั้น — คืน true ถ้าสำเร็จ (ตัวเรียกหักเงินเองก่อน)</summary>
    public bool Upgrade()
    {
        if (IsMaxLevel) return false;
        _level++;
        Debug.Log($"[PlayerStatsSO] Lv{_level} | HP:{GetHP()} Spd:{GetSpeed():F2} Def:{GetDefense():F2} Dmg:{GetDamage():F2}");
        return true;
    }

    public void SetLevel(int lv) => _level = Mathf.Clamp(lv, 0, maxLevel);
    public void ResetLevel() => _level = 0;

#if UNITY_EDITOR
    [ContextMenu("Preview All Levels")]
    private void Preview()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {name} ===");
        sb.AppendLine($"{"Lv",-4}{"HP",-8}{"Spd",-8}{"Def",-8}{"Dmg",-8}{"Cost",-8}");
        sb.AppendLine(new string('-', 44));
        for (int i = 0; i <= maxLevel; i++)
        {
            int hp = Mathf.RoundToInt(baseHP * Mathf.Pow(hpMultiplier, i));
            float spd = baseSpeed * Mathf.Pow(speedMultiplier, i);
            float def = baseDefense * Mathf.Pow(defenseMultiplier, i);
            float dmg = baseDamage * Mathf.Pow(damageMultiplier, i);
            int cost = i < maxLevel ? Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(costEscalation, i)) : 0;
            sb.AppendLine($"{i,-4}{hp,-8}{spd,-8:F2}{def,-8:F2}{dmg,-8:F2}{(i < maxLevel ? cost.ToString() : "MAX"),-8}");
        }
        Debug.Log(sb.ToString());
    }
#endif
}