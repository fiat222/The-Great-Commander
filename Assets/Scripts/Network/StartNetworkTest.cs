using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.Sockets;
using System.Linq;

// [TEST VERSION] ระบบ Room Code แบบ LAN
// Room Code = IP + Salt เข้ารหัสเป็น Base36 (7 ตัวอักษร)
// สุ่มใหม่ทุกครั้งที่ Host กด Create Room แม้ IP เดิม
public class StartNetworkTest : MonoBehaviour
{
    public static StartNetworkTest Instance { get; private set; }

    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string characterSelectSceneName = "CharacterSelectScene";
    [SerializeField] private string solocharacterSelectSceneName = "SoloCharactor";
    [SerializeField] private ushort port = 7777;

    public static bool IsSolo { get; private set; }

    private const string BASE36_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int IP_CODE_LENGTH = 7;   // encode IP (32-bit) = log36(2^32) ≈ 6.2
    private const int SALT_CODE_LENGTH = 2; // encode salt (0–1295)
    private const int TOTAL_LENGTH = IP_CODE_LENGTH + SALT_CODE_LENGTH; // 7 ตัว

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // PUBLIC API

    public void StartSolo()
    {
        IsSolo = true;
        Debug.Log("<color=cyan>[RoomTest]</color> Solo Play! Loading SoloCharactor ...");
        SceneManager.LoadScene(solocharacterSelectSceneName, LoadSceneMode.Single);
    }

    // Host: สุ่ม salt → encode IP+salt → StartHost
    public string CreateRoom()
    {
        IsSolo = false;
        string localIP = GetLocalIPAddress();

        int salt = Random.Range(0, 36 * 36); // 0–1295
        string code = EncodeIPToCode(localIP, salt);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(localIP, port);

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        Debug.Log($"<color=cyan>[RoomTest]</color> Created! IP={localIP} Salt={salt} Code={code}");
        return code;
    }

    // Client: decode code → IP แล้ว StartClient
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

    public void CancelConnection()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        if (NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    // ENCODING

    // IP + salt → Base36 Code (9 ตัว) เช่น "192.168.1.5" + salt=42 → "0VB40052A"
    public string EncodeIPToCode(string ipAddress, int salt)
    {
        try
        {
            string[] parts = ipAddress.Split('.');
            if (parts.Length != 4) return null;

            uint ipNum = 0;
            foreach (var p in parts) ipNum = (ipNum << 8) | uint.Parse(p);

            // XOR ip กับ salt เพื่อให้ code ต่างกันทุกครั้ง
            uint encoded = ipNum ^ ((uint)salt << 2);

            string ipPart   = ToBase36(encoded).PadLeft(IP_CODE_LENGTH, '0');
            string saltPart = ToBase36((uint)salt).PadLeft(SALT_CODE_LENGTH, '0');
            return ipPart + saltPart;
        }
        catch { return null; }
    }

    // Base36 Code (9 ตัว) → IP — คืน null ถ้า code ผิด
    public string DecodeCodeToIP(string code)
    {
        try
        {
            if (code.Length != TOTAL_LENGTH) return null;

            string ipPart   = code.Substring(0, IP_CODE_LENGTH);
            string saltPart = code.Substring(IP_CODE_LENGTH, SALT_CODE_LENGTH);

            uint salt    = FromBase36(saltPart);
            uint encoded = FromBase36(ipPart);
            uint ipNum   = encoded ^ (salt << 2);

            return $"{(ipNum >> 24) & 0xFF}.{(ipNum >> 16) & 0xFF}.{(ipNum >> 8) & 0xFF}.{ipNum & 0xFF}";
        }
        catch { return null; }
    }

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