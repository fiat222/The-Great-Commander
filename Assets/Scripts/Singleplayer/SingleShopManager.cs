using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SingleShopManager — ร้านค้า Solo Mode
/// มี 2 แท็บ:
/// 0 = Minion
/// 1 = Upgrade
/// </summary>
public class SingleShopManager : MonoBehaviour
{
    public static SingleShopManager Instance { get; private set; }

    private void Awake() => Instance = this;

    // ==================== Data ====================

    [Header("Minion Data")]
    [SerializeField] private MinionData[] minionDataList;

    // ==================== Prefabs ====================

    [Header("Card Prefabs")]
    [SerializeField] private GameObject shopItemCardPrefab;
    [SerializeField] private GameObject upgradeItemCardPrefab;

    // ==================== Containers ====================

    [Header("UI Containers")]
    [SerializeField] private Transform minionContainer;
    [SerializeField] private Transform upgradeContainer;

    // ==================== Tab Buttons ====================

    [Header("Tab Buttons")]
    [SerializeField] private Image minionsBtnImg;
    [SerializeField] private Image upgradeBtnImg;

    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.7f,0.7f,0.7f,1f);

    // ==================== Player Upgrade ====================

    [Header("Player Upgrade UI")]
    [SerializeField] private Button playerUpgradeBtn;
    [SerializeField] private TextMeshProUGUI playerUpgradeCostText;
    [SerializeField] private TextMeshProUGUI playerLevelText;

    // ==================== Shop Panel ====================

    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject openShopButton;

    // ==================== Lifecycle ====================

    private void Start()
    {
        GenerateAllCards();
        SetActiveTabVisual(0);
        SetupPlayerUpgradeBtn();

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged += _ => RefreshPlayerUpgradeUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (shopPanel.activeSelf)
                CloseShop();
            else
                OpenShop();
        }
    }

    // ==================== Generate Cards ====================

    private void GenerateAllCards()
    {
        GenerateShopGroup(minionDataList, minionContainer);
        GenerateUpgradeGroup(minionDataList, upgradeContainer);
    }

    private void GenerateShopGroup(MinionData[] dataList, Transform container)
    {
        if (container == null || dataList == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        for (int i = 0; i < dataList.Length; i++)
        {
            if (dataList[i] == null) continue;

            var card = Instantiate(shopItemCardPrefab, container);
            card.GetComponent<ShopItemCard>()?.Setup(dataList[i], ShopItemCard.ShopItemType.Minion, i);
        }
    }

    private void GenerateUpgradeGroup(MinionData[] dataList, Transform container)
    {
        if (container == null || dataList == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child.GetComponent<UpgradeItemCard>() != null)
                Destroy(child.gameObject);
        }

        for (int i = 0; i < dataList.Length; i++)
        {
            if (dataList[i] == null) continue;

            var card = Instantiate(upgradeItemCardPrefab, container);
            card.GetComponent<UpgradeItemCard>()?.Setup(dataList[i], i);
        }
    }

    // ==================== Tabs ====================

    // เอาไว้ให้ Button UI ใน Unity เรียก (OnClick)
    public void SelectMinionTab() => SetActiveTabVisual(0);
    public void SelectUpgradeTab() => SetActiveTabVisual(1);

    /// 0 = Minion
    /// 1 = Upgrade
    private void SetActiveTabVisual(int tabIndex)
    {
        if (minionContainer != null) minionContainer.gameObject.SetActive(tabIndex == 0);
        if (upgradeContainer != null) upgradeContainer.gameObject.SetActive(tabIndex == 1);

        if (minionsBtnImg != null) minionsBtnImg.color = tabIndex == 0 ? activeColor : inactiveColor;
        if (upgradeBtnImg != null) upgradeBtnImg.color = tabIndex == 1 ? activeColor : inactiveColor;

        if (tabIndex == 1)
            RefreshPlayerUpgradeUI();
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
    }

    private void RefreshPlayerUpgradeUI()
    {
        if (UpgradeManager.Instance == null) return;

        PlayerStatsSO reference =
            UpgradeManager.Instance.warriorStats != null
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

        if (openShopButton != null)
            openShopButton.SetActive(false);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (openShopButton != null)
            openShopButton.SetActive(true);
    }
}