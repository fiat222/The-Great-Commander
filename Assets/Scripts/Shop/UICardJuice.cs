using UnityEngine;
using UnityEngine.EventSystems;

public class UICardJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    [Tooltip("ขนาดตอนเอาเมาส์ชี้")]
    public float hoverScale = 1.05f;
    [Tooltip("ขนาดตอนถูกคลิก")]
    public float clickScale = 0.95f;
    [Tooltip("ความลื่นไหลของการปรับขนาด")]
    public float smoothSpeed = 15f;

    [Header("3D Tilt Settings")]
    public bool enableTilt = true;
    [Tooltip("องศาการเอียงสูงสุด")]
    public float maxTiltAngle = 10f;
    [Tooltip("ความลื่นไหลตอนเอียง")]
    public float tiltSpeed = 15f;

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 targetScale;
    private Quaternion targetRotation;

    private RectTransform rectTransform;
    private bool isHovering = false;
    private bool isClicking = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
        
        targetScale = originalScale;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        // ทำการค่อยๆ ปรับขนาด (Smooth Scale)
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * smoothSpeed);

        // ทำการค่อยๆ ปรับความเอียง (Smooth Tilt)
        if (enableTilt)
        {
            if (isHovering && !isClicking)
            {
                // คำนวณหาตำแหน่งเมาส์บน UI
                Vector2 localMousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out localMousePos);
                
                // หาอัตราส่วนแกน X และ Y (ช่วงจุดกึ่งกลางคือ 0)
                float xPct = localMousePos.x / (rectTransform.rect.width * 0.5f);
                float yPct = localMousePos.y / (rectTransform.rect.height * 0.5f);

                xPct = Mathf.Clamp(xPct, -1f, 1f);
                yPct = Mathf.Clamp(yPct, -1f, 1f);

                // สลับแกน X/Y สำหรับการหมุนให้ถูกต้องตามแกน 3D
                targetRotation = originalRotation * Quaternion.Euler(yPct * maxTiltAngle, -xPct * maxTiltAngle, 0f);
            }
            else
            {
                targetRotation = originalRotation; // กลับสู่สภาพเดิม
            }

            rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (!isClicking) targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (!isClicking) targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isClicking = true;
        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isClicking = false;
        targetScale = isHovering ? (originalScale * hoverScale) : originalScale;
    }
}
