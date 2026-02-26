using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UpgradeItemCard — การ์ดในแท็บ Upgrade สำหรับ Minion แต่ละชนิด
/// แสดง: ชื่อ, icon, Level ปัจจุบัน, ราคา Upgrade, ปุ่ม Upgrade
/// ปุ่มจะ disable อัตโนมัติถ้าเงินไม่พอหรือถึง Max Level แล้ว
/// มีระบบ Highlight เลือกได้ทีละใบ
/// </summary>
public class UpgradeItemCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button upgradeBtn;

    [Header("Highlight Settings")]
    [SerializeField] private GameObject highlightObj;

    public static System.Action<UpgradeItemCard> OnAnyUpgradeCardSelected;

    // ==================== Data ====================
    private MinionData _data;
    private int _index;

    // ==================== Lifecycle ====================

    private void Awake()
    {
        if (upgradeBtn == null)
            upgradeBtn = GetComponentInChildren<Button>();

        if (upgradeBtn != null)
        {
            upgradeBtn.onClick.RemoveAllListeners();
            upgradeBtn.onClick.AddListener(OnUpgradeClicked);
        }

        if (highlightObj != null)
            highlightObj.SetActive(false);
    }

    private void OnEnable()
    {
        MinionData.OnMinionUpgraded += OnMinionUpgraded;

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged += OnMoneyChanged;

        OnAnyUpgradeCardSelected += CheckHighlightStatus;
    }

    private void OnDisable()
    {
        MinionData.OnMinionUpgraded -= OnMinionUpgraded;

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged -= OnMoneyChanged;

        OnAnyUpgradeCardSelected -= CheckHighlightStatus;
    }

    // ==================== Setup ====================

    public void Setup(MinionData data, int index)
    {
        _data = data;
        _index = index;

        if (nameText != null)
            nameText.text = data.minionName;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (highlightObj != null)
            highlightObj.SetActive(false);

        RefreshUI();
    }

    // ==================== Button ====================

    private void OnUpgradeClicked()
    {
        // 🔥 ยิง Event ให้ใบอื่นดับ Highlight
        OnAnyUpgradeCardSelected?.Invoke(this);

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.UpgradeMinion(_index);
    }

    // ==================== Event Callbacks ====================

    private void OnMinionUpgraded(MinionData upgraded)
    {
        if (upgraded == _data)
            RefreshUI();
    }

    private void OnMoneyChanged(int _) => RefreshUI();

    private void CheckHighlightStatus(UpgradeItemCard selectedCard)
    {
        if (highlightObj != null)
            highlightObj.SetActive(selectedCard == this);
    }

    // ==================== UI Refresh ====================

    private void RefreshUI()
    {
        if (_data == null) return;

        int currentMoney = PlacementManager.Instance != null
            ? PlacementManager.Instance.Money
            : 0;

        bool isMax = _data.IsMaxLevel;
        int cost = _data.GetUpgradeCost();

        if (levelText != null)
            levelText.text = $"Lv {_data.CurrentLevel} / {_data.maxLevel}";

        if (costText != null)
            costText.text = isMax ? "MAX" : $"{cost}";

        if (upgradeBtn != null)
            upgradeBtn.interactable = !isMax && currentMoney >= cost;
    }
}