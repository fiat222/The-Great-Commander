using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.Sockets;
using System.Linq;

// [TEST VERSION] ระบบ Room Code แบบ LAN
// Room Code = IP Address ที่เข้ารหัสเป็น Base36 (7 ตัวอักษร)
// ไม่ยุ่งกับ StartNetwork.cs ตัวเดิม
public class StartNetworkTest : MonoBehaviour
{
    public static StartNetworkTest Instance { get; private set; }

    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string characterSelectSceneName = "CharacterSelectScene";
    [SerializeField] private string solocharacterSelectSceneName = "SoloCharactor";
    [SerializeField] private ushort port = 7777;

    /// <summary>True = Solo Play (Host-only), False = Duo Play (Networked 2 players)</summary>
    public static bool IsSolo { get; private set; }

    private const string BASE36_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int CODE_LENGTH = 7;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // PUBLIC API

    // Solo Play: ไม่ใช้ Netcode — โหลด SoloCharactor Scene โดยตรง
    public void StartSolo()
    {
        IsSolo = true;
        Debug.Log("<color=cyan>[RoomTest]</color> Solo Play! Loading SoloCharactor ...");
        SceneManager.LoadScene(solocharacterSelectSceneName, LoadSceneMode.Single);
    }

    // Host: StartHost แล้วรอ Client เชื่อมต่อ
    // Room Code 7 ตัวที่ Client ต้องพิมพ์
    public string CreateRoom()
    {
        IsSolo = false;
        string localIP = GetLocalIPAddress();

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(localIP, port);

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        string code = EncodeIPToCode(localIP);
        Debug.Log($"<color=cyan>[RoomTest]</color> Created! IP={localIP} Code={code}");
        return code;
    }

    // Client: ถอดรหัส Room Code → IP แล้ว StartClient
    // คืน false ถ้า code ไม่ถูกต้อง
    public bool JoinRoom(string roomCode)
    {
        string ip = DecodeCodeToIP(roomCode.Trim().ToUpper());
        if (ip == null)
        {
            Debug.LogError($"<color=red>[RoomTest]</color> Invalid Code: {roomCode}");
            return false;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        NetworkManager.Singleton.StartClient();
        Debug.Log($"<color=cyan>[RoomTest]</color> Joining Code={roomCode} → IP={ip}");
        return true;
    }

    //ยกเลิก connection แล้วกลับ Main Panel
    public void CancelConnection()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        if (NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    // ENCODING HELPERS

    // IP → Base36 Code (7 ตัว) เช่น "192.168.1.5" → "0VB4005"
    public string EncodeIPToCode(string ipAddress)
    {
        try
        {
            string[] parts = ipAddress.Split('.');
            if (parts.Length != 4) return null;
            uint ipNum = 0;
            foreach (var p in parts) ipNum = (ipNum << 8) | uint.Parse(p);
            return ToBase36(ipNum).PadLeft(CODE_LENGTH, '0');
        }
        catch { return null; }
    }

    // Base36 Code → IP เช่น "0VB4005" → "192.168.1.5" — คืน null ถ้า code ผิด
    public string DecodeCodeToIP(string code)
    {
        try
        {
            uint ipNum = FromBase36(code.ToUpper());
            return $"{(ipNum >> 24) & 0xFF}.{(ipNum >> 16) & 0xFF}.{(ipNum >> 8) & 0xFF}.{ipNum & 0xFF}";
        }
        catch { return null; }
    }

    //ดึง LAN IP ของเครื่อง
    public string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(
                a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
            return ip?.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }

    // PRIVATE

    //เมื่อ Client เชื่อมต่อสำเร็จ — ถ้ามี 2 คนแล้วให้โหลด CharacterSelectScene
    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            Debug.Log("<color=green>[RoomTest]</color> 2 Players ready! Loading CharacterSelectScene...");
            NetworkManager.Singleton.SceneManager.LoadScene(characterSelectSceneName, LoadSceneMode.Single);
        }
    }

    private string ToBase36(uint value)
    {
        if (value == 0) return "0";
        string result = "";
        while (value > 0) { result = BASE36_CHARS[(int)(value % 36)] + result; value /= 36; }
        return result;
    }

    private uint FromBase36(string code)
    {
        uint result = 0;
        foreach (char c in code)
        {
            int idx = BASE36_CHARS.IndexOf(c);
            if (idx < 0) throw new System.Exception($"Invalid char: {c}");
            result = result * 36 + (uint)idx;
        }
        return result;
    }
}
