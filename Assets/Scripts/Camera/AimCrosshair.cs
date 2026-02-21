using UnityEngine;
using UnityEngine.UI;

public class AimCrosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public RectTransform crosshairRect;     // Image ของ crosshair
    public float maxSize = 120f;            // ขนาดตอนเริ่มเล็ง (กว้าง)
    public float minSize = 20f;             // ขนาดเล็กสุด (เล็งนิ่ง)
    public float shrinkTime = 1.5f;         // ใช้เวลากี่วินาทีจึงหดถึง minSize

    [Header("Color")]
    public Image crosshairImage;
    public Color defaultColor = Color.white;
    public Color readyColor = new Color(1f, 0.8f, 0f); // เหลืองทอง = พร้อมยิง

    private float aimTimer = 0f;
    private bool isShowing = false;

    private void Start()
    {
        if (crosshairRect != null)
            crosshairRect.gameObject.SetActive(true);
    }

    // เรียกจาก PlayerController
    public void StartAim()
    {
        aimTimer = 0f;
        isShowing = true;
    }

    public void StopAim()
    {
        isShowing = false;
        aimTimer = 0f;

        // รีเซ็ตขนาดกลับไปใหญ่สุด
        if (crosshairRect != null)
            crosshairRect.sizeDelta = new Vector2(maxSize, maxSize);

        // รีเซ็ตสี
        if (crosshairImage != null)
            crosshairImage.color = defaultColor;
    }

    // คืนค่า accuracy 0-1 (0 = เพิ่งเล็ง, 1 = เล็งเต็มแล้ว)
    public float GetAccuracy()
    {
        return Mathf.Clamp01(aimTimer / shrinkTime);
    }

    private void Update()
    {
        if (!isShowing) return;

        aimTimer += Time.deltaTime;

        float accuracy = GetAccuracy();

        // หดขนาด crosshair
        float currentSize = Mathf.Lerp(maxSize, minSize, accuracy);
        if (crosshairRect != null)
            crosshairRect.sizeDelta = new Vector2(currentSize, currentSize);

        // เปลี่ยนสีเมื่อพร้อมยิง
        if (crosshairImage != null)
            crosshairImage.color = Color.Lerp(defaultColor, readyColor, accuracy);
    }
}