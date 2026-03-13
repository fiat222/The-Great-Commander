using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script สำหรับช่วยติดตั้ง HP Text บน HealthSystem อย่างง่าย
/// </summary>
[CustomEditor(typeof(HealthSystem))]
public class HealthTextSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        HealthSystem healthSystem = (HealthSystem)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("HP Text Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add HP Text to Canvas"))
        {
            AddHPTextToHealthSystem(healthSystem);
        }
        
        if (GUILayout.Button("Find Existing Text"))
        {
            FindExistingText(healthSystem);
        }
    }
    
    private void AddHPTextToHealthSystem(HealthSystem healthSystem)
    {
        if (healthSystem.healthCanvas == null)
        {
            EditorUtility.DisplayDialog("Error", "ไม่พบ Health Canvas! กรุณากำหนด Canvas ก่อน", "OK");
            return;
        }
        
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
        
        EditorUtility.DisplayDialog("Success", "เพิ่ม HP Text เรียบร้อยแล้ว! ✅\nสามารถปรับแต่งขนาด/สี/ตำแหน่งได้ตามต้องการ", "OK");
        
        Debug.Log($"<color=lime>[HealthTextSetup]</color> เพิ่ม HP Text บน {healthSystem.gameObject.name} เรียบร้อย ✅");
    }
    
    private void FindExistingText(HealthSystem healthSystem)
    {
        TextMeshProUGUI existingText = healthSystem.GetComponentInChildren<TextMeshProUGUI>();
        
        if (existingText != null)
        {
            healthSystem.healthText = existingText;
            EditorUtility.SetDirty(healthSystem);
            
            EditorUtility.DisplayDialog("Found", $"พบ TextMeshPro: {existingText.gameObject.name} ✅", "OK");
            Debug.Log($"<color=lime>[HealthTextSetup]</color> พบ TextMeshPro: {existingText.gameObject.name} ✅");
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found", "ไม่พบ TextMeshPro ใน hierarchy\nกรุณาสร้างใหม่ด้วยปุ่ม Add HP Text", "OK");
        }
    }
}
