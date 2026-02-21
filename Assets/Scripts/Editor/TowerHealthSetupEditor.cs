using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class TowerHealthSetupEditor : Editor
{
    [MenuItem("Tools/Setup Tower Health UI")]
    public static void SetupTowerHealthUI()
    {
        // รับค่า GameObject ที่เรากำลังเลือกอยู่ใน Hierarchy
        GameObject selectedTower = Selection.activeGameObject;

        if (selectedTower == null)
        {
            EditorUtility.DisplayDialog("Error", "กรุณาคลิกเลือก GameObject ของป้อม (Tower) ใน Scene หรือ Hierarchy ก่อนครับ!", "OK");
            return;
        }

        // 1. เพิ่ม Script HealthSystem ให้ป้อม (ถ้ายังไม่มี)
        HealthSystem towerHealth = selectedTower.GetComponent<HealthSystem>();
        if (towerHealth == null)
        {
            towerHealth = selectedTower.AddComponent<HealthSystem>();
            Undo.RegisterCreatedObjectUndo(towerHealth, "Add HealthSystem Component");
        }

        // เช็คว่ามี Canvas อยู่แล้วหรือเปล่า จะได้ไม่สร้างซ้ำ
        Canvas existingCanvas = selectedTower.GetComponentInChildren<Canvas>();
        if (existingCanvas != null && existingCanvas.gameObject.name == "HealthCanvas")
        {
            EditorUtility.DisplayDialog("Warning", "ป้อมนี้มี HealthCanvas อยู่แล้วครับ!", "OK");
            return;
        }

        // 2. สร้าง Canvas
        GameObject canvasGO = new GameObject("HealthCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Health UI");
        canvasGO.transform.SetParent(selectedTower.transform);
        canvasGO.transform.localPosition = new Vector3(0, 3f, 0); // ยกขึ้นไว้บนหัวป้อม (ปรับความสูงตรงนี้ได้)
        canvasGO.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f); // ย่อขนาดให้พอดีแบบ World Space
        
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100; // ให้ภาพชัดขึ้น

        // 3. สร้าง Background Image (กรอบหลังสีดำโปร่งแสง)
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f); 
        
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(150, 20); // ความกว้างและความสูงหลอดเลือด

        // 4. สร้าง Smooth Bar (หลอดเลือดสีแดง ที่ค่อยๆ ลดตาม)
        GameObject smoothGO = new GameObject("SmoothBar");
        smoothGO.transform.SetParent(bgGO.transform, false);
        Image smoothImage = smoothGO.AddComponent<Image>();
        smoothImage.color = new Color(0.8f, 0.1f, 0.1f, 1f); // สีแดง
        smoothImage.type = Image.Type.Filled;
        smoothImage.fillMethod = Image.FillMethod.Horizontal;
        smoothImage.fillAmount = 1f;
        // เซ็ตให้มันลดจากขวาไปซ้าย
        smoothImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        RectTransform smoothRect = smoothGO.GetComponent<RectTransform>();
        smoothRect.anchorMin = Vector2.zero;
        smoothRect.anchorMax = Vector2.one;
        smoothRect.sizeDelta = Vector2.zero; // ให้ขนาดเท่ากับ Background

        // 5. สร้าง Fill Bar (หลอดเลือดสีเขียว ที่ลดปุ๊บปั๊บ)
        GameObject fillGO = new GameObject("FillBar");
        fillGO.transform.SetParent(bgGO.transform, false);
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.1f, 0.8f, 0.2f, 1f); // สีเขียว
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero; // ให้ขนาดเท่ากับ Background

        // 6. โยง UI ทั้งหมดกลับไปที่ Script TowerHealth ให้อัตโนมัติ
        towerHealth.healthCanvas = canvas;
        towerHealth.healthBarSmooth = smoothImage;
        towerHealth.healthBarFill = fillImage;

        // บันทึกการเปลี่ยนแปลง เพื่อให้เซฟ Scene ได้
        EditorUtility.SetDirty(selectedTower);

        Debug.Log("✅ สร้างหลอดเลือดให้ป้อม " + selectedTower.name + " เสร็จเรียบร้อย!");
    }
}
