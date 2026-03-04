using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private float customDamage = -1f;
    private HashSet<Collider> hitThisSwing = new HashSet<Collider>();

    private const float HeadshotMultiplier = 1.3f;   // +30%

    void Awake() => hitboxCollider = GetComponent<Collider>();

    public void SetDamage(float dmg) => customDamage = dmg;
    public void ClearHitList() => hitThisSwing.Clear();

    private void OnTriggerEnter(Collider other)
    {
        bool isHead = other.CompareTag("EnemyHead");
        bool isBody = other.CompareTag("Enemy");
        if (!isHead && !isBody) return;

        // ป้องกันโดนซ้ำใน swing เดียว — ใช้ root เป็น key
        GameObject enemyRoot = isHead
            ? (other.GetComponent<EnemyHeadHitbox>()?.GetEnemyRoot() ?? other.transform.root.gameObject)
            : other.gameObject;

        Collider rootCol = enemyRoot.GetComponent<Collider>() ?? other;
        if (hitThisSwing.Contains(rootCol)) return;
        hitThisSwing.Add(rootCol);

        // ── คำนวณ damage ──────────────────────────────────────────────────────
        float baseDamage;
        if (customDamage > 0)
            baseDamage = customDamage;
        else
        {
            var pc = GetComponentInParent<PlayerController>();
            baseDamage = pc != null ? pc.AttackDamage : 10f;
        }

        bool isHeadshot = isHead;
        float finalDamage = isHeadshot ? baseDamage * HeadshotMultiplier : baseDamage;
        int dmgInt = Mathf.RoundToInt(finalDamage);

        // ── ส่งดาเมจ ──────────────────────────────────────────────────────────
        HealthSystem hp = enemyRoot.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.TakeDamage(dmgInt);
        }
        else
        {
            EnemyAI enemyAI = enemyRoot.GetComponent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.TakeDamage(finalDamage);
            else
            {
                ImpAI impAI = enemyRoot.GetComponent<ImpAI>();
                if (impAI != null) impAI.TakeDamage(dmgInt);
            }
        }

        // ── Damage Number ──────────────────────────────────────────────────────
        Vector3 spawnPos = new Vector3(rootCol.bounds.center.x,
                                       rootCol.bounds.max.y,
                                       rootCol.bounds.center.z);
        DamageNumberSpawner.Show(dmgInt, spawnPos);

        if (isHeadshot)
            Debug.Log($"<color=orange>[PlayerHitbox]</color> HEADSHOT {enemyRoot.name} for {dmgInt} (+30%)");
        else
            Debug.Log($"<color=green>[PlayerHitbox]</color> Hit {enemyRoot.name} for {dmgInt}");
    }
}