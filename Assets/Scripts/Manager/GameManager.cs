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

    public TextMeshProUGUI countdownText;

    // เก็บสถานะ Phase ปัจจุบัน : Server เขียนได้, ทุกคนอ่านได้
    private NetworkVariable<GamePhase> currentPhase = new NetworkVariable<GamePhase>(GamePhase.Planning);
    public GamePhase CurrentPhase => currentPhase.Value;

    [Header("Debug Player Death State")]
    [Tooltip("แสดงสถานะการตายของ Host (P0) และ Client (P1)")]
    public bool debugP0Dead = false;
    public bool debugP1Dead = false;

    // เก็บสถานะว่าตอนเล่นรอบนั้นใครตายไปแล้วบ้าง
    public NetworkVariable<bool> p0Dead = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> p1Dead = new NetworkVariable<bool>(false);

    [Header("Debug")]
    [Tooltip("แสดงเฉพาะไว้ดูใน Inspector")]
    [SerializeField] private GamePhase displayPhase;

    [Header("Wave System")]
    public TextMeshProUGUI waveText;
    private NetworkVariable<int> currentWave = new NetworkVariable<int>(1);

    [Header("Planning Phase Timer & Readiness")]
    public float planningDuration = 30f;
    public NetworkVariable<float> planningTimer = new NetworkVariable<float>(30f);
    public NetworkVariable<bool> p0Ready = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> p1Ready = new NetworkVariable<bool>(false);
    public TextMeshProUGUI nextPhaseButtonText; // ลาก Text ในปุ่ม Next Phase มาใส่ครับ

    [Header("PvE System Wave")]
    public MinionData[] systemEnemyPool; // สุ่มจาก List นี้ครับ
    // เก็บข้อมูลเวฟที่สุ่มได้ในรูปแบบ "index:count|index:count"
    public NetworkVariable<FixedString512Bytes> systemWaveDraft = new NetworkVariable<FixedString512Bytes>(
        new FixedString512Bytes(""), 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

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

    private bool isManualUnlock = false; // ⭐ สำหรับโหมดปลดล็อคเมาส์อิสระ (ไม่ล็อคกลับเมื่อคลิก)
    public bool IsManualUnlock => isManualUnlock;
    private bool currentModeWantsLock = false; // ⭐ สำหรับโหมดที่ต้องการล็อคเมาส์เป็นกรณีพิเศษ (เช่น Free Fly)
    private void Awake()
    {
        Instance = this;
        // --- เตรียม NetworkList ตามจำนวนมอนเตอร์ที่มีใน Pool ---
        p0SentCounts = new NetworkList<int>();
        p1SentCounts = new NetworkList<int>();
    }

    private void OnEnable()
    {
        Debug.Log("<color=green>[Cursor]</color> GameManager: OnEnable (Script has been enabled)");
        if (currentPhase != null) currentPhase.OnValueChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        Debug.Log($"<color=red>[Cursor]</color> GameManager: OnDisable (Script disabled). ActiveSelf: {gameObject.activeSelf}, ActiveInHierarchy: {gameObject.activeInHierarchy}");
        
        // ตรวจสอบลำดับ Hierarchy ว่าใครเป็นคนพาสั่งปิด
        Transform current = transform;
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        Debug.Log($"<color=red>[Cursor]</color> GameManager Hierarchy Path: {path}");

        if (currentPhase != null) currentPhase.OnValueChanged -= OnPhaseChanged;
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

        displayPhase = currentPhase.Value;
        // currentPhase.OnValueChanged += OnPhaseChanged; // ย้ายไป OnEnable แล้ว
        
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

        // --- 🗺️ เซ็ตค่า UI เริ่มต้น (Minimap/Wave) ---
        if (MinimapUI.Instance != null)
            MinimapUI.Instance.SetVisible(currentPhase.Value == GamePhase.Combat);
        
        if (waveText != null)
            waveText.gameObject.SetActive(currentPhase.Value == GamePhase.Planning);

        // --- 🌊 เซ็ตค่าเวฟต้นเกมสำหรับ Server ---
        if (IsServer && currentWave.Value == 1)
        {
            GenerateSystemWave();
        }

        // ⭐ แจ้งเตือน UI ให้รีเฟรชข้อมูลทั้งหมดตอนเน็ตเวิร์คพร้อม (แก้บัคชุดข้อมูลตอน Join เข้าไปใหม่)
        OnPhaseChangedGlobal?.Invoke(currentPhase.Value);

        Debug.Log($"<color=yellow>[GameManager]</color> Game Started! Initial Phase: <b>{currentPhase.Value}</b> | Draft: {systemWaveDraft.Value}");
    }

    private void OnPhaseChanged(GamePhase previousValue, GamePhase newValue)
    {
        displayPhase = newValue;
        UpdatePhaseUI(newValue);
        if (CameraManager.Instance != null) CameraManager.Instance.SetPhaseCamera(newValue);
        isManualUnlock = false; // รีเซ็ตเมื่อเปลี่ยนเฟส
        currentModeWantsLock = false; // รีเซ็ตโหมดพิเศษเมื่อเปลี่ยนเฟส
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

            p0Dead.Value = false;
            p1Dead.Value = false;

            // Reset Timer and Readiness
            planningTimer.Value = planningDuration;
            p0Ready.Value = false;
            p1Ready.Value = false;

            GenerateSystemWave(); // 🌊 สุ่มเวฟถัดไปทันที
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

    // --- 🗺️ จัดการเปิด/ปิด Minimap และ Wave UI ตามเฟส ---
    if (MinimapUI.Instance != null)
    {
        // Minimap: เปิดเฉพาะ Combat
        MinimapUI.Instance.SetVisible(newValue == GamePhase.Combat);
    }

    if (waveText != null)
    {
        // Wave Text: เปิดเฉพาะ Planning (หรือปิดตอน Combat)
        waveText.gameObject.SetActive(newValue == GamePhase.Planning);
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
    // --- 🎮 ระบบล็อค/ปลดล็อคเมาส์ (Esc) - ย้ายมาไว้บนสุดเพื่อความชัวร์ ---
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        isManualUnlock = !isManualUnlock;
        Debug.Log($"<color=red>[Cursor]</color> Esc Pressed! <b>ManualUnlock: {isManualUnlock}</b> | Phase: {currentPhase.Value}");
        
        if (isManualUnlock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            UpdateCursorState(currentPhase.Value);
        }
    }

    // --- 🎮 [Debug] Heartbeat ทุกๆ 300 เฟรม (ประมาณ 5 วินาที) ---
    if (Time.frameCount % 300 == 0) Debug.Log($"<color=grey>[Cursor]</color> GameManager Alive (Phase: {currentPhase.Value})");
    
    // อัปเดตตัวแปร Debug ให้เห็นใน Inspector เรียลไทม์
    debugP0Dead = p0Dead.Value;
    debugP1Dead = p1Dead.Value;

    // --- ⏱️ ระบบนับเวลาถอยหลัง (เฉพาะ Server) ---
    if (IsServer && currentPhase.Value == GamePhase.Planning)
    {
        if (planningTimer.Value > 0)
        {
            planningTimer.Value -= Time.deltaTime;
        }

        // เงื่อนไขข้ามเฟส: เวลาหมด หรือ พร้อมทั้งคู่
        if (planningTimer.Value <= 0 || (p0Ready.Value && p1Ready.Value))
        {
            ChangePhaseServerRpc(); // เปลี่ยนเป็น Combat
        }
    }

    // ทุกคนอัปเดต UI เวลาถ้าอยู่ในเฟสวางแผน
    if (currentPhase.Value == GamePhase.Planning)
    {
        UpdatePhaseUI(currentPhase.Value);

        // --- 🔘 อัปเดตข้อความปุ่ม Next Phase ของตัวเอง ---
        if (nextPhaseButtonText != null)
        {
            bool amIReady = (NetworkManager.Singleton.LocalClientId == 0) ? p0Ready.Value : p1Ready.Value;
            nextPhaseButtonText.text = amIReady ? "Cancel" : "Next Phase";
        }
    }
    else
    {
        // เฟส Combat: รีเซ็ตปุ่มเป็น Next Phase เผื่อไว้
        if (nextPhaseButtonText != null) nextPhaseButtonText.text = "Next Phase";
    }


    // ถ้าเมาส์เป็นอิสระอยู่ (และไม่ได้เปิดโหมดแมนนวล) ให้กลับมาล็อคใหม่เมื่อคลิกจอ (สำหรับ Combat หรือ Free Fly)
    if (!isManualUnlock && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
    {
        bool shouldModeLock = (currentPhase.Value == GamePhase.Combat) || (currentPhase.Value == GamePhase.Planning && currentModeWantsLock);
        if (shouldModeLock)
        {
            ApplyCursorState(true);
        }
    }
}
    /// <summary>⭐ เรียกจาก Camera Controller เพื่อบอกว่าโหมดนี้ต้องการล็อคเมาส์หรือไม่ (เช่น Free Fly)</summary>
    public void SetCursorMode(bool wantsLock)
    {
        currentModeWantsLock = wantsLock;
        UpdateCursorState(currentPhase.Value);
    }

    private void UpdateCursorState(GamePhase phase)
    {
        // Debug เพื่อดูว่าเรากำลังสั่งปิดอะไรบ้าง
        Debug.Log($"<color=white>[Cursor]</color> UpdateCursorState: Phase={phase}. Crosshair={crosshairObject?.name}, SkillUI={skillUI?.name}, NextButton={nextPhaseButton?.name}");

        // โหมดวางแผนแบบปกติ -> ไม่ล็อคเมาส์ (ยกเว้นเข้า Free Fly)
        if (phase == GamePhase.Planning)
        {
            ApplyCursorState(currentModeWantsLock); 

            SafeSetActive(crosshairObject, false, "GameManager.UpdateCursorState");
            SafeSetActive(skillUI,         false, "GameManager.UpdateCursorState");
            SafeSetActive(nextPhaseButton, true,  "GameManager.UpdateCursorState");
        }
        else // โหมดต่อสู้ -> ล็อคเมาส์
        {
            ApplyCursorState(true); // Default combat is locked mouse

            SafeSetActive(crosshairObject, true,  "GameManager.UpdateCursorState");
            SafeSetActive(skillUI,         true,  "GameManager.UpdateCursorState");
            SafeSetActive(nextPhaseButton, false, "GameManager.UpdateCursorState");
        }
    }

    /// <summary>
    /// สั่งการล็อคเมาส์จริงๆ โดยเช็คสถานะ manualUnlock ด้วย
    /// </summary>
    public void ApplyCursorState(bool shouldLock)
    {
        // ถ้าผู้ใช้สั่งปลดล็อคเมาส์เองถาวร (Esc) ก็ต้องปล่อยให้มัน Unlocked
        if (isManualUnlock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log($"<color=white>[Cursor]</color> ApplyCursorState({shouldLock}) -> Forced <b>Unlocked</b> (ManualUnlock is ON)");
        }
        else
        {
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
            Debug.Log($"<color=white>[Cursor]</color> ApplyCursorState({shouldLock}) -> Result: <b>{Cursor.lockState}</b>");
        }
    }

private void UpdatePhaseUI(GamePhase phase)
{
    string status = "";
    
    if (phase == GamePhase.Planning)
    {
        int seconds = Mathf.CeilToInt(planningTimer.Value);
        status = $"{seconds} S";
    }
    // Combat phase: status = "" (ไม่ต้องโชว์อะไรเลย)
    
    if (countdownText != null) countdownText.text = status;
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

        systemWaveDraft.SetDirty(true); // บังคับให้ Sync ทันที
        systemWaveDraft.Value = new FixedString512Bytes(draft);
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

                // เล่นเสียงเมื่อส่งศัตรูสำเร็จ
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(AudioManager.SoundType.Buy);
                }
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
        if (currentPhase.Value == GamePhase.Planning)
        {
            // Toggle Readiness
            bool currentReady = (NetworkManager.Singleton.LocalClientId == 0) ? p0Ready.Value : p1Ready.Value;
            SetPlayerReadyServerRpc(!currentReady);
        }
        else
        {
            ChangePhaseServerRpc();
        }
    }

[Rpc(SendTo.Server, RequireOwnership = false)]
public void SetPlayerReadyServerRpc(bool ready, RpcParams rpcParams = default)
{
    ulong clientId = rpcParams.Receive.SenderClientId;
    if (clientId == 0) p0Ready.Value = ready;
    else p1Ready.Value = ready;

    Debug.Log($"<color=yellow>[GameManager]</color> Client {clientId} is {(ready ? "READY" : "NOT READY")}");
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
            
            // ⚠️ ไม่ต้อง currentWave++ ตรงนี้ เพราะ OnPhaseChanged จะทำให้อยู่แล้วครับ
            currentPhase.Value = GamePhase.Planning;
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void NotifyPlayerDiedServerRpc(ulong clientId)
    {
        Debug.Log($"<color=cyan>[GameManager]</color> NotifyPlayerDiedServerRpc called for ClientID: {clientId}");
        if (clientId == 0) p0Dead.Value = true;
        else p1Dead.Value = true;
        
        Debug.Log($"<color=cyan>[GameManager]</color> p0Dead={p0Dead.Value}, p1Dead={p1Dead.Value}");
    }

    // ==================== [Safety Utility] ====================
    
    /// <summary>⭐ ใช้แทน SetActive เพื่อป้องกันการเผลอไปปิด Object ที่เป็นทางผ่านของ Manager</summary>
    public static void SafeSetActive(GameObject target, bool active, string callerName = "")
    {
        if (target == null) return;
        
        // ถ้าจะสั่ง "ปิด" ให้เช็คก่อนว่าเป้าหมายกระทบถึง GameManager ไหม
        if (active == false && Instance != null)
        {
            // IsChildOf จะคืนค่า True ถ้า target คือตัวเราเอง หรือเป็นพ่อนางสภา (Hierarchy) ของเรา
            if (target == Instance.gameObject || Instance.transform.IsChildOf(target.transform))
            {
                 Debug.LogError($"<color=red>[Cursor Utility]</color> <b>CRITICAL SAFETY BLOCKED!</b> " +
                                $"Script <b>'{callerName}'</b> tried to deactivate <b>'{target.name}'</b>, " +
                                $"which would KILL GameManager! <b>Action skipped.</b>");
                 return;
            }
        }
        
        target.SetActive(active);
    }
}
