using UnityEngine;

public class SellMinion : MonoBehaviour
{
    private MinionData minionData;
    private int cellX;
    private int cellY;

    /// <summary>เรียกตอน Place เพื่อเก็บข้อมูล SO และพิกัดใน Grid</summary>
    public void Setup(MinionData data, int gridX, int gridY)
    {
        minionData = data;
        cellX = gridX;
        cellY = gridY;
    }

    void OnMouseDown()
    {
        if (PlacementManager.Instance == null) return;
        if (!PlacementManager.Instance.IsSellingMode) return;
        if (minionData == null) return;

        int sellValue = minionData.cost;

        // ⭐ เคลียร์ช่องใน Grid ก่อน destroy
        PlacementManager.Instance.ClearGridCell(cellX, cellY);
        PlacementManager.Instance.SellMinion(sellValue);
        Destroy(gameObject);
    }
}

