using UnityEngine;

/// <summary>
/// วางบนพื้น ทำ DoT ไฟในรัศมีที่กำหนด
/// ArcherSkill จะ Instantiate script นี้หลังจาก 1 วิ
/// </summary>
public class BurningZone : MonoBehaviour
{
    [HideInInspector] public float radius = 6f;
    [HideInInspector] public int damagePerSecond = 10;
    [HideInInspector] public float duration = 5f;

    private float tickTimer = 0f;
    private float lifeTimer = 0f;

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= 1f)
        {
            tickTimer = 0f;
            DealDamageInZone();
        }
    }

    private void DealDamageInZone()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        // ใช้ HashSet กัน root ซ้ำ กรณี Enemy มีหลาย Collider / MeshCollider
        var hitRoots = new System.Collections.Generic.HashSet<GameObject>();

        foreach (var hit in hits)
        {
            // หา root ขึ้นไปจาก collider ที่โดน
            GameObject root = hit.transform.root.gameObject;

            if (hitRoots.Contains(root)) continue;

            // เช็ค tag ที่ root หรือ collider ที่โดนตรงๆ
            bool isEnemy = hit.CompareTag("Enemy") || root.CompareTag("Enemy");
            if (!isEnemy) continue;

            hitRoots.Add(root);

            // หา component ที่ root ก่อน ถ้าไม่มีค่อย GetComponentInChildren
            var hp = root.GetComponent<HealthSystem>()
                  ?? root.GetComponentInChildren<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(damagePerSecond);
                ShowDamageNumber(root);
                continue;
            }

            var enemy = root.GetComponent<EnemyAI>()
                     ?? root.GetComponentInChildren<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damagePerSecond);
                ShowDamageNumber(root);
                continue;
            }

            var imp = root.GetComponent<ImpAI>()
                   ?? root.GetComponentInChildren<ImpAI>();
            if (imp != null)
            {
                imp.TakeDamage(damagePerSecond);
                ShowDamageNumber(root);
            }
        }
    }

    private void ShowDamageNumber(GameObject root)
    {
        var col = root.GetComponentInChildren<Collider>();
        Vector3 pos = col != null
            ? new Vector3(col.bounds.center.x, col.bounds.max.y, col.bounds.center.z)
            : root.transform.position + Vector3.up * 2f;
        DamageNumberSpawner.Show(damagePerSecond, pos);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}