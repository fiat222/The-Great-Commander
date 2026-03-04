using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float gravity = 9.81f;
    public float lifetime = 5f;

    private float speed;
    private int damage;
    private Vector3 velocity;
    private bool isFlying = false;
    private bool noGravity = false;

    private const float HeadshotMultiplier = 1.3f;   // +30%

    private readonly Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f);

    /// <summary>ยิงปกติ (charge shot) — มี gravity</summary>
    public void Launch(Vector3 direction, float launchSpeed, int launchDamage)
    {
        LaunchInternal(direction, launchSpeed, launchDamage, false);
    }

    /// <summary>ยิงเร็ว — ไม่มี gravity บินตรงเสมอ</summary>
    public void LaunchStraight(Vector3 direction, float launchSpeed, int launchDamage)
    {
        LaunchInternal(direction, launchSpeed, launchDamage, true);
    }

    private void LaunchInternal(Vector3 direction, float launchSpeed, int launchDamage, bool straight)
    {
        speed = launchSpeed;
        damage = launchDamage;
        noGravity = straight;
        velocity = direction.normalized * speed;
        isFlying = true;
        transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;

        if (!noGravity)
            velocity.y -= gravity * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;

        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("Minion")) return;

        isFlying = false;

        // ── ตรวจว่าโดนหัวไหม ──────────────────────────────────────────────────
        bool isHeadshot = other.CompareTag("EnemyHead");
        int finalDamage = isHeadshot
            ? Mathf.RoundToInt(damage * HeadshotMultiplier)
            : damage;

        // หา root ของ enemy (กรณีโดน HeadHitbox ให้ขึ้นไปหา root)
        GameObject enemyRoot = null;
        if (isHeadshot)
        {
            var head = other.GetComponent<EnemyHeadHitbox>();
            enemyRoot = head != null ? head.GetEnemyRoot() : other.transform.root.gameObject;
        }

        bool hitEnemy = other.CompareTag("Enemy") || isHeadshot;

        if (hitEnemy)
        {
            // ถ้าโดนหัว ให้ใช้ root เป็นเป้า, ถ้าโดน body ปกติใช้ other
            GameObject target = isHeadshot ? enemyRoot : other.gameObject;

            HealthSystem hp = target.GetComponent<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(finalDamage);
            }
            else
            {
                EnemyAI enemyAI = target.GetComponent<EnemyAI>();
                if (enemyAI != null)
                    enemyAI.TakeDamage(finalDamage);
                else
                {
                    ImpAI impAI = target.GetComponent<ImpAI>();
                    if (impAI != null) impAI.TakeDamage(finalDamage);
                }
            }

            Collider displayCol = isHeadshot ? enemyRoot.GetComponentInChildren<Collider>() : other;
            Vector3 spawnPos = displayCol != null
                ? new Vector3(displayCol.bounds.center.x, displayCol.bounds.max.y, displayCol.bounds.center.z)
                : other.bounds.center;

            DamageNumberSpawner.Show(finalDamage, spawnPos);

            if (isHeadshot)
                Debug.Log($"<color=orange>[Arrow]</color> HEADSHOT! Damage: {finalDamage} (+30%)");
            else
                Debug.Log($"<color=red>[Arrow]</color> Hit Enemy! Damage: {finalDamage}");
        }

        transform.SetParent(other.transform);
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        Destroy(gameObject, 10f);
    }
}