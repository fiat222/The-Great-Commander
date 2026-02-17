using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
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

    void Start()
    {
        if (sellCursorOverlay != null)
        {
            sellCursorRect = sellCursorOverlay.GetComponent<RectTransform>();
            sellCursorOverlay.SetActive(false);
        }
    }

    void Update()
    {
        if (isPlacing)
            HandlePlacing();

        if (isSellingMode)
        {
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

        // ปิด collider
        foreach (Collider c in ghost.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // ทำโปร่งใส
        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
        {
            Color col = r.material.color;
            col.a = 0.5f;
            r.material.color = col;
        }
    }

    void HandlePlacing()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildLayer))
        {
            ghost.transform.position = hit.point;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (Physics.Raycast(ray, out hit, 100f, buildLayer))
            {
                PlaceMinion(hit.point);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }
    void PlaceMinion(Vector3 pos)
    {
        if (currentMinionData == null) return;
        if (Money < currentMinionData.cost) return;

        Money -= currentMinionData.cost;
        OnMoneyChanged?.Invoke(Money);

        GameObject obj = Instantiate(currentMinionData.prefab, pos, Quaternion.identity);

        // ⭐ เพิ่ม 4 บรรทัดนี้
        SellMinion sell = obj.GetComponent<SellMinion>();
        if (sell != null)
            sell.Setup(currentMinionData);

        Destroy(ghost);
        isPlacing = false;
        currentMinionData = null;
    }

    // ===== CANCEL =====
    public void CancelPlacement()
    {
        if (!isPlacing) return;

        Destroy(ghost);
        isPlacing = false;
        currentMinionData = null;
    }

    // ===== SELL =====
    public void SellMinion(int value)
    {
        Money += value;
        OnMoneyChanged?.Invoke(Money);
    }

    // ===== TOGGLE SELL MODE =====
    public void ToggleSellMode()
    {
        isSellingMode = !isSellingMode;

        if (sellCursorOverlay != null)
            sellCursorOverlay.SetActive(isSellingMode);
    }
}
