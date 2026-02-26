using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ShopManager — ร้านค้า 3 แท็บ: Minion / Enemy / Upgrade
///
/// แท็บ Upgrade:
///   - ด้านบน: ปุ่ม Upgrade Player (อัพ Warrior + Archer พร้อมกัน)
///   - ด้านล่าง: การ์ด UpgradeItemCard สำหรับ Minion แต่ละชนิด
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    private void Awake() => Instance = this;

    // ==================== Data ====================
    [Header("Data Lists")]
    [SerializeField] private MinionData[] minionDataList;

    // ==================== Prefabs ====================
    [Header("Card Prefabs")]
    [SerializeField] private GameObject shopItemCardPrefab;     // ใช้กับแท็บ Minion / Enemy
    [SerializeField] private GameObject upgradeItemCardPrefab;  // ใช้กับแท็บ Upgrade (ลาก Prefab ที่ติด UpgradeItemCard)

    // ==================== Containers ====================
    [Header("UI Containers")]
    [SerializeField] private Transform minionContainer;
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Transform upgradeContainer;   // Layout Group ของการ์ด Upgrade Minion

    // ==================== Tab Buttons ====================
    [Header("Tab Buttons")]
    [SerializeField] private Image minionsBtnImg;
    [SerializeField] private Image enemyBtnImg;
    [SerializeField] private Image upgradeBtnImg;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    // ==================== Player Upgrade UI (อยู่บนสุดของแท็บ Upgrade) ====================
    [Header("Player Upgrade UI (ในแท็บ Upgrade)")]
    [SerializeField] private Button playerUpgradeBtn;
    [SerializeField] private TextMeshProUGUI playerUpgradeCostText;
    [SerializeField] private TextMeshProUGUI playerLevelText;

    // ==================== Shop Panel ====================
    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject openShopButton;

    // ==================== Lifecycle ====================

    void Start()
    {
        GenerateAllCards();
        SetActiveTabVisual(0);
        SetupPlayerUpgradeBtn();

        // ผูก Event เพื่อให้ปุ่ม Player Upgrade refresh เมื่อเงินเปลี่ยน
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged += _ => RefreshPlayerUpgradeUI();
    }

    // ==================== Generate Cards ====================

    void GenerateAllCards()
    {
        // แท็บ Minion
        GenerateShopGroup(minionDataList, minionContainer, ShopItemCard.ShopItemType.Minion);

        // แท็บ Enemy
        if (GameManager.Instance?.systemEnemyPool != null)
            GenerateShopGroup(GameManager.Instance.systemEnemyPool, enemyContainer, ShopItemCard.ShopItemType.Enemy);

        // แท็บ Upgrade (การ์ด Minion แต่ละชนิด)
        GenerateUpgradeGroup(minionDataList, upgradeContainer);
    }

    void GenerateShopGroup(MinionData[] dataList, Transform container, ShopItemCard.ShopItemType type)
    {
        if (container == null || dataList == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);

        for (int i = 0; i < dataList.Length; i++)
        {
            if (dataList[i] == null) continue;
            var card = Instantiate(shopItemCardPrefab, container);
            card.GetComponent<ShopItemCard>()?.Setup(dataList[i], type, i);
        }
    }

    void GenerateUpgradeGroup(MinionData[] dataList, Transform container)
    {
        if (container == null || dataList == null) return;

        // 🔥 ลบเฉพาะการ์ดที่เป็น UpgradeItemCard เท่านั้น
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);

            if (child.GetComponent<UpgradeItemCard>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        // สร้างใหม่
        for (int i = 0; i < dataList.Length; i++)
        {
            if (dataList[i] == null) continue;

            var card = Instantiate(upgradeItemCardPrefab, container);
            card.GetComponent<UpgradeItemCard>()?.Setup(dataList[i], i);
        }
    }

    // ==================== Tab ====================

    /// <summary>
    /// 0 = Minion, 1 = Enemy, 2 = Upgrade
    /// ผูกกับ OnClick ของปุ่มแท็บใน Inspector ได้เลยครับ
    /// </summary>
    public void SetActiveTabVisual(int tabIndex)
    {
        if (minionContainer != null) minionContainer.gameObject.SetActive(tabIndex == 0);
        if (enemyContainer != null) enemyContainer.gameObject.SetActive(tabIndex == 1);
        if (upgradeContainer != null) upgradeContainer.gameObject.SetActive(tabIndex == 2);

        if (minionsBtnImg != null) minionsBtnImg.color = tabIndex == 0 ? activeColor : inactiveColor;
        if (enemyBtnImg != null) enemyBtnImg.color = tabIndex == 1 ? activeColor : inactiveColor;
        if (upgradeBtnImg != null) upgradeBtnImg.color = tabIndex == 2 ? activeColor : inactiveColor;

        // Refresh ปุ่ม Player Upgrade ทุกครั้งที่เปิดแท็บ Upgrade
        if (tabIndex == 2) RefreshPlayerUpgradeUI();
    }

    // ==================== Player Upgrade ====================

    private void SetupPlayerUpgradeBtn()
    {
        if (playerUpgradeBtn != null)
        {
            playerUpgradeBtn.onClick.RemoveAllListeners();
            playerUpgradeBtn.onClick.AddListener(OnPlayerUpgradeClicked);
        }
        RefreshPlayerUpgradeUI();
    }

    private void OnPlayerUpgradeClicked()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.UpgradePlayer();
        // RefreshPlayerUpgradeUI จะถูกเรียกผ่าน OnMoneyChanged อัตโนมัติครับ
    }

    private void RefreshPlayerUpgradeUI()
    {
        if (UpgradeManager.Instance == null) return;

        // ดึง reference SO จาก UpgradeManager
        PlayerStatsSO reference = UpgradeManager.Instance.warriorStats != null
            ? UpgradeManager.Instance.warriorStats
            : UpgradeManager.Instance.archerStats;

        if (reference == null) return;

        int currentMoney = PlacementManager.Instance != null ? PlacementManager.Instance.Money : 0;
        bool isMax = reference.IsMaxLevel;
        int cost = reference.GetUpgradeCost();

        if (playerLevelText != null)
            playerLevelText.text = $"Player Lv {reference.CurrentLevel} / {reference.maxLevel}";

        if (playerUpgradeCostText != null)
            playerUpgradeCostText.text = isMax ? "MAX" : $"{cost} Orb";

        if (playerUpgradeBtn != null)
            playerUpgradeBtn.interactable = !isMax && currentMoney >= cost;
    }

    // ==================== Open / Close ====================

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            SetActiveTabVisual(0);
        }
        if (openShopButton != null) openShopButton.SetActive(false);
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (openShopButton != null) openShopButton.SetActive(true);
    }
}