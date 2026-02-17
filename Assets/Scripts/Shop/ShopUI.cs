using TMPro;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private MinionData minionData;
    // ใส่ SO ของตัวที่ปุ่มนี้ขาย

    [SerializeField] private TextMeshProUGUI costText;
    void Start()
    {
        if (minionData != null && costText != null)
        {
            costText.text = "Cost:" + minionData.cost.ToString();
        }
    }

    public void BuyMinion()
    {
        placementManager.StartPlacing(minionData);
    }

    public void CancelBuy()
    {
        placementManager.CancelPlacement();
    }
}
