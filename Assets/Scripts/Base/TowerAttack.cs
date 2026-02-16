using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Stats")]
    public float range = 15f;
    public float fireRate = 1f;
    public int damage = 10;

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float fireCountdown = 0f;
    private Transform target;

    void Update()
    {
        UpdateTarget();

        // Safety Check: ต้องมีเป้าหมาย และเป้าหมายต้องยังไม่ถูกทำลาย
        if (target != null && fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }
        fireCountdown -= Time.deltaTime;
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue; // ข้ามถ้าศัตรูตัวนั้นกำลังจะตาย

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= range)
            target = nearestEnemy.transform;
        else
            target = null;
    }

    void Shoot()
    {
        if (firePoint == null || target == null) return;

        GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile proj = projGO.GetComponent<Projectile>();

        if (proj != null)
        {
            // ส่งตัวป้อม (this.transform) และระยะยิง (range) ไปให้กระสุนด้วย
            proj.Seek(target, damage, transform, range);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}