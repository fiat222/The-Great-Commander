using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class BaseHealthSetupEditor : Editor
{
    [MenuItem("Tools/Setup Base Health UI")]
    public static void SetupBaseHealthUI()
    {
        // ลองหาจาก Selection ก่อน ถ้าไม่มีก็ Auto-find จาก Scene
        GameObject baseObj = Selection.activeGameObject;

        if (baseObj == null || baseObj.GetComponent<BaseHealth>() == null)
        {
            // Auto-find GameObject ที่มี BaseHealth ใน Scene
            BaseHealth found = Object.FindFirstObjectByType<BaseHealth>();
            if (found != null)
                baseObj = found.gameObject;
        }

        if (baseObj == null)
        {
            EditorUtility.DisplayDialog("Error",
                "ไม่พบ GameObject ที่มี BaseHealth Component ครับ\n" +
                "ลองคลิกเลือก GameObject ของป้อมหลัก (Castle/Base) ใน Hierarchy ก่อน",
                "OK");
            return;
        }

        BaseHealth baseHealth = baseObj.GetComponent<BaseHealth>();
        if (baseHealth == null)
        {
            EditorUtility.DisplayDialog("Error",
                "GameObject ที่เลือกไม่มี BaseHealth Component ครับ",
                "OK");
            return;
        }

        // เช็คว่ามี BaseHealthCanvas อยู่แล้วหรือยัง จะได้ไม่สร้างซ้ำ
        Transform existingCanvas = baseObj.transform.Find("BaseHealthCanvas");
        if (existingCanvas != null)
        {
            bool overwrite = EditorUtility.DisplayDialog("Warning",
                "ป้อมหลักนี้มี BaseHealthCanvas อยู่แล้วครับ\nต้องการลบของเดิมแล้วสร้างใหม่ไหม?",
                "ใช่ สร้างใหม่", "ยกเลิก");

            if (!overwrite) return;

            Undo.DestroyObjectImmediate(existingCanvas.gameObject);
        }

        // ─── 1. สร้าง Canvas (World Space) ──────────────────────────────────────
        GameObject canvasGO = new GameObject("BaseHealthCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create BaseHealth UI");
        canvasGO.transform.SetParent(baseObj.transform);
        canvasGO.transform.localPosition = new Vector3(0f, 12f, 0f); // สูงกว่าป้อม Tower เพราะป้อมหลักใหญ่กว่า
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ─── 2. Background (กรอบดำโปร่งแสง) ────────────────────────────────────
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(300f, 28f); // กว้างกว่าป้อมธรรมดา เพราะนี่คือฐานหลัก
        bgRect.anchoredPosition = Vector2.zero;

        // ─── 3. Smooth Bar (สีแดง — หลอดเลือดตาม ค่อยๆ ลด) ──────────────────
        GameObject smoothGO = new GameObject("SmoothBar");
        smoothGO.transform.SetParent(bgGO.transform, false);
        Image smoothImage = smoothGO.AddComponent<Image>();
        smoothImage.color = new Color(0.85f, 0.1f, 0.1f, 1f);

        RectTransform smoothRect = smoothGO.GetComponent<RectTransform>();
        smoothRect.anchorMin = Vector2.zero;
        smoothRect.anchorMax = Vector2.one;
        smoothRect.pivot = new Vector2(0f, 0.5f);
        smoothRect.anchoredPosition = Vector2.zero;
        smoothRect.offsetMin = new Vector2(4f, 4f);   // padding เล็กน้อย
        smoothRect.offsetMax = new Vector2(-4f, -4f);

        // ─── 4. Fill Bar (สีเขียว — หลอดเลือดจริง ลดทันที) ─────────────────
        GameObject fillGO = new GameObject("FillBar");
        fillGO.transform.SetParent(bgGO.transform, false);
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.15f, 0.85f, 0.25f, 1f);

        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        // ─── 5. Label ข้อความ "BASE HP" บนหลอด ────────────────────────────────
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(bgGO.transform, false);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "BASE HP";
        label.fontSize = 14f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableAutoSizing = false;

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // ─── 6. โยง reference กลับไปที่ HealthSystem (เจ้าของ UI) ──────────────
        HealthSystem healthSys = baseObj.GetComponent<HealthSystem>();
        if (healthSys == null)
            healthSys = baseObj.GetComponentInChildren<HealthSystem>();
        if (healthSys == null)
            healthSys = baseObj.AddComponent<HealthSystem>();

        Undo.RecordObject(healthSys, "Assign HealthSystem UI References");
        healthSys.maxHealth = baseHealth.maxHealth;
        healthSys.AssignUIReferences(canvas, fillImage, smoothImage);

        EditorUtility.SetDirty(healthSys);
        EditorUtility.SetDirty(baseHealth);
        EditorSceneManager.MarkSceneDirty(baseObj.scene);

        Debug.Log($"✅ [BaseHealthSetupEditor] สร้างหลอดเลือดป้อมหลักให้ <b>{baseObj.name}</b> เสร็จเรียบร้อย!");
        EditorUtility.DisplayDialog("สำเร็จ!",
            $"สร้าง BaseHealthCanvas ให้ {baseObj.name} เสร็จแล้วครับ\n\n" +
            "ถ้าหลอดเลือดอยู่ในตำแหน่งไม่ดี ปรับ localPosition ของ BaseHealthCanvas ได้เลย",
            "OK");
    }
}
