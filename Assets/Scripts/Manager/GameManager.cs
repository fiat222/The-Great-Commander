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
    [SerializeField] private CinemachineCamera planningCam;
    [SerializeField] private CinemachineCamera combatCam;

    public static GameManager Instance { get; private set; }

    public TextMeshProUGUI phaseStatusText;

    // เก็บสถานะ Phase ปัจจุบัน : Server เขียนได้, ทุกคนอ่านได้
    private NetworkVariable<GamePhase> currentPhase = new NetworkVariable<GamePhase>(GamePhase.Planning);

    [Header("Enemy Sending System")]
    private EnemySpawner globalSpawner; 
    
    // จำนวนศัตรูที่ค้างส่ง: 0 สำหรับ Host (P1) และ 1 สำหรับ Client (P2)
    private NetworkVariable<int> p0PendingEnemies = new NetworkVariable<int>(0);
    private NetworkVariable<int> p1PendingEnemies = new NetworkVariable<int>(0);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // หา Spawner ตัวเดียวในซีน
        globalSpawner = FindFirstObjectByType<EnemySpawner>();
        if (globalSpawner != null) Debug.Log("<color=green>[GameManager]</color> Global Spawner Linked.");

        currentPhase.OnValueChanged += OnPhaseChanged;
        p0PendingEnemies.OnValueChanged += (old, newVal) => UpdatePhaseUI(currentPhase.Value);
        p1PendingEnemies.OnValueChanged += (old, newVal) => UpdatePhaseUI(currentPhase.Value);

        UpdatePhaseUI(currentPhase.Value);
        SwitchCamera(currentPhase.Value);
    }

    private void OnPhaseChanged(GamePhase previousValue, GamePhase newValue)
    {
        UpdatePhaseUI(newValue);
        SwitchCamera(newValue);
    }

    private void SwitchCamera(GamePhase current)
    {
        if (current == GamePhase.Planning)
        {
            planningCam.Priority.Value = 20;
            combatCam.Priority.Value = 10;
        }
        else
        {
            planningCam.Priority.Value = 10;
            combatCam.Priority.Value = 20;
        }
    }
    private void UpdatePhaseUI(GamePhase phase)
    {
        string status = phase.ToString() + " Phase\n";
        status += $"Host Sends: {p0PendingEnemies.Value} | Client Sends: {p1PendingEnemies.Value}";
        phaseStatusText.text = status;
    }

    public void RequestBuyEnemy()
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        int currentCount = (myId == 0) ? p0PendingEnemies.Value : p1PendingEnemies.Value;
        Debug.Log($"<color=cyan>[Shop]</color> You sent an enemy! Total to send: {currentCount + 1}");
        BuyEnemyServerRpc(myId);
    }

    [Rpc(SendTo.Server)]
    void BuyEnemyServerRpc(ulong clientId)
    {
        if (currentPhase.Value != GamePhase.Planning) return;

        if (clientId == 0) p0PendingEnemies.Value++;
        else p1PendingEnemies.Value++;
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
                globalSpawner.SpawnEnemiesRpc(p0PendingEnemies.Value, 1);
                
                // Client (ID 1) ส่งไปหา Host (ID 0)
                globalSpawner.SpawnEnemiesRpc(p1PendingEnemies.Value, 0);
            } 

            currentPhase.Value = GamePhase.Combat;
        }
        else
        {
            p0PendingEnemies.Value = 0;
            p1PendingEnemies.Value = 0;
            currentPhase.Value = GamePhase.Planning;
        }
    }
}
