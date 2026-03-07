using UnityEngine;

public class CursorUI : MonoBehaviour
{
    [Header("Custom Cursor Settings")]
    [Tooltip("ใส่รูปเมาส์ที่ต้องการเปลี่ยนตรงนี้ (อย่าลืมปรับ Texture Type เป็น Cursor ใน Inspector)")]
    public Texture2D customCursor;
    
    [Tooltip("จุดศูนย์กลางของการคลิก (0,0 คือมุมซ้ายบนของรูปภาพ)")]
    public Vector2 hotSpot = Vector2.zero;
    
    public CursorMode cursorMode = CursorMode.Auto;

    void Start()
    {
        ApplyCustomCursor();
    }

    void Update()
    {
        // เล่นเสียงเมื่อคลิกซ้าย
        if (Input.GetMouseButtonDown(0))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(AudioManager.SoundType.Click);
            }
        }
    }

    public void ApplyCustomCursor()
    {
        if (customCursor != null)
        {
            Cursor.SetCursor(customCursor, hotSpot, cursorMode);
        }
        else
        {
            Debug.LogWarning("[CursorUI] Custom Cursor is missing! Please assign a Texture2D.");
        }
    }

    public void ResetToDefaultCursor()
    {
        // รีเซ็ตเมาส์กลับเป็นแบบปกติของระบบ
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
