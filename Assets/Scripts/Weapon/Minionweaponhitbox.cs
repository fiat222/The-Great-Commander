using UnityEngine;

public class MinionWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private MinionAI ownerAI;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        ownerAI = GetComponentInParent<MinionAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // ดึงค่า damage จาก MinionData ถ้ามี ถ้าไม่มีใช้ค่า default
        int damage = (ownerAI != null && ownerAI.data != null) ? (int)ownerAI.data.damage : 10;

        bool hitTarget = false;

        if (other.CompareTag("Enemy"))
        {
            var enemyAI = other.GetComponent<EnemyAI>();
            var impAI = other.GetComponent<ImpAI>();
            if (enemyAI != null)
            {
                enemyAI.TakeDamage(damage);
                hitTarget = true;
            }
            else if (impAI != null)
            {
                impAI.TakeDamage(damage);
                hitTarget = true;
            }
        }

        // ถ้าโจมตีโดนเป้าหมายแล้ว ให้ปิด Collider ทันที (One-hit-per-swing)
        if (hitTarget)
        {
            Debug.Log($"<color=cyan>[MinionHitbox]</color> Hit {other.name} for {damage} damage! Disabling hitbox to prevent multi-hit.");
            if (hitboxCollider != null) hitboxCollider.enabled = false;
        }
    }
}