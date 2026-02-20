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
        isDead = true;
        OnTowerDestroyed?.Invoke();
        
        // ซ่อนหลอดเลือด หรือปิดตัวป้อมไป
        if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);

        // คุณสามารถใส่ Effect ระเบิด หรือ Logic เกมโอเวอร์/เงินเด้งตรงนี้ได้
        Debug.Log(gameObject.name + " has been destroyed!");
        
        // Destory หรือ Disable วัตถุ
        Destroy(gameObject, 0.5f); // รอ 0.5 วิเผื่อมี Effect ให้แสดงผล
    }
}
