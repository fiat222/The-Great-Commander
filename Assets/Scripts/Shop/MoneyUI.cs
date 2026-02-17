using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private PlacementManager placementManager;

    void Start()
    {
        placementManager.OnMoneyChanged += UpdateMoney;
        UpdateMoney(placementManager.Money);
    }

    void UpdateMoney(int amount)
    {
        moneyText.text = "Money: " + amount;
    }
    void OnDestroy()
    {
        placementManager.OnMoneyChanged -= UpdateMoney;
    }
}
