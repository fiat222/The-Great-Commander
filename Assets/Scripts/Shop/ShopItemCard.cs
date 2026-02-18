using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;

    private MinionData minionData;

    public void Setup(MinionData data)
    {
        minionData = data;

        if (nameText != null)
            nameText.text = data.minionName;

        if (costText != null)
            costText.text = "Cost: " + data.cost;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;
    }

    public void OnBuyClicked()
    {
        PlacementManager.Instance.StartPlacing(minionData);
    }
}
