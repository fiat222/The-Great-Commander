using System.Collections;
using UnityEngine;

/// <summary>
/// แนบไว้ที่ YouWinUI / YouLostUI
/// เมื่อ SetActive(true) จะเล่น animation หมุน + ขยายแบบ elastic pop-in
/// </summary>
public class UIPopAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("ใช้เวลากี่วินาทีจากเล็กสุดถึงขนาดจริง")]
    public float duration = 0.6f;

    [Tooltip("องศาที่หมุนตอนขยาย (เช่น 360 = หมุนรอบ)")]
    public float rotateDegrees = 360f;

    [Tooltip("Overshoot สำหรับ elastic bounce (ค่าปกติ 1.70158 = 튀ขึ้นนิดหน่อย)")]
    public float overshoot = 1.70158f;

    private Vector3 originalScale;
    private Coroutine activeAnim;

    void Awake()
    {
        // บันทึก scale จริงที่ตั้งไว้ใน Inspector
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        // เริ่ม pop animation ทุกครั้งที่ถูกเปิด
        if (activeAnim != null) StopCoroutine(activeAnim);
        activeAnim = StartCoroutine(PlayPop());
    }

    void OnDisable()
    {
        if (activeAnim != null)
        {
            StopCoroutine(activeAnim);
            activeAnim = null;
        }
        // Reset กลับขนาดจริงก่อนปิด (เผื่อถูกเปิดครั้งหน้า)
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;
    }

    private IEnumerator PlayPop()
    {
        float elapsed = 0f;

        // เริ่มจากขนาด 0 หมุน -rotateDegrees องศา
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0f, 0f, -rotateDegrees);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Back-out easing (elastic overshoot)
            float s = EaseOutBack(t, overshoot);

            transform.localScale    = originalScale * s;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-rotateDegrees, 0f, t));

            yield return null;
        }

        // snap ค่าสุดท้ายให้เป๊ะ
        transform.localScale    = originalScale;
        transform.localRotation = Quaternion.identity;
        activeAnim = null;
    }

    // Back-out easing: ขยายเกินนิดหนึ่งแล้วดีด กลับมาขนาดจริง
    private static float EaseOutBack(float t, float overshoot)
    {
        float c1 = overshoot;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
