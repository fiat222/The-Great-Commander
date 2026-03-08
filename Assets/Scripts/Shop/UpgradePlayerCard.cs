using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradePlayerCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button upgradeButton;

    private void Awake()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }
    }

    private void OnEnable()
    {
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged += OnMoneyChanged;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    private void OnUpgradeClicked()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.UpgradePlayer();
        }
        
        RefreshUI();
    }

    private void OnMoneyChanged(int _)
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (UpgradeManager.Instance == null) return;

        PlayerStatsSO reference = UpgradeManager.Instance.warriorStats != null
            ? UpgradeManager.Instance.warriorStats
            : UpgradeManager.Instance.archerStats;

        if (reference == null) return;

        int currentMoney = PlacementManager.Instance != null
            ? PlacementManager.Instance.Money
            : 0;

        bool isMax = reference.IsMaxLevel;
        int cost = reference.GetUpgradeCost();

        if (levelText != null)
            levelText.text = $"Player Lv {reference.CurrentLevel} / {reference.maxLevel}";

        if (costText != null)
            costText.text = isMax ? "MAX" : $"{cost} Orb";

        if (upgradeButton != null)
            upgradeButton.interactable = !isMax && currentMoney >= cost;
    }
}

