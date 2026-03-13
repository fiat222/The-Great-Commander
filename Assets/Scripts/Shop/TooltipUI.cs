using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    public enum TooltipSize { Small, Large }

    [Header("UI References")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;

    [Header("Layout Settings")]
    [SerializeField] private Vector2 offset = new Vector2(50f, 390f);
    [SerializeField] private float screenPadding = 10f;

    [Header("Tooltip Sizes")]
    [SerializeField] private Vector2 smallSize = new Vector2(333.2896f, 420f);   // ShopItemCard
    [SerializeField] private Vector2 largeSize = new Vector2(333.2896f, 505.4028f);  // UpgradeCard

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

        rootCanvas = GetComponentInParent<Canvas>();
        canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

        if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.gameObject.activeSelf)
            FollowMouse();
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>แสดง Tooltip — size ค่า default เป็น Small</summary>
    public void Show(string title, string content, TooltipSize size = TooltipSize.Large)
    {
        if (tooltipPanel == null) return;

        if (titleText != null)   titleText.text   = title;
        if (contentText != null) contentText.text = content;

        // ปรับขนาดตาม size
        tooltipPanel.sizeDelta = size == TooltipSize.Large ? largeSize : smallSize;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

        tooltipPanel.gameObject.SetActive(true);
        FollowMouse();
    }

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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                rootCanvas.worldCamera,
                out localPoint
            );
        }
        else
        {
            localPoint = Input.mousePosition;

            if (rootCanvas != null && canvasRect != null)
            {
                float scaleFactor = rootCanvas.scaleFactor;
                localPoint /= scaleFactor;

                Vector2 canvasSize = canvasRect.sizeDelta;
                localPoint -= canvasSize * 0.5f;
            }
        }

        localPoint += offset;

        if (canvasRect != null)
        {
            Vector2 canvasSize = canvasRect.sizeDelta;
            Vector2 panelSize  = tooltipPanel.sizeDelta;
            float halfCanvasW  = canvasSize.x * 0.5f;
            float halfCanvasH  = canvasSize.y * 0.5f;

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