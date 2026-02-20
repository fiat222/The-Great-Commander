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

    private MinionData minionData;

    private void Awake()
    {
        // ถ้าไม่ได้ลาก Button มาใส่ใน Inspector ให้พยายามหาใน GameObject นี้ (หรือลูกของมัน)
        if (button == null) button = GetComponentInChildren<Button>();
        
        // ผูก Event การกดปุ่มด้วยโค้ด (Best Practice ไม่ต้องไปนั่งลาก OnClick() เองในหน้าต่าง Inspector)
        if (button != null)
        {
            button.onClick.AddListener(OnBuyClicked);
        }
    }

    public void Setup(MinionData data)
    {
        minionData = data;

        // อัปเดตข้อมูล UI ตาม Scriptable Object
        if (nameText != null)
            nameText.text = data.minionName;

        if (costText != null)
            costText.text = data.cost.ToString() + " Bath"; // ใส่คำว่า Bath ตามในรูปของคุณ

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;
    }

    private void OnBuyClicked()
    {
        Debug.Log($"Click Buy: {minionData.minionName} (Cost: {minionData.cost})");

        // เรียกใช้งาน PlacementManager
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.StartPlacing(minionData);
        }
        else
        {
            Debug.LogWarning("ยังไม่มีระบบ PlacementManager ในฉากนะ!");
        }
    }
}
