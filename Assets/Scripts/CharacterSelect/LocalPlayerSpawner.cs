using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawn Player ในเครื่องของเราเอง (Local Instantiate)
/// แต่ละเครื่องมี Player ของตัวเอง → "2 โลก" แยกกัน
/// วางบน GameObject ธรรมดาใน GameScene (ไม่ต้อง NetworkObject)
/// </summary>
public class LocalPlayerSpawner : MonoBehaviour
{
    public static LocalPlayerSpawner Instance { get; private set; }

    [Header("Fallback Prefab")]
    public GameObject fallbackPrefab;

    [Header("Spawn Points")]
    public Transform p1SpawnPoint;
    public Transform p2SpawnPoint;

    [Header("Character Database (Fallback)")]
    public CharacterDataSO[] characters;

    private GameObject spawnedPlayer;

    private void Awake() => Instance = this;

    private void Start() => Invoke(nameof(SpawnMyPlayer), 0.1f);

    private void SpawnMyPlayer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            Invoke(nameof(SpawnMyPlayer), 0.5f);
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;
        int myPlayerIndex = isHost ? 0 : 1;

        // Fallback: ถ้าไม่ได้ผ่านหน้าเลือกตัวละคร
        if (CharacterSelectData.Characters == null && characters != null && characters.Length > 0)
            CharacterSelectData.Characters = characters;

        // หา Prefab
        GameObject prefab = CharacterSelectData.GetMyPlayerPrefab(myPlayerIndex) ?? fallbackPrefab;
        if (prefab == null)
        {
            Debug.LogError("[LocalPlayerSpawner] No prefab available. Check CharacterDataSO or fallback.");
            return;
        }

        // หาตำแหน่ง Spawn
        Vector3 spawnPos;
        Quaternion spawnRot;

        if (isHost && p1SpawnPoint != null)
        {
            spawnPos = p1SpawnPoint.position;
            spawnRot = p1SpawnPoint.rotation;
        }
        else if (!isHost && p2SpawnPoint != null)
        {
            spawnPos = p2SpawnPoint.position;
            spawnRot = p2SpawnPoint.rotation;
        }
        else
        {
            spawnPos = isHost ? new Vector3(-15f, 1f, 0f) : new Vector3(15f, 1f, 0f);
            spawnRot = isHost ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        }

        // Local Instantiate — Player มีแค่ในเครื่องนี้
        spawnedPlayer = Instantiate(prefab, spawnPos, spawnRot);

        // ลบ NetworkObject ที่ติดมากับ Prefab (ถ้ามี)
        var netObj = spawnedPlayer.GetComponent<NetworkObject>();
        if (netObj != null) Destroy(netObj);
    }

    public GameObject GetSpawnedPlayer() => spawnedPlayer;
}
