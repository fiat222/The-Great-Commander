using UnityEngine;

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

    /// <summary>อัพตาม StatsSO ที่ส่งมาตรงๆ — รองรับทั้ง Solo และ Duo</summary>
    public void UpgradePlayerByStats(PlayerStatsSO stats)
    {
        if (stats == null)
        {
            Debug.LogWarning("[UpgradeManager] ไม่มี stats!");
            return;
        }

        if (stats.IsMaxLevel)
        {
            Debug.Log("[UpgradeManager] Player อยู่ระดับสูงสุดแล้ว!");
            return;
        }

        int cost = stats.GetUpgradeCost();
        if (CurrentMoney < cost)
        {
            Debug.LogWarning($"[UpgradeManager] เงินไม่พอ! ต้องการ {cost} มีแค่ {CurrentMoney}");
            return;
        }

        SpendMoney(cost);
        stats.Upgrade();

        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            pc.ApplyStats();

        foreach (var ac in FindObjectsByType<Archer>(FindObjectsSortMode.None))
            ac.ApplyStats();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(AudioManager.SoundType.Upgrade);

        // ⭐ Heal เต็มหลอดหลัง Upgrade
        HealLocalPlayer();

        Debug.Log($"[UpgradeManager] Player Upgraded! Lv{stats.CurrentLevel} | เงินเหลือ: {CurrentMoney}");
    }

    /// <summary>Legacy — ยังเผื่อไว้ถ้ามีที่เรียกใช้</summary>
    public void UpgradePlayer()
    {
        UpgradeWarrior();
        UpgradeArcher();
    }

    public void UpgradeWarrior()
    {
        if (warriorStats == null)
        {
            Debug.LogWarning("[UpgradeManager] ไม่มี warriorStats!");
            return;
        }

        if (warriorStats.IsMaxLevel)
        {
            Debug.Log("[UpgradeManager] Warrior อยู่ระดับสูงสุดแล้ว!");
            return;
        }

        int cost = warriorStats.GetUpgradeCost();
        if (CurrentMoney < cost)
        {
            Debug.LogWarning($"[UpgradeManager] เงินไม่พอ! Warrior ต้องการ {cost} มีแค่ {CurrentMoney}");
            return;
        }

        SpendMoney(cost);
        warriorStats.Upgrade();

        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            pc.ApplyStats();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(AudioManager.SoundType.Upgrade);

        // ⭐ Heal เต็มหลอดหลัง Upgrade Warrior
        HealLocalPlayer();

        Debug.Log($"[UpgradeManager] Warrior Upgraded! Lv{warriorStats.CurrentLevel} | เงินเหลือ: {CurrentMoney}");
    }

    public void UpgradeArcher()
    {
        if (archerStats == null)
        {
            Debug.LogWarning("[UpgradeManager] ไม่มี archerStats!");
            return;
        }

        if (archerStats.IsMaxLevel)
        {
            Debug.Log("[UpgradeManager] Archer อยู่ระดับสูงสุดแล้ว!");
            return;
        }

        int cost = archerStats.GetUpgradeCost();
        if (CurrentMoney < cost)
        {
            Debug.LogWarning($"[UpgradeManager] เงินไม่พอ! Archer ต้องการ {cost} มีแค่ {CurrentMoney}");
            return;
        }

        SpendMoney(cost);
        archerStats.Upgrade();

        foreach (var ac in FindObjectsByType<Archer>(FindObjectsSortMode.None))
            ac.ApplyStats();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(AudioManager.SoundType.Upgrade);

        // ⭐ Heal เต็มหลอดหลัง Upgrade Archer
        HealLocalPlayer();

        Debug.Log($"[UpgradeManager] Archer Upgraded! Lv{archerStats.CurrentLevel} | เงินเหลือ: {CurrentMoney}");
    }

    // ==================== Minion Upgrade ====================

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
        data.Upgrade();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(AudioManager.SoundType.Upgrade);

        Debug.Log($"[UpgradeManager] {data.minionName} Upgraded! Lv{data.CurrentLevel} | เงินเหลือ: {CurrentMoney}");
    }

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

    // ==================== ⭐ Heal Utility ====================

    /// <summary>
    /// Heal Local Player เต็มหลอด — รองรับทั้ง Multiplayer (NetworkBehaviour.IsOwner)
    /// และ Single Player (หา PlayerController/Archer ตัวแรกที่เจอ)
    /// </summary>
    private void HealLocalPlayer()
    {
        // ลองหาผ่าน GameManager (Multiplayer)
        if (GameManager.Instance != null)
        {
            GameManager.HealLocalPlayer();
            return;
        }

        // Solo: หาตัวแรกที่เจอในซีน
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            pc.HealToFull();
            return;
        }
        foreach (var ac in FindObjectsByType<Archer>(FindObjectsSortMode.None))
        {
            ac.HealToFull();
            return;
        }
    }
}