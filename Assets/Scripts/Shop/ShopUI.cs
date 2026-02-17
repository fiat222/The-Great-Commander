using UnityEngine;

public class ShopUI : MonoBehaviour
{
    public PlacementManager placementManager;

    public void BuyMinion()
    {
        placementManager.StartPlacing();
    }

    public void CancelBuy()
    {
        placementManager.CancelPlacement();
    }
}
