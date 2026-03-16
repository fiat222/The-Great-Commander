using UnityEngine;
using PlayerAudio;

/// <summary>
/// Skill Arrow — ทะลุทุกตัว, ดาเมจสูง, หายเร็ว
/// ต่างจาก ArrowProjectile ตรงที่:
///   - ไม่หยุดเมื่อชน Enemy (ทะลุผ่าน)
///   - ไม่ปักค้าง ไม่ SetParent
///   - lifetime สั้น (2–3 วิ) และเร็วกว่าปกติ
///   - แต่ละตัวโดนได้ครั้งเดียว (hitTargets กัน hit ซ้ำ)
///   - ดาเมจ = damage ที่รับมาจาก ArcherSkill (คูณมาแล้ว)
///
/// Setup:
///   Duplicate Arrow prefab → เปลี่ยน ArrowProjectile เป็น SkillArrowProjectile
///   ลาก prefab ใหม่ใส่ช่อง skillArrowPrefab ของ ArcherSkill
/// </summary>
public class SkillArrowProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float gravity = 2f;    // แรงโน้มถ่วงน้อยกว่าปกติ → ยิงตรงกว่า
    public float lifetime = 2.5f;  // หายเร็ว

    private float speed;
    private int damage;
    private PlayerAudioComponent ownerAudio;
    private Vector3 velocity;
    private bool isFlying = false;

    private readonly Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f);

    // กันโดน enemy ตัวเดิมซ้ำขณะทะลุผ่าน
    private System.Collections.Generic.HashSet<Collider> hitTargets = new();

    public void Launch(Vector3 direction, float launchSpeed, int launchDamage, PlayerAudioComponent audio = null)
    {
        ownerAudio = audio;
        speed = launchSpeed;
        damage = launchDamage;
        velocity = direction.normalized * speed;
        isFlying = true;
        transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;
        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("Minion")) return;

        // ถ้าโดนตัวนี้ไปแล้ว ข้ามเลย
        if (hitTargets.Contains(other)) return;

        if (other.CompareTag("Enemy"))
        {
            hitTargets.Add(other);
            DealDamage(other);
            if (ownerAudio != null) ownerAudio.PlaySound(PlayerSoundType.AttackHit);
            Debug.Log($"<color=magenta>[SkillArrow]</color> ทะลุ! Hit: {other.name} Dmg: {damage}");

            // ไม่หยุด ไม่ปัก → ทะลุต่อ (isFlying ยังเป็น true)
            return;
        }

        // ชนวัตถุที่ไม่ใช่ Enemy (กำแพง พื้น ฯลฯ) → หยุด
        isFlying = false;
        Destroy(gameObject, 0.3f);
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