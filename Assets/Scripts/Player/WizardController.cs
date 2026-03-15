using UnityEngine;
using System.Collections;

/// <summary>
/// Wizard Controller — ตัวละครสายเวทย์ที่ใช้การโจมตีระยะไกลและโล่ป้องกัน
/// </summary>
public class WizardController : BaseCharacter
{
    [Header("Wizard Spells")]
    public GameObject arcaneOrbPrefab;
    public Transform castPoint;
    public float attackCooldown = 0.6f;
    private float lastAttackTime;

    [Header("VFX")]
    public GameObject manaShieldVFX;
    private bool isShieldActive;

    private void Update()
    {
        if (!IsOwner || isDead) return;

        HandleInput();
    }

    private void HandleInput()
    {
        // 1. โจมตีปกติ (Arcane Orbs)
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }

        // 2. กางโล่ (Mana Shield) - กดค้างเพื่อกาง
        if (Input.GetMouseButtonDown(1)) ToggleShield(true);
        if (Input.GetMouseButtonUp(1)) ToggleShield(false);

        // 3. เคลื่อนที่ (ช้าลง 70% ถ้ากางโล่อยู่)
        float speedMult = isShieldActive ? 0.3f : 1.0f;
        HandleStandardMovement(speedMult);
    }

    private void Attack()
    {
        lastAttackTime = Time.time;
        if (animator != null) animator.SetTrigger("Attack");
        
        // เสียงโจมตี
        playerAudio?.PlaySound(PlayerAudio.PlayerSoundType.Attack1);
    }

    // เรียกผ่าน Animation Event 'SpawnArcaneOrb' (ต้องตั้งใน Unity Animation)
    public void SpawnArcaneOrb()
    {
        if (arcaneOrbPrefab == null || castPoint == null) return;

        // คำนวณทิศทางยิงไปที่กลางหน้าจอ
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 100f) ? hit.point : ray.GetPoint(100f);
        Vector3 direction = (targetPoint - castPoint.position).normalized;

        Instantiate(arcaneOrbPrefab, castPoint.position, Quaternion.LookRotation(direction));
    }

    private void ToggleShield(bool active)
    {
        isShieldActive = active;
        if (manaShieldVFX != null) manaShieldVFX.SetActive(active);
        if (animator != null) animator.SetBool("isShielding", active);
        
        // ถ้ากางโล่ ให้ได้รับความเสียหายน้อยลง (เพิ่ม Logic ใน TakeDamage ได้)
    }

    public override void TakeDamage(int damage)
    {
        // ถ้ากางโล่อยู่ ลดดาเมจลง 50%
        int finalDamage = isShieldActive ? Mathf.RoundToInt(damage * 0.5f) : damage;
        base.TakeDamage(finalDamage);
        
        if (!isShieldActive && animator != null) animator.SetTrigger("Damage");
    }
}
