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
    [Tooltip("เอฟเฟกต์ตอนฐาน/ป้อมหลักถูกทำลาย")]
    public GameObject deathVfxPrefab;
    [Tooltip("เวลาที่ให้เอฟเฟกต์อยู่บนจอก่อนถูกลบออก")]
    public float deathVfxDuration = 2f;
    [Tooltip("ดีเลย์ก่อนแสดง Game Over หลังฐานหลักตาย")]
    public float gameOverDelay = 2.5f;
    [Tooltip("เวลาที่รอกล้องแพนไปถึงจุดหมายก่อนเริ่มระเบิด/จม (Cinematic)")]
    public float cameraArriveDelay = 0.8f;
    [Tooltip("ติ๊กถูกถ้าฐานหลักนี้ควรทำให้เกมจบเมื่อ HP หมด")]
    public bool triggerGameOverOnDeath = true;
    [Tooltip("ถ้าติ๊ก ป้อมจะค่อยๆ จมลงตอนถูกทำลาย")]
    public bool sinkOnDeath = true;
    [Tooltip("ระยะที่ป้อมจะจมลง (หน่วยเป็นเมตร)")]
    public float sinkDistance = 4f;
    [Tooltip("เวลาที่ใช้ให้ป้อมจมลงครบระยะ")]
    public float sinkDuration = 2f;

    [Header("UI (Optional)")]
    [Tooltip("ใส่ Canvas ของหลอดเลือดที่นี่")]
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

        if (healthCanvas != null && mainCamera != null)
            healthCanvas.transform.rotation = mainCamera.transform.rotation;

        if (smoothRect != null && fillRect != null)
        {
            float target = GetNormalizedHealth();

            if (displayedSmoothHealth > target)
                displayedSmoothHealth = Mathf.Lerp(displayedSmoothHealth, target, Time.deltaTime * smoothSpeed);
            else
                displayedSmoothHealth = target;

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

        Debug.Log($"<color=orange>[HP]</color> {gameObject.name} โดนตี! เลือดเหลือ {currentHealth}/{maxHealth}");

        UpdateHealthUI(syncSmoothImmediately: false);
        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
            Die();
    }

    private void UpdateHealthUI(bool syncSmoothImmediately)
    {
        float normalized = GetNormalizedHealth();

        if (fillRect != null)
            ApplyBarValue(fillRect, normalized);

        if (syncSmoothImmediately)
        {
            displayedSmoothHealth = normalized;
            if (smoothRect != null)
                ApplyBarValue(smoothRect, normalized);
        }

        UpdateHealthText();
    }

    private void SyncUIInstant() => UpdateHealthUI(syncSmoothImmediately: true);

    private float GetNormalizedHealth()
        => Mathf.Approximately(maxHealth, 0f) ? 0f : Mathf.Clamp01(currentHealth / maxHealth);

    private void CacheBarRects()
    {
        fillRect   = healthBarFill   != null ? healthBarFill.rectTransform   : null;
        smoothRect = healthBarSmooth != null ? healthBarSmooth.rectTransform : null;
    }

    private static void ConfigureBarRect(RectTransform r)
    {
        if (r == null) return;
        r.anchorMin        = new Vector2(0f, 0f);
        r.anchorMax        = new Vector2(1f, 1f);
        r.pivot            = new Vector2(0f, 0.5f);
        r.anchoredPosition = Vector2.zero;
    }

    private static void ApplyBarValue(RectTransform r, float normalized)
    {
        if (r == null) return;
        Vector3 s = r.localScale;
        s.x = Mathf.Clamp01(normalized);
        s.y = 1f;
        s.z = 1f;
        r.localScale = s;
    }

    public void ForceSetHealth(float current, float max)
    {
        maxHealth     = Mathf.Max(1f, max);
        currentHealth = Mathf.Clamp(current, 0, max);
        SyncUIInstant();
    }

    /// <summary>
    /// เล่นแอนิเมชันจมลงของวัตถุที่กำหนด
    /// เรียกใช้ได้จากทั้ง HealthSystem และ BaseHealth
    /// </summary>
    public IEnumerator ExecuteSinkAnimation(Transform target)
    {
        if (target == null || !sinkOnDeath || sinkDuration <= 0f || Mathf.Abs(sinkDistance) <= 0.01f)
            yield break;

        Vector3 start   = target.position;
        Vector3 end     = start + Vector3.down * sinkDistance;
        float   elapsed = 0f;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            target.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / sinkDuration));
            yield return null;
        }
    }

    private void Die()
    {
        isDead = true;
        OnDie?.Invoke();

        if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);
        Debug.Log($"<color=red>[Dead]</color> {gameObject.name} ถูกทำลายแล้ว!");

        if (isMainBase && triggerGameOverOnDeath)
        {
            if (!mainBaseDeathSequenceStarted)
            {
                mainBaseDeathSequenceStarted = true;
                StartCoroutine(MainBaseDeathSequence());
            }
            return;
        }

        if (GetComponent<EnemyAI>() != null || GetComponent<ImpAI>() != null)
            return;

        Destroy(gameObject, 0.5f);
    }

    private void UpdateHealthText()
    {
        if (healthText == null) return;
        healthText.text  = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
        healthText.color = GetNormalizedHealth() <= 0.25f ? Color.red : Color.white;
    }

    private IEnumerator MainBaseDeathSequence()
    {
        // 1) กล้องก่อน
        if (CameraManager.Instance != null)
            CameraManager.Instance.FocusInitialView();

        // 2) รอกล้องแพนไปถึง
        if (cameraArriveDelay > 0f)
            yield return new WaitForSeconds(cameraArriveDelay);

        // 3) VFX ระเบิด
        if (deathVfxPrefab != null)
        {
            GameObject vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
            if (deathVfxDuration > 0f) Destroy(vfx, deathVfxDuration);
        }

        // 4) ป้อมจม
        yield return ExecuteSinkAnimation(transform);

        // 5) delay ก่อน Game Over
        if (gameOverDelay > 0f)
            yield return new WaitForSeconds(gameOverDelay);

        // 6) Game Over ตามโหมด
        if (SoloEnemyTracker.Instance != null)
            SoloEnemyTracker.Instance.NotifyPlayerDied();
        else if (SoloGameManager.Instance != null)
            SoloGameManager.Instance.OnGameEnded();
        else if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnded();
        else if (EnemyTracker.Instance != null && SoloGameManager.Instance != null)
        {
            // ทำงานเฉพาะ Solo เท่านั้น
            EnemyTracker.Instance.ShowGameResultClientRpc(0);
        }
    }
}