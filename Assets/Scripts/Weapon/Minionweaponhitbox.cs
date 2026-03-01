using UnityEngine;
using System.Collections.Generic;

public class MinionWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private HashSet<Collider> hitThisSwing = new HashSet<Collider>();

    void Awake() => hitboxCollider = GetComponent<Collider>();

    public void ClearHitList() => hitThisSwing.Clear();

    public void EnableHitbox()
    {
        ClearHitList();
        if (hitboxCollider != null) hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (hitThisSwing.Contains(other)) return;
        hitThisSwing.Add(other);

        // Get damage from MinionData (supports upgrade)
        float damage = 10f;
        var minion = GetComponentInParent<MinionAI>();
        if (minion != null && minion.data != null)
            damage = minion.data.GetDamage();

        int dmgInt = Mathf.RoundToInt(damage);

        // Hit EnemyAI
        EnemyAI enemyAI = other.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.TakeDamage(damage);
            Vector3 spawnPos = new Vector3(other.bounds.center.x, other.bounds.max.y, other.bounds.center.z);
            DamageNumberSpawner.Show(dmgInt, spawnPos);
            Debug.Log($"<color=cyan>[MinionHitbox]</color> Hit {other.name} for {damage} damage!");
            return;
        }

        // Hit ImpAI
        ImpAI impAI = other.GetComponent<ImpAI>();
        if (impAI != null)
        {
            impAI.TakeDamage(dmgInt);
            Vector3 spawnPos = new Vector3(other.bounds.center.x, other.bounds.max.y, other.bounds.center.z);
            DamageNumberSpawner.Show(dmgInt, spawnPos);
            Debug.Log($"<color=cyan>[MinionHitbox]</color> Hit {other.name} for {damage} damage!");
        }
    }
}
