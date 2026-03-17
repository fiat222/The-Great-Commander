using UnityEngine;
using PlayerAudio;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float gravity = 9.81f;
    public float lifetime = 5f;

    private float speed;
    private int damage;
    private PlayerAudioComponent ownerAudio;
    private AimCrosshair crosshair;
    private Vector3 velocity;
    private bool isFlying = false;
    private bool noGravity = false;

    private const float HeadshotMultiplier = 1.3f;   // +30%

    private readonly Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f);

    /// <summary>ยิงปกติ (charge shot) — มี gravity</summary>
    public void Launch(Vector3 direction, float launchSpeed, int launchDamage, AimCrosshair crosshairRef = null, PlayerAudioComponent audio = null)
    {
        LaunchInternal(direction, launchSpeed, launchDamage, crosshairRef, false, audio);
    }

    /// <summary>ยิงเร็ว — ไม่มี gravity บินตรงเสมอ</summary>
    public void LaunchStraight(Vector3 direction, float launchSpeed, int launchDamage, AimCrosshair crosshairRef = null, PlayerAudioComponent audio = null)
    {
        LaunchInternal(direction, launchSpeed, launchDamage, crosshairRef, true, audio);
    }

    private void LaunchInternal(Vector3 direction, float launchSpeed, int launchDamage, AimCrosshair crosshairRef, bool straight, PlayerAudioComponent audio)
    {
        ownerAudio = audio;
        crosshair = crosshairRef;
        speed = launchSpeed;
        damage = launchDamage;
        noGravity = straight;
        velocity = direction.normalized * speed;
        isFlying = true;
        transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;

        if (!noGravity)
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

        isFlying = false;

        // ── ตรวจว่าโดนหัวไหม ──────────────────────────────────────────────────
        bool isHeadshot = other.CompareTag("EnemyHead");
        int finalDamage = isHeadshot
            ? Mathf.RoundToInt(damage * HeadshotMultiplier)
            : damage;

        // หา root ของ enemy (กรณีโดน HeadHitbox ให้ขึ้นไปหา root)
        GameObject enemyRoot = null;
        if (isHeadshot)
        {
            var head = other.GetComponent<EnemyHeadHitbox>();
            enemyRoot = head != null ? head.GetEnemyRoot() : other.transform.root.gameObject;
        }

        bool hitEnemy = other.CompareTag("Enemy") || isHeadshot;

        if (hitEnemy)
        {
            // ถ้าโดนหัว ให้ใช้ root เป็นเป้า, ถ้าโดน body ปกติใช้ other
            GameObject target = isHeadshot ? enemyRoot : other.gameObject;

            // เตรียมพิกัดตัวเลขดาเมจก่อนส่งดาเมจ (ป้องกันบัคศัตรูตายแล้วลบ hitbox ทิ้ง)
            Collider displayCol = isHeadshot ? enemyRoot.GetComponentInChildren<Collider>() : other;
            Vector3 spawnPos = displayCol != null
                ? new Vector3(displayCol.bounds.center.x, displayCol.bounds.max.y, displayCol.bounds.center.z)
                : other.bounds.center;

            // ── ส่งดาเมจ โดยเรียงลำดับความสำคัญของ Component ──────────────────────
            // เราเช็ค AI scripts ก่อน เพราะ AI มักจะมี HealthSystem อยู่ด้วยเพื่อโชว์ UI 
            // แต่ตัวจัดการ HP จริงๆ คือ AI script เอง

            EnemyAI enemyAI = target.GetComponent<EnemyAI>();
            ImpAI impAI = target.GetComponent<ImpAI>();
            MinionAI minionAI = target.GetComponent<MinionAI>();
            ArcherAI archerAI = target.GetComponent<ArcherAI>();

            if (enemyAI != null)
            {
                enemyAI.TakeDamage(finalDamage);
            }
            else if (impAI != null)
            {
                impAI.TakeDamage(finalDamage);
            }
            else if (minionAI != null)
            {
                minionAI.TakeDamage(finalDamage);
            }
            else if (archerAI != null)
            {
                archerAI.TakeDamage(finalDamage);
            }
            else
            {
                // ถ้าไม่ใช่กลุ่ม AI ให้ลองเช็คกลุ่ม Base/Tower หรือ HealthSystem ทั่วไป
                BaseHealth baseHP = target.GetComponent<BaseHealth>();
                TowerHealth towerHP = target.GetComponent<TowerHealth>();
                HealthSystem genericHP = target.GetComponent<HealthSystem>();

                if (baseHP != null) baseHP.TakeDamage(finalDamage);
                else if (towerHP != null) towerHP.TakeDamage(finalDamage);
                else if (genericHP != null) genericHP.TakeDamage(finalDamage);
            }

            DamageNumberSpawner.Show(finalDamage, spawnPos);
            
            if (crosshair != null) crosshair.TriggerHitMarker();
            if (ownerAudio != null) ownerAudio.PlaySound(PlayerSoundType.AttackHit);

            if (isHeadshot)
                Debug.Log($"<color=orange>[Arrow]</color> HEADSHOT! Damage: {finalDamage} (+30%)");
            else
                Debug.Log($"<color=red>[Arrow]</color> Hit Enemy! Damage: {finalDamage}");
        }

        transform.SetParent(other.transform);
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        Destroy(gameObject, 10f);
    }
}