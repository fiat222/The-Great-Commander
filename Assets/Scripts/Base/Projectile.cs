using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("VFX Settings")]
    public GameObject hitVFXPrefab;
    public float speed = 35f;

    private Transform target;
    private Vector3 targetPoint;   // ปลายเส้น — fixed ตอน Seek()
    private Vector3 dir;           // ทิศทางคงที่ตลอด
    private int damage;
    private bool ready = false;

    public void Seek(Transform _target, int _damage, Transform _tower, float _range)
    {
        target  = _target;
        damage  = _damage;

        // ปลายเส้นตรงกับ laser เป๊ะๆ — snapshot ทันที
        targetPoint = GetTargetCenter(_target);
        dir         = (targetPoint - transform.position).normalized;
        ready       = true;
    }

    void Update()
    {
        if (!ready || target == null)
        {
            Destroy(gameObject);
            return;
        }

        // อัปเดต target ทุก frame — ไล่ตาม enemy ตลอด
        Vector3 currentTarget = GetTargetCenter(target);

        // บินไปหา enemy ด้วย MoveTowards
        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget,
            speed * Time.deltaTime
        );

        // หมุนหัวชี้ไปทาง enemy ตลอดเวลา
        Vector3 liveDir = (currentTarget - transform.position).normalized;
        if (liveDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(liveDir);

        // ถึงตัว enemy แล้ว
        if (Vector3.Distance(transform.position, currentTarget) < 0.3f)
            HitTarget();
    }


    /// <summary>คืน world-space center ของ enemy จาก Collider bounds</summary>
    static Vector3 GetTargetCenter(Transform t)
    {
        Collider col = t.GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : t.position + Vector3.up;
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (target != null)
        {
            // ลอง HealthSystem ก่อน
            HealthSystem hp = target.GetComponent<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
            else
            {
                // ถ้าไม่มี HealthSystem ให้โจมตีผ่าน EnemyAI หรือ ImpAI โดยตรง
                EnemyAI enemyAI = target.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.TakeDamage(damage);
                }
                else
                {
                    ImpAI impAI = target.GetComponent<ImpAI>();
                    if (impAI != null) impAI.TakeDamage(damage);
                }
            }

            // Spawn VFX
            VFXManager.Instance?.Play(hitVFXPrefab, target.position);
        }

        Destroy(gameObject);
    }
}