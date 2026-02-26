using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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

    void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        UpdateHealthUI();
    }

    void Update()
    {
        // 1. หมุนป้ายเลือดให้หันเข้าหากล้องเสมอ (ถ้ามี UI)
        if (healthCanvas != null && mainCamera != null)
        {
            healthCanvas.transform.rotation = mainCamera.transform.rotation;
        }

        // 2. จัดการเรื่องหลอดเลือดไหลแบบ Smooth (ถ้ามี UI)
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

        // แสดงผลผ่าน Console (แทน BaseHealth เดิม)
        Debug.Log($"<color=orange>[HP]</color> {gameObject.name} โดนตี! เลือดเหลือ {currentHealth}/{maxHealth}");

        UpdateHealthUI();
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
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
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
