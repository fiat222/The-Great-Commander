using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopItemCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button; // ลาก Button มาใส่ หรือปล่อยว่างเดี๋ยวสคริปต์หาเอง

    [Header("Highlight Settings")]
    [Tooltip("ลาก GameObject ที่เป็นกรอบไฟหรือตัวหนังสือที่จะใช้แสดงผลว่า 'เลือกแล้ว' มาใส่ที่นี่")]
    [SerializeField] private GameObject highlightObj; 

    // Event ทำหน้าที่ประกาศให้การ์ดทุกใบรู้ว่า "มีคนถูกคลิกเลือกนะ"
    public static event Action<ShopItemCard> OnAnyCardSelected;

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

        // ค่าเริ่มต้น ปิดไฮไลต์ไว้ก่อน
        if (highlightObj != null) highlightObj.SetActive(false);
    }

    private void OnEnable()
    {
        // เมื่อการ์ดนี้ปรากฏขึ้นมา ให้รอฟังว่ามีการ์ดไหนโดนเลือกหรือเปล่า
        OnAnyCardSelected += CheckHighlightStatus;
    }

    private void OnDisable()
    {
        // เมื่อการ์ดโดนซ่อนหรือถูกปิดแอป ให้เลิกฟัง ป้องกันบัคขยะความจำ (Memory Leak)
        OnAnyCardSelected -= CheckHighlightStatus;
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
            costText.text = data.cost.ToString();

        if (iconImage != null && data.picture != null)
            iconImage.sprite = data.picture;
            
        // ตัดไฮไลต์ออกเสมอเวลารีเซ็ตข้อมูลใหม่
        if (highlightObj != null) highlightObj.SetActive(false);
    }

    private void OnBuyClicked()
    {
        Debug.Log($"Click Buy: {minionData.minionName} (Type: {itemType}, Index: {itemIndex}, Cost: {minionData.cost})");

        // ประกาศให้โลกรู้ว่า "การ์ดใบนี้ (this) โดนเลือกแล้ว!"
        OnAnyCardSelected?.Invoke(this);

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

    private void CheckHighlightStatus(ShopItemCard selectedCard)
    {
        if (highlightObj != null)
        {
            // ถ้าการ์ดที่ถูกเลือก 'คือการ์ดใบนี้' ก็ให้เปิด Highlight ถ้า 'ไม่ใช่' ก็ปิดมันซะ 
            bool isMeSelected = (selectedCard == this);
            highlightObj.SetActive(isMeSelected);
        }
    }
}
