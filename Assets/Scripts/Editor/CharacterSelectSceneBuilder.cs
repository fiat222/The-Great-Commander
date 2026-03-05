using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor Script สำหรับสร้าง CharacterSelect UI อัตโนมัติ
/// ใช้งาน: เปิด CharacterSelectScene → เมนู Tools → Character Select → Build All
/// </summary>
public class CharacterSelectSceneBuilder : Editor
{
    // ==================== สี (RGBA 0-1) ====================
    static readonly Color COL_BG             = new Color(0.06f, 0.06f, 0.10f, 1f);
    static readonly Color COL_PANEL_P1       = new Color(0.10f, 0.12f, 0.22f, 0.85f);
    static readonly Color COL_PANEL_P2       = new Color(0.22f, 0.10f, 0.12f, 0.85f);
    static readonly Color COL_HEADER_BG      = new Color(0f, 0f, 0f, 0.5f);
    static readonly Color COL_CARD_BG        = new Color(0.16f, 0.16f, 0.20f, 0.86f);
    static readonly Color COL_P1_BORDER      = new Color(0f, 0.59f, 1f, 0.78f);   // ฟ้า
    static readonly Color COL_P2_BORDER      = new Color(1f, 0.24f, 0.24f, 0.78f); // แดง
    static readonly Color COL_P1_NAME        = new Color(0.39f, 0.78f, 1f, 1f);
    static readonly Color COL_P2_NAME        = new Color(1f, 0.39f, 0.39f, 1f);
    static readonly Color COL_VS             = new Color(1f, 0.84f, 0.25f, 1f);    // ทอง
    static readonly Color COL_READY_BTN      = new Color(0.12f, 0.59f, 0.20f, 0.90f);
    static readonly Color COL_COUNTDOWN      = new Color(1f, 0.86f, 0.31f, 1f);
    static readonly Color COL_TRANSPARENT    = new Color(1f, 1f, 1f, 0f);
    static readonly Color COL_DIMMED_WHITE   = new Color(1f, 1f, 1f, 0.3f);
    static readonly Color COL_READY_GREEN    = new Color(0f, 1f, 0.39f, 1f);
    static readonly Color COL_GRAY_TEXT      = new Color(0.7f, 0.7f, 0.7f, 1f);
    static readonly Color COL_WHITE          = Color.white;

    // ==================== Menu: สร้างทั้ง Prefab + Canvas ====================

    [MenuItem("Tools/Character Select/1 — Build ALL (Prefab + Canvas)", false, 10)]
    static void BuildAll()
    {
        BuildCharacterCardPrefab();
        BuildCanvasUI();
        Debug.Log("<color=lime>[CharSelectBuilder]</color> ✅ สร้างทุกอย่างเสร็จเรียบร้อย!");
    }

    // ==================== Menu: สร้าง Prefab อย่างเดียว ====================

    [MenuItem("Tools/Character Select/2 — Build CharacterCard Prefab Only", false, 20)]
    static void BuildCharacterCardPrefab()
    {
        // สร้าง folder ถ้ายังไม่มี
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        // สร้าง Temp Canvas (จำเป็นเพราะ UI ต้องอยู่ใต้ Canvas)
        var tempCanvas = new GameObject("__TempCanvas__");
        var canvas = tempCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tempCanvas.AddComponent<CanvasScaler>();
        tempCanvas.AddComponent<GraphicRaycaster>();

        // ==================== CharacterCard Root ====================
        GameObject card = CreatePanel(tempCanvas.transform, "CharacterCard", 200, 280, COL_CARD_BG);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);

        // ==================== CharacterIcon ====================
        GameObject iconGO = CreateImage(card.transform, "CharacterIcon", 160, 160, COL_WHITE);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 1f);
        iconRT.anchoredPosition = new Vector2(0, -100);
        iconGO.GetComponent<Image>().preserveAspect = true;
        iconGO.GetComponent<Image>().raycastTarget = false;

        // ==================== NameText ====================
        GameObject nameGO = CreateTMP(card.transform, "NameText", "Character Name",
            20, TextAlignmentOptions.Center, COL_WHITE, 180, 30);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = nameRT.anchorMax = new Vector2(0.5f, 0f);
        nameRT.anchoredPosition = new Vector2(0, 55);

        // ==================== ClassText ====================
        GameObject classGO = CreateTMP(card.transform, "ClassText", "Class",
            14, TextAlignmentOptions.Center, COL_GRAY_TEXT, 180, 25);
        var classRT = classGO.GetComponent<RectTransform>();
        classRT.anchorMin = classRT.anchorMax = new Vector2(0.5f, 0f);
        classRT.anchoredPosition = new Vector2(0, 28);

        // ==================== SelectButton (โปร่งใส คลุมทั้ง Card) ====================
        GameObject btnGO = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(card.transform, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = Vector2.zero; btnRT.anchorMax = Vector2.one;
        btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        btnGO.GetComponent<Image>().color = COL_TRANSPARENT;

        // ==================== P1Border (สีฟ้า) ====================
        GameObject p1Border = CreateBorder(card.transform, "P1Border", COL_P1_BORDER);
        p1Border.SetActive(false);

        // ==================== P2Border (สีแดง) ====================
        GameObject p2Border = CreateBorder(card.transform, "P2Border", COL_P2_BORDER);
        p2Border.SetActive(false);

        // ==================== เพิ่ม CharacterCardUI Script + Link Ref ====================
        var cardUI = card.AddComponent<CharacterCardUI>();
        cardUI.characterIcon     = iconGO.GetComponent<Image>();
        cardUI.nameText          = nameGO.GetComponent<TextMeshProUGUI>();
        cardUI.classText         = classGO.GetComponent<TextMeshProUGUI>();
        cardUI.selectButton      = btnGO.GetComponent<Button>();
        cardUI.p1HighlightBorder = p1Border;
        cardUI.p2HighlightBorder = p2Border;

        // ==================== บันทึกเป็น Prefab ====================
        string prefabPath = "Assets/Prefabs/UI/CharacterCard.prefab";

        // ลบ prefab เก่าถ้ามี
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);

        PrefabUtility.SaveAsPrefabAsset(card, prefabPath);

        // ลบ temp canvas
        DestroyImmediate(tempCanvas);

        AssetDatabase.Refresh();
        Debug.Log($"<color=lime>[CharSelectBuilder]</color> ✅ CharacterCard Prefab สร้างเสร็จ → {prefabPath}");
    }

    // ==================== Menu: สร้าง Canvas อย่างเดียว ====================

    [MenuItem("Tools/Character Select/3 — Build Canvas UI Only (need open scene)", false, 30)]
    static void BuildCanvasUI()
    {
        // ลบ Canvas เก่าถ้ามี (ชื่อ CharSelectCanvas)
        var old = GameObject.Find("CharSelectCanvas");
        if (old != null) DestroyImmediate(old);

        // ==================== Canvas ====================
        GameObject canvasGO = new GameObject("CharSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ==================== Background ====================
        GameObject bg = CreateImage(canvasGO.transform, "Background", 0, 0, COL_BG);
        StretchFull(bg);
        bg.GetComponent<Image>().raycastTarget = false;

        // ==================== Panel_Header ====================
        GameObject header = CreatePanel(canvasGO.transform, "Panel_Header", 0, 80, COL_HEADER_BG);
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, 80);

        GameObject titleText = CreateTMP(header.transform, "TitleText", "⚔️ เลือกตัวละคร",
            40, TextAlignmentOptions.Center, COL_WHITE, 0, 0);
        StretchFull(titleText);

        // ==================== Panel_P1 (ซ้าย) ====================
        GameObject panelP1 = CreatePanel(canvasGO.transform, "Panel_P1", 350, 450, COL_PANEL_P1);
        var p1RT = panelP1.GetComponent<RectTransform>();
        p1RT.anchorMin = p1RT.anchorMax = new Vector2(0.22f, 0.55f);
        p1RT.anchoredPosition = Vector2.zero;

        // P1 Portrait
        GameObject p1Portrait = CreateImage(panelP1.transform, "P1Portrait", 250, 250, COL_DIMMED_WHITE);
        var p1PortRT = p1Portrait.GetComponent<RectTransform>();
        p1PortRT.anchorMin = p1PortRT.anchorMax = new Vector2(0.5f, 1f);
        p1PortRT.anchoredPosition = new Vector2(0, -30);
        p1Portrait.GetComponent<Image>().preserveAspect = true;
        p1Portrait.GetComponent<Image>().raycastTarget = false;

        // P1 Name
        GameObject p1Name = CreateTMP(panelP1.transform, "P1NameText", "Player 1",
            28, TextAlignmentOptions.Center, COL_P1_NAME, 300, 40);
        var p1NameRT = p1Name.GetComponent<RectTransform>();
        p1NameRT.anchorMin = p1NameRT.anchorMax = new Vector2(0.5f, 0f);
        p1NameRT.anchoredPosition = new Vector2(0, 80);

        // P1 Class
        GameObject p1Class = CreateTMP(panelP1.transform, "P1ClassText", "เลือกตัวละคร...",
            18, TextAlignmentOptions.Center, COL_GRAY_TEXT, 300, 30);
        var p1ClassRT = p1Class.GetComponent<RectTransform>();
        p1ClassRT.anchorMin = p1ClassRT.anchorMax = new Vector2(0.5f, 0f);
        p1ClassRT.anchoredPosition = new Vector2(0, 48);

        // P1 Ready Icon
        GameObject p1Ready = CreateImage(panelP1.transform, "P1ReadyIcon", 40, 40, COL_READY_GREEN);
        var p1ReadyRT = p1Ready.GetComponent<RectTransform>();
        p1ReadyRT.anchorMin = p1ReadyRT.anchorMax = new Vector2(0.5f, 0f);
        p1ReadyRT.anchoredPosition = new Vector2(0, 15);
        p1Ready.GetComponent<Image>().raycastTarget = false;
        p1Ready.SetActive(false); // ปิดไว้

        // ==================== VS Text (ตรงกลาง) ====================
        GameObject vsGO = CreateTMP(canvasGO.transform, "VS_Text", "VS",
            80, TextAlignmentOptions.Center, COL_VS, 200, 100);
        var vsRT = vsGO.GetComponent<RectTransform>();
        vsRT.anchorMin = vsRT.anchorMax = new Vector2(0.5f, 0.58f);
        vsRT.anchoredPosition = Vector2.zero;
        vsGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // ==================== Panel_P2 (ขวา) ====================
        GameObject panelP2 = CreatePanel(canvasGO.transform, "Panel_P2", 350, 450, COL_PANEL_P2);
        var p2RT = panelP2.GetComponent<RectTransform>();
        p2RT.anchorMin = p2RT.anchorMax = new Vector2(0.78f, 0.55f);
        p2RT.anchoredPosition = Vector2.zero;

        // P2 Portrait
        GameObject p2Portrait = CreateImage(panelP2.transform, "P2Portrait", 250, 250, COL_DIMMED_WHITE);
        var p2PortRT = p2Portrait.GetComponent<RectTransform>();
        p2PortRT.anchorMin = p2PortRT.anchorMax = new Vector2(0.5f, 1f);
        p2PortRT.anchoredPosition = new Vector2(0, -30);
        p2Portrait.GetComponent<Image>().preserveAspect = true;
        p2Portrait.GetComponent<Image>().raycastTarget = false;

        // P2 Name
        GameObject p2Name = CreateTMP(panelP2.transform, "P2NameText", "Player 2",
            28, TextAlignmentOptions.Center, COL_P2_NAME, 300, 40);
        var p2NameRT = p2Name.GetComponent<RectTransform>();
        p2NameRT.anchorMin = p2NameRT.anchorMax = new Vector2(0.5f, 0f);
        p2NameRT.anchoredPosition = new Vector2(0, 80);

        // P2 Class
        GameObject p2Class = CreateTMP(panelP2.transform, "P2ClassText", "เลือกตัวละคร...",
            18, TextAlignmentOptions.Center, COL_GRAY_TEXT, 300, 30);
        var p2ClassRT = p2Class.GetComponent<RectTransform>();
        p2ClassRT.anchorMin = p2ClassRT.anchorMax = new Vector2(0.5f, 0f);
        p2ClassRT.anchoredPosition = new Vector2(0, 48);

        // P2 Ready Icon
        GameObject p2Ready = CreateImage(panelP2.transform, "P2ReadyIcon", 40, 40, COL_READY_GREEN);
        var p2ReadyRT = p2Ready.GetComponent<RectTransform>();
        p2ReadyRT.anchorMin = p2ReadyRT.anchorMax = new Vector2(0.5f, 0f);
        p2ReadyRT.anchoredPosition = new Vector2(0, 15);
        p2Ready.GetComponent<Image>().raycastTarget = false;
        p2Ready.SetActive(false); // ปิดไว้

        // ==================== CardContainer (HorizontalLayoutGroup) ====================
        GameObject cardContainer = CreatePanel(canvasGO.transform, "CardContainer", 650, 300, COL_TRANSPARENT);
        var ccRT = cardContainer.GetComponent<RectTransform>();
        ccRT.anchorMin = ccRT.anchorMax = new Vector2(0.5f, 0.15f);
        ccRT.anchoredPosition = Vector2.zero;
        cardContainer.GetComponent<Image>().raycastTarget = false;

        var hlg = cardContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 25;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // ==================== ReadyButton ====================
        GameObject readyBtnGO = new GameObject("ReadyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        readyBtnGO.transform.SetParent(canvasGO.transform, false);
        var readyBtnRT = readyBtnGO.GetComponent<RectTransform>();
        readyBtnRT.anchorMin = readyBtnRT.anchorMax = new Vector2(0.5f, 0.03f);
        readyBtnRT.sizeDelta = new Vector2(300, 60);
        readyBtnRT.anchoredPosition = Vector2.zero;
        readyBtnGO.GetComponent<Image>().color = COL_READY_BTN;

        GameObject readyBtnText = CreateTMP(readyBtnGO.transform, "ReadyButtonText", "✅ พร้อม!",
            28, TextAlignmentOptions.Center, COL_WHITE, 0, 0);
        StretchFull(readyBtnText);

        // ==================== CountdownText ====================
        GameObject countdownGO = CreateTMP(canvasGO.transform, "CountdownText",
            "🎮 เริ่มเกมใน 2 วินาที...",
            36, TextAlignmentOptions.Center, COL_COUNTDOWN, 600, 60);
        var cdRT = countdownGO.GetComponent<RectTransform>();
        cdRT.anchorMin = cdRT.anchorMax = new Vector2(0.5f, 0.5f);
        cdRT.anchoredPosition = new Vector2(0, -30);
        countdownGO.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        countdownGO.SetActive(false); // ปิดไว้

        // ==================== เพิ่ม CharacterSelectUI Script + Link ====================
        var selectUI = canvasGO.AddComponent<CharacterSelectUI>();

        // Link ทุกอย่างอัตโนมัติ
        selectUI.p1Portrait       = p1Portrait.GetComponent<Image>();
        selectUI.p1NameText       = p1Name.GetComponent<TextMeshProUGUI>();
        selectUI.p1ClassText      = p1Class.GetComponent<TextMeshProUGUI>();
        selectUI.p1ReadyIndicator = p1Ready;

        selectUI.p2Portrait       = p2Portrait.GetComponent<Image>();
        selectUI.p2NameText       = p2Name.GetComponent<TextMeshProUGUI>();
        selectUI.p2ClassText      = p2Class.GetComponent<TextMeshProUGUI>();
        selectUI.p2ReadyIndicator = p2Ready;

        selectUI.vsText           = vsGO.GetComponent<TextMeshProUGUI>();
        selectUI.cardContainer    = cardContainer.transform;
        selectUI.readyButton      = readyBtnGO.GetComponent<Button>();
        selectUI.readyButtonText  = readyBtnText.GetComponent<TextMeshProUGUI>();
        selectUI.countdownText    = countdownGO.GetComponent<TextMeshProUGUI>();

        // ลิงก์ CharacterCard Prefab จาก Assets
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CharacterCard.prefab");
        if (cardPrefab != null)
        {
            selectUI.characterCardPrefab = cardPrefab;
            Debug.Log("<color=lime>[CharSelectBuilder]</color> ✅ CharacterCard Prefab linked อัตโนมัติ");
        }
        else
        {
            Debug.LogWarning("<color=yellow>[CharSelectBuilder]</color> ⚠️ ไม่พบ CharacterCard.prefab → กรุณารัน 'Build CharacterCard Prefab Only' ก่อน แล้วค่อยรันอันนี้อีกครั้ง");
        }

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("<color=lime>[CharSelectBuilder]</color> ✅ Canvas UI สร้างเสร็จ + Link ทุก Reference แล้ว!");
        EditorGUIUtility.PingObject(canvasGO);
        Selection.activeGameObject = canvasGO;
    }

    // ==================== Utilities ====================

    static GameObject CreatePanel(Transform parent, string name, float width, float height, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateImage(Transform parent, string name, float width, float height, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateTMP(Transform parent, string name, string text,
        float fontSize, TextAlignmentOptions align, Color color, float width, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return go;
    }

    static GameObject CreateBorder(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        // Stretch เต็ม + ยื่นออก 4px  
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-4, -4);
        rt.offsetMax = new Vector2(4, 4);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false; // สำคัญ! ไม่ให้บัง Button
        return go;
    }

    static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
