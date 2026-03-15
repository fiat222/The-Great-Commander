using UnityEngine;
using TMPro;
using Unity.Netcode;

/// <summary>
/// ควบคุม UI ทั้งหมดของ Main Menu Scene
/// ผูก Script นี้กับ Canvas แล้วลาก Reference ของ Panel ต่างๆ ใน Inspector
///
/// โครงสร้าง Canvas:
///   MainPanel
///     Button_StartGame   → OnStartGameClicked()
///     Button_Audio       → OnAudioClicked()
///     Button_Quit        → OnQuitClicked()
///   StartGamePanel
///     Button_Solo        → OnSoloClicked()
///     Button_Duo         → OnDuoClicked()
///     Button_Back        → OnBackToMainClicked()
///   DuoPanel
///     Button_CreateRoom  → OnCreateRoomClicked()
///     Button_JoinRoom    → OnJoinRoomClicked()
///     Button_Back        → OnBackToStartGameClicked()
///   HostPanel
///     Text_Code (TMP)    → roomCodeText
///     Text_Status (TMP)  → hostStatusText
///     Button_Copy        → OnCopyCodeClicked()
///     Button_Cancel      → OnCancelHostClicked()
///   ClientPanel
///     InputField (TMP)   → roomCodeInput
///     Text_Status (TMP)  → clientStatusText
///     Button_Connect     → OnConnectClicked()
///     Button_Back        → OnBackToDuoClicked()
///   AudioPanel (Popup)
///     เปิดผ่าน AudioSettingsUI.cs
///     Button_Close       → OnCloseAudioClicked()
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Panels
    // ─────────────────────────────────────────

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject startGamePanel;
    [SerializeField] private GameObject duoPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject clientPanel;
    [SerializeField] private GameObject audioPanel;

    // ─────────────────────────────────────────
    //  Host Panel References
    // ─────────────────────────────────────────

    [Header("Host Panel")]
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TextMeshProUGUI hostStatusText;    // ใช้แสดงข้อความสั้น เช่น Copied!
    [SerializeField] private TypewriterStatus hostTypewriter;   // ใช้ตอนรอเชื่อมต่อ

    // ─────────────────────────────────────────
    //  Client Panel References
    // ─────────────────────────────────────────

    [Header("Client Panel")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TextMeshProUGUI clientStatusText;  // ใช้แสดง error
    [SerializeField] private TypewriterStatus clientTypewriter; // ใช้ตอน Connecting...
    [SerializeField] private GameObject connectButton;          // ปุ่ม check sign — ซ่อนตอน connecting

    // ─────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────

    private void Start()
    {
        ShowPanel(mainPanel);

        // บังคับ Input เป็น Uppercase ตลอดเวลา
        if (roomCodeInput != null)
            roomCodeInput.onValueChanged.AddListener(v =>
                roomCodeInput.SetTextWithoutNotify(v.ToUpper()));
    }

    // ─────────────────────────────────────────
    //  MAIN PANEL
    // ─────────────────────────────────────────

    public void OnStartGameClicked()
    {
        ShowPanel(startGamePanel);
    }

    public void OnAudioClicked()
    {
        if (audioPanel != null) audioPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────
    //  START GAME PANEL
    // ─────────────────────────────────────────

    public void OnSoloClicked()
    {
        StartNetworkTest.Instance.StartSolo();
    }

    public void OnDuoClicked()
    {
        ShowPanel(duoPanel);
    }

    public void OnBackToMainClicked()
    {
        ShowPanel(mainPanel);
    }

    // ─────────────────────────────────────────
    //  DUO PANEL
    // ─────────────────────────────────────────

    public void OnCreateRoomClicked()
    {
        string code = StartNetworkTest.Instance.CreateRoom();
        if (roomCodeText != null) roomCodeText.text = code;

        // เปิด Panel ก่อน แล้วค่อยเริ่ม Typewriter (Panel ต้อง Active ก่อน StartCoroutine)
        ShowPanel(hostPanel);

        if (hostTypewriter != null) hostTypewriter.Play("Waiting for other player");
        else if (hostStatusText != null) hostStatusText.text = "Waiting for other player...";
    }

    public void OnJoinRoomClicked()
    {
        if (roomCodeInput != null)    roomCodeInput.text = "";
        if (clientStatusText != null) clientStatusText.text = "";
        ShowPanel(clientPanel);
    }

    public void OnBackToStartGameClicked()
    {
        ShowPanel(startGamePanel);
    }

    // ─────────────────────────────────────────
    //  HOST PANEL
    // ─────────────────────────────────────────

    public void OnCopyCodeClicked()
    {
        if (roomCodeText == null) return;
        GUIUtility.systemCopyBuffer = roomCodeText.text;

        // หยุด typewriter ชั่วคราว แล้วแสดง "Copied!"
        if (hostTypewriter != null) hostTypewriter.Stop();
        if (hostStatusText != null) hostStatusText.text = "Copied!";
        Invoke(nameof(ResetHostStatus), 2f);
    }

    public void OnCancelHostClicked()
    {
        if (hostTypewriter != null) hostTypewriter.Stop();
        StartNetworkTest.Instance.CancelConnection();
        ShowPanel(duoPanel);
    }

    private void ResetHostStatus()
    {
        if (hostTypewriter != null) hostTypewriter.Play("Waiting for other player");
        else if (hostStatusText != null) hostStatusText.text = "Waiting for other player...";
    }

    // ─────────────────────────────────────────
    //  CLIENT PANEL
    // ─────────────────────────────────────────

    public void OnConnectClicked()
    {
        if (roomCodeInput == null) return;

        string code = roomCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            SetClientError("Enter Code"); return;
        }
        if (code.Length != 9)
        {
            SetClientError("Room Code must be 9 characters"); return;
        }

        bool ok = StartNetworkTest.Instance.JoinRoom(code);
        if (ok)
        {
            // ซ่อนปุ่ม Connect แล้วเริ่ม Typewriter
            if (connectButton != null) connectButton.SetActive(false);
            if (clientTypewriter != null) clientTypewriter.Play("Connecting");
            else SetClientStatus("Connecting ...");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnConnectionFailed;
        }
        else
        {
            SetClientError("Room Code is incorrect. Please try again.");
        }
    }

    public void OnBackToDuoClicked()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionFailed;
        if (clientTypewriter != null) clientTypewriter.Stop();
        if (connectButton != null) connectButton.SetActive(true); // แสดงปุ่มคืนเมื่อปิด panel
        StartNetworkTest.Instance.CancelConnection();
        ShowPanel(duoPanel);
    }

    private void OnConnectionFailed(ulong clientId)
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionFailed;
        if (clientTypewriter != null) clientTypewriter.Stop();
        if (connectButton != null) connectButton.SetActive(true); // แสดงปุ่มคืน เพื่อให้ลองใหม่
        SetClientError("Connection failed. Please check your code and try again.");
    }

    // ─────────────────────────────────────────
    //  AUDIO PANEL
    // ─────────────────────────────────────────

    public void OnCloseAudioClicked()
    {
        if (audioPanel != null) audioPanel.SetActive(false);
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────

    /// <summary>เปิด Panel ที่ระบุ ปิดอีก Panel ที่เหลือ (ยกเว้น AudioPanel ที่จัดการแยก)</summary>
    private void ShowPanel(GameObject target)
    {
        if (mainPanel != null)      mainPanel.SetActive(target == mainPanel);
        if (startGamePanel != null) startGamePanel.SetActive(target == startGamePanel);
        if (duoPanel != null)       duoPanel.SetActive(target == duoPanel);
        if (hostPanel != null)      hostPanel.SetActive(target == hostPanel);
        if (clientPanel != null)    clientPanel.SetActive(target == clientPanel);
        // audioPanel ไม่อยู่ใน ShowPanel — เปิด/ปิดแยก ทับ Panel อื่นได้
    }

    private void SetClientStatus(string msg)
    {
        if (clientStatusText == null) return;
        clientStatusText.color = Color.white;
        clientStatusText.text  = msg;
    }

    private void SetClientError(string msg)
    {
        if (clientStatusText == null) return;
        clientStatusText.color = Color.red;
        clientStatusText.text  = msg;
    }
}
