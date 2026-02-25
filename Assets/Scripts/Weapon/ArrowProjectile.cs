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
    private readonly Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f);

    public void Launch(Vector3 direction, float launchSpeed, int launchDamage)
    {
        speed = launchSpeed;
        damage = launchDamage;
        velocity = direction.normalized * speed;
        isFlying = true;
        transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;

        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;

        // ป้องกันไม่ให้โดนตัวเองตอนยิง
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("Minion")) return;

        isFlying = false;

        if (other.CompareTag("Enemy"))
        {
            // ลอง HealthSystem ก่อน
            HealthSystem hp = other.GetComponent<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
            else
            {
                // ถ้าไม่มี HealthSystem ให้โจมตีผ่าน EnemyAI หรือ ImpAI โดยตรง
                EnemyAI enemyAI = other.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.TakeDamage(damage);
                }
                else
                {
                    ImpAI impAI = other.GetComponent<ImpAI>();
                    if (impAI != null) impAI.TakeDamage(damage);
                }
            }

            Debug.Log($"<color=red>[Arrow]</color> Hit Enemy! Damage: {damage}");
        }

        // ปักค้างไว้ที่วัตถุที่โดน
        transform.SetParent(other.transform);

        // ปิด Collider เพื่อไม่ให้โดนซ้ำ
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        Destroy(gameObject, 10f);
    }
}