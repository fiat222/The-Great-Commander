using UnityEngine;

public class SellMinion : MonoBehaviour
{
    public int sellValue = 50;

    void OnMouseDown()
    {
        if (PlacementManager.Instance == null) return;

        if (!PlacementManager.Instance.IsSellingMode) return;

        PlacementManager.Instance.SellMinion(sellValue);
        Debug.Log("CLICKED MINION");
        Destroy(gameObject);
    }
}
