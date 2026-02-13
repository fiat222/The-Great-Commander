using UnityEngine;
using Unity.Netcode;              // ใช้ระบบ Multiplayer ของ Unity (NGO)
using UnityEngine.SceneManagement;

public class StartNetwork : MonoBehaviour
{
    private void Awake()
    {
        // ทำให้ GameObject นี้ (ซึ่งมี NetworkManager อยู่ด้วย)
        // ไม่ถูกทำลายเมื่อเปลี่ยน Scene
        // สำคัญมาก เพราะถ้า NetworkManager หาย Multiplayer จะหลุดทันที
        DontDestroyOnLoad(gameObject);
    }

    public void StartHost()
    {
        // เริ่มโหมด Host
        // Host = Server + Client ในเครื่องเดียวกัน
        NetworkManager.Singleton.StartHost();

        // สั่งให้ Server โหลด Scene ชื่อ "GameScene"
        // และจะ Sync Scene ไปยัง Client อัตโนมัติ (เพราะเปิด Enable Scene Management)
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public void StartClient()
    {
        // เริ่มโหมด Client
        // Client จะพยายามเชื่อมต่อไปยัง Host
        NetworkManager.Singleton.StartClient();
    }
}
