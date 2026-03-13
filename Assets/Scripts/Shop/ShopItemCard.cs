using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// ShopItemCard — การ์ดในแท็บ Minion และ Enemy
/// รองรับ Tooltip เมื่อนำเมาส์ไปชี้การ์ด Minion และ Enemy (ข้อมูลจาก MinionData และ EnemyStatsSO)
/// </summary>
public class ShopItemCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    [Header("Highlight Settings")]
    [SerializeField] private GameObject highlightObj;

    public static event Action<ShopItemCard> OnAnyCardSelected;

    public enum ShopItemType { Minion, Enemy }
    private ShopItemType itemType;
    private int itemIndex;

    // ✅ เก็บข้อมูลทั้งสองประเภทไว้แยกกัน
    private MinionData minionData;
    private EnemyStatsSO enemyData;

    private void Awake()
    {
        if (button == null) button = GetComponentInChildren<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnBuyClicked);
            button.onClick.AddListener(OnBuyClicked);
        }
        if (highlightObj != null) highlightObj.SetActive(false);
    }

    private void OnEnable() => OnAnyCardSelected += CheckHighlightStatus;
    private void OnDisable()
    {
        OnAnyCardSelected -= CheckHighlightStatus;
        // ซ่อน Tooltip ทันทีเมื่อ Card ถูกปิด
        TooltipUI.Instance?.Hide();
    }

    // ─────────────────────────────────────────────
    //  Setup สำหรับการ์ด Minion (ข้อมูลจาก MinionData)
    // ─────────────────────────────────────────────
    public void Setup(MinionData data, ShopItemType type, int index)
    {
        minionData = data;
        enemyData  = null;
        itemType   = type;
        itemIndex  = index;

        if (nameText != null) nameText.text = data.minionName;
        if (costText != null) costText.text = data.cost.ToString();
        if (iconImage != null && data.picture != null) iconImage.sprite = data.picture;
        if (highlightObj != null) highlightObj.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  ✅ Setup สำหรับการ์ด Enemy (ข้อมูลจาก EnemyStatsSO)
    // ─────────────────────────────────────────────
    public void Setup(EnemyStatsSO data, ShopItemType type, int index)
    {
        enemyData  = data;
        minionData = null;
        itemType   = type;
        itemIndex  = index;

        if (nameText != null) nameText.text = data.enemyName;
        if (costText != null) costText.text = data.cost.ToString();
        if (iconImage != null && data.icon != null) iconImage.sprite = data.icon;
        if (highlightObj != null) highlightObj.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Tooltip — เมื่อนำเมาส์เข้า/ออก
    // ─────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance == null) return;

        if (itemType == ShopItemType.Minion && minionData != null)
        {
            string content = BuildMinionTooltip(minionData);
            TooltipUI.Instance.Show(minionData.minionName, content, TooltipUI.TooltipSize.Large);
        }
        else if (itemType == ShopItemType.Enemy && enemyData != null)
        {
            string content = BuildEnemyTooltip(enemyData);
            TooltipUI.Instance.Show(enemyData.enemyName, content, TooltipUI.TooltipSize.Large);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance?.Hide();
    }

    // ─────────────────────────────────────────────
    //  สร้างข้อความ Tooltip
    // ─────────────────────────────────────────────
    private string BuildMinionTooltip(MinionData data)
    {
        // ✅ ใช้ GetXxx() เพื่อแสดงค่าที่ Scale ตาม Level ปัจจุบันแล้วครับ
        string levelLabel = data.IsMaxLevel ? "MAX" : $"Lv {data.CurrentLevel}";
        string upgradeInfo = data.IsMaxLevel
            ? ""
            : $"\n<b><color=#00BDFA>Upgrade Cost:</b> {data.GetUpgradeCost()} Orb</color>";

        return $"<b><color=#FFDD44>Cost:</b> {data.cost}  <i>({levelLabel})</i> </color>\n" +
               $"<b>HP:</b> {data.GetHP():F0}\n" +
               $"<b>ATK:</b> {data.GetDamage():F1}\n" +
               $"<b>DEF:</b> {data.GetDefense():F1}\n" +
               $"<b>Speed:</b> {data.GetSpeed():F1}\n" +
               $"<b>Range:</b> {data.attackrange}" +
               upgradeInfo;
    }

    private string BuildEnemyTooltip(EnemyStatsSO data)
    {
        // ✅ ใช้ GetXxx() เพื่อแสดงค่าที่ Scale ตาม Wave ปัจจุบันแล้วครับ
        return $"<b>Cost:</b> {data.cost}  <i>(Wave {data.CurrentWave})</i>\n" +
               $"<b>HP:</b> {data.GetHP():F0}\n" +
               $"<b>ATK:</b> {data.GetDamage():F1}\n" +
               $"<b>DEF:</b> {data.GetDefense():F1}\n" +
               $"<b>Speed:</b> {data.GetSpeed():F2}\n" +
               $"<b>Orb Drop:</b> {data.orbDrop}";  
    }

    // ─────────────────────────────────────────────
    //  Buy Logic — รองรับทั้ง Multiplayer และ Solo
    // ─────────────────────────────────────────────

    /// <summary>ดึง CurrentPhase จาก Manager ที่ Active อยู่ในขณะนั้น</summary>
    private GamePhase GetCurrentPhase()
    {
        if (GameManager.Instance != null)     return GameManager.Instance.CurrentPhase;
        if (SoloGameManager.Instance != null) return SoloGameManager.Instance.CurrentPhase;
        return GamePhase.Planning; // Fallback
    }

    private void OnBuyClicked()
    {
        OnAnyCardSelected?.Invoke(this);

        if (itemType == ShopItemType.Minion)
        {
            // ✅ เช็ค Phase จาก Manager ที่ Active (ทั้ง Multiplayer และ Solo)
            if (GetCurrentPhase() != GamePhase.Planning)
            {
                Debug.LogWarning("[Shop] วาง Minion ได้เฉพาะช่วง Planning เท่านั้น!");
                return;
            }

            if (PlacementManager.Instance != null && minionData != null)
                PlacementManager.Instance.StartPlacing(minionData);
        }
        else // Enemy
        {
            // ✅ Solo Mode ไม่มีระบบส่ง Enemy (Tab นี้ควรถูกซ่อนไว้แล้ว)
            if (GameManager.Instance != null)
                GameManager.Instance.RequestBuyEnemy(itemIndex);
        }
    }

    private void CheckHighlightStatus(ShopItemCard selectedCard)
    {
        if (highlightObj != null)
            highlightObj.SetActive(selectedCard == this);
    }
}