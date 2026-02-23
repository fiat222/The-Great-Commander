using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public enum GamePhase
{
    Planning,
    Combat
}
public class GameManager : NetworkBehaviour
{
// Camera references removed, now handled by CameraManager

    public static GameManager Instance { get; private set; }

    public TextMeshProUGUI phaseStatusText;

    // เก็บสถานะ Phase ปัจจุบัน : Server เขียนได้, ทุกคนอ่านได้
    private NetworkVariable<GamePhase> currentPhase = new NetworkVariable<GamePhase>(GamePhase.Planning);
    public GamePhase CurrentPhase => currentPhase.Value;

    [Header("Wave System")]
    public TextMeshProUGUI waveText;
    private NetworkVariable<int> currentWave = new NetworkVariable<int>(1);

    [Header("Enemy Sending System")]
    private EnemySpawner globalSpawner; 
    
    // จำนวนศัตรูที่ค้างส่งแยกตามประเภท (0 = Footman, 1 = Turtle)
    // Player 0 (Host)
    private NetworkVariable<int> p0Type0Count = new NetworkVariable<int>(0);
    private NetworkVariable<int> p0Type1Count = new NetworkVariable<int>(0);
    // Player 1 (Client)
    private NetworkVariable<int> p1Type0Count = new NetworkVariable<int>(0);
    private NetworkVariable<int> p1Type1Count = new NetworkVariable<int>(0);

    [Header("Economy Settings")]
    public int footmanCost = 10;
    public int turtleCost = 20;

    // Crosshair สำหรับปิด/เปิด ในแต่ละเฟส
    [SerializeField] private GameObject crosshairObject;
    private void Awake()
    {
        Instance = this;
        //if (crosshairObject != null)
        //    crosshairObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        // หา Spawner ตัวเดียวในซีน
        globalSpawner = FindFirstObjectByType<EnemySpawner>();
        if (globalSpawner != null) Debug.Log("<color=green>[GameManager]</color> Global Spawner Linked.");

        currentPhase.OnValueChanged += OnPhaseChanged;
        
        // ผูก Event ให้ UI อัปเดตเมื่อค่าเปลี่ยนครับ
        p0Type0Count.OnValueChanged += (old, newVal) => UpdatePhaseUI(currentPhase.Value);
        p0Type1Count.OnValueChanged += (old, newVal) => UpdatePhaseUI(currentPhase.Value);
        p1Type0Count.OnValueChanged += (old, newVal) => UpdatePhaseUI(currentPhase.Value);
        p1Type1Count.OnValueChanged += (old, newVal) => UpdatePhaseUI(currentPhase.Value);

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

        Debug.Log($"<color=yellow>[GameManager]</color> Game Started! Initial Phase: <b>{currentPhase.Value}</b>");
    }

    private void OnPhaseChanged(GamePhase previousValue, GamePhase newValue)
    {
        UpdatePhaseUI(newValue);
        if (CameraManager.Instance != null) CameraManager.Instance.SetPhaseCamera(newValue);
        UpdateCursorState(newValue);
        
        // --- 🛒 จัดการเปิด/ปิดร้านค้าอัตโนมัติตามเฟส ---
        if (ShopManager.Instance != null)
        {
            if (newValue == GamePhase.Planning)
                ShopManager.Instance.OpenShop();
            else
                ShopManager.Instance.CloseShop();
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
            // ปิด crosshair ในเฟส Planning
            if (crosshairObject != null)
                crosshairObject.SetActive(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // เปิด crosshair ในเฟส Combat
            if (crosshairObject != null)
                crosshairObject.SetActive(true);
        }
    }

    // SwitchCamera logic moved to CameraManager
    private void UpdatePhaseUI(GamePhase phase)
    {
        string status = phase.ToString() + " Phase\n";
        status += $"Host Types: [{p0Type0Count.Value}, {p0Type1Count.Value}] | Client Types: [{p1Type0Count.Value}, {p1Type1Count.Value}]";
        phaseStatusText.text = status;
    }

    private void UpdateWaveUI(int wave)
    {
        if (waveText != null)
            waveText.text = "Wave " + wave;
    }

    // แก้ฟังก์ชันนี้ให้รับ parameter เพื่อรู้ว่ากดปุ่มไหนมาครับ
    public void RequestBuyEnemy(int typeIndex)
    {
        // 1. เช็คราคาก่อนครับ
        int cost = (typeIndex == 0) ? footmanCost : turtleCost;

        // 2. เช็คเงินจาก PlacementManager (ระบบเงินหลักของเรา)
        if (PlacementManager.Instance != null)
        {
            if (PlacementManager.Instance.Money >= cost)
            {
                // เงินพอ -> หักตังและส่ง ServerRpc
                PlacementManager.Instance.Money -= cost;
                PlacementManager.Instance.OnMoneyChanged?.Invoke(PlacementManager.Instance.Money);

                ulong myId = NetworkManager.Singleton.LocalClientId;
                Debug.Log($"<color=cyan>[Shop]</color> You sent enemy type {typeIndex}! (Cost: {cost}) Money left: {PlacementManager.Instance.Money}");
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

        if (clientId == 0)
        {
            if (typeIndex == 0) p0Type0Count.Value++;
            else if (typeIndex == 1) p0Type1Count.Value++;
        }
        else
        {
            if (typeIndex == 0) p1Type0Count.Value++;
            else if (typeIndex == 1) p1Type1Count.Value++;
        }
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
                if (p0Type0Count.Value > 0) globalSpawner.SpawnEnemiesRpc(p0Type0Count.Value, 0, 1);
                if (p0Type1Count.Value > 0) globalSpawner.SpawnEnemiesRpc(p0Type1Count.Value, 1, 1);
                
                // Client (ID 1) ส่งไปหา Host (ID 0)
                if (p1Type0Count.Value > 0) globalSpawner.SpawnEnemiesRpc(p1Type0Count.Value, 0, 0);
                if (p1Type1Count.Value > 0) globalSpawner.SpawnEnemiesRpc(p1Type1Count.Value, 1, 0);
            } 

            currentPhase.Value = GamePhase.Combat;
        }
        else
        {
            // Reset ค่าเมื่อกลับสู่ Planning
            p0Type0Count.Value = 0;
            p0Type1Count.Value = 0;
            p1Type0Count.Value = 0;
            p1Type1Count.Value = 0;
            
            // --- 🌊 ขึ้นเวฟใหม่เมื่อกลับสู่ช่วงวางแผน ---
            currentWave.Value++;
            
            currentPhase.Value = GamePhase.Planning;
        }
    }
}
