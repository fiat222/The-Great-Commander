using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Collections;

public enum GamePhase
{
    Planning,
    Combat
}
public class GameManager : NetworkBehaviour
{
// Camera references removed, now handled by CameraManager

    public static GameManager Instance { get; private set; }
    public static System.Action<int> OnSystemEnemyDied; // สำหรับแจ้ง UI เมื่อมอนสเตอร์รายทางตายครับ
    public static System.Action<GamePhase> OnPhaseChangedGlobal; // ⭐ สำหรับแจ้ง UI เมื่อเปลี่ยนเฟส

    public TextMeshProUGUI phaseStatusText;

    // เก็บสถานะ Phase ปัจจุบัน : Server เขียนได้, ทุกคนอ่านได้
    private NetworkVariable<GamePhase> currentPhase = new NetworkVariable<GamePhase>(GamePhase.Planning);
    public GamePhase CurrentPhase => currentPhase.Value;

    [Header("Wave System")]
    public TextMeshProUGUI waveText;
    private NetworkVariable<int> currentWave = new NetworkVariable<int>(1);

    [Header("PvE System Wave")]
    public MinionData[] systemEnemyPool; // สุ่มจาก List นี้ครับ
    // เก็บข้อมูลเวฟที่สุ่มได้ในรูปแบบ "index:count|index:count"
    public NetworkVariable<FixedString512Bytes> systemWaveDraft = new NetworkVariable<FixedString512Bytes>("");

    [Header("Enemy Stats SOs")]
    [Tooltip("ลาก EnemyStatsSO ให้ตรงลำดับกับ systemEnemyPool ทุกตัวครับ")]
    public EnemyStatsSO[] enemyStatsSOs;

    [Header("Enemy Sending System")]
    private EnemySpawner globalSpawner; 
    
    // จำนวนศัตรูที่ค้างส่งแยกตามประเภท (ใช้ NetworkList เพื่อรองรับไม่จำกัดประเภท)
    public NetworkList<int> p0SentCounts; // Player 0 (Host)
    public NetworkList<int> p1SentCounts; // Player 1 (Client)

    // Economy Settings ย้ายไปอยู่ใน MinionData แล้วครับ

    // Crosshair + Skill UI สำหรับปิด/เปิด ในแต่ละเฟส
    [SerializeField] private GameObject crosshairObject;
    [SerializeField] private GameObject skillUI; // รูปสกิล (โชว์เฉพาะ Combat)
    [SerializeField] private GameObject nextPhaseButton; // ปุ่มเปลี่ยนเฟส (โชว์เฉพาะ Planning)
    private void Awake()
    {
        Instance = this;

        // --- เตรียม NetworkList ตามจำนวนมอนเตอร์ที่มีใน Pool ---
        p0SentCounts = new NetworkList<int>();
        p1SentCounts = new NetworkList<int>();
    }

    void Start()
    {
        Debug.Log($"[Minimap] Start! IsServer={IsServer} IsClient={IsClient} IsHost={IsHost}");
    }

    public override void OnNetworkSpawn()
    {
        // หา Spawner ตัวเดียวในซีน
        globalSpawner = FindFirstObjectByType<EnemySpawner>();
        if (globalSpawner != null) Debug.Log("<color=green>[GameManager]</color> Global Spawner Linked.");

        currentPhase.OnValueChanged += OnPhaseChanged;
        
        // ผูก Event ให้ UI อัปเดตเมื่อค่าใน List เปลี่ยนครับ
        p0SentCounts.OnListChanged += (changeEvent) => UpdatePhaseUI(currentPhase.Value);
        p1SentCounts.OnListChanged += (changeEvent) => UpdatePhaseUI(currentPhase.Value);

        // --- 🌊 เซ็ตค่าเริ่มต้นสำหรับ NetworkList (เฉพาะ Server) ---
        if (IsServer)
        {
            for (int i = 0; i < systemEnemyPool.Length; i++)
            {
                p0SentCounts.Add(0);
                p1SentCounts.Add(0);
            }
        }

        currentWave.OnValueChanged += (old, newVal) => UpdateWaveUI(newVal);

        UpdatePhaseUI(currentPhase.Value);
        UpdateWaveUI(currentWave.Value);

        if (CameraManager.Instance != null) CameraManager.Instance.SetPhaseCamera(currentPhase.Value);
        UpdateCursorState(currentPhase.Value);

        // --- 🛒 เซ็ตค่าร้านค้าเริ่มต้นตอนเข้าเกม ---
        if (ShopManager.Instance != null)
        {
            if (currentPhase.Value == GamePhase.Planning)
                ShopManager.Instance.OpenShop();
            else
                ShopManager.Instance.CloseShop();
        }

        // --- 🌊 เซ็ตค่าเวฟต้นเกมสำหรับ Server ---
        if (IsServer && currentWave.Value == 1)
        {
            GenerateSystemWave();
        }

        Debug.Log($"<color=yellow>[GameManager]</color> Game Started! Initial Phase: <b>{currentPhase.Value}</b>");
    }

    private void OnPhaseChanged(GamePhase previousValue, GamePhase newValue)
    {
        UpdatePhaseUI(newValue);
        if (CameraManager.Instance != null) CameraManager.Instance.SetPhaseCamera(newValue);
        UpdateCursorState(newValue);

        if (newValue == GamePhase.Planning)
        {
            EnemyTracker.Instance?.ResetForNewWaveServerRpc();
            if (IsServer)
            {
                currentWave.Value++;

                // ⭐ Notify EnemyStatsSOs ด้วย (ก่อน GenerateSystemWave)
                if (enemyStatsSOs != null)
                    foreach (var so in enemyStatsSOs)
                        if (so != null) so.SetWave(currentWave.Value);
            }
            CleanupEnemies();
        }

        // --- 🛒 จัดการเปิด/ปิดร้านค้าอัตโนมัติตามเฟส ---
        if (ShopManager.Instance != null)
        {
            if (newValue == GamePhase.Planning)
                ShopManager.Instance.OpenShop();
            else
                ShopManager.Instance.CloseShop();
        }

        OnPhaseChangedGlobal?.Invoke(newValue);
    }

    private void CleanupEnemies()
    {
        if (!IsServer) return; // เฉพาะ Server เท่านั้นที่ Despawn ได้

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            NetworkObject netObj = enemy.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(); // sync ทุก client
            else
                Destroy(enemy);  // fallback สำหรับ non-network object
        }
    }

    private void Update()
    {
        // ถ้าอยู่ในเฟส Combat และกด Esc ให้ปลดล็อกเมาส์มาชั่วคราวเพื่อกดปุ่มได้
        if (currentPhase.Value == GamePhase.Combat && Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void UpdateCursorState(GamePhase phase)
    {
        if (phase == GamePhase.Planning)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // ปิด crosshair + skill UI ในเฟส Planning
            if (crosshairObject != null)
                crosshairObject.SetActive(false);
            if (skillUI != null)
                skillUI.SetActive(false);
            // โชว์ปุ่ม Next Phase ในเฟส Planning
            if (nextPhaseButton != null)
                nextPhaseButton.SetActive(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // เปิด crosshair + skill UI ในเฟส Combat
            if (crosshairObject != null)
                crosshairObject.SetActive(true);
            if (skillUI != null)
                skillUI.SetActive(true);
            // ซ่อนปุ่ม Next Phase ในเฟส Combat
            if (nextPhaseButton != null)
                nextPhaseButton.SetActive(false);
        }
    }

    // SwitchCamera logic moved to CameraManager
    private void UpdatePhaseUI(GamePhase phase)
    {
        string status = phase.ToString() + " Phase\n";
        
        // แสดงข้อมูลสรุปสั้นๆ (ถ้าพี่อยากโชว์ทั้งหมดต้องวนลูปครับ)
        int hostTotal = 0; foreach(int c in p0SentCounts) hostTotal += c;
        int clientTotal = 0; foreach(int c in p1SentCounts) clientTotal += c;
        
        status += $"Host Total Sent: {hostTotal} | Client Total Sent: {clientTotal}";
        phaseStatusText.text = status;
    }

    private void UpdateWaveUI(int wave)
    {
        if (waveText != null)
            waveText.text = "Wave " + wave;
    }

    // --- 🎲 ระบบสุ่มเวฟ (PvE) ---
    private void GenerateSystemWave()
    {
        if (!IsServer) return;
        if (systemEnemyPool == null || systemEnemyPool.Length == 0) return;

        int totalToSpawn = 1 + (currentWave.Value - 1) * 2;
        int[] counts = new int[systemEnemyPool.Length];

        // สุ่มแจกจ่ายจำนวนให้ครบ totalToSpawn
        for (int i = 0; i < totalToSpawn; i++)
        {
            int randIndex = Random.Range(0, systemEnemyPool.Length);
            counts[randIndex]++;
        }

        // แปลงเป็น String: "0:3|1:3"
        string draft = "";
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] > 0)
            {
                if (draft != "") draft += "|";
                draft += $"{i}:{counts[i]}";
            }
        }

        systemWaveDraft.Value = draft;
        Debug.Log($"<color=orange>[GameManager]</color> Generated Wave {currentWave.Value}: {draft} (Total: {totalToSpawn})");

        if (enemyStatsSOs != null)
        {
            foreach (var so in enemyStatsSOs)
            {
                if (so != null) so.SetWave(currentWave.Value);
            }
        }

    }

    // แก้ฟังก์ชันนี้ให้รับ parameter เพื่อรู้ว่ากดปุ่มไหนมาครับ
    public void RequestBuyEnemy(int typeIndex)
    {
        // 1. เช็คว่า Index ถูกต้องและดึงราคาจาก MinionData โดยตรงครับ
        if (systemEnemyPool == null || typeIndex >= systemEnemyPool.Length) return;
        int cost = systemEnemyPool[typeIndex].cost;

        // 2. เช็คเงินจาก PlacementManager
        if (PlacementManager.Instance != null)
        {
            if (PlacementManager.Instance.Money >= cost)
            {
                PlacementManager.Instance.Money -= cost;
                PlacementManager.Instance.OnMoneyChanged?.Invoke(PlacementManager.Instance.Money);

                ulong myId = NetworkManager.Singleton.LocalClientId;
                BuyEnemyServerRpc(myId, typeIndex);
            }
            else
            {
                // เงินไม่พอ -> ด่า เอ๊ย แจ้งเตือนครับ
                Debug.LogWarning($"<color=red>[Shop]</color> เงินไม่พอ! ต้องการ {cost} แต่มีแค่ {PlacementManager.Instance.Money}");
            }
        }
    }

    [Rpc(SendTo.Server)]
    void BuyEnemyServerRpc(ulong clientId, int typeIndex)
    {
        if (currentPhase.Value != GamePhase.Planning) return;
        if (typeIndex >= p0SentCounts.Count) return;

        if (clientId == 0)
            p0SentCounts[typeIndex]++;
        else
            p1SentCounts[typeIndex]++;
    }

    public void RequestNextPhase()
    {
        ChangePhaseServerRpc();
    }

    [Rpc(SendTo.Server)]
    void ChangePhaseServerRpc()
    {
        if (currentPhase.Value == GamePhase.Planning)
        {
            if (globalSpawner != null)
            {
                // Host (ID 0) ส่งไปหา Client (ID 1)
                for (int i = 0; i < p0SentCounts.Count; i++)
                {
                    if (p0SentCounts[i] > 0) globalSpawner.SpawnEnemiesRpc(p0SentCounts[i], i, 1);
                }
                
                // Client (ID 1) ส่งไปหา Host (ID 0)
                for (int i = 0; i < p1SentCounts.Count; i++)
                {
                    if (p1SentCounts[i] > 0) globalSpawner.SpawnEnemiesRpc(p1SentCounts[i], i, 0);
                }

                // --- 🌊 สั่งสปอนศัตรูรายทาง (PvE) สำหรับทุกคน ---
                globalSpawner.SpawnSystemEnemiesRpc(systemWaveDraft.Value.ToString());
            } 

            currentPhase.Value = GamePhase.Combat;
        }
        else
        {
            // Reset ค่าใน List เป็น 0 เมื่อกลับสู่ Planning
            for (int i = 0; i < p0SentCounts.Count; i++) p0SentCounts[i] = 0;
            for (int i = 0; i < p1SentCounts.Count; i++) p1SentCounts[i] = 0;
            
            // --- 🌊 ขึ้นเวฟใหม่เมื่อกลับสู่ช่วงวางแผน ---
            currentWave.Value++;
            GenerateSystemWave(); // สุ่มเวฟถัดไปทันที
            
            currentPhase.Value = GamePhase.Planning;
        }
    }
}
