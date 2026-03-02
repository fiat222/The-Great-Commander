using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Animator))]
public class WarriorSkill : MonoBehaviour
{
    // ==================== Skill 1 (Ice Smash) ====================
    [Header("Skill 1 (Ice Smash) Settings")]
    public GameObject iceSmashPrefab;
    [Tooltip("จุดที่พรีแฟบก้อนน้ำแข็งจะโผล่ขึ้นมา (สร้าง Empty Obj ไปวางไว้ปลายดาบ/พื้น)")]
    public Transform skillSpawnPoint;
    [Tooltip("ชดเชยองศาการหันของเอฟเฟค เช่น เบี้ยวขวาให้ใส่ Y = -90")]
    public Vector3 vfxRotationOffset = Vector3.zero;
    [Tooltip("ตัวคูณดาเมจของสกิล (เอาไปคูณกับ AttackDamage ของ Player)")]
    public float damageMultiplier = 1.5f;
    public float skill1Cooldown = 5f;
    [Tooltip("ระยะเวลาที่ก้อนน้ำแข็งจะคงอยู่ก่อนหายไป (วินาที)")]
    public float vfxLifetime = 3f;
    [Tooltip("กราฟควบคุมความเร็วและระยะเวลาในการพุ่งไปข้างหน้าตอนใช้สกิล")]
    public AnimationCurve skillMoveCurve = AnimationCurve.Linear(0f, 15f, 0.5f, 0f);

    private float skill1CooldownTimer;
    private bool isCastingSkill;

    private PlayerController player;
    private CharacterController controller;
    private Animator animator;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (player.IsDead) return;

        if (skill1CooldownTimer > 0) skill1CooldownTimer -= Time.deltaTime;

        HandleSkillInput();
    }

    private void HandleSkillInput()
    {
        // เมื่อกด R และสกิลพร้อม
        if (Input.GetKeyDown(KeyCode.R) && skill1CooldownTimer <= 0)
        {
            var sInfo = animator.GetCurrentAnimatorStateInfo(0);
            var nInfo = animator.GetNextAnimatorStateInfo(0);

            // เช็คว่าไม่ได้กำลังทำอย่างอื่นอยู่ เช่น กลิ้ง โดนตี แพรี่ หรือร่ายสกิลอื่น
            bool isBusy = sInfo.IsTag("Roll") || nInfo.IsTag("Roll") ||
                          sInfo.IsTag("Hit") || nInfo.IsTag("Hit") ||
                          sInfo.IsTag("Parry") || nInfo.IsTag("Parry") ||
                          sInfo.IsTag("Skill") || nInfo.IsTag("Skill");

            // เช็คว่ายืนอยู่บนพื้น (PlayerController อัปเดต isGrounded ลง Animator ไว้ให้แล้ว)
            bool isGrounded = animator.GetBool("isGrounded");
            
            // อ่านค่า isDodging แบบ public จาก PlayerController
            if (isGrounded && !player.IsDodging && !isBusy && !isCastingSkill)
            {
                animator.ResetTrigger("Attack"); 
                animator.SetTrigger("Skill1");
                
                // รีเซ็ตแรงพุ่ง/คอมโบดาบจาก PlayerController
                player.ResetComboAndDash(); 
                skill1CooldownTimer = skill1Cooldown;

                StartCoroutine(SkillMovementRoutine());
            }
        }
    }

    // ==================== Skill Movement Handling ====================
    
    // ตั้งค่าความไวในการหันตอนเล่นสกิล (ปรับแต่งได้)
    private float skillTurnSpeed = 8f;

    private System.Collections.IEnumerator SkillMovementRoutine()
    {
        isCastingSkill = true;
        
        // อ่านระยะเวลาของ Skill จากจุดสุดท้ายของกราฟ
        float duration = 0.5f;
        if (skillMoveCurve != null && skillMoveCurve.length > 0)
        {
            duration = skillMoveCurve[skillMoveCurve.length - 1].time;
        }

        float timer = 0f;
        Transform camTransform = Camera.main.transform;

        while (timer < duration)
        {
            if (player.IsDead) break;

            // 1. รับ Input จากผู้เล่นให้หันซ้าย-ขวาได้ตอนกำลังลอย
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v).normalized;

            if (inputDir.magnitude >= 0.1f)
            {
                // คืนค่ามุมเป้าหมายที่อิงจากมุมกล้อง
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                
                // ค่อยๆ หมุนโมเดลไปทางที่กด (เลี้ยว)
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, skillTurnSpeed * Time.deltaTime);
            }

            // 2. พุ่งตรงไปข้างหน้า ณ ทิศทาง "ปัจจุบัน" ของโมเดลเสมอ
            float curveSpeed = skillMoveCurve.Evaluate(timer);
            Vector3 forwardMove = transform.forward * curveSpeed;
            
            if (controller != null)
            {
                controller.Move(forwardMove * Time.deltaTime);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        isCastingSkill = false;
    }

    // ==================== Animation Events ====================

    /// <summary>
    /// ให้ใส่ Event โค้ดนี้ใน Animation Event ตอนดาบกระแทกพื้น
    /// </summary>
    public void SpawnIceSmashVFX()
    {
        if (iceSmashPrefab != null && skillSpawnPoint != null)
        {
            // บวกลบค่าองศาที่ชดเชยเข้าไป
            Quaternion spawnRot = skillSpawnPoint.rotation * Quaternion.Euler(vfxRotationOffset);
            
            // เสกที่ตำแหน่งจุด Spawn Point
            GameObject vfx = Instantiate(iceSmashPrefab, skillSpawnPoint.position, spawnRot);
            
            // 🔥 เซ็ตความแรงให้ PlayerWeaponHitbox (ถ้ามีติดมากับพรีแฟบเอฟเฟค)
            PlayerWeaponHitbox hitbox = vfx.GetComponent<PlayerWeaponHitbox>();
            if (hitbox == null) hitbox = vfx.GetComponentInChildren<PlayerWeaponHitbox>();
            
            if (hitbox != null)
            {
                float finalDamage = player.AttackDamage * damageMultiplier;
                hitbox.SetDamage(finalDamage);
                hitbox.ClearHitList(); // รีเซ็ตรายการศัตรูที่เคยโดนตี
            }
            
            // สั่งลบตัวเองทิ้งหลังจากผ่านไป vfxLifetime วินาที
            Destroy(vfx, vfxLifetime);
        }
        else if (skillSpawnPoint == null)
        {
            Debug.LogWarning("<color=yellow>[WarriorSkill]</color> ยังไม่ได้ลากจุด Spawn Point ใส่ใน WarriorSkill!");
        }
        else
        {
            Debug.LogWarning("<color=yellow>[WarriorSkill]</color> ยังไม่ได้ใส่ Prefab ของ IceSmashVFX ใน WarriorSkill!");
        }
    }
}
