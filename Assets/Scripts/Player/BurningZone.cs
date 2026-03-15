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

    private System.Collections.Generic.HashSet<Collider> enemiesInZone = new();
    private float tickTimer = 0f;
    private float lifeTimer = 0f;

    private void Start()
    {
        // ถ้ามีความต้องการให้รัศมีจาก ArcherSkill มามีผลกับ SphereCollider
        var col = GetComponent<SphereCollider>();
        if (col != null) col.radius = radius;
    }

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("EnemyHead"))
        {
            enemiesInZone.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (enemiesInZone.Contains(other))
        {
            enemiesInZone.Remove(other);
        }
    }

    private void DealDamageInZone()
    {
        // ล้าง Collider ที่ถูก Destroy ไปแล้วออกจาก Set (เช่น ศัตรูตายไปแล้ว)
        enemiesInZone.RemoveWhere(c => c == null);

        var hitRoots = new System.Collections.Generic.HashSet<GameObject>();

        foreach (var enemyCollider in enemiesInZone)
        {
            GameObject root = enemyCollider.transform.root.gameObject;
            if (hitRoots.Contains(root)) continue;
            hitRoots.Add(root);

            // ดึงดาเมจที่ปัดเศษแล้ว
            int dmgInt = Mathf.RoundToInt(damagePerSecond);

            // ── ส่งดาเมจ (ตามลำดับความสำคัญเหมือนระบบส่วนกลาง) ────────────────────────
            HealthSystem hp = root.GetComponent<HealthSystem>() ?? root.GetComponentInChildren<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(dmgInt);
                ShowDamageNumber(root);
                continue;
            }

            EnemyAI enemyAI = root.GetComponent<EnemyAI>() ?? root.GetComponentInChildren<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.TakeDamage(damagePerSecond);
                ShowDamageNumber(root);
                continue;
            }

            ImpAI impAI = root.GetComponent<ImpAI>() ?? root.GetComponentInChildren<ImpAI>();
            if (impAI != null)
            {
                impAI.TakeDamage(dmgInt);
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