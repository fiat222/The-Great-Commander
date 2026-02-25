using UnityEngine;

public class EnemyWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private EnemyAI ownerAI; // ในกรณีที่ต้องการส่งดาเมจตามตัวแปรของมอนสเตอร์

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        ownerAI = GetComponentInParent<EnemyAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. เช็คว่าเจอเป้าหมายที่เราอยากโจมตีไหม
        bool hitTarget = false;
        int damage = 1;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            var archer = other.GetComponent<Archer>();

            if (player != null)
            {
                player.TakeDamage(damage);
                hitTarget = true;
            }
            else if (archer != null)
            {
                archer.TakeDamage(damage);
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
        else if (other.CompareTag("Tower"))
        {
            var towerHp = other.GetComponent<TowerHealth>();
            if (towerHp != null)
            {
                towerHp.TakeDamage(damage);
                hitTarget = true;
            }
        }

        // 2. 🛡️ ถ้าโจมตีโดนเป้าหมายแล้ว ให้ปิด Collider ตัวเองทันที!
        // เพื่อป้องกันการเกิดดาเมจซ้ำในท่าฟันเดิม (One-hit-per-swing)
        if (hitTarget)
        {
            Debug.Log($"<color=red>[Hitbox]</color> Hit {other.name}! Disabling hitbox to prevent multi-hit.");
            if (hitboxCollider != null) hitboxCollider.enabled = false;
        }
    }
}
