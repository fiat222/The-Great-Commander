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

    public void SpawnWaveFromDraft(string draft, EnemyStatsSO[] pool)
    {
        StartCoroutine(SpawnDraftRoutine(draft, pool));
    }

    public void SpawnEnemySolo(EnemyStatsSO stats, int typeIndex = 0)
    {
        if (stats == null || stats.prefab == null) return;

        GameObject enemy = InstantiateAtRandom(stats.prefab);
        SetupEnemy(enemy, stats, typeIndex);
    }

    // ─── COROUTINES ───────────────────────────────────────────

    IEnumerator SpawnDraftRoutine(string draft, EnemyStatsSO[] pool)
    {
        if (string.IsNullOrEmpty(draft) || pool == null || pool.Length == 0) yield break;

        string[] parts = draft.Split('|');
        foreach (string p in parts)
        {
            string[] sub = p.Split(':');
            if (sub.Length == 2)
            {
                int typeIndex = int.Parse(sub[0]);
                int count = int.Parse(sub[1]);

                if (typeIndex >= 0 && typeIndex < pool.Length)
                {
                    EnemyStatsSO stats = pool[typeIndex];
                    if (stats != null && stats.prefab != null)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            GameObject enemy = InstantiateAtRandom(stats.prefab);
                            SetupEnemy(enemy, stats, typeIndex);
                            yield return new WaitForSeconds(spawnInterval);
                        }
                    }
                }
            }
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
    void SetupEnemy(GameObject enemy, EnemyStatsSO stats, int typeIndex)
    {
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.stats = stats;           // assign ก่อน Start() รัน
            ai.typeIndex = typeIndex;   // บอกว่าตัวนี้คือ typeIndex อะไร
            ai.countsInWaveUI = true;   // ให้ Die() ยิง OnSystemEnemyDied
            return;
        }

        ImpAI imp = enemy.GetComponent<ImpAI>();
        if (imp != null)
        {
            imp.stats = stats;
            imp.typeIndex = typeIndex;
            imp.countsInWaveUI = true;
        }
    }
}