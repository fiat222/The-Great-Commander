using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// ShopItemCard — การ์ดในแท็บ Minion และ Enemy (ไม่เปลี่ยนแปลงจากเดิม)
/// แท็บ Upgrade ใช้ UpgradeItemCard แยกต่างหากครับ
/// </summary>
public class ShopItemCard : MonoBehaviour
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
    private MinionData minionData;

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
    private void OnDisable() => OnAnyCardSelected -= CheckHighlightStatus;

    public void Setup(MinionData data, ShopItemType type, int index)
    {
        minionData = data;
        itemType = type;
        itemIndex = index;

        if (nameText != null) nameText.text = data.minionName;
        if (costText != null) costText.text = data.cost.ToString();
        if (iconImage != null && data.picture != null) iconImage.sprite = data.picture;
        if (highlightObj != null) highlightObj.SetActive(false);
    }

    private void OnBuyClicked()
    {
        OnAnyCardSelected?.Invoke(this);

        if (itemType == ShopItemType.Minion)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.Planning)
            {
                Debug.LogWarning("[Shop] วาง Minion ได้เฉพาะช่วง Planning เท่านั้น!");
                return;
            }

            if (PlacementManager.Instance != null)
                PlacementManager.Instance.StartPlacing(minionData);
        }
        else
        {
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