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
    [SerializeField] private GameObject sellButton; // ปุ่ม Sell บน UI (ลากมาใส่ใน Inspector)
    private RectTransform sellCursorRect;

    private bool isSellingMode = false;
    public bool IsSellingMode => isSellingMode;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
    }

    void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
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

        // ⭐ swap material ทุกชิ้นเป็น Transparent/Unlit ใหม่
        // เพื่อให้ SetGhostColor ทำงานได้แน่นอน ไม่ขึ้น warning
        var ghostMat = new Material(Shader.Find("Unlit/Color"));
        ghostMat.color = new Color(0f, 1f, 0f, 0.45f);

        // เปิด Transparency (สำหรับ Unlit/Color ต้องตั้งค่า render queue)
        ghostMat.SetFloat("_Mode", 3);
        ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMat.SetInt("_ZWrite", 0);
        ghostMat.DisableKeyword("_ALPHATEST_ON");
        ghostMat.EnableKeyword("_ALPHABLEND_ON");
        ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMat.renderQueue = 3000;

        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
            r.material = ghostMat;

        if (gridVisual != null) gridVisual.gameObject.SetActive(true);
    }

    private void SetGhostColor(bool canPlace)
    {
        if (ghost == null) return;
        Color col = canPlace
            ? new Color(0f, 1f, 0f, 0.45f)   // 🟢 โปร่งใสเขียว
            : new Color(1f, 0f, 0f, 0.45f);   // 🔴 โปร่งใสแดง

        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
            r.material.color = col;  // material นี้เป็น Unlit/Color ที่เราสร้างเอง ไม่มี warning
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

        // เล่นเสียงเมื่อวางสำเร็จ
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.SoundType.Place);
        }

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

    // ===== PHASE CHANGE HANDLER =====
    private void HandlePhaseChanged(GamePhase phase)
    {
        // ยกเลิกสถานะ Placing/Selling ทุกครั้งที่เปลี่ยนเฟส (reset state ให้สะอาด)
        if (isPlacing) CancelPlacement();
        if (isSellingMode) ToggleSellMode();

        if (phase == GamePhase.Combat)
        {
            // Planning→Combat: Minion อยู่เพื่อสู้ แค่ซ่อนปุ่ม Sell
            GameManager.SafeSetActive(sellButton, false, "PlacementManager");
        }
        else // Planning (Combat→Planning)
        {
            // Combat→Planning: ลบ Minion ทั้งหมด เริ่มรอบใหม่
            DespawnAllMinions();
            GameManager.SafeSetActive(sellButton, true, "PlacementManager");
        }
    }

    /// <summary>ทำลาย Minion ทั้งหมดที่วางบน Grid และเคลียร์ข้อมูลกริด</summary>
    private void DespawnAllMinions()
    {
        if (hexGrid == null) return;

        int width  = hexGrid.GridSize.x;
        int height = hexGrid.GridSize.y;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject obj = hexGrid.GetGridObject(x, y);
                if (obj != null)
                {
                    Destroy(obj);
                    hexGrid.RemoveGridObject(x, y);
                }
            }
        }

        Debug.Log("<color=orange>[PlacementManager]</color> All minions despawned.");
    }
}
