using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Script สำหรับเพิ่ม HP Text ให้กับทุก GameObject ที่มี HealthSystem ใน scene ปัจจุบัน
/// </summary>
public class AutoAddHPText
{
    [MenuItem("Tools/Health System/Add HP Text to All")]
    public static void AddHPTextToAllHealthSystems()
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
            
            // ข้ามถ้าไม่มี Canvas
            if (healthSystem.healthCanvas == null)
            {
                Debug.LogWarning($"<color=yellow>[AutoAddHPText]</color> {healthSystem.gameObject.name} ไม่มี HealthCanvas ข้าม ❌");
                continue;
            }
            
            // สร้าง HP Text
            if (CreateHPText(healthSystem))
            {
                successCount++;
            }
        }
        
        // แสดงผลลัพธ์
        EditorUtility.DisplayDialog("เสร็จสิ้น!", 
            $"เพิ่ม HP Text สำเร็จ: {successCount} ตัว\nข้าม (มีอยู่แล้ว): {skipCount} ตัว", 
            "OK");
        
        Debug.Log($"<color=lime>[AutoAddHPText]</color> เพิ่ม HP Text สำเร็จ {successCount} ตัว ✅");
    }
    
    [MenuItem("Tools/Health System/Add HP Text to Selected")]
    public static void AddHPTextToSelected()
    {
        GameObject[] selected = Selection.gameObjects;
        
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "กรุณาเลือก GameObject ใน Hierarchy ก่อน", "OK");
            return;
        }
        
        int successCount = 0;
        
        foreach (GameObject obj in selected)
        {
            HealthSystem healthSystem = obj.GetComponent<HealthSystem>();
            
            if (healthSystem == null)
            {
                Debug.LogWarning($"<color=yellow>[AutoAddHPText]</color> {obj.name} ไม่มี HealthSystem ข้าม ❌");
                continue;
            }
            
            if (healthSystem.healthText != null)
            {
                Debug.LogWarning($"<color=yellow>[AutoAddHPText]</color> {obj.name} มี HP Text อยู่แล้ว ข้าม ❌");
                continue;
            }
            
            if (healthSystem.healthCanvas == null)
            {
                Debug.LogWarning($"<color=yellow>[AutoAddHPText]</color> {obj.name} ไม่มี HealthCanvas ข้าม ❌");
                continue;
            }
            
            if (CreateHPText(healthSystem))
            {
                successCount++;
            }
        }
        
        EditorUtility.DisplayDialog("เสร็จสิ้น!", 
            $"เพิ่ม HP Text สำเร็จ: {successCount} ตัว", 
            "OK");
    }
    
    private static bool CreateHPText(HealthSystem healthSystem)
    {
        try
        {
            // สร้าง TextMeshPro object
            GameObject textObj = new GameObject("HP_Text");
            textObj.transform.SetParent(healthSystem.healthCanvas.transform, false);
            
            // เพิ่ม TextMeshProUGUI component
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            
            // ตั้งค่าพื้นฐาน
            textComponent.text = "100/100";
            textComponent.fontSize = 8f;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            
            // ตั้งค่า RectTransform
            RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(100f, 20f);
            
            // กำหนดให้ HealthSystem
            healthSystem.healthText = textComponent;
            
            // บันทึกการเปลี่ยนแปลง
            EditorUtility.SetDirty(healthSystem);
            
            Debug.Log($"<color=lime>[AutoAddHPText]</color> เพิ่ม HP Text บน {healthSystem.gameObject.name} เรียบร้อย ✅");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[AutoAddHPText]</color> ไม่สามารถเพิ่ม HP Text บน {healthSystem.gameObject.name}: {e.Message} ❌");
            return false;
        }
    }
    
    [MenuItem("Tools/Health System/Clean All HP Text")]
    public static void CleanAllHPText()
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
        
        EditorUtility.DisplayDialog("ลบเรียบร้อย!", 
            $"ลบ HP Text ทั้งหมด {removedCount} ตัว", 
            "OK");
        
        Debug.Log($"<color=orange>[AutoAddHPText]</color> ลบ HP Text {removedCount} ตัวเรียบร้อย 🧹");
    }
}
