using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TooltipUI — Singleton ที่จัดการแสดง Tooltip ทั่วทั้งเกมครับ
///
/// วิธีใช้งาน:
///   1. สร้าง GameObject ในฉาก Canvas ชื่อ "TooltipUI"
///   2. ใส่ Script นี้เข้าไป
///   3. ลาก Panel (Background), titleText, contentText ให้ครบ
///   4. TooltipUI จะ Follow เมาส์อัตโนมัติครับ
///
/// ไฟล์อื่นเรียกใช้ผ่าน:
///   TooltipUI.Instance.Show("ชื่อ", "เนื้อหา");
///   TooltipUI.Instance.Hide();
/// </summary>
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel พื้นหลัง Tooltip (ใช้เพื่อปรับขนาดอัตโนมัติ)")]
    [SerializeField] private RectTransform tooltipPanel;

    [Tooltip("ข้อความชื่อการ์ด (หัวข้อ)")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("ข้อความรายละเอียดสถานะ")]
    [SerializeField] private TextMeshProUGUI contentText;

    [Header("Layout Settings")]
    [Tooltip("ระยะห่างระหว่างเมาส์กับ Tooltip (pixel)")]
    [SerializeField] private Vector2 offset = new Vector2(15f, 15f); // ขวาบนของเมาส์

    [Tooltip("ขอบกันชน ไม่ให้ Tooltip หลุดออกนอกจอ (pixel)")]
    [SerializeField] private float screenPadding = 10f;

    private Canvas rootCanvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rootCanvas  = GetComponentInParent<Canvas>();
        canvasRect  = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

        // ซ่อน Tooltip ตอนเริ่มเกมทันที
        if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        // ติดตามตำแหน่งเมาส์ตลอดเวลาที่ Tooltip โชว์อยู่
        if (tooltipPanel != null && tooltipPanel.gameObject.activeSelf)
        {
            FollowMouse();
        }
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>แสดง Tooltip พร้อมชื่อและรายละเอียดครับ</summary>
    public void Show(string title, string content)
    {
        if (tooltipPanel == null) return;

        if (titleText != null)   titleText.text   = title;
        if (contentText != null) contentText.text = content;

        // อัปเดต Layout ก่อนวาง เพื่อให้ขนาด Panel ถูกต้อง
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

        tooltipPanel.gameObject.SetActive(true);
        FollowMouse();
    }

    /// <summary>ซ่อน Tooltip ครับ</summary>
    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────

    private void FollowMouse()
    {
        if (tooltipPanel == null) return;

        Vector2 localPoint;

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && canvasRect != null)
        {
            // World Space Canvas: แปลงตำแหน่งเมาส์ผ่าน Camera
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                rootCanvas.worldCamera,
                out localPoint
            );
        }
        else
        {
            // Screen Space Overlay Canvas: ใช้ตำแหน่งตรงๆ
            localPoint = Input.mousePosition;

            if (rootCanvas != null && canvasRect != null)
            {
                // Scale ตาม Canvas Scale Factor
                float scaleFactor = rootCanvas.scaleFactor;
                localPoint /= scaleFactor;

                // แปลงจาก Screen Center เป็น Canvas Local
                Vector2 canvasSize = canvasRect.sizeDelta;
                localPoint -= canvasSize * 0.5f;
            }
        }

        // เพิ่ม Offset
        localPoint += offset;

        // Clamp ไม่ให้หลุดนอกจอ
        if (canvasRect != null)
        {
            Vector2 canvasSize   = canvasRect.sizeDelta;
            Vector2 panelSize    = tooltipPanel.sizeDelta;
            float halfCanvasW    = canvasSize.x * 0.5f;
            float halfCanvasH    = canvasSize.y * 0.5f;

            float minX = -halfCanvasW + screenPadding;
            float maxX =  halfCanvasW - panelSize.x - screenPadding;
            float minY = -halfCanvasH + panelSize.y + screenPadding;
            float maxY =  halfCanvasH - screenPadding;

            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);
        }

        tooltipPanel.anchoredPosition = localPoint;
    }
}