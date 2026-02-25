using UnityEngine;
using UnityEngine.UI;

public class AimCrosshair : MonoBehaviour
{
    [Header("Crosshair Panels")]
    [SerializeField] private GameObject normalPanel;
    [SerializeField] private GameObject fullChargePanel;

    [Header("Shrink Settings (Normal)")]
    [SerializeField] private RectTransform shrinkPart; 
    public float maxSpread = 100f;
    public float minSpread = 20f;

    [Header("Charge Settings")]
    public float maxChargeTime = 1.2f; // ใช้เวลากี่วินาทีถึงชาร์จเต็ม
    
    private float currentCharge = 0f;
    private bool isAiming = false;


    void Update()
    {
        if (!isAiming)
        {
            // --- สถานะปกติ (ไม่ได้ง้าง) ---
            ResetToIdle();
            return;
        }

        // เพิ่มค่าชาร์จตามเวลา
        currentCharge += Time.deltaTime;
        float progress = Mathf.Clamp01(currentCharge / maxChargeTime);

        if (progress < 1f)
        {
            // --- ช่วงกำลังชาร์จ (Normal) ---
            ShowNormal();

            // ทำเอฟเฟกต์หุบเป้า
            if (shrinkPart != null)
            {
                float currentSpread = Mathf.Lerp(maxSpread, minSpread, progress);
                shrinkPart.sizeDelta = new Vector2(currentSpread, currentSpread);
            }
        }
        else
        {
            // --- ชาร์จเต็ม 100% (Full Charge) ---
            ShowFullCharge();
        }
    }

    private void ResetToIdle()
    {
        ShowNormal();
        if (shrinkPart != null)
        {
            shrinkPart.sizeDelta = new Vector2(maxSpread, maxSpread);
        }
    }

    private void ShowNormal()
    {
        if (normalPanel != null && !normalPanel.activeSelf) normalPanel.SetActive(true);
        if (fullChargePanel != null && fullChargePanel.activeSelf) fullChargePanel.SetActive(false);
    }

    private void ShowFullCharge()
    {
        if (normalPanel != null && normalPanel.activeSelf) normalPanel.SetActive(false);
        if (fullChargePanel != null && !fullChargePanel.activeSelf) fullChargePanel.SetActive(true);
    }

    // ==================== Public Methods (เรียกจาก Archer.cs) ====================

    public void StartAim()
    {
        isAiming = true;
        currentCharge = 0f;
    }

    public void StopAim()
    {
        isAiming = false;
        currentCharge = 0f;
        
        // ไม่สั่ง SetActive(false) แล้วครับ เพื่อให้เป้าค้างอยู่ตลอด
        ResetToIdle();
    }

    /// <summary>
    /// คืนค่าความแม่นยำ 0.0 - 1.0 (0 = ต่ำสุด, 1 = สูงสุด/ชาร์จเต็ม)
    /// </summary>
    public float GetAccuracy()
    {
        return Mathf.Clamp01(currentCharge / maxChargeTime);
    }
}
