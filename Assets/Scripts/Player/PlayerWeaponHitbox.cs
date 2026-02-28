using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private float customDamage = -1f;
    private HashSet<Collider> hitThisSwing = new HashSet<Collider>();

    void Awake() => hitboxCollider = GetComponent<Collider>();

    public void SetDamage(float dmg) => customDamage = dmg;

    public void ClearHitList() => hitThisSwing.Clear();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (hitThisSwing.Contains(other)) return;
        hitThisSwing.Add(other);

        float damage = 10f;
        if (customDamage > 0)
        {
            damage = customDamage;
        }
        else
        {
            var pc = GetComponentInParent<PlayerController>();
            if (pc != null) damage = pc.AttackDamage;
        }

        int dmgInt = Mathf.RoundToInt(damage);

        EnemyAI enemyAI = other.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.TakeDamage(damage);
            // ── แสดงตัวเลขดาเมจ ──
            Vector3 spawnPos = new Vector3(other.bounds.center.x, other.bounds.max.y, other.bounds.center.z);
            DamageNumberSpawner.Show(dmgInt, spawnPos);
            OnHitSuccess(other.name, damage);
            return;
        }

        ImpAI impAI = other.GetComponent<ImpAI>();
        if (impAI != null)
        {
            impAI.TakeDamage(dmgInt);
            Vector3 spawnPos2 = new Vector3(other.bounds.center.x, other.bounds.max.y, other.bounds.center.z);
            DamageNumberSpawner.Show(dmgInt, spawnPos2);
            OnHitSuccess(other.name, damage);
        }
    }

    private void OnHitSuccess(string targetName, float dmg)
    {
        Debug.Log($"<color=green>[PlayerHitbox]</color> Hit {targetName} for {dmg} damage!");
    }
}