using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button; // ลาก Button มาใส่ หรือปล่อยว่างเดี๋ยวสคริปต์หาเอง

    public enum ShopItemType { Minion, Enemy }
    private ShopItemType itemType;
    private int itemIndex;
    private MinionData minionData;

    private void Awake()
    {
        // ถ้าไม่ได้ลาก Button มาใส่ใน Inspector ให้พยายามหาใน GameObject นี้ (หรือลูกของมัน)
        if (button == null) button = GetComponentInChildren<Button>();
        
        // ผูก Event การกดปุ่มด้วยโค้ด
        if (button != null)
        {
            // ป้องกันการเบิ้ล: ลบของเก่าออกก่อน (ถ้ามี) แล้วค่อยใส่เข้าไปใหม่ครับ
            button.onClick.RemoveListener(OnBuyClicked);
            button.onClick.AddListener(OnBuyClicked);
        }
    }

    public void Setup(MinionData data, ShopItemType type, int index)
    {
        minionData = data;
        itemType = type;
        itemIndex = index;

        // อัปเดตข้อมูล UI ตาม Scriptable Object
        if (nameText != null)
            nameText.text = data.minionName;

        if (costText != null)
            costText.text = data.cost.ToString() + " Bath";

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;
    }

    private void OnBuyClicked()
    {
        Debug.Log($"Click Buy: {minionData.minionName} (Type: {itemType}, Index: {itemIndex}, Cost: {minionData.cost})");

        if (itemType == ShopItemType.Minion)
        {
            // --- ซื้อทหารฝั่งเรา ---
            if (PlacementManager.Instance != null)
            {
                PlacementManager.Instance.StartPlacing(minionData);
            }
        }
        else
        {
            // --- ซื้อศัตรูส่งไปบุก ---
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RequestBuyEnemy(itemIndex);
            }
        }
    }
}
