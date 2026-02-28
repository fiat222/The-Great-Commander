using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private float customDamage = -1f;

    // เก็บรายชื่อศัตรูที่โดนไปแล้วในการฟันรอบนี้
    private HashSet<Collider> hitThisSwing = new HashSet<Collider>();

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
    }

    // ดึงจาก PlayerController อัตโนมัติ
    public void SetDamage(float dmg) => customDamage = dmg;

    /// <summary>เรียกจาก WeaponHandler ตอนเปิด Hitbox → เคลียร์รายชื่อ</summary>
    public void ClearHitList()
    {
        hitThisSwing.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // ถ้าโดนตัวนี้ไปแล้วรอบนี้ → ข้ามเลย
            if (hitThisSwing.Contains(other)) return;
            hitThisSwing.Add(other);

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
    }
}
