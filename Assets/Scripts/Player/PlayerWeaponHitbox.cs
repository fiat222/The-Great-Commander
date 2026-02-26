using UnityEngine;

public class PlayerWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private float customDamage = -1f;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
    }

    // ดึงจาก PlayerController อัตโนมัติ
    public void SetDamage(float dmg) => customDamage = dmg;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            float damage = 10f;

            // 1. หาดาเมจจากแหล่งกำเนิด
            if (customDamage > 0)
            {
                damage = customDamage;
            }
            else
            {
                // ลองหาจากตัวพ่อ (PlayerController หรือ Archer)
                var pc = GetComponentInParent<PlayerController>();
                if (pc != null) damage = pc.AttackDamage;
            }

            // 2. ส่งดาเมจไปที่ศัตรู (ใช้ GetComponentInParent เพื่อรองรับ Child Collider แบบ Minotaur)
            EnemyAI enemyAI = other.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.TakeDamage(damage);
                OnHitSuccess(other.name, damage);
                return;
            }

            ImpAI impAI = other.GetComponent<ImpAI>();
            if (impAI != null)
            {
                impAI.TakeDamage((int)damage);
                OnHitSuccess(other.name, damage);
                return;
            }
        }
    }

    private void OnHitSuccess(string targetName, float dmg)
    {
        Debug.Log($"<color=green>[PlayerHitbox]</color> Hit {targetName} for {dmg} damage!");
        // ปิด Collider เพื่อไม่ให้โดนซ้ำในเฟรมเดียว (ดาเมจแบบ One-hit-per-swing)
        //if (hitboxCollider != null) hitboxCollider.enabled = false;
    }
}
