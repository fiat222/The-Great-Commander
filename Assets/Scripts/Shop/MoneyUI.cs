using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private PlacementManager placementManager;

    void Start()
    {
        if (placementManager != null)
        {
            placementManager.OnMoneyChanged += UpdateMoney;
            UpdateMoney(placementManager.Money);
        }
        else
        {
            Debug.LogError("<color=red>[MoneyUI]</color> หา PlacementManager ไม่พบ! กรุณาลากใส่ใน Inspector ของ MoneyUI ด้วยครับ");
        }
    }

    void UpdateMoney(int amount)
    {
        moneyText.text = amount.ToString();
    }
    void OnDestroy()
    {
        placementManager.OnMoneyChanged -= UpdateMoney;
    }
}
