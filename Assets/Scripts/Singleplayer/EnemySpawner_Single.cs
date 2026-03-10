using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner_Single : MonoBehaviour
{
    [Header("Spawner Settings")]
    public List<Transform> spawnPoints;

    [Header("Spawn Delay")]
    public float spawnInterval = 0.4f;

    // ─── PUBLIC API ───────────────────────────────────────────

    public void SpawnWaveBatch(EnemyStatsSO[] pool, int total)
    {
        StartCoroutine(SpawnBatchRoutine(pool, total));
    }

    public void SpawnEnemySolo(EnemyStatsSO stats)
    {
        if (stats == null || stats.prefab == null) return;

        GameObject enemy = InstantiateAtRandom(stats.prefab);
        SetupEnemy(enemy, stats);
    }

    // ─── COROUTINES ───────────────────────────────────────────

    IEnumerator SpawnBatchRoutine(EnemyStatsSO[] pool, int total)
    {
        for (int i = 0; i < total; i++)
        {
            EnemyStatsSO stats = pool[Random.Range(0, pool.Length)];

            if (stats != null && stats.prefab != null)
            {
                GameObject enemy = InstantiateAtRandom(stats.prefab);
                SetupEnemy(enemy, stats);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // ─── HELPERS ─────────────────────────────────────────────

    GameObject InstantiateAtRandom(GameObject prefab)
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
        return Instantiate(prefab, point.position, point.rotation);
    }

    /// <summary>
    /// Assign stats + เปิด countsInWaveUI ให้ SoloEnemyTracker นับได้
    /// Start() ของ EnemyAI/ImpAI จะ ApplyStatsFromSO() เองหลัง Instantiate
    /// </summary>
    void SetupEnemy(GameObject enemy, EnemyStatsSO stats)
    {
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.stats = stats;           // assign ก่อน Start() รัน
            ai.countsInWaveUI = true;   // ให้ Die() ยิง OnSystemEnemyDied
            return;
        }

        ImpAI imp = enemy.GetComponent<ImpAI>();
        if (imp != null)
        {
            imp.stats = stats;
            imp.countsInWaveUI = true;
        }
    }
}