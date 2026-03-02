using UnityEngine;
using System.Collections.Generic;

public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    [Header("Map Settings — ต้องตรงกับ Terrain จริง")]
    public RectTransform minimapPanel; // ลาก MapUI ใส่ตรงนี้
    public float mapWorldSizeX = 250f;
    public float mapWorldSizeZ = 300f;
    public float terrainOffsetX = -126f;
    public float terrainOffsetZ = -156f;

    [Header("Dot Prefabs")]
    public GameObject whiteDot;
    public GameObject blueDot;
    public GameObject redDot;

    private List<GameObject> activeIcons = new List<GameObject>();

    private void Awake() => Instance = this;

    public void Refresh(MinimapUnitData[] units)
    {
        ClearIcons();

        // ดึงขนาด Panel จริงๆ ณ runtime (ไม่ hardcode)
        float panelW = minimapPanel.rect.width;
        float panelH = minimapPanel.rect.height;

        foreach (var unit in units)
        {
            GameObject prefab = GetPrefab(unit.UnitType);
            if (prefab == null) continue;

            GameObject icon = Instantiate(prefab);
            icon.transform.SetParent(minimapPanel, false);

            // unit.Position.x = world X, unit.Position.y = world Z (เก็บใน Vector2.y)
            float xRatio = Mathf.Clamp01((unit.Position.x - terrainOffsetX) / mapWorldSizeX);
            float zRatio = Mathf.Clamp01((unit.Position.y - terrainOffsetZ) / mapWorldSizeZ);

            RectTransform rt = icon.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xRatio * panelW, zRatio * panelH);

            activeIcons.Add(icon);
        }
    }

    private GameObject GetPrefab(byte type) => type switch
    {
        0 => whiteDot,
        1 => blueDot,
        _ => redDot
    };

    private void ClearIcons()
    {
        foreach (var icon in activeIcons)
            if (icon != null) Destroy(icon);
        activeIcons.Clear();
    }
}