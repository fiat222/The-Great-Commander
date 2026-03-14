using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CursorUI : MonoBehaviour
{
    // ⭐ Singleton Pattern เพื่อให้เรียกใช้ง่ายและป้องกันตัวซ้ำ
    public static CursorUI Instance { get; private set; }

    [Header("Custom Cursor Settings")]
    public Texture2D defaultCursor;
    public Texture2D hoverCursor;
    public Vector2 hotSpot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    [Header("Click VFX Settings")]
    public GameObject clickVFXPrefab;
    public Transform vfxParent;

    private bool _isHovering = false;

    void Awake()
    {
        // ⭐ ระบบจัดการ DontDestroyOnLoad และป้องกันตัวซ้ำ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // สั่งให้ไม่ถูกทำลายเมื่อเปลี่ยนซีน
        }
        else
        {
            Destroy(gameObject); // ถ้ามีตัวเดิมอยู่แล้ว ให้ทำลายตัวใหม่ที่เพิ่งเกิดทิ้ง
            return;
        }
    }

    void Start()
    {
        ApplyDefaultCursor();
    }

    void Update()
    {
        HandleCursorVisuals();

        if (Input.GetMouseButtonDown(0))
        {
            if (AudioManager.Instance != null && IsPointerOverUIButton())
            {
                AudioManager.Instance.PlaySound(AudioManager.SoundType.Click);
            }

            // เช็คสถานะ Phase (ใส่ null check ไว้แล้ว ปลอดภัยแม้ในหน้าเมนูที่ไม่มี Manager)
            bool isMultiplayerCombat = (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Combat);
            bool isSoloCombat = (SoloGameManager.Instance != null && SoloGameManager.Instance.CurrentPhase == GamePhase.Combat);

            bool canPlayVFX = !isMultiplayerCombat && !isSoloCombat;

            if (canPlayVFX && clickVFXPrefab != null)
            {
                SpawnClickVFX();
            }
        }
    }

    private void HandleCursorVisuals()
    {
        bool currentlyOverButton = IsPointerOverUIButton();

        if (currentlyOverButton != _isHovering)
        {
            _isHovering = currentlyOverButton;
            if (_isHovering && hoverCursor != null)
                Cursor.SetCursor(hoverCursor, hotSpot, cursorMode);
            else
                ApplyDefaultCursor();
        }
    }

    private void SpawnClickVFX()
    {
        GameObject vfx = null;
        Vector3 spawnPos = Input.mousePosition;

        // ในกรณี DontDestroyOnLoad ถ้า vfxParent (Canvas) ถูกทำลายไปในซีนเก่า 
        // เราต้องเช็ค null และอาจจะต้องหา Canvas ใหม่ในซีนปัจจุบัน
        if (vfxParent != null)
        {
            vfx = Instantiate(clickVFXPrefab, spawnPos, Quaternion.identity, vfxParent);
        }
        else
        {
            // ถ้าไม่มี Parent หรือ Parent หายไป ให้หา Main Camera ในซีนนั้นๆ
            if (Camera.main != null)
                spawnPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));

            vfx = Instantiate(clickVFXPrefab, spawnPos, Quaternion.identity);
        }

        if (vfx != null) Destroy(vfx, 1.5f);
    }

    public void ApplyDefaultCursor()
    {
        if (defaultCursor != null)
            Cursor.SetCursor(defaultCursor, hotSpot, cursorMode);
    }

    private bool IsPointerOverUIButton()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null)
                return true;
        }
        return false;
    }
}