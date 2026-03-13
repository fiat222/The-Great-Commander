using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI; // เพิ่มบรรทัดนี้

/// <summary>
/// Script เร่งด่วนสำหรับเพิ่ม HP Text ให้ทุกอย่างใน scene ทันที
/// </summary>
public class QuickAddHPText
{
    [MenuItem("Tools/Quick HP/Add to ALL (Player/Enemy/Minion)")]
    public static void AddToAll()
    {
        // หาทุก HealthSystem ใน scene
        HealthSystem[] allHealthSystems = Object.FindObjectsOfType<HealthSystem>();
        
        int successCount = 0;
        int skipCount = 0;
        
        foreach (HealthSystem healthSystem in allHealthSystems)
        {
            // ข้ามถ้ามี HP Text อยู่แล้ว
            if (healthSystem.healthText != null)
            {
                skipCount++;
                continue;
            }
            
            // สร้าง HP Text แม้ไม่มี Canvas (สร้างใหม่)
            if (CreateHPTextWithCanvas(healthSystem))
            {
                successCount++;
            }
        }
        
        // แสดงผลลัพธ์
        EditorUtility.DisplayDialog("✅ เสร็จสิ้น!", 
            $"เพิ่ม HP Text สำเร็จ: {successCount} ตัว\nข้าม (มีอยู่แล้ว): {skipCount} ตัว\n\nตอนนี้ทุกอย่างมีเลข HP แบบ 150/150 แล้ว!", 
            "เยี่ยม!");
        
        Debug.Log($"<color=lime>[QuickAddHPText]</color> ✅ เพิ่ม HP Text สำเร็จ {successCount} ตัว! ทุกอย่างมีเลข HP แล้ว");
    }
    
    private static bool CreateHPTextWithCanvas(HealthSystem healthSystem)
    {
        try
        {
            GameObject canvasObj = null;
            Canvas canvas = null;
            
            // ถ้าไม่มี Canvas ให้สร้างใหม่
            if (healthSystem.healthCanvas == null)
            {
                canvasObj = new GameObject("HealthCanvas");
                canvasObj.transform.SetParent(healthSystem.transform, false);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // ตั้งค่า Canvas
                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.localPosition = Vector3.up * 2f; // ยกขึ้นเหนือหัว
                canvasRect.localRotation = Quaternion.identity;
                canvasRect.localScale = Vector3.one * 0.01f;
                
                healthSystem.healthCanvas = canvas;
                
                Debug.Log($"<color=yellow>[QuickAddHPText]</color> สร้าง Canvas ใหม่ให้ {healthSystem.gameObject.name}");
            }
            else
            {
                canvas = healthSystem.healthCanvas;
            }
            
            // สร้าง TextMeshPro object
            GameObject textObj = new GameObject("HP_Text");
            textObj.transform.SetParent(canvas.transform, false);
            
            // เพิ่ม TextMeshProUGUI component
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            
            // ตั้งค่าพื้นฐาน - ทำให้เห็นชัด!
            textComponent.text = "100/100";
            textComponent.fontSize = 12f; // ใหญ่ขึ้น
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.outlineWidth = 0.2f;
            textComponent.outlineColor = Color.black;
            
            // เพิ่ม RectTransform ถ้ายังไม่มี
            RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = textObj.AddComponent<RectTransform>();
            }
            
            // ตั้งค่า RectTransform
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(200f, 30f);
            
            // กำหนดให้ HealthSystem
            healthSystem.healthText = textComponent;
            
            // บันทึกการเปลี่ยนแปลง
            EditorUtility.SetDirty(healthSystem);
            
            Debug.Log($"<color=lime>[QuickAddHPText]</color> ✅ เพิ่ม HP Text บน {healthSystem.gameObject.name} เรียบร้อย");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[QuickAddHPText]</color> ❌ ไม่สามารถเพิ่ม HP Text บน {healthSystem.gameObject.name}: {e.Message}");
            return false;
        }
    }
    
    [MenuItem("Tools/Quick HP/Remove ALL HP Text")]
    public static void RemoveAll()
    {
        HealthSystem[] allHealthSystems = Object.FindObjectsOfType<HealthSystem>();
        int removedCount = 0;
        
        foreach (HealthSystem healthSystem in allHealthSystems)
        {
            if (healthSystem.healthText != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(healthSystem.healthText.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(healthSystem.healthText.gameObject);
                }
                
                healthSystem.healthText = null;
                EditorUtility.SetDirty(healthSystem);
                removedCount++;
            }
        }
        
        EditorUtility.DisplayDialog("🧹 ลบเรียบร้อย!", 
            $"ลบ HP Text ทั้งหมด {removedCount} ตัวเรียบร้อยแล้ว", 
            "OK");
        
        Debug.Log($"<color=orange>[QuickAddHPText]</color> 🧹 ลบ HP Text {removedCount} ตัวเรียบร้อย");
    }
}
