using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class HealthSystem : MonoBehaviour
{
    [Header("Main Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Tooltip("ติ๊กถูกถ้าตัวนี้คือฐานแม่ (ป้อมหลัก) ถ้าตายแล้วเกมจบ")]
    public bool isMainBase = false;

    [Header("Death Presentation (Main Base)")]
    [Tooltip("เอฟเฟกต์ตอนฐาน/ป้อมหลักถูกทำลาย (เช่น ระเบิด, พัง)")]
    public GameObject deathVfxPrefab;
    [Tooltip("เวลาที่ให้เอฟเฟกต์อยู่บนจอก่อนถูกลบออก")]
    public float deathVfxDuration = 2f;
    [Tooltip("ดีเลย์ก่อนแสดง Game Over หลังฐานหลักตาย")]
    public float gameOverDelay = 2.5f;
    [Tooltip("ติ๊กถูกถ้าฐานหลักนี้ควรทำให้เกมจบเมื่อ HP หมด")]
    public bool triggerGameOverOnDeath = true;
    [Tooltip("ถ้าติ๊ก ป้อมจะค่อย ๆ จมลงตอนถูกทำลาย (เอฟเฟกต์พัง)")]
    public bool sinkOnDeath = true;
    [Tooltip("ระยะที่ป้อมจะจมลง (หน่วยเป็นเมตร)")]
    public float sinkDistance = 4f;
    [Tooltip("เวลาที่ใช้ให้ป้อมจมลงครบระยะ")]
    public float sinkDuration = 2f;

    [Header("UI (Optional - ไม่ใส่ก็ได้)")]
    [Tooltip("ใส่ Canvas ของหลอดเลือดที่นี่ (ถ้าไม่มี มันจะไม่พยายามวาด UI)")]
    public Canvas healthCanvas;
    public Image healthBarFill;
    public Image healthBarSmooth;
    public float smoothSpeed = 5f;
    
    [Header("HP Text (Optional)")]
    [Tooltip("TextMeshPro สำหรับแสดงเลข HP (เช่น 150/150)")]
    public TextMeshProUGUI healthText;

    [Header("Events")]
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDie;

    private bool isDead = false;
    public bool IsDead => isDead;

    private Camera mainCamera;
    private RectTransform fillRect;
    private RectTransform smoothRect;
    private float displayedSmoothHealth = 1f;
    private bool mainBaseDeathSequenceStarted = false;

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
        
        // อัปเดตข้อความ HP
        UpdateHealthText();
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

        // กรณีฐานหลัก (main base): ใช้ sequence แบบ Cinematic + Spectator + GameOver
        if (isMainBase && triggerGameOverOnDeath)
        {
            if (!mainBaseDeathSequenceStarted)
            {
                mainBaseDeathSequenceStarted = true;
                StartCoroutine(MainBaseDeathSequence());
            }
            return;
        }

        // กรณีทั่วไป: ปล่อยให้ AI หรือระบบอื่นจัดการ Destroy เหมือนเดิม
        if (GetComponent<EnemyAI>() != null || GetComponent<ImpAI>() != null)
            return;

        Destroy(gameObject, 0.5f);
    }
    
    private void UpdateHealthText()
    {
        if (healthText == null) return;
        
        // จัดรูปแบบข้อความ: current/max
        string formattedText = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
        healthText.text = formattedText;
        
        // เปลี่ยนสีตาม HP
        float normalizedHealth = GetNormalizedHealth();
        if (normalizedHealth <= 0.25f)
        {
            healthText.color = Color.red;
        }
        else
        {
            healthText.color = Color.white;
        }
    }

    /// <summary>
    /// ลำดับตอนฐานหลักตาย:
    /// 1) เล่นเอฟเฟกต์ระเบิด/พัง
    /// 2) โฟกัสกล้อง Spectator ไปที่ฐานหลัก และล็อคมุมกล้อง
    /// 3) รอให้ผู้เล่นชมฉากสักพัก
    /// 4) เรียก Game Over ผ่านระบบที่มีอยู่ (Solo / Network)
    /// </summary>
    private IEnumerator MainBaseDeathSequence()
    {
        // 1) เอฟเฟกต์ทำลายฐานหลัก
        if (deathVfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
            if (deathVfxDuration > 0f)
            {
                Destroy(vfxInstance, deathVfxDuration);
            }
        }

        // 2) โฟกัสกล้อง Spectator ไปที่ฐานหลัก (ถ้ามี CameraManager)
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.FocusSpectator(transform);
        }

        // 3) ทำเอฟเฟกต์ให้ป้อมค่อย ๆ จมลง (เหมือนพังลง) ถ้าถูกเปิดใช้งาน
        if (sinkOnDeath && sinkDuration > 0f && Mathf.Abs(sinkDistance) > 0.01f)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos   = startPos + Vector3.down * sinkDistance;
            float elapsed    = 0f;

            while (elapsed < sinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sinkDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
        }

        // 4) รอให้ผู้เล่นชมฉากสักพักก่อนจบเกม (ดีเลย์ Game Over)
        if (gameOverDelay > 0f)
        {
            yield return new WaitForSeconds(gameOverDelay);
        }

        // 5) แจ้งระบบ Game Over ตามโหมดที่ใช้งานอยู่
        // Solo Mode
        if (SoloEnemyTracker.Instance != null)
        {
            SoloEnemyTracker.Instance.NotifyPlayerDied();
        }
        else if (SoloGameManager.Instance != null)
        {
            SoloGameManager.Instance.OnGameEnded();
        }
        // Network / PvP Mode: ให้ GameManager + EnemyTracker จัดการ UI/Result ต่อ
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameEnded();
        }
    }
}
