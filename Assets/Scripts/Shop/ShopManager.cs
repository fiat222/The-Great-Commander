using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private MinionData[] minionDataList;
    [SerializeField] private GameObject shopItemCardPrefab;
    [SerializeField] private Transform shopContainer;

    void Start()
    {
        GenerateShopCards();
    }

    void GenerateShopCards()
    {
        if (shopItemCardPrefab == null)
        {
            Debug.LogError("[ShopManager] shopItemCardPrefab ยังเป็น None! ผูก ShopItemCard prefab ใน Inspector ก่อน");
            return;
        }
        if (shopContainer == null)
        {
            Debug.LogError("[ShopManager] shopContainer ยังเป็น None! ผูก Shop Container ใน Inspector ก่อน");
            return;
        }
        if (minionDataList == null || minionDataList.Length == 0)
        {
            Debug.LogWarning("[ShopManager] minionDataList ว่างเปล่า! ใส่ MinionData SO ใน Inspector ก่อน");
            return;
        }

        foreach (Transform child in shopContainer)
            Destroy(child.gameObject);

        Debug.Log($"[ShopManager] กำลัง generate {minionDataList.Length} cards ใน '{shopContainer.name}'");

        foreach (MinionData data in minionDataList)
        {
            if (data == null) continue;

            GameObject card = Instantiate(shopItemCardPrefab, shopContainer);
            ShopItemCard itemCard = card.GetComponent<ShopItemCard>();
            if (itemCard != null)
                itemCard.Setup(data);

            Debug.Log($"[ShopManager] สร้าง card: {data.minionName}");
        }
    }
}
