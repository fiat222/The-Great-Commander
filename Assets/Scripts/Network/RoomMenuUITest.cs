using UnityEngine;
using TMPro;
using Unity.Netcode;

/// <summary>
/// [TEST VERSION] ควบคุม UI ของ MenuSceneTest
/// ผูก Script นี้กับ Canvas แล้วลาก Reference ของ Panel/Text/InputField ใน Inspector
///
/// โครงสร้าง Canvas:
///   MainPanel
///     Button_CreateRoom  → OnCreateRoomClicked()
///     Button_JoinRoom    → OnJoinRoomClicked()
///   HostPanel
///     Text_Code (TMP)    → roomCodeText
///     Text_Status (TMP)  → hostStatusText
///     Button_Cancel      → OnCancelHostClicked()
///   ClientPanel
///     InputField (TMP)   → roomCodeInput
///     Text_Status (TMP)  → clientStatusText
///     Button_Connect     → OnConnectClicked()
///     Button_Back        → OnBackClicked()
/// </summary>
public class RoomMenuUITest : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject clientPanel;

    [Header("Host Panel")]
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TextMeshProUGUI hostStatusText;

    [Header("Client Panel")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TextMeshProUGUI clientStatusText;

    private void Start()
    {
        ShowPanel(mainPanel);
        // เปลี่ยนตัวอักษรเป็น uppercase
        if (roomCodeInput != null)
            roomCodeInput.onValueChanged.AddListener(v =>
                roomCodeInput.SetTextWithoutNotify(v.ToUpper()));
    }

    // MAIN PANEL

    public void OnSoloClicked()
    {
        StartNetworkTest.Instance.StartSolo();
    }

    public void OnCreateRoomClicked()
    {
        string code = StartNetworkTest.Instance.CreateRoom();

        if (roomCodeText != null)    roomCodeText.text = code;
        if (hostStatusText != null)  hostStatusText.text = "Waiting for other player ...";

        ShowPanel(hostPanel);
    }

    public void OnJoinRoomClicked()
    {
        if (roomCodeInput != null)    roomCodeInput.text = "";
        if (clientStatusText != null) clientStatusText.text = "";
        ShowPanel(clientPanel);
    }

    // HOST PANEL

    public void OnCancelHostClicked()
    {
        StartNetworkTest.Instance.CancelConnection();
        ShowPanel(mainPanel);
    }

    //คัดลอก Room Code
    public void OnCopyCodeClicked()
    {
        if (roomCodeText == null) return;
        GUIUtility.systemCopyBuffer = roomCodeText.text;
        if (hostStatusText != null) hostStatusText.text = "Copied!";
        // เปลี่ยนกลับหลัง 2 วินาที
        Invoke(nameof(ResetHostStatus), 2f);
    }

    private void ResetHostStatus()
    {
        if (hostStatusText != null) hostStatusText.text = "Waiting for other player ...";
    }

    // CLIENT PANEL

    public void OnConnectClicked()
    {
        if (roomCodeInput == null) return;

        string code = roomCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            SetClientError("Enter Code"); return;
        }
        if (code.Length != 7)
        {
            SetClientError("Room Code must be 7 characters"); return;
        }

        bool ok = StartNetworkTest.Instance.JoinRoom(code);
        if (ok)
        {
            SetClientStatus("Connecting ...");
            // ถ้า NGO disconnect ทันที = connect ไม่ได้ (IP ผิด / ไม่มี Host)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnConnectionFailed;
        }
        else
        {
            SetClientError("Room Code is incorrect. Please try again.");
        }
    }

    public void OnBackClicked()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionFailed;
        StartNetworkTest.Instance.CancelConnection();
        ShowPanel(mainPanel);
    }

    //เรียกเมื่อ NGO disconnect = Host ไม่ตอบ / IP ผิด
    private void OnConnectionFailed(ulong clientId)
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionFailed;
        SetClientError("Connection failed. Please check your code and try again.");
    }

    // HELPERS

    private void ShowPanel(GameObject target)
    {
        if (mainPanel != null)   mainPanel.SetActive(target == mainPanel);
        if (hostPanel != null)   hostPanel.SetActive(target == hostPanel);
        if (clientPanel != null) clientPanel.SetActive(target == clientPanel);
    }

    private void SetClientStatus(string msg)
    {
        if (clientStatusText == null) return;
        clientStatusText.color = Color.black;
        clientStatusText.text = msg;
    }

    private void SetClientError(string msg)
    {
        if (clientStatusText == null) return;
        clientStatusText.color = Color.red;
        clientStatusText.text = msg;
    }
}
