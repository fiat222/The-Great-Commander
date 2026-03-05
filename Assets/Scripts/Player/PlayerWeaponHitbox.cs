using UnityEngine;
using System.Collections.Generic;
using PlayerAudio;

public class PlayerWeaponHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private float customDamage = -1f;
    private HashSet<GameObject> hitThisSwing = new HashSet<GameObject>();

    private const float HeadshotMultiplier = 1.3f;   // +30%

    void Awake() => hitboxCollider = GetComponent<Collider>();

    public void SetDamage(float dmg) => customDamage = dmg;
    public void ClearHitList() => hitThisSwing.Clear();

    /// <summary>ปิด collider โดยตรง + ล้างสมุดจด</summary>
    public void ForceDisable()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = false;
        hitThisSwing.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        // ถ้า hitbox ถูกปิดไปแล้ว (เช่น โดนตีกลางท่าฟัน) ไม่ต้องทำอะไรอีก
        if (hitboxCollider != null && !hitboxCollider.enabled) return;
        if (other == null) return;

        bool isHead = other.CompareTag("EnemyHead");
        bool isBody = other.CompareTag("Enemy");
        if (!isHead && !isBody) return;

        // ใช้ root ของ hierarchy เป็น key กันโดนซ้ำ — ไม่ว่าจะโดนชิ้นส่วนไหนก็ map กลับศัตรูตัวเดียว
        GameObject enemyRoot = other.transform.root.gameObject;

        if (hitThisSwing.Contains(enemyRoot)) return;
        hitThisSwing.Add(enemyRoot);

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

        // ดึงจุดกึ่งกลางจาก other โดยตรงชัวร์สุด เพราะ other คืนค่ามาแล้วแน่ๆ ว่าไม่เป็น null
        Vector3 spawnPos = new Vector3(other.bounds.center.x, 
                                       other.bounds.max.y, 
                                       other.bounds.center.z);

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

        // ── เสียงตอนโดนตี ────────────────────────────────────────────────────────
        var audioComp = GetComponentInParent<PlayerAudioComponent>();
        if (audioComp != null)
        {
            audioComp.PlaySound(PlayerSoundType.AttackHit);
        }

        // ── Damage Number ──────────────────────────────────────────────────────
        DamageNumberSpawner.Show(dmgInt, spawnPos);

        if (isHeadshot)
            Debug.Log($"<color=orange>[PlayerHitbox]</color> HEADSHOT {enemyRoot.name} for {dmgInt} (+30%)");
        else
            Debug.Log($"<color=green>[PlayerHitbox]</color> Hit {enemyRoot.name} for {dmgInt}");
    }
}