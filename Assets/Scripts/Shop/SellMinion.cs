using UnityEngine;

public class SellMinion : MonoBehaviour
{
    private MinionData minionData;

    public void Setup(MinionData data)
    {
        minionData = data;
    }

    void OnMouseDown()
    {
        if (PlacementManager.Instance == null) return;
        if (!PlacementManager.Instance.IsSellingMode) return;
        if (minionData == null) return;

        int sellValue = minionData.cost;

        PlacementManager.Instance.SellMinion(sellValue);
        Destroy(gameObject);
    }
}
