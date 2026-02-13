using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab; 
    public List<Transform> spawnPoints; 

    public void SpawnEnemy()
    {
        if (!IsServer) return; // เฉพาะ Server/Host เท่านั้นที่สั่ง Spawn ได้

        if (enemyPrefab == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: Prefab หรือ SpawnPoints ยังไม่ได้เซ็ต!");
            return;
        }

        // สุ่มเลือกจุดเกิดจาก List
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedPoint = spawnPoints[randomIndex];

        // สร้าง Object ขึ้นมาบน Server
        GameObject enemyInstance = Instantiate(enemyPrefab, selectedPoint.position, selectedPoint.rotation);

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
    public void SpawnEnemiesRpc(int count, ulong targetedClientId)
    {
        // เช็คว่าเราคือคนที่ต้องโดนสปอนใส่ไหม? 
        // (เช็ค LocalClientId เทียบกับ ID ที่ Server ส่งมา)
        if (NetworkManager.Singleton.LocalClientId == targetedClientId)
        {
            Debug.Log($"<color=red>[Spawner]</color> Receiving order to spawn {count} enemies!");
            StartCoroutine(SpawnRoutine(count));
        }
    }

    private System.Collections.IEnumerator SpawnRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemyLocally();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SpawnEnemyLocally()
    {
        if (enemyPrefab == null || spawnPoints.Count == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedPoint = spawnPoints[randomIndex];

        // เกิดแบบ Local (ไม่สั่ง .Spawn()) ตามคอนเซปต์แยกโลก
        Instantiate(enemyPrefab, selectedPoint.position, selectedPoint.rotation);
    }
}
