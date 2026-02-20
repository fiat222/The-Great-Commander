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
            rangeInstance = Instantiate(rangeVFXPrefab, transform.position, Quaternion.identity);
            rangeInstance.transform.SetParent(this.transform);
            rangeInstance.transform.localPosition = new Vector3(0, 0f, 0);

            // แก้ vfx เพี้ยน
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
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
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

    void HandleVisuals()
    {
        if (target != null)
        {
            attackLaser.enabled = true;
            attackLaser.SetPosition(0, firePoint.position);
            attackLaser.SetPosition(1, target.position);

            // แสดงขอบเขตค้างไว้ ไม่กะพริบ 
            if (rangeInstance != null) rangeInstance.SetActive(true);
        }
        else
        {
            attackLaser.enabled = false;
            if (rangeInstance != null) rangeInstance.SetActive(false);
        }
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