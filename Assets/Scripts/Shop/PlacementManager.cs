using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;
    public System.Action<int> OnMoneyChanged;

    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private LayerMask buildLayer;
    [SerializeField] private int minionCost = 50;

    private GameObject ghost;
    private bool isPlacing = false;
    public bool IsPlacing => isPlacing;

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
            // ให้ overlay ตามเมาส์
            sellCursorRect.position = Input.mousePosition + new Vector3(40, -35, 0);

            // คลิกขวาออกจากโหมดขาย
            if (Input.GetMouseButtonDown(1))
                ToggleSellMode();
        }
    }

    // ===== BUY =====
    public void StartPlacing()
    {
        // ถ้าอยู่ในโหมดขาย ให้ปิดก่อน
        if (isSellingMode)
            ToggleSellMode();

        if (Money < minionCost) return;
        if (isPlacing) return;

        isPlacing = true;

        ghost = Instantiate(minionPrefab);

        foreach (Collider c in ghost.GetComponentsInChildren<Collider>())
            c.enabled = false;

        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
        {
            Color c = r.material.color;
            c.a = 0.5f;
            r.material.color = c;
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
        if (Money < minionCost) return;

        Money -= minionCost;
        OnMoneyChanged?.Invoke(Money);

        Instantiate(minionPrefab, pos, Quaternion.identity);

        Destroy(ghost);
        isPlacing = false;
    }

    // ===== CANCEL =====
    public void CancelPlacement()
    {
        if (!isPlacing) return;

        Destroy(ghost);
        isPlacing = false;
    }

    // ===== SELL FUNCTION =====
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
