using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CursorUI : MonoBehaviour
{
    [Header("Custom Cursor Settings")]
    [Tooltip("ใส่รูปเมาส์ที่ต้องการเปลี่ยนตรงนี้ (อย่าลืมปรับ Texture Type เป็น Cursor ใน Inspector)")]
    public Texture2D customCursor;
    
    [Tooltip("จุดศูนย์กลางของการคลิก (0,0 คือมุมซ้ายบนของรูปภาพ)")]
    public Vector2 hotSpot = Vector2.zero;
    
    public CursorMode cursorMode = CursorMode.Auto;

    [Header("Click VFX Settings")]
    [Tooltip("Prefab ของ VFX หน้าจอเวลาคลิก (เช่น Particle System หรือ Animation UI)")]
    public GameObject clickVFXPrefab;
    [Tooltip("ให้ลาก Canvas หน้าจอหลักมาใส่ (ถ้า VFX เป็น UI) หรือปล่อยว่างไว้ถ้า VFX เป็น 3D")]
    public Transform vfxParent;

    void Start()
    {
        ApplyCustomCursor();
    }

    void Update()
    {
        // เล่นเสียงเมื่อคลิกซ้าย
        if (Input.GetMouseButtonDown(0))
        {
            // เช็คว่าคลิกโดนปุ่ม UI หรือไม่ ก่อนเล่นเสียงคลิก
            if (AudioManager.Instance != null && IsPointerOverUIButton())
            {
                AudioManager.Instance.PlaySound(AudioManager.SoundType.Click);
            }

            // แจ้งเตือน/เล่น VFX ถ้าไม่ได้อยู่ใน Combat Phase
            bool canPlayVFX = GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Combat;
            
            if (canPlayVFX && clickVFXPrefab != null)
            {
                GameObject vfx = null;
                Vector3 spawnPos = Input.mousePosition;

                if (vfxParent != null)
                {
                    vfx = Instantiate(clickVFXPrefab, spawnPos, Quaternion.identity, vfxParent);
                }
                else
                {
                    if (Camera.main != null)
                    {
                        spawnPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
                    }
                    vfx = Instantiate(clickVFXPrefab, spawnPos, Quaternion.identity);
                }

                // สั่งลบ VFX ทิ้งอัตโนมัติ 1.5 วินาทีหลังเกิด เพื่อไม่ให้รก Memory
                if (vfx != null) Destroy(vfx, 1.5f);
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

    private bool IsPointerOverUIButton()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // ตรวจสอบว่า GameObject ที่โดนคลิกมี Component Button (หรืออยู่ในปุ่ม) หรือไม่
            if (result.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null)
            {
                return true;
            }
        }
        return false;
    }
}
