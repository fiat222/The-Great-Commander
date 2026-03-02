using UnityEngine;

public class TrollRockThrower : MonoBehaviour
{
    [Header("Visuals in Hand")]
    [Tooltip("ก้อนหินปลอมที่อยู่ในมือของ Troll (เปิดตอนง้าง, ปิดตอนปา)")]
    public GameObject heldRockVisual;

    [Header("Projectile Setup")]
    [Tooltip("Prefab ของก้อนหินจริงที่จะพุ่งไปหาเพลเยอร์")]
    public GameObject rockPrefab;
    [Tooltip("จุดที่ก้อนหินจะเกิด (ควรอยู่ที่มือ หรือใกล้ๆ มือ)")]
    public Transform throwPoint;
    [Tooltip("ความเร็วของก้อนหินที่ปาออกไป")]
    public float throwSpeed = 15f;

    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        
        // ซ่อนหินในมือไว้ก่อนตอนเริ่มเกม
        if (heldRockVisual != null)
        {
            heldRockVisual.SetActive(false);
        }
    }

    // ==========================================
    // ฟังก์ชันเหล่านี้ เอาไปผูกกับ Animation Event
    // ==========================================

    /// <summary>
    /// เรียกตอนเริ่มง้างปาหิน (เพื่อให้หินโผล่ขึ้นมาที่มือ)
    /// </summary>
    public void ShowHeldRock()
    {
        if (heldRockVisual != null)
        {
            heldRockVisual.SetActive(true);
        }
    }

    /// <summary>
    /// เรียกจังหวะที่ปาหินออกไป (หินในมือหายไป และเสกหินจริงพุ่งไปหาเป้าหมาย)
    /// </summary>
    public void ThrowRock()
    {
        // 1. ซ่อนหินในมือ
        if (heldRockVisual != null)
        {
            heldRockVisual.SetActive(false);
        }

        // 2. ปาหินจริงออกไป
        if (rockPrefab == null || throwPoint == null || enemyAI == null) return;

        // ดาเมจดึงมาจาก EnemyStatsSO ผ่าน EnemyAI
        int damage = enemyAI.stats != null ? (int)enemyAI.stats.GetDamage() : 10;

        // หาเป้าหมายที่อยู่ใกล้ที่สุด
        Transform target = GetBestTarget();
        if (target != null)
        {
            // เล็งไปที่เป้าหมาย
            Vector3 targetPos = target.position + Vector3.up * 1f; // เล็งกลางๆ ตัว
            
            // ถ้าเป้าหมายเป็นเพลเยอร์ พยายามเล็งที่จุดศูนย์กลางจริงๆ
            Collider targetCol = target.GetComponent<Collider>();
            if (targetCol != null)
            {
                targetPos = targetCol.bounds.center;
            }

            Vector3 direction = (targetPos - throwPoint.position).normalized;

            // สร้างก้อนหิน
            GameObject rock = Instantiate(rockPrefab, throwPoint.position, Quaternion.LookRotation(direction));
            
            // สั่งให้ก้อนหินพุ่ง
            TrollRockProjectile rockProj = rock.GetComponent<TrollRockProjectile>();
            if (rockProj != null)
            {
                rockProj.Launch(direction, throwSpeed, damage);
            }
        }
        else
        {
            // ถ้าไม่มีเป้าหมาย ก็ปาตรงๆ ไปข้างหน้า (ตามหน้า Troll)
            GameObject rock = Instantiate(rockPrefab, throwPoint.position, transform.rotation);
            TrollRockProjectile rockProj = rock.GetComponent<TrollRockProjectile>();
            if (rockProj != null)
            {
                rockProj.Launch(transform.forward, throwSpeed, damage);
            }
        }
    }

    // ฟังก์ชันช่วยหาว่า Troll กำลังเล็งใครอยู่ (Player, Base, หรือ Minion)
    private Transform GetBestTarget()
    {
        // 1. เช็คว่าอยู่ใกล้ฐานไหม
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null && Vector3.Distance(transform.position, baseObj.transform.position) <= enemyAI.baseAttackRange + 2f)
        {
            return baseObj.transform;
        }

        // 2. เช็คว่าสู้อยู่กับใครระหว่าง Player กับ Minion
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject[] minions = GameObject.FindGameObjectsWithTag("Minion");

        Transform bestTarget = null;
        float closestDist = Mathf.Infinity;

        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closestDist && dist <= enemyAI.chaseRange)
            {
                closestDist = dist;
                bestTarget = player.transform;
            }
        }

        foreach (var m in minions)
        {
            if (m == null || !m.activeInHierarchy) continue;
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist < closestDist && dist <= enemyAI.chaseRange)
            {
                closestDist = dist;
                bestTarget = m.transform;
            }
        }

        return bestTarget;
    }
}
