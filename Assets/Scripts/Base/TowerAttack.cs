using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Tower Stats")]
    public float range = 33f;
    public float fireRate = 0.7f;
    public int damage = 10;

    [Header("VFX Prefabs")]
    public GameObject projectileVFXPrefab;
    public GameObject muzzleFlashVFXPrefab;
    public GameObject rangeVFXPrefab; 

    [Header("References")]
    public Transform firePoint;
    public LineRenderer attackLaser;

    [Header("VFX Calibration")]
    public float vfxDefaultSize = 3f; // ค่าจาก Start Size ใน Particle System เปลี่นยตาม vfx ชอบเขต

    private GameObject rangeInstance;
    private float fireCountdown = 0f;
    private Transform target;

    void Start()
    {
        if (rangeVFXPrefab != null)
        {
            // ⭐ ไม่ parent ใต้ Tower เพราะ Tower มี scale ใหญ่ (เช่น 8,11,8)
            // ทำให้ localScale ถูก multiply ทับจนวงกลมใหญ่เกินหรือผิดรูป
            rangeInstance = Instantiate(rangeVFXPrefab, transform.position, Quaternion.identity);

            // คำนวณ scale ใน world space โดยตรง
            float finalScale = (range * 2f) / vfxDefaultSize;
            rangeInstance.transform.localScale = new Vector3(finalScale, 1f, finalScale);

            rangeInstance.SetActive(false);
        }
    }

    void Update()
    {
        UpdateTarget();
        HandleVisuals();

        if (target != null && fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }
        fireCountdown -= Time.deltaTime;
    }

    void UpdateTarget()
    {
        // ล้าง target ถ้าตายแล้ว (ไม่รอ Destroy)
        if (target != null && IsEnemyDead(target.gameObject))
            target = null;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            // ข้าม enemy ที่กำลังเล่น animation ตาย
            if (IsEnemyDead(enemy)) continue;

            Vector3 towerPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 enemyPos = new Vector3(enemy.transform.position.x, 0, enemy.transform.position.z);
            float dist = Vector3.Distance(towerPos, enemyPos);

            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                nearestEnemy = enemy;
            }
        }

        target = (nearestEnemy != null && shortestDistance <= range) ? nearestEnemy.transform : null;
    }

    /// <summary>เช็คว่า enemy ตายแล้วหรือยัง — รองรับทั้ง HealthSystem, EnemyAI และ ImpAI</summary>
    bool IsEnemyDead(GameObject enemy)
    {
        HealthSystem hs = enemy.GetComponent<HealthSystem>();
        if (hs != null) return hs.IsDead;

        EnemyAI ea = enemy.GetComponent<EnemyAI>();
        if (ea != null) return ea.IsDead;

        ImpAI imp = enemy.GetComponent<ImpAI>();
        if (imp != null) return imp.IsDead;

        return false;
    }

    void HandleVisuals()
    {
        if (target != null)
        {
            if (attackLaser != null)
            {
                attackLaser.enabled = true;
                attackLaser.SetPosition(0, firePoint.position);
                attackLaser.SetPosition(1, GetEnemyCenter(target));
            }

            if (rangeInstance != null)
            {
                // ⭐ ติดตามตำแหน่ง Tower แทนการ parent (หลีกเลี่ยง scale บิดเบือน)
                rangeInstance.transform.position = new Vector3(
                    transform.position.x,
                    rangeInstance.transform.position.y, // คงความสูงเดิมของ VFX
                    transform.position.z);
                rangeInstance.SetActive(true);
            }
        }
        else
        {
            if (attackLaser != null) attackLaser.enabled = false;
            if (rangeInstance != null) rangeInstance.SetActive(false);
        }
    }

    /// <summary>คืน world-space center ของ enemy จาก Collider bounds</summary>
    Vector3 GetEnemyCenter(Transform t)
    {
        Collider col = t.GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : t.position;
    }

    void Shoot()
    {
        if (projectileVFXPrefab == null || firePoint == null) return;

        if (muzzleFlashVFXPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashVFXPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 1f);
        }

        GameObject projGO = Instantiate(projectileVFXPrefab, firePoint.position, Quaternion.identity);
        Projectile proj = projGO.GetComponent<Projectile>();
        if (proj != null) proj.Seek(target, damage, firePoint, range);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}