using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI; // เพิ่มบรรทัดนี้

/// <summary>
/// Script ปลอดภัยสำหรับเพิ่ม HP Text แก้ไขปัญหา RectTransform และ Component ที่หายไป
/// </summary>
public class SafeHPTextAdder
{
    [MenuItem("Tools/Safe HP/Add HP Text to All (FIXED)")]
    public static void AddToAllSafe()
    {
        // หาทุก HealthSystem ใน scene
        HealthSystem[] allHealthSystems = Object.FindObjectsOfType<HealthSystem>();
        
        int successCount = 0;
        int skipCount = 0;
        int errorCount = 0;
        
        foreach (HealthSystem healthSystem in allHealthSystems)
        {
            try
            {
                // ข้ามถ้ามี HP Text อยู่แล้ว
                if (healthSystem.healthText != null)
                {
                    skipCount++;
                    continue;
                }
                
                // สร้าง HP Text อย่างปลอดภัย
                if (CreateSafeHPText(healthSystem))
                {
                    successCount++;
                }
            }
            catch (System.Exception e)
            {
                errorCount++;
                Debug.LogError($"<color=red>[SafeHPTextAdder]</color> ❌ Error บน {healthSystem.gameObject.name}: {e.Message}");
            }
        }
        
        // แสดงผลลัพธ์
        string message = $"✅ สำเร็จ: {successCount} ตัว\n⏭️ ข้าม: {skipCount} ตัว";
        if (errorCount > 0)
        {
            message += $"\n❌ Error: {errorCount} ตัว";
        }
        message += $"\n\n🎮 ตอนนี้ทุกอย่างมีเลข HP แบบ 150/150 แล้ว!";
        
        EditorUtility.DisplayDialog("🎉 เสร็จสิ้น!", message, "เยี่ยม!");
        
        Debug.Log($"<color=lime>[SafeHPTextAdder]</color> ✅ สำเร็จ {successCount} | ข้าม {skipCount} | Error {errorCount}");
    }
    
    private static bool CreateSafeHPText(HealthSystem healthSystem)
    {
        try
        {
            // 1. ตรวจสอบและสร้าง Canvas อย่างปลอดภัย
            Canvas canvas = EnsureCanvasExists(healthSystem);
            if (canvas == null) return false;
            
            // 2. สร้าง TextMeshPro object อย่างปลอดภัย
            GameObject textObj = new GameObject("HP_Text");
            textObj.transform.SetParent(canvas.transform, false);
            
            // 3. เพิ่ม RectTransform ก่อนเสมอ
            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            
            // 4. เพิ่ม TextMeshProUGUI
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            
            // 5. ตั้งค่าพื้นฐานอย่างปลอดภัย
            SetupTextComponent(textComponent);
            
            // 6. ตั้งค่า RectTransform อย่างปลอดภัย
            SetupRectTransform(rectTransform);
            
            // 7. กำหนดให้ HealthSystem
            healthSystem.healthText = textComponent;
            
            // 8. บันทึกการเปลี่ยนแปลง
            EditorUtility.SetDirty(healthSystem);
            
            Debug.Log($"<color=lime>[SafeHPTextAdder]</color> ✅ เพิ่ม HP Text บน {healthSystem.gameObject.name} สำเร็จ");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[SafeHPTextAdder]</color> ❌ ไม่สามารถเพิ่ม HP Text บน {healthSystem.gameObject.name}: {e.Message}");
            return false;
        }
    }
    
    private static Canvas EnsureCanvasExists(HealthSystem healthSystem)
    {
        try
        {
            if (healthSystem.healthCanvas != null)
            {
                return healthSystem.healthCanvas;
            }
            
            // สร้าง Canvas ใหม่
            GameObject canvasObj = new GameObject("HealthCanvas");
            canvasObj.transform.SetParent(healthSystem.transform, false);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // เพิ่ม Component ที่จำเป็น
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // ตั้งค่า RectTransform ของ Canvas
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.localPosition = Vector3.up * 2f;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.01f;
            
            healthSystem.healthCanvas = canvas;
            
            Debug.Log($"<color=yellow>[SafeHPTextAdder]</color> 🔧 สร้าง Canvas ใหม่ให้ {healthSystem.gameObject.name}");
            return canvas;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[SafeHPTextAdder]</color> ❌ ไม่สามารถสร้าง Canvas: {e.Message}");
            return null;
        }
    }
    
    private static void SetupTextComponent(TextMeshProUGUI textComponent)
    {
        textComponent.text = "100/100";
        textComponent.fontSize = 12f;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontStyle = FontStyles.Bold;
        
        // เพิ่ม outline ให้เห็นชัด
        textComponent.outlineWidth = 0.2f;
        textComponent.outlineColor = Color.black;
    }
    
    private static void SetupRectTransform(RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(200f, 30f);
    }
    
    [MenuItem("Tools/Safe HP/Remove All HP Text (SAFE)")]
    public static void RemoveAllSafe()
    {
        HealthSystem[] allHealthSystems = Object.FindObjectsOfType<HealthSystem>();
        int removedCount = 0;
        int errorCount = 0;
        
        foreach (HealthSystem healthSystem in allHealthSystems)
        {
            try
            {
                if (healthSystem.healthText != null)
                {
                    GameObject textObj = healthSystem.healthText.gameObject;
                    
                    if (Application.isPlaying)
                    {
                        Object.Destroy(textObj);
                    }
                    else
                    {
                        Object.DestroyImmediate(textObj);
                    }
                    
                    healthSystem.healthText = null;
                    EditorUtility.SetDirty(healthSystem);
                    removedCount++;
                }
            }
            catch (System.Exception e)
            {
                errorCount++;
                Debug.LogError($"<color=red>[SafeHPTextAdder]</color> ❌ Error ลบ HP Text บน {healthSystem.gameObject.name}: {e.Message}");
            }
        }
        
        string message = $"🧹 ลบสำเร็จ: {removedCount} ตัว";
        if (errorCount > 0)
        {
            message += $"\n❌ Error: {errorCount} ตัว";
        }
        
        EditorUtility.DisplayDialog("🧹 ลบเรียบร้อย!", message, "OK");
        
        Debug.Log($"<color=orange>[SafeHPTextAdder]</color> 🧹 ลบ HP Text {removedCount} ตัวเรียบร้อย (Error: {errorCount})");
    }
    
    [MenuItem("Tools/Safe HP/Fix Missing Components")]
    public static void FixMissingComponents()
    {
        // แก้ไขปัญหา Missing Component ใน scene
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        
        int fixedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // ตรวจสอบ UI elements ที่ไม่มี RectTransform
            if (obj.GetComponent<Canvas>() != null && obj.GetComponent<RectTransform>() == null)
            {
                obj.AddComponent<RectTransform>();
                fixedCount++;
                Debug.Log($"<color=yellow>[SafeHPTextAdder]</color> 🔧 เพิ่ม RectTransform ให้ {obj.name}");
            }
            
            // ตรวจสอบ TextMeshPro ที่ไม่มี RectTransform
            if (obj.GetComponent<TextMeshProUGUI>() != null && obj.GetComponent<RectTransform>() == null)
            {
                obj.AddComponent<RectTransform>();
                fixedCount++;
                Debug.Log($"<color=yellow>[SafeHPTextAdder]</color> 🔧 เพิ่ม RectTransform ให้ {obj.name}");
            }
        }
        
        EditorUtility.DisplayDialog("🔧 แก้ไขเรียบร้อย!", 
            $"แก้ไข Missing Component ทั้งหมด {fixedCount} ตัวเรียบร้อยแล้ว", 
            "OK");
        
        Debug.Log($"<color=lime>[SafeHPTextAdder]</color> 🔧 แก้ไข Missing Component {fixedCount} ตัวเรียบร้อย");
    }
}
