using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ลาก Image ของปุ่มแต่ละปุ่มที่สร้างใน Canvas มาใส่ Inspector
/// Script จะ Highlight สีให้อัตโนมัติตามปุ่มที่กดอยู่
/// </summary>
public class CameraHintsUI : MonoBehaviour
{
    [Header("References")]
    public TopDownCameraController cameraController;

    [Header("Panels")]
    public GameObject mainPanel; // ⭐ ลาก Panel หลักของ UI นี้มาใส่ตรงนี้
    public GameObject rtsModePanel;
    public GameObject freeFlyModePanel;

    [Header("RTS Keys")]
    public Image ui_ScrollUp;
    public Image ui_ScrollDown;
    public Image ui_Pan;
    public Image ui_Q;
    public Image ui_E;
    public Image ui_F_RTS;

    [Header("Free Fly Keys")]
    public Image ui_W;
    public Image ui_A;
    public Image ui_S;
    public Image ui_D;
    public Image ui_Space;
    public Image ui_Shift;
    public Image ui_Ctrl;
    public Image ui_F_FreeFly;

    [Header("Highlight Colors")]
    public Color normalColor    = new Color(1f, 1f, 1f, 0.35f);
    public Color highlightColor = new Color(1f, 0.82f, 0.1f, 1f);

    private void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal += OnPhaseChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        // แสดง UI เฉพาะเฟส Planning เท่านั้น
        if (mainPanel != null)
        {
            GameManager.SafeSetActive(mainPanel, phase == GamePhase.Planning, "CameraHintsUI (mainPanel)");
        }
        else
        {
            // ถ้าไม่ได้ใส่ mainPanel จะแจ้งเตือน
            Debug.LogWarning("[CameraHintsUI] กรุณาลาก Main Panel มาใส่ใน Inspector เพื่อให้ระบบซ่อน UI ได้ถูกต้อง");
        }
    }

    private void Update()
    {
        if (cameraController == null) return;

        bool freeFly = cameraController.IsFreeFly;

        if (rtsModePanel     != null) GameManager.SafeSetActive(rtsModePanel,     !freeFly, "CameraHintsUI");
        if (freeFlyModePanel != null) GameManager.SafeSetActive(freeFlyModePanel, freeFly,  "CameraHintsUI");

        if (!freeFly) UpdateRTS();
        else          UpdateFreeFly();
    }

    private void UpdateRTS()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        var kb    = UnityEngine.InputSystem.Keyboard.current;

        float scroll = mouse.scroll.ReadValue().y;
        Highlight(ui_ScrollUp,   scroll > 0.01f);
        Highlight(ui_ScrollDown, scroll < -0.01f);
        Highlight(ui_Pan,        mouse.middleButton.isPressed);
        Highlight(ui_Q,          kb.qKey.isPressed);
        Highlight(ui_E,          kb.eKey.isPressed);
        Highlight(ui_F_RTS,      kb.fKey.isPressed);
    }

    private void UpdateFreeFly()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;

        Highlight(ui_W,         kb.wKey.isPressed);
        Highlight(ui_A,         kb.aKey.isPressed);
        Highlight(ui_S,         kb.sKey.isPressed);
        Highlight(ui_D,         kb.dKey.isPressed);
        Highlight(ui_Space,     kb.spaceKey.isPressed);
        Highlight(ui_Shift,     kb.leftShiftKey.isPressed);
        Highlight(ui_Ctrl,      kb.leftCtrlKey.isPressed);
        Highlight(ui_F_FreeFly, kb.fKey.isPressed);
    }

    private void Highlight(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? highlightColor : normalColor;
    }
}