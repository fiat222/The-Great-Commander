using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component สำหรับแสดงตัวเลข HP บนหลอดเลือด (เช่น 150/150)
/// ใช้ร่วมกับ HealthSystem เพื่ออัปเดต UI แบบ real-time
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthSystem))]
public class HealthBarWithText : MonoBehaviour
{
    [Header("Text Settings")]
    [Tooltip("TextMeshPro Text สำหรับแสดงเลข HP (เช่น 150/150)")]
    public TextMeshProUGUI healthText;
    
    [Tooltip("อย่างน้อย 3 ตัวอักษร (เช่น 100)")]
    public int minTextWidth = 3;
    
    [Tooltip("เว้นวรรคระหว่าง current/max")]
    public bool useSpace = true;
    
    [Tooltip("สีข้อความเมื่อ HP เต็ม")]
    public Color fullHealthColor = Color.white;
    
    [Tooltip("สีข้อความเมื่อ HP เหลือน้อย")]
    public Color lowHealthColor = Color.red;
    
    [Tooltip("เปลี่ยนสีเมื่อ HP เหลือน้อยกว่านี้ (เปอร์เซ็นต์)")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.25f;

    private HealthSystem healthSystem;
    private Camera mainCamera;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        mainCamera = Camera.main;
        
        // หา TextMeshPro ถ้าไม่ได้กำหนด
        if (healthText == null)
        {
            healthText = FindTextInHierarchy();
        }
        
        if (healthText == null)
        {
            Debug.LogWarning($"<color=yellow>[HealthBarWithText]</color> ไม่พบ TextMeshPro บน {gameObject.name} ❌");
        }
        else
        {
            Debug.Log($"<color=lime>[HealthBarWithText]</color> พบ TextMeshPro บน {healthText.gameObject.name} ✅");
        }
    }

    private void Start()
    {
        if (healthSystem != null)
        {
            // อัปเดตครั้งแรก
            UpdateHealthText();
        }
    }

    private void LateUpdate()
    {
        // อัปเดตข้อความทุก frame (เผื่อมีการเปลี่ยนแปลงจาก TakeDamage)
        if (healthText != null && healthSystem != null)
        {
            UpdateHealthText();
            
            // หมุนข้อความให้หันเข้าหากล้อง (billboard effect)
            if (mainCamera != null)
            {
                healthText.transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    private void UpdateHealthText()
    {
        if (healthText == null || healthSystem == null) return;

        // ดึงค่า HP จาก HealthSystem (ใช้ reflection ถ้าจำเป็น)
        float currentHealth = GetCurrentHealth();
        float maxHealth = GetMaxHealth();
        
        // จัดรูปแบบข้อความ
        string separator = useSpace ? " / " : "/";
        string formattedText = $"{Mathf.RoundToInt(currentHealth)}{separator}{Mathf.RoundToInt(maxHealth)}";
        
        // Padding ให้ความกว้างคงที่ (ป้องกันข้อความกระดับ)
        if (formattedText.Length < minTextWidth)
        {
            formattedText = formattedText.PadLeft(minTextWidth);
        }
        
        healthText.text = formattedText;
        
        // เปลี่ยนสีตาม HP
        UpdateTextColor(currentHealth, maxHealth);
    }

    private void UpdateTextColor(float current, float max)
    {
        if (healthText == null) return;
        
        float normalizedHealth = Mathf.Approximately(max, 0f) ? 0f : current / max;
        
        if (normalizedHealth <= lowHealthThreshold)
        {
            healthText.color = lowHealthColor;
        }
        else
        {
            healthText.color = fullHealthColor;
        }
    }

    private TextMeshProUGUI FindTextInHierarchy()
    {
        // ค้นหาใน Canvas ของ HealthSystem ก่อน
        if (healthSystem.healthCanvas != null)
        {
            var textInCanvas = healthSystem.healthCanvas.GetComponentInChildren<TextMeshProUGUI>();
            if (textInCanvas != null) return textInCanvas;
        }
        
        // ค้นหาใน children ทั้งหมด
        return GetComponentInChildren<TextMeshProUGUI>();
    }

    // Helper methods สำหรับดึงค่า HP (ใช้ reflection เพื่อความ flexible)
    private float GetCurrentHealth()
    {
        if (healthSystem == null) return 0f;
        
        // ลองดึงจาก field หรือ property
        var field = typeof(HealthSystem).GetField("currentHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (float)field.GetValue(healthSystem);
        }
        
        // fallback: ใช้ public method ถ้ามี
        return healthSystem.maxHealth; // ค่าเริ่มต้น
    }

    private float GetMaxHealth()
    {
        if (healthSystem == null) return 100f;
        return healthSystem.maxHealth;
    }

    /// <summary>
    /// สำหรับ external call (เช่น จาก BaseHealth) ให้อัปเดตข้อความทันที
    /// </summary>
    public void ForceUpdateText()
    {
        UpdateHealthText();
    }

    /// <summary>
    /// ตั้งค่า Text component จากภายนอก
    /// </summary>
    public void SetHealthText(TextMeshProUGUI text)
    {
        healthText = text;
        if (text != null)
        {
            Debug.Log($"<color=lime>[HealthBarWithText]</color> กำหนด TextMeshPro: {text.gameObject.name} ✅");
        }
    }
}
