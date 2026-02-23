using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [Header("Data Lists")]
    [SerializeField] private MinionData[] minionDataList;
    [SerializeField] private MinionData[] enemyDataList; // รายชื่อศัตรูที่จะส่งไปบุกครับ

    [Header("UI Containers")]
    [SerializeField] private GameObject shopItemCardPrefab;
    [SerializeField] private Transform minionContainer; // ใส่ MinionLayoutGroup ตรงนี้ครับ
    [SerializeField] private Transform enemyContainer;  // ใส่ EnemyLayoutGroup ตรงนี้ครับ

    [Header("Tab Buttons (Optional for Color Feedback)")]
    [SerializeField] private Image minionsBtnImg;
    [SerializeField] private Image enemyBtnImg;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel; // เอาไว้ปิดร้านค้า
    [SerializeField] private GameObject openShopButton; // ปุ่มเปิดร้านค้าที่พี่ต้องการให้ซ่อน/โชว์ครับ

    void Start()
    {
        GenerateAllCards();
        SetActiveTabVisual(0); // เซ็ตค่าเริ่มต้นให้ปุ่มแสดงสถานะ Active ตั้งแต่เริ่มครับ
    }

    void GenerateAllCards()
    {
        // 1. สร้างการ์ดสำหรับทหารฝั่งเรา
        GenerateGroup(minionDataList, minionContainer, ShopItemCard.ShopItemType.Minion);
        
        // 2. สร้างการ์ดสำหรับศัตรูส่งบุก
        GenerateGroup(enemyDataList, enemyContainer, ShopItemCard.ShopItemType.Enemy);
    }

    void GenerateGroup(MinionData[] dataList, Transform container, ShopItemCard.ShopItemType type)
    {
        if (container == null || dataList == null) return;

        // ล้างของเก่า
        foreach (Transform child in container)
            Destroy(child.gameObject);

        for (int i = 0; i < dataList.Length; i++)
        {
            if (dataList[i] == null) continue;

            GameObject card = Instantiate(shopItemCardPrefab, container);
            ShopItemCard itemCard = card.GetComponent<ShopItemCard>();
            if (itemCard != null)
                itemCard.Setup(dataList[i], type, i);
        }
    }

    // ฟังก์ชันเปลี่ยนสีปุ่ม และ เปิด/ปิด Layout (เรียกจาก OnClick ของปุ่มได้เลยครับ)
    public void SetActiveTabVisual(int tabIndex)
    {
        bool isMinions = (tabIndex == 0);

        // 1. เปลี่ยนสีปุ่ม
        if (minionsBtnImg != null) minionsBtnImg.color = isMinions ? activeColor : inactiveColor;
        if (enemyBtnImg != null) enemyBtnImg.color = isMinions ? inactiveColor : activeColor;

        // 2. เปิด/ปิด Container (LayoutGroup)
        if (minionContainer != null) minionContainer.gameObject.SetActive(isMinions);
        if (enemyContainer != null) enemyContainer.gameObject.SetActive(!isMinions);
    }

    // ฟังก์ชันเปิิดร้านค้า
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            SetActiveTabVisual(0); // เปิดมาให้เข้าหน้า Minions เสมอครับ
        }
        
        // ซ่อนปุ่มเปิดร้านค้า
        if (openShopButton != null) openShopButton.SetActive(false);
    }

    // ฟังก์ชันปิดร้านค้า (ผูกกับปุ่ม X)
    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        
        // โชว์ปุ่มเปิดร้านค้ากลับคืนมา
        if (openShopButton != null) openShopButton.SetActive(true);
    }
}
