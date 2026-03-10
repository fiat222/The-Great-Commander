using UnityEngine;
using System.Collections.Generic;

public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    [Header("Map Settings — ต้องตรงกับ Terrain จริง")]
    public RectTransform minimapPanel;
    public float mapWorldSizeX = 250f;
    public float mapWorldSizeZ = 300f;
    public float terrainOffsetX = -126f;
    public float terrainOffsetZ = -156f;

    [Header("Dot Prefabs")]
    public GameObject whiteDot;  // Player ฝั่งตรงข้าม
    public GameObject blueDot;   // Minion ฝั่งตรงข้าม
    public GameObject redDot;    // Enemy ฝั่งตรงข้าม

    private List<GameObject> activeIcons = new List<GameObject>();

    private void Awake()
    {
        Instance = this;

        // ✅ ดึงค่า Terrain อัตโนมัติ ไม่ต้องกรอก Inspector
        var t = Terrain.activeTerrain;
        if (t != null)
        {
            terrainOffsetX = t.transform.position.x;
            terrainOffsetZ = t.transform.position.z;
            mapWorldSizeX = t.terrainData.size.x;
            mapWorldSizeZ = t.terrainData.size.z;
            Debug.Log($"[Terrain] offset=({terrainOffsetX}, {terrainOffsetZ}) size=({mapWorldSizeX}, {mapWorldSizeZ})");
        }
        else
        {
            Debug.LogWarning("[MinimapUI] ไม่พบ Terrain — ใช้ค่าจาก Inspector แทน");
        }
    }

    // เรียกจาก ReceiveOpponentDataClientRpc
    public void Refresh(MinimapUnitData[] units)
    {
        ClearIcons();
        foreach (var unit in units)
            SpawnIcon(unit);
    }

    void SpawnIcon(MinimapUnitData unit)
    {
        GameObject prefab = GetPrefab(unit.UnitType);
        if (prefab == null) return;

        GameObject icon = Instantiate(prefab, minimapPanel, false);

        float xRatio = Mathf.Clamp01((unit.Position.x - terrainOffsetX) / mapWorldSizeX);
        float zRatio = Mathf.Clamp01((unit.Position.y - terrainOffsetZ) / mapWorldSizeZ);

        RectTransform rt = icon.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // ✅ คำนวณตรงๆ ไม่ clamp ด้วย iconHalf
        rt.anchoredPosition = new Vector2(
            xRatio * minimapPanel.rect.width,
            zRatio * minimapPanel.rect.height
        );

        Debug.Log($"[Minimap] WorldPos=({unit.Position.x:F1}, {unit.Position.y:F1}) " +
                $"xR={xRatio:F2} zR={zRatio:F2} " +
                $"panel=({minimapPanel.rect.width:F0}x{minimapPanel.rect.height:F0}) " +
                $"pos=({rt.anchoredPosition.x:F0},{rt.anchoredPosition.y:F0})");

        activeIcons.Add(icon);
    }

    private GameObject GetPrefab(byte type) => type switch
    {
        0 => whiteDot,
        1 => blueDot,
        _ => redDot
    };

    public void SetVisible(bool visible)
    {
        if (minimapPanel != null)
            minimapPanel.gameObject.SetActive(visible);
    }

    private void ClearIcons()
    {
        foreach (var icon in activeIcons)
            if (icon != null) Destroy(icon);
        activeIcons.Clear();
    }
}