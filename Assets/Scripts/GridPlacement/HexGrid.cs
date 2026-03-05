using UnityEngine;
using DT.GridSystem;

/// <summary>
/// HexGrid ที่ใช้งานจริงในเกม — เก็บ GameObject ของ Minion ที่วางอยู่ในแต่ละช่อง
/// null = ช่องว่าง, มีค่า = ช่องจับจองแล้ว
/// </summary>
public class HexGrid : HexGridSystem3D<GameObject>
{
    public HexOrientation Orientation => hexOrientation;

    /// <summary>ตรวจสอบว่าช่อง (x, y) ว่างอยู่ไหม</summary>
    public bool IsCellEmpty(int x, int y) => GetGridObject(x, y) == null;

    /// <summary>ตรวจสอบว่าช่องจาก world position ว่างอยู่ไหม</summary>
    public bool IsCellEmpty(UnityEngine.Vector3 worldPos)
    {
        GetGridPosition(worldPos, out int x, out int y);
        return IsCellEmpty(x, y);
    }
}