using UnityEngine;
using UnityEngine.EventSystems;
using DT.GridSystem;
public class PlacementManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public static PlacementManager Instance;
    public System.Action<int> OnMoneyChanged;

    [SerializeField] private LayerMask buildLayer;

    private GameObject ghost;
    private bool isPlacing = false;
    public bool IsPlacing => isPlacing;

    private MinionData currentMinionData;   // ⭐ ตัวที่กำลังจะวาง

    public int Money = 200;

    // ===== SELL OVERLAY =====
    [SerializeField] private GameObject sellCursorOverlay;
    private RectTransform sellCursorRect;

    private bool isSellingMode = false;
    public bool IsSellingMode => isSellingMode;

    void Awake()
    {
        Instance = this;
    }

    [Header("Grid Visuals")]
    public HexGridVisualizer gridVisual; // ลาก GridVisual มาใส่ช่องนี้

    void Start()
    {
        // ดึง RectTransform จาก sellCursorOverlay
        if (sellCursorOverlay != null)
            sellCursorRect = sellCursorOverlay.GetComponent<RectTransform>();

        // สร้าง Mesh รอไว้เลยแต่แรก
        if (gridVisual != null)
        {
            gridVisual.CreateGridMesh();
            gridVisual.gameObject.SetActive(false); // ปิดไว้ก่อน
        }
    }

    void Update()
    {
        if (isPlacing)
            HandlePlacing();

        if (isSellingMode)
        {
            if (sellCursorRect != null) // เพิ่มการเช็คเพื่อความปลอดภัย
                sellCursorRect.position = Input.mousePosition + new Vector3(40, -35, 0);

            if (Input.GetMouseButtonDown(1))
                ToggleSellMode();
        }
    }

    // ==============================
    // ===== START PLACING (SO) =====
    // ==============================
    public void StartPlacing(MinionData data)
    {
        if (isSellingMode)
            ToggleSellMode();

        if (data == null) return;
        if (Money < data.cost) return;
        if (isPlacing) return;

        currentMinionData = data;
        isPlacing = true;

        ghost = Instantiate(data.prefab);

        // ปิด collider และ AI บน ghost ทั้งหมด
        foreach (Collider c in ghost.GetComponentsInChildren<Collider>())
            c.enabled = false;
        foreach (MonoBehaviour mb in ghost.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = false;

        // สีเริ่มต้น — จะถูก update ทุก frame ใน HandlePlacing
        SetGhostColor(true);

        if (gridVisual != null) gridVisual.gameObject.SetActive(true);
    }

    // -------------------------------------------------------
    // เปลี่ยนสี ghost: เขียว = วางได้, แดง = ช่องเต็ม/นอก grid
    // -------------------------------------------------------
    private void SetGhostColor(bool canPlace)
    {
        if (ghost == null) return;
        Color col = canPlace
            ? new Color(0f, 1f, 0f, 0.45f)   // 🟢 โปร่งใสเขียว
            : new Color(1f, 0f, 0f, 0.45f);   // 🔴 โปร่งใสแดง

        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
        {
            // เปลี่ยนผ่าน MaterialPropertyBlock เพื่อไม่ต้อง clone material
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", col);
            r.SetPropertyBlock(mpb);

            // Fallback: เขียนตรงๆ ทับ material color ด้วย (รองรับทุก shader)
            r.material.color = col;
        }
    }

    void HandlePlacing()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildLayer))
        {
            if (hexGrid == null) return;

            hexGrid.GetGridPosition(hit.point, out int x, out int y);
            Vector3 snappedPos = hexGrid.GetWorldPosition(x, y, true);

            ghost.transform.position = snappedPos;

            bool cellEmpty = hexGrid.IsCellEmpty(x, y);
            SetGhostColor(cellEmpty);

            if (Input.GetMouseButtonDown(0))
            {
                PlaceMinion(snappedPos, x, y);
            }
        }

        // คลิกขวาเพื่อยกเลิกการวาง
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }


    void PlaceMinion(Vector3 pos, int gridX, int gridY)
    {
        if (currentMinionData == null) return;
        if (Money < currentMinionData.cost) return;

        // ⭐ เช็คว่าช่องว่างก่อนวาง
        if (!hexGrid.IsCellEmpty(gridX, gridY))
        {
            Debug.LogWarning($"[PlacementManager] ช่อง ({gridX},{gridY}) มีตัวละครอยู่แล้ว!");
            return;
        }

        Money -= currentMinionData.cost;
        OnMoneyChanged?.Invoke(Money);

        GameObject obj = Instantiate(currentMinionData.prefab, pos, Quaternion.identity);

        // ⭐ ลงทะเบียนใน Grid ว่าช่องนี้จับจองแล้ว
        hexGrid.AddGridObject(gridX, gridY, obj);

        // เซ็ตข้อมูลสำหรับ Sell
        SellMinion sell = obj.GetComponent<SellMinion>();
        if (sell != null)
            sell.Setup(currentMinionData, gridX, gridY);

        Destroy(ghost);
        ghost = null;
        isPlacing = false;
        currentMinionData = null;

        if (gridVisual != null) gridVisual.gameObject.SetActive(false);
    }

    // ===== CANCEL =====
    public void CancelPlacement()
    {
        if (!isPlacing) return;
        if (gridVisual != null) gridVisual.gameObject.SetActive(false);

        Destroy(ghost);
        ghost = null;
        isPlacing = false;
        currentMinionData = null;
    }

    // ===== SELL =====
    public void SellMinion(int value)
    {
        Money += value;
        OnMoneyChanged?.Invoke(Money);
    }

    /// <summary>เรียกจาก SellMinion เพื่อเคลียร์ช่องใน Grid ด้วย</summary>
    public void ClearGridCell(int gridX, int gridY)
    {
        if (hexGrid != null)
            hexGrid.RemoveGridObject(gridX, gridY);
    }

    // ===== TOGGLE SELL MODE =====
    public void ToggleSellMode()
    {
        isSellingMode = !isSellingMode;

        if (sellCursorOverlay != null)
            sellCursorOverlay.SetActive(isSellingMode);
    }
}
