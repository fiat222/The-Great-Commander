using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    public List<Transform> spawnPoints; 

    public void SpawnEnemy(int typeIndex)
    {
        if (!IsServer) return; // เฉพาะ Server/Host เท่านั้นที่สั่ง Spawn ได้

        if (GameManager.Instance == null || GameManager.Instance.systemEnemyPool == null || 
            typeIndex >= GameManager.Instance.systemEnemyPool.Length || spawnPoints.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner: ไม่พบข้อมูลมอนสเตอร์ (Index {typeIndex}) หรือ SpawnPoints!");
            return;
        }

        GameObject prefab = GameManager.Instance.systemEnemyPool[typeIndex].prefab;
        if (prefab == null) return;

        // เลือกจุดเกิดจาก List
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedPoint = spawnPoints[randomIndex];

        // สร้าง Object ขึ้นมาบน Server
        GameObject enemyInstance = Instantiate(prefab, selectedPoint.position, selectedPoint.rotation);

        // สั่งให้ Object นี้เกิดบนหน้าจอของทุกคน (Network Sync)
        NetworkObject netObj = enemyInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError("EnemySpawner: Prefab ของคุณไม่มี NetworkObject! กรุณาเพิ่มก่อน");
        }
    }

    // สั่งเกิดมอนสเตอร์เป็นชุด (ทำงานทุกเครื่องที่ได้รับ RPC)
    [Rpc(SendTo.Everyone)]
    public void SpawnEnemiesRpc(int count, int typeIndex, ulong targetedClientId)
    {
        // เช็คว่าเราคือคนที่ต้องโดนสปอนใส่ไหม? 
        if (NetworkManager.Singleton.LocalClientId == targetedClientId)
        {
            Debug.Log($"<color=red>[Spawner]</color> Receiving order to spawn {count} enemies of type {typeIndex}!");
            StartCoroutine(SpawnRoutine(count, typeIndex));
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SpawnSystemEnemiesRpc(string draft)
    {
        Debug.Log($"<color=orange>[Spawner]</color> Receiving system wave draft: {draft}");
        StartCoroutine(SystemSpawnRoutine(draft));
    }

    private System.Collections.IEnumerator SpawnRoutine(int count, int typeIndex)
    {
        if (GameManager.Instance == null || GameManager.Instance.systemEnemyPool == null || 
            typeIndex >= GameManager.Instance.systemEnemyPool.Length) yield break;

        GameObject prefab = GameManager.Instance.systemEnemyPool[typeIndex].prefab;

        for (int i = 0; i < count; i++)
        {
            SpawnEnemyFromPrefab(prefab, typeIndex, false); // ⭐ ให้มีผลกับ UI (ถ้าต้องการนับ)
            yield return new WaitForSeconds(0.5f);
        }
    }

    private System.Collections.IEnumerator SystemSpawnRoutine(string draft)
    {
        if (string.IsNullOrEmpty(draft)) yield break;

        // "0:3|1:3"
        string[] parts = draft.Split('|');
        foreach (string p in parts)
        {
            string[] sub = p.Split(':');
            if (sub.Length == 2)
            {
                int typeIndex = int.Parse(sub[0]);
                int count = int.Parse(sub[1]);

                if (GameManager.Instance != null && GameManager.Instance.systemEnemyPool != null)
                {
                    if (typeIndex < GameManager.Instance.systemEnemyPool.Length)
                    {
                        GameObject prefab = GameManager.Instance.systemEnemyPool[typeIndex].prefab;
                        for (int i = 0; i < count; i++)
                        {
                            SpawnEnemyFromPrefab(prefab, typeIndex, true);
                            yield return new WaitForSeconds(0.4f);
                        }
                    }
                }
            }
        }
    }

    // ฟังก์ชันนี้ไม่ได้ถูกใช้งานแล้ว เนื่องจากเราหันไปใช้ SpawnEnemyFromPrefab แทนครับ
    private void SpawnEnemyFromPoolIndex(int typeIndex, bool isSystem = false)
    {
        if (GameManager.Instance == null || GameManager.Instance.systemEnemyPool == null || 
            typeIndex >= GameManager.Instance.systemEnemyPool.Length) return;

        GameObject prefab = GameManager.Instance.systemEnemyPool[typeIndex].prefab;
        SpawnEnemyFromPrefab(prefab, typeIndex, isSystem);
    }

    private void SpawnEnemyFromPrefab(GameObject prefab, int index = -1, bool isSystem = false)
    {
        if (prefab == null || spawnPoints.Count == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedPoint = spawnPoints[randomIndex];

        GameObject enemy = Instantiate(prefab, selectedPoint.position, selectedPoint.rotation);
        
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.typeIndex = index;
            ai.countsInWaveUI = isSystem;
        }
    }
}
