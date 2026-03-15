using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hover Effect สไตล์ Highlight Bar
/// มี Background Image ที่ Fade in/out เมื่อ Hover
/// ผูก Script นี้กับ Button แต่ละปุ่ม
///
/// โครงสร้าง Button ที่แนะนำ:
///   Button (MenuButtonHover อยู่ที่นี่)
///     ├── Background (Image — สี่เหลี่ยมทึ่บ ครึ่งโปร่งใส)
///     └── Text (TextMeshProUGUI)
/// </summary>
public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Background Highlight")]
    [Tooltip("Image สี่เหลี่ยมด้านหลัง Text — Fade in เมื่อ Hover")]
    public Image backgroundImage;
    [Tooltip("สีของ Background เมื่อ Hover")]
    public Color highlightColor = new Color(1f, 1f, 1f, 0.898f); // #FFFFFF A=229
    [Tooltip("ความเร็ว Fade")]
    public float fadeSpeed = 8f;

    [Header("Text Scale (Pop-up)")]
    [Tooltip("เปิด/ปิด เอ็ฟเฟกต์ Text ขยายเมื่อ Hover")]
    public bool enableScale = true;
    [Tooltip("ขนาด Scale เมื่อ Hover (1 = ปกติ)")]
    public float hoverScale = 1.1f;
    [Tooltip("ความเร็ว Scale")]
    public float scaleSpeed = 10f;

    [Header("Text Color")]
    public bool enableColorChange = true;
    [Tooltip("สีปกติ")]
    public Color normalColor = new Color(0.75f, 0.75f, 0.75f, 1f);   // เทาอ่อน
    [Tooltip("สีเมื่อ Hover")]
    public Color hoverColor  = Color.white;

    [Header("References")]
    [Tooltip("ถ้าไม่กำหนด จะหาอัตโนมัติใน Children")]
    [SerializeField] private TextMeshProUGUI buttonText;

    // ─────────────────────────────────────────
    private Color targetBGColor;
    private Color clearColor;
    private Vector3 textOriginalScale;
    private Vector3 textTargetScale;
    private bool isSelected; // เพิ่มสถานะว่าถูกเลือกอยู่หรือไม่

    private void Awake()
    {
        clearColor    = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
        targetBGColor = clearColor;

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            buttonText.color = normalColor;
            textOriginalScale = buttonText.transform.localScale;
            textTargetScale   = textOriginalScale;
        }

        if (backgroundImage != null)
            backgroundImage.color = clearColor;
    }

    private void Update()
    {
        // Smooth Fade Background
        if (backgroundImage != null)
            backgroundImage.color = Color.Lerp(
                backgroundImage.color, targetBGColor, Time.deltaTime * fadeSpeed);

        // Smooth Scale Text
        if (enableScale && buttonText != null)
            buttonText.transform.localScale = Vector3.Lerp(
                buttonText.transform.localScale, textTargetScale, Time.deltaTime * scaleSpeed);
    }

    // ─────────────────────────────────────────
    //  Public Methods
    // ─────────────────────────────────────────
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (isSelected)
        {
            targetBGColor = highlightColor;
            if (enableColorChange && buttonText != null) buttonText.color = hoverColor;
            if (enableScale && buttonText != null) textTargetScale = textOriginalScale * hoverScale;
        }
        else
        {
            targetBGColor = clearColor;
            if (enableColorChange && buttonText != null) buttonText.color = normalColor;
            if (enableScale && buttonText != null) textTargetScale = textOriginalScale;
        }
    }

    // ─────────────────────────────────────────
    //  Pointer Events
    // ─────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetBGColor = highlightColor;

        if (enableColorChange && buttonText != null)
            buttonText.color = hoverColor;

        if (enableScale && buttonText != null)
            textTargetScale = textOriginalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ถ้าถูกเลือกอยู่ ไม่ต้องคืนค่ากลับเป็นสีปกติ
        if (isSelected) return;

        targetBGColor = clearColor;

        if (enableColorChange && buttonText != null)
            buttonText.color = normalColor;

        if (enableScale && buttonText != null)
            textTargetScale = textOriginalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // กระพริบ Background นิดนึงเมื่อคลิก
        if (backgroundImage != null)
            backgroundImage.color = new Color(
                highlightColor.r, highlightColor.g, highlightColor.b, 0.4f);

        // กระตุก Scale นิดนึง
        if (enableScale && buttonText != null)
            buttonText.transform.localScale = textOriginalScale * (hoverScale * 0.9f);
    }

    private void OnDisable()
    {
        // Reset เมื่อปิด Panel
        isSelected = false;
        targetBGColor = clearColor;

        if (backgroundImage != null)
            backgroundImage.color = clearColor;

        if (enableColorChange && buttonText != null)
            buttonText.color = normalColor;

        if (enableScale && buttonText != null)
        {
            textTargetScale = textOriginalScale;
            buttonText.transform.localScale = textOriginalScale;
        }
    }
}
