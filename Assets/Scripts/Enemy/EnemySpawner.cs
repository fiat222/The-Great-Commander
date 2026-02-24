using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] enemyPrefabs; // เปลี่ยนเป็น Array เพื่อรองรับหลายประเภทครับ
    public List<Transform> spawnPoints; 

    public void SpawnEnemy(int typeIndex)
    {
        if (!IsServer) return; // เฉพาะ Server/Host เท่านั้นที่สั่ง Spawn ได้

        if (enemyPrefabs == null || typeIndex >= enemyPrefabs.Length || enemyPrefabs[typeIndex] == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner: Prefab (Index {typeIndex}) หรือ SpawnPoints ยังไม่ได้เซ็ต!");
            return;
        }

        // สุ่มเลือกจุดเกิดจาก List
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedPoint = spawnPoints[randomIndex];

        // สร้าง Object ขึ้นมาบน Server
        GameObject enemyInstance = Instantiate(enemyPrefabs[typeIndex], selectedPoint.position, selectedPoint.rotation);

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
        for (int i = 0; i < count; i++)
        {
            SpawnEnemyFromPoolIndex(typeIndex, true); // ⭐ ให้มีผลกับ UI ด้วยครับ
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

    private void SpawnEnemyFromPoolIndex(int typeIndex, bool isSystem = false)
    {
        if (enemyPrefabs == null || typeIndex >= enemyPrefabs.Length || enemyPrefabs[typeIndex] == null || spawnPoints.Count == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedPoint = spawnPoints[randomIndex];

        GameObject enemy = Instantiate(enemyPrefabs[typeIndex], selectedPoint.position, selectedPoint.rotation);
        
        // เซ็ตค่าพื้นฐานถ้ามี EnemyAI
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.typeIndex = typeIndex;
            ai.countsInWaveUI = isSystem; 
        }
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
