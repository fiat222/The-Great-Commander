using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    [Header("Settings")]
    public RectTransform minimapPanel; // ตัวกรอบรูปแมพ

    // ตั้งค่าตามค่าที่ได้จาก Terrain
    public float mapWorldSizeX = 250f;
    public float mapWorldSizeZ = 300f;
    public float terrainOffsetX = -126f;
    public float terrainOffsetZ = -156f;

    [Header("Prefabs")]
    public GameObject whiteDot;
    public GameObject blueDot;
    public GameObject redDot;

    private List<GameObject> activeIcons = new List<GameObject>();

    private void Awake() => Instance = this;

    public void Refresh(MinimapUnitData[] units)
    {
        ClearIcons();
        foreach (var unit in units)
        {
            GameObject icon = GetIcon(unit.UnitType);

            // สำคัญ: ต้องเซ็ต Parent ก่อนคำนวณตำแหน่ง
            icon.transform.SetParent(minimapPanel, false);
            icon.SetActive(true);

            // คำนวณพิกัดจากโลกจริง -> อัตราส่วน 0 ถึง 1
            float xRatio = (unit.Position.x - terrainOffsetX) / mapWorldSizeX;
            float zRatio = (unit.Position.y - terrainOffsetZ) / mapWorldSizeZ;

            // คูณกับขนาดจริงของ Rect ใน UI
            // ไม่ว่ามินิแมพจะอยู่ขวาบนหรือที่ไหน anchoredPosition จะอ้างอิงจากมุมซ้ายล่างของ Panel เสมอ (ถ้า Icon Pivot เป็น 0.5)
            float uiX = xRatio * minimapPanel.rect.width;
            float uiY = zRatio * minimapPanel.rect.height;

            RectTransform rt = icon.GetComponent<RectTransform>();
            // ตั้งค่า Anchor ของ Icon ให้เป็น Bottom-Left (0,0) เพื่อให้คำนวณจากมุมซ้ายล่างของกรอบ
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = new Vector2(uiX, uiY);

            activeIcons.Add(icon);
        }
    }

    private GameObject GetIcon(byte type)
    {
        GameObject prefab = type == 0 ? whiteDot : (type == 1 ? blueDot : redDot);
        return Instantiate(prefab);
    }

    private void ClearIcons()
    {
        foreach (var icon in activeIcons) Destroy(icon);
        activeIcons.Clear();
    }
}