using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class HealthSystem : MonoBehaviour
{
    [Header("Main Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Tooltip("ติ๊กถูกถ้าตัวนี้คือฐานแม่ (ป้อมหลัก) ถ้าตายแล้วเกมจบ")]
    public bool isMainBase = false;

    [Header("UI (Optional - ไม่ใส่ก็ได้)")]
    [Tooltip("ใส่ Canvas ของหลอดเลือดที่นี่ (ถ้าไม่มี มันจะไม่พยายามวาด UI)")]
    public Canvas healthCanvas;
    public Image healthBarFill;
    public Image healthBarSmooth;
    public float smoothSpeed = 5f;

    [Header("Events")]
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDie;

    private bool isDead = false;
    public bool IsDead => isDead;

    private Camera mainCamera;
    private RectTransform fillRect;
    private RectTransform smoothRect;
    private float displayedSmoothHealth = 1f;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        mainCamera = Camera.main;

        CacheBarRects();
        ConfigureBarRect(fillRect);
        ConfigureBarRect(smoothRect);
        SyncUIInstant();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // 1. หมุนป้ายเลือดให้หันเข้าหากล้องเสมอ (ถ้ามี UI)
        if (healthCanvas != null && mainCamera != null)
        {
            healthCanvas.transform.rotation = mainCamera.transform.rotation;
        }

        // 2. จัดการเรื่องหลอดเลือดไหลแบบ Smooth (ถ้ามี UI)
        if (smoothRect != null && fillRect != null)
        {
            float targetNormalizedHealth = GetNormalizedHealth();

            if (displayedSmoothHealth > targetNormalizedHealth)
            {
                displayedSmoothHealth = Mathf.Lerp(displayedSmoothHealth, targetNormalizedHealth, Time.deltaTime * smoothSpeed);
            }
            else
            {
                displayedSmoothHealth = targetNormalizedHealth;
            }

            ApplyBarValue(smoothRect, displayedSmoothHealth);
        }
    }

    public void AssignUIReferences(Canvas canvas, Image fill, Image smooth)
    {
        healthCanvas = canvas;
        healthBarFill = fill;
        healthBarSmooth = smooth;
        CacheBarRects();
        ConfigureBarRect(fillRect);
        ConfigureBarRect(smoothRect);
        SyncUIInstant();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // แสดงผลผ่าน Console (แทน BaseHealth เดิม)
        Debug.Log($"<color=orange>[HP]</color> {gameObject.name} โดนตี! เลือดเหลือ {currentHealth}/{maxHealth}");

        UpdateHealthUI(syncSmoothImmediately: false);
        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI(bool syncSmoothImmediately)
    {
        float normalizedHealth = GetNormalizedHealth();

        if (fillRect != null)
        {
            ApplyBarValue(fillRect, normalizedHealth);
        }

        if (syncSmoothImmediately)
        {
            displayedSmoothHealth = normalizedHealth;

            if (smoothRect != null)
            {
                ApplyBarValue(smoothRect, normalizedHealth);
            }
        }
    }

    private void SyncUIInstant()
    {
        UpdateHealthUI(syncSmoothImmediately: true);
    }

    private float GetNormalizedHealth()
    {
        return Mathf.Approximately(maxHealth, 0f)
            ? 0f
            : Mathf.Clamp01(currentHealth / maxHealth);
    }

    private void CacheBarRects()
    {
        fillRect = healthBarFill != null ? healthBarFill.rectTransform : null;
        smoothRect = healthBarSmooth != null ? healthBarSmooth.rectTransform : null;
    }

    private static void ConfigureBarRect(RectTransform barRect)
    {
        if (barRect == null)
            return;

        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.anchoredPosition = Vector2.zero;
    }

    private static void ApplyBarValue(RectTransform barRect, float normalizedHealth)
    {
        if (barRect == null)
            return;

        Vector3 scale = barRect.localScale;
        scale.x = Mathf.Clamp01(normalizedHealth);
        scale.y = 1f;
        scale.z = 1f;
        barRect.localScale = scale;
    }

    /// <summary>
    /// ให้ภายนอก (เช่น BaseHealth) สั่งเซ็ตเลือดตรงๆ โดยไม่ต้องผ่าน TakeDamage
    /// HealthSystem จะจัดการ UI, Billboard, Smooth ให้ทั้งหมด
    /// </summary>
    public void ForceSetHealth(float current, float max)
    {
        maxHealth = Mathf.Max(1f, max);
        currentHealth = Mathf.Clamp(current, 0, max);
        SyncUIInstant();
    }

    private void Die()
    {
        isDead = true;
        OnDie?.Invoke();
        
        if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);

        Debug.Log($"<color=red>[Dead]</color> {gameObject.name} ถูกทำลายแล้ว!");

        if (isMainBase)
        {
            Debug.LogError("‼️ ฐานหลักพังแล้ว! จบเกม (Game Over) ‼️");
        }
        else
        {
            // ถ้ามี EnemyAI หรือ ImpAI ให้ AI จัดการ Destroy เอง (มี animation + PowerBall)
            // HealthSystem ไม่ต้อง Destroy ซ้ำ
            if (GetComponent<EnemyAI>() != null || GetComponent<ImpAI>() != null)
                return;

            Destroy(gameObject, 0.5f);
        }
    }
}
