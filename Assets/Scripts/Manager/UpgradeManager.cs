using UnityEngine;

/// <summary>
/// UpgradeManager — จัดการระบบ Upgrade
/// ใช้ PlacementManager.Money เป็นสกุลเงินเดียว (Orb = Money)
/// ไม่มีระบบเงินของตัวเอง
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Player Stats SOs")]
    [Tooltip("SO ของ Warrior (PlayerController)")]
    public PlayerStatsSO warriorStats;
    [Tooltip("SO ของ Archer")]
    public PlayerStatsSO archerStats;

    [Header("Minion Stats SOs")]
    [Tooltip("ลาก MinionData SO ทุกชนิดมาใส่ตามลำดับ")]
    public MinionData[] minionStats;

    // ==================== Shortcut ====================
    private int CurrentMoney =>
        PlacementManager.Instance != null ? PlacementManager.Instance.Money : 0;

    private void SpendMoney(int amount)
    {
        if (PlacementManager.Instance == null) return;

        PlacementManager.Instance.Money -= amount;
        PlacementManager.Instance.OnMoneyChanged?.Invoke(PlacementManager.Instance.Money);
    }

    // ==================== Lifecycle ====================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==================== Player Upgrade ====================

    /// <summary>
    /// กดปุ่มเดียว → อัพทั้ง Warrior และ Archer พร้อมกัน
    /// หักเงินจาก PlacementManager.Money ครั้งเดียว
    /// </summary>
    public void UpgradePlayer()
    {
        PlayerStatsSO reference = warriorStats != null ? warriorStats : archerStats;
        if (reference == null)
        {
            Debug.LogWarning("[UpgradeManager] ไม่มี PlayerStatsSO!");
            return;
        }

        if (reference.IsMaxLevel)
        {
            Debug.Log("[UpgradeManager] Player อยู่ระดับสูงสุดแล้ว!");
            return;
        }

        int cost = reference.GetUpgradeCost();
        if (CurrentMoney < cost)
        {
            Debug.LogWarning($"[UpgradeManager] เงินไม่พอ! ต้องการ {cost} มีแค่ {CurrentMoney}");
            return;
        }

        // หักเงินครั้งเดียว แล้วอัพทั้งสอง SO
        SpendMoney(cost);
        warriorStats?.Upgrade();
        archerStats?.Upgrade();

        // แจ้ง PlayerController และ Archer ทุกตัวในซีนให้ refresh stat
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            pc.ApplyStats();

        foreach (var ac in FindObjectsByType<Archer>(FindObjectsSortMode.None))
            ac.ApplyStats();

        Debug.Log($"[UpgradeManager] Player Upgraded! Warrior Lv{warriorStats?.CurrentLevel} / Archer Lv{archerStats?.CurrentLevel} | เงินเหลือ: {CurrentMoney}");
    }

    // ==================== Minion Upgrade ====================

    /// <summary>
    /// อัพ Minion ชนิดที่ระบุ index
    /// MinionData.OnMinionUpgraded จะ fire → MinionAI refresh ทุกตัวเอง
    /// </summary>
    public void UpgradeMinion(int minionIndex)
    {
        if (minionStats == null || minionIndex >= minionStats.Length) return;
        MinionData data = minionStats[minionIndex];
        if (data == null) return;

        if (data.IsMaxLevel)
        {
            Debug.Log($"[UpgradeManager] {data.minionName} อยู่ระดับสูงสุดแล้ว!");
            return;
        }

        int cost = data.GetUpgradeCost();
        if (CurrentMoney < cost)
        {
            Debug.LogWarning($"[UpgradeManager] เงินไม่พอ! {data.minionName} ต้องการ {cost} มีแค่ {CurrentMoney}");
            return;
        }

        SpendMoney(cost);
        data.Upgrade(); // → fire OnMinionUpgraded → MinionAI refresh

        Debug.Log($"[UpgradeManager] {data.minionName} Upgraded! Lv{data.CurrentLevel} | เงินเหลือ: {CurrentMoney}");
    }

    /// <summary>
    /// Overload รับ MinionData โดยตรง (UI ส่ง SO มาตรงๆ)
    /// </summary>
    public void UpgradeMinion(MinionData data)
    {
        if (data == null || minionStats == null) return;

        for (int i = 0; i < minionStats.Length; i++)
        {
            if (minionStats[i] == data)
            {
                UpgradeMinion(i);
                return;
            }
        }
    }
}