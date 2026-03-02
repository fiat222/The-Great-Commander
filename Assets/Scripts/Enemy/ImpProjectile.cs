using UnityEngine;

public class ImpProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float lifetime = 6f;

    private float speed;
    private int damage;
    private Vector3 velocity;
    private bool isFlying = false;

    public void Launch(Vector3 direction, float launchSpeed, int launchDamage)
    {
        speed = launchSpeed;
        damage = launchDamage;
        velocity = direction.normalized * speed;
        isFlying = true;

        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;

        transform.position += velocity * Time.deltaTime;

        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;

        // ไม่โดนตัวเองหรือ Enemy ด้วยกัน
        if (other.CompareTag("Enemy")) return;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>()?.TakeDamage(damage, transform.position);
            other.GetComponent<Archer>()?.TakeDamage(damage, transform.position);
            Debug.Log($"<color=red>[ImpProjectile]</color> Hit Player! Damage: {damage}");
        }
        else if (other.CompareTag("Minion"))
        {
            other.GetComponent<MinionAI>()?.TakeDamage(damage);
            other.GetComponent<ArcherAI>()?.TakeDamage(damage);
            Debug.Log($"<color=red>[ImpProjectile]</color> Hit Minion! Damage: {damage}");
        }
        else if (other.CompareTag("Base"))
        {
            other.GetComponent<BaseHealth>()?.TakeDamage(damage);
            Debug.Log($"<color=red>[ImpProjectile]</color> Hit Base! Damage: {damage}");
        }
        else
        {
            // ชนอย่างอื่น (พื้น กำแพง ฯลฯ) → บินต่อ
            return;
        }

        isFlying = false;
        Destroy(gameObject);
    }
}