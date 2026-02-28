using UnityEngine;

/// <summary>
/// ลูกธนูฝน — ยิงลงมาจากด้านบนเฉียงตามทิศกล้อง
/// ทะลุได้ (ไม่หยุดเมื่อชน Enemy) แต่หยุดเมื่อชนพื้น/กำแพง
/// </summary>
public class RainArrowProjectile : MonoBehaviour
{
    public float lifetime = 3f;

    private int damage;
    private Vector3 velocity;
    private bool isFlying = false;

    private readonly Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f);
    private System.Collections.Generic.HashSet<Collider> hitTargets = new();

    public void Launch(Vector3 direction, float speed, int dmg)
    {
        damage = dmg;
        velocity = direction.normalized * speed;
        isFlying = true;
        transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;
        transform.position += velocity * Time.deltaTime;
        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("Minion")) return;
        if (hitTargets.Contains(other)) return;

        if (other.CompareTag("Enemy"))
        {
            hitTargets.Add(other);
            DealDamage(other);
            Debug.Log($"<color=yellow>[RainArrow]</color> Hit: {other.name} Dmg: {damage}");
            return; // ทะลุต่อ
        }

        // ชนพื้น/กำแพง → หยุด
        isFlying = false;
        Destroy(gameObject, 0.5f);
    }

    private void DealDamage(Collider other)
    {
        HealthSystem hp = other.GetComponent<HealthSystem>();
        if (hp != null) { hp.TakeDamage(damage); return; }

        EnemyAI enemyAI = other.GetComponent<EnemyAI>();
        if (enemyAI != null) { enemyAI.TakeDamage(damage); return; }

        ImpAI impAI = other.GetComponent<ImpAI>();
        if (impAI != null) impAI.TakeDamage(damage);
    }
}