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
    public GameObject mainPanel;

    [Header("Free Fly Keys")]
    public Image ui_W;
    public Image ui_A;
    public Image ui_S;
    public Image ui_D;
    public Image ui_Space;
    public Image ui_Shift;
    public Image ui_Ctrl;
    public Image ui_Esc;

    [Header("Esc Label — เปลี่ยน Text ตามสถานะเมาส์")]
    [Tooltip("ลาก TMP_Text ที่อยู่ใต้ปุ่ม Esc มาใส่ตรงนี้")]
    public TMP_Text ui_EscLabel;
    public string textWhenLocked   = "Unlock Mouse";
    public string textWhenUnlocked = "Lock Mouse";

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
        if (mainPanel != null)
            mainPanel.SetActive(phase == GamePhase.Planning);
        else
            Debug.LogWarning("[CameraHintsUI] กรุณาลาก Main Panel มาใส่ใน Inspector");
    }

    private void Update()
    {
        if (cameraController == null) return;

        var kb = UnityEngine.InputSystem.Keyboard.current;

        Highlight(ui_W,     kb.wKey.isPressed);
        Highlight(ui_A,     kb.aKey.isPressed);
        Highlight(ui_S,     kb.sKey.isPressed);
        Highlight(ui_D,     kb.dKey.isPressed);
        Highlight(ui_Space, kb.spaceKey.isPressed);
        Highlight(ui_Shift, kb.leftShiftKey.isPressed);
        Highlight(ui_Ctrl,  kb.leftCtrlKey.isPressed);
        Highlight(ui_Esc,   kb.escapeKey.isPressed);

        // อัปเดต label ของ Esc ตามสถานะเมาส์ปัจจุบัน
        if (ui_EscLabel != null)
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            ui_EscLabel.text = isLocked ? textWhenLocked : textWhenUnlocked;
        }
    }

    private void Highlight(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? highlightColor : normalColor;
    }
}