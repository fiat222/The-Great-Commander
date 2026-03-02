using UnityEngine;

public class EnemyWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private EnemyAI ownerAI;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        ownerAI = GetComponentInParent<EnemyAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 🔥 ใช้ EnemyStatsSO แทน data
        int damage = 10;

        if (ownerAI != null && ownerAI.stats != null)
        {
            damage = (int)ownerAI.stats.GetDamage();
        }

        bool hitTarget = false;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            var archer = other.GetComponent<Archer>();

            if (player != null)
            {
                player.TakeDamage(damage, ownerAI != null ? ownerAI.transform.position : transform.position);
                hitTarget = true;
            }
            else if (archer != null)
            {
                archer.TakeDamage(damage, ownerAI != null ? ownerAI.transform.position : transform.position);
                hitTarget = true;
            }
        }
        else if (other.CompareTag("Base"))
        {
            var baseHp = other.GetComponent<BaseHealth>();
            if (baseHp != null)
            {
                baseHp.TakeDamage(damage);
                hitTarget = true;
            }
        }
        else if (other.CompareTag("Minion"))
        {
            var minionAI = other.GetComponent<MinionAI>();
            var archerAI = other.GetComponent<ArcherAI>();

            if (minionAI != null)
            {
                minionAI.TakeDamage(damage);
                hitTarget = true;
            }
            else if (archerAI != null)
            {
                archerAI.TakeDamage(damage);
                hitTarget = true;
            }
        }

        // One-hit-per-swing
        if (hitTarget)
        {
            Debug.Log($"<color=red>[Hitbox]</color> Hit {other.name} for {damage} damage! Disabling hitbox.");
            if (hitboxCollider != null)
                hitboxCollider.enabled = false;
        }
    }
}