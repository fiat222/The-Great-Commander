using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events; // สำหรับใช้งาน Event

public class TowerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 1000f;
    private float currentHealth;

    [Header("UI (Canvas แบบ World Space)")]
    public Canvas healthCanvas;
    public Image healthBarFill;
    public Image healthBarSmooth; // สำหรับทำเลือดลดแบบค่อยเป็นค่อยไปลดตามหลัง
    public float smoothSpeed = 5f;

    [Header("Death Presentation")]
    [Tooltip("เอฟเฟกต์ตอนป้อมถูกทำลาย (เช่น ระเบิด, พัง)")]
    public GameObject deathVfxPrefab;
    [Tooltip("เวลาที่ให้เอฟเฟกต์อยู่บนจอก่อนถูกลบออก")]
    public float deathVfxDuration = 2f;
    [Tooltip("ดีเลย์ก่อนแสดง Game Over หลังป้อมพัง")]
    public float gameOverDelay = 2.5f;
    [Tooltip("ติ๊กถูกถ้าให้ป้อมนี้เป็นตัวจบเกมเมื่อถูกทำลาย")]
    public bool triggerGameOverOnDeath = true;

    [Header("Events")]
    public UnityEvent OnTakeDamage;
    public UnityEvent OnTowerDestroyed;

    private bool isDead = false;
    private Camera mainCamera;

    void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        // อัปเดตหลอดเลือดตอนเริ่มเกม
        UpdateHealthUI();
    }

    void Update()
    {
        // ทำให้ Canvas หันหน้าเข้าหากล้องตลอดเวลา (Billboard)
        if (healthCanvas != null && mainCamera != null)
        {
            healthCanvas.transform.rotation = mainCamera.transform.rotation;
        }

        // ทำให้หลอดเลือดสีแดง(หลอดตาม) ค่อยๆ ลดลงตามหลอดเลือดจริง แบบสมูท
        if (healthBarSmooth != null && healthBarFill != null)
        {
            if (healthBarSmooth.fillAmount > healthBarFill.fillAmount)
            {
                healthBarSmooth.fillAmount = Mathf.Lerp(healthBarSmooth.fillAmount, healthBarFill.fillAmount, Time.deltaTime * smoothSpeed);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        // แจ้งเตือนว่าโดนตี (อาจจะเอาไปต่อกับระบบเสียง หรือ Partical กระพริบสีแดง)
        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            // คำนวณเป็นเปอร์เซ็นต์ (0.0 - 1.0) สำหรับ fillAmount
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        OnTowerDestroyed?.Invoke();

        // ซ่อนหลอดเลือดทันทีหลังป้อมตาย
        if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);

        // เริ่มลำดับ Cinematic ตอนป้อมถูกทำลาย
        StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// ลำดับเหตุการณ์ตอนป้อมถูกทำลาย:
    /// 1) เล่นเอฟเฟกต์ระเบิด / พัง
    /// 2) โฟกัสกล้อง Spectator ไปที่ป้อม แล้วล็อคมุมกล้องไม่ให้ผู้เล่นหมุน
    /// 3) รอให้ผู้เล่นดูฉากสวยๆ สักพัก
    /// 4) เรียก Game Over (Solo) แล้วค่อยลบป้อมออกจากซีน
    /// </summary>
    private IEnumerator DeathSequence()
    {
        // 1) เอฟเฟกต์ทำลายป้อม
        if (deathVfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
            if (deathVfxDuration > 0f)
            {
                Destroy(vfxInstance, deathVfxDuration);
            }
        }

        // 2) โฟกัสกล้อง Spectator ไปที่มุมมองเริ่มต้น (หุ่นนริศ)
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.FocusInitialView();
        }

        // 3) รอให้ผู้เล่นชมฉากป้อมพังก่อน
        if (gameOverDelay > 0f)
        {
            yield return new WaitForSeconds(gameOverDelay);
        }

        // 4) แจ้งระบบ Game Over (รองรับ Solo ก่อน, ถ้าอยู่นอก Solo ก็อย่างน้อยให้เกมหยุดอินพุต)
        if (triggerGameOverOnDeath)
        {
            // Solo Mode
            if (SoloEnemyTracker.Instance != null)
            {
                SoloEnemyTracker.Instance.NotifyPlayerDied();
            }
            else if (SoloGameManager.Instance != null)
            {
                // เผื่อกรณีที่ไม่มี Tracker แต่ยังอยากให้เกมจบ และปลดล็อคเมาส์
                SoloGameManager.Instance.OnGameEnded();
            }
            // ในโหมด Network ฐานหลักจะใช้ BaseHealth + EnemyTracker อยู่แล้ว จึงไม่เรียกซ้ำจาก TowerHealth
        }

        // 5) ลบป้อมออกจากซีนหลังจบลำดับทั้งหมด
        Destroy(gameObject);
    }
}
