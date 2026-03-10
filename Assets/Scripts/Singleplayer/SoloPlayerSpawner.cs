using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Spawn Player ในเครื่องเดียว สำหรับ SoloGameScene (ไม่ใช้ Netcode)
/// อ่าน CharacterSelectData.P1CharacterIndex ที่ SingleCharacterSelectManager บันทึกไว้
/// วางบน GameObject "SoloPlayerSpawner" ใน SoloGameScene
/// </summary>
public class SoloPlayerSpawner : MonoBehaviour
{
    public static SoloPlayerSpawner Instance { get; private set; }

    [Header("Spawn Point")]
    [Tooltip("ตำแหน่งที่ Player จะ Spawn — ถ้าไม่ได้ลากมาจะใช้ Vector3.zero")]
    public Transform spawnPoint;

    [Header("Fallback Prefab")]
    [Tooltip("Prefab สำรองถ้าไม่ได้เลือกตัวละครมาจากหน้า Select")]
    public GameObject fallbackPrefab;

    [Header("Character Database (Fallback)")]
    [Tooltip("ลาก CharacterDataSO ทั้งหมดมาใส่ — ใช้เมื่อ CharacterSelectData ว่าง")]
    public CharacterDataSO[] characters;

    private GameObject _spawnedPlayer;
    public  GameObject SpawnedPlayer => _spawnedPlayer;

    // ─────────────────────────────────────────────────────────────────
    private void Awake() => Instance = this;

    private void OnEnable()
    {
        SoloGameManager.OnPhaseChangedGlobal += OnPhaseChanged;
    }

    private void OnDisable()
    {
        SoloGameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
    }

    private void Start()
    {
        SpawnPlayer();
    }

    // ─────────────────────────────────────────────────────────────────
    //  PHASE
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Respawn Player ทุกครั้งที่เปลี่ยน Phase (กลับจาก Combat → Planning)</summary>
    private void OnPhaseChanged(GamePhase phase)
    {
        // Respawn เฉพาะตอนเข้า Planning (คือจบ Combat ไปแล้ว)
        if (phase == GamePhase.Planning)
            RespawnPlayer();
    }

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────

    /// <summary>ทำลาย Player เดิม แล้ว Spawn ใหม่ที่ Spawn Point</summary>
    public void RespawnPlayer()
    {
        if (_spawnedPlayer != null)
        {
            Destroy(_spawnedPlayer);
            _spawnedPlayer = null;
        }
        SpawnPlayer();
    }

    // ─────────────────────────────────────────────────────────────────
    //  SPAWN
    // ─────────────────────────────────────────────────────────────────
    private void SpawnPlayer()
    {
        if (_spawnedPlayer != null) return;

        // Fallback: ถ้า CharacterSelectData ยังไม่มีข้อมูล (เช่น เข้า Scene ตรง)
        if (CharacterSelectData.Characters == null && characters != null && characters.Length > 0)
        {
            CharacterSelectData.Characters = characters;
            if (CharacterSelectData.P1CharacterIndex < 0)
                CharacterSelectData.P1CharacterIndex = 0; // เลือกตัวแรกเป็น default
        }

        // หา Prefab จาก CharacterSelectData (playerIndex = 0 เสมอ เพราะ Solo)
        GameObject prefab = CharacterSelectData.GetMyPlayerPrefab(0) ?? fallbackPrefab;

        if (prefab == null)
        {
            Debug.LogError("[SoloPlayerSpawner] ไม่พบ Prefab! ตรวจสอบ CharacterDataSO หรือ Fallback Prefab");
            return;
        }

        // หาตำแหน่ง Spawn
        Vector3    pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // Instantiate
        _spawnedPlayer = Instantiate(prefab, pos, rot);
        _spawnedPlayer.name = prefab.name + "(Solo)";

        // ลบ NetworkObject ออก (ถ้า Prefab มีติดมา)
        var netObj = _spawnedPlayer.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            Destroy(netObj);
            Debug.Log("[SoloPlayerSpawner] ลบ NetworkObject ออกจาก Player (Solo mode ไม่ใช้ Netcode)");
        }

        // เชื่อม Camera กับ Player
        RegisterCameraTargets(_spawnedPlayer);

        string charName = CharacterSelectData.GetCharacter(0)?.characterName ?? prefab.name;
        Debug.Log($"[SoloPlayerSpawner] Spawned '{charName}' at {pos}");
    }

    // ─────────────────────────────────────────────────────────────────
    //  CAMERA SETUP
    // ─────────────────────────────────────────────────────────────────
    private void RegisterCameraTargets(GameObject player)
    {
        if (CameraManager.Instance == null) return;

        // หา Cinemachine cameras ที่ติดมากับ Player prefab
        var freeLook   = player.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        var targetLock = player.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>().Length > 1
                         ? player.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>()[1]
                         : null;

        if (freeLook != null)
        {
            CameraManager.Instance.RegisterPlayerCameras(freeLook, targetLock);
            Debug.Log("[SoloPlayerSpawner] Camera registered to CameraManager.");
        }
    }
}
