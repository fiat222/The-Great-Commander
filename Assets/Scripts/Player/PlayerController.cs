using UnityEngine;
using PlayerAudio;
using Unity.Cinemachine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // ==================== SO ====================
    [Header("Stats SO")]
    [Tooltip("ลาก PlayerStatsSO มาใส่ตรงนี้ครับ")]
    public PlayerStatsSO stats;

    // ==================== Movement ====================
    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float rotationSpeed = 13f;
    public float jumpHeight = 3f;
    public float gravity = -19.62f;

    // ==================== Combo ====================
    [Header("Combo Settings")]
    public float comboResetTime = 3.0f;
    public float attackDashForce = 5f;
    public float dashDecay = 10f;
    [Range(0, 1)] public float forceTime = 0.2f;
    [Range(0, 1)] public float comboWindowTime = 0.5f;
    [Range(0, 1)] public float finisherWindowTime = 0.85f;

    // ==================== Roll / Dodge ====================
    [Header("Roll / Dodge Settings")]
    public AnimationCurve dodgeCurve = AnimationCurve.Linear(0f, 15f, 0.5f, 0f);
    private bool isDodging;
    private float dodgeTimer;
    private float dodgeCooldownTimer;

    private int comboStep;
    private float lastClickTime;
    private Vector3 currentDashVelocity;  // แรงพุ่งตอนฟัน
    private Vector3 currentHitVelocity;   // แรงกระเด็นตอนโดนทำดาเมจ
    private Vector3 rollDirection;
    private bool alreadyAppliedForce;
    private bool bufferCombo;

    // ==================== Target Lock ====================
    [Header("Target Lock Settings")]
    public float lockRange = 15f;
    public CinemachineTargetGroup targetGroup;
    public Transform cameraPivot;
    private Transform currentTarget;
    private bool isLockedOn;

    // ==================== References ====================
    public WeaponHandler weaponHandler;
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private PlayerAudioComponent playerAudio;

    [Header("VFX")]
    public GameObject parryHitVFXPrefab;
    public Transform parryVFXSpawnPoint;
    public GameObject hitVFXPrefab;       // เอฟเฟคตอนโดนตี (เลือดกระเซ็น/แสงกระแทก)
    public Transform hitVFXSpawnPoint;    // จุดเล่นเอฟเฟค (แนะนำตรงลำตัว)

    // ==================== Ground Check ====================
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    // ==================== Health UI ====================
    [Header("Health UI")]
    public int maxHP = 100;
    public Slider healthBar;

    // ==================== Runtime Stats ====================
    public float AttackDamage { get; private set; } = 15f;
    public float Defense { get; private set; } = 5f;

    private int currentHP;
    private float rotationVelocity;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private bool isInvincible;
    private bool isParrying;
    private bool isDead;
    public bool IsDead => isDead;
    public bool IsDodging => isDodging; // เพิ่มเพื่อเปิดให้ WorkerSkill เข้าถึงสถานะกลิ้งได้
    private Coroutine rotationCoroutine;

    public bool isMovementLocked { get; set; }

    // ==================== Lifecycle ====================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        playerAudio = GetComponent<PlayerAudioComponent>();

        if (dodgeCurve != null && dodgeCurve.length > 0)
            dodgeTimer = dodgeCurve[dodgeCurve.length - 1].time;
        else
            dodgeTimer = 0.5f;

        // หา HealthBar จาก Tag ถ้ายังไม่ได้ลากใส่ Inspector
        if (healthBar == null)
        {
            var hpBarObj = GameObject.FindWithTag("HPBar");
            if (hpBarObj != null)
                healthBar = hpBarObj.GetComponent<Slider>();
            else
                Debug.LogWarning("[PlayerController] ไม่พบ GameObject ที่มี Tag 'HPBar'");
        }

        ApplyStats(isFirstInit: true);
    }

    /// <summary>
    /// ดึงค่าจาก SO มาใช้
    /// เรียกได้ทุกครั้งหลัง Upgrade สำเร็จ — ค่าจะมีผลทันที
    /// </summary>
    public void ApplyStats(bool isFirstInit = false)
    {
        if (stats != null)
        {
            maxHP = stats.GetHP();
            moveSpeed = stats.GetSpeed() * 2.4f;   // SO Speed(5-10) → moveSpeed(12-24)
            AttackDamage = stats.GetDamage();
            Defense = stats.GetDefense();
        }

        if (isFirstInit)
        {
            currentHP = maxHP;
        }

        if (healthBar != null) 
        { 
            healthBar.maxValue = maxHP; 
            healthBar.value = currentHP; 
        }

        Debug.Log($"[Player] Stats Lv{(stats != null ? stats.CurrentLevel : 0)} | HP:{maxHP} Spd:{moveSpeed:F1} Def:{Defense:F1} Dmg:{AttackDamage:F1}");
    }

    private void Update()
    {
        if (isDead) return;

        if (dodgeCooldownTimer > 0) dodgeCooldownTimer -= Time.deltaTime;

        HandleTargetLockInput();
        HandleRollInput();
        HandleParryInput();
        HandleAttackInput();
        CheckAnimationLogic();
        UpdateWeaponEffect();

        if (!isDodging)
        {
            Move();
        }
        else
        {
            // ให้ลอยตกลงพื้นได้แม้กำลังกลิ้งอยู่
            ApplyGravityDuringDodge();
        }
    }

    private void ApplyGravityDuringDodge()
    {
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            
        if (isGrounded && verticalVelocity.y < 0) 
            verticalVelocity.y = -2f;
        else 
            verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void UpdateWeaponEffect()
    {
        if (weaponHandler == null || animator == null) return;

        var sInfo = animator.GetCurrentAnimatorStateInfo(0);
        var nInfo = animator.GetNextAnimatorStateInfo(0);

        bool inAttack = sInfo.IsTag("Attack") || nInfo.IsTag("Attack");
        bool inRoll = sInfo.IsTag("Roll") || nInfo.IsTag("Roll");
        bool inJump = sInfo.IsTag("Jump") || nInfo.IsTag("Jump");

        bool shouldShow = inAttack || inRoll || inJump;

        weaponHandler.SetEffectActive(shouldShow);
    }

    // ==================== Target Lock ====================

    private void HandleTargetLockInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isLockedOn) UnlockTarget();
            else FindNearestTarget();
        }
        if (isLockedOn && currentTarget == null) UnlockTarget();
        if (isLockedOn && currentTarget != null &&
            Vector3.Distance(transform.position, currentTarget.position) > lockRange + 2f)
            UnlockTarget();
    }

    private void FindNearestTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, lockRange);
        float closestDist = lockRange;
        Transform closest = null;

        foreach (var c in cols)
        {
            if (!c.CompareTag("Enemy")) continue;
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < closestDist) { closestDist = d; closest = c.transform; }
        }
        if (closest == null) return;

        currentTarget = closest;
        isLockedOn = true;

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetTargetLock(true);
            var vcam = CameraManager.Instance.TargetLockCamera;

            if (cameraPivot != null)
            {
                var aligner = cameraPivot.GetComponent<CameraPivotAligner>();
                if (aligner != null) aligner.SetTarget(currentTarget);
            }

            if (targetGroup != null)
            {
                while (targetGroup.Targets.Count > 1)
                    targetGroup.RemoveMember(targetGroup.Targets[1].Object);
                targetGroup.AddMember(currentTarget, 1f, 0f);
                if (vcam != null) vcam.LookAt = targetGroup.transform;
            }
            else { if (vcam != null) vcam.LookAt = currentTarget; }
        }
    }

    public void UnlockTarget()
    {
        isLockedOn = false;
        if (targetGroup != null && currentTarget != null) targetGroup.RemoveMember(currentTarget);
        currentTarget = null;
        if (CameraManager.Instance != null) CameraManager.Instance.SetTargetLock(false);
        if (cameraPivot != null)
        {
            var aligner = cameraPivot.GetComponent<CameraPivotAligner>();
            if (aligner != null) aligner.SetTarget(null);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockRange);
    }

    // ==================== Roll ====================

    private void HandleRollInput()
    {
        var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
        bool isBusy = animator != null && (sInfo.IsTag("Roll") || nInfo.IsTag("Roll") ||
                                           sInfo.IsTag("Attack") || nInfo.IsTag("Attack"));
                                           
        if (!Input.GetKeyDown(KeyCode.LeftShift) || !isGrounded || isBusy || isDodging || dodgeCooldownTimer > 0) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            float angle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg +
                          (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
            rollDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
            rotationCoroutine = StartCoroutine(SmoothRotate(angle));
        }
        else { rollDirection = transform.forward; }

        StartCoroutine(DodgeRoutine());
    }

    private System.Collections.IEnumerator DodgeRoutine()
    {
        isDodging = true;
        dodgeCooldownTimer = dodgeTimer + 0.15f; // Add slight cooldown after roll completes

        if (animator != null)
        {
            animator.ResetTrigger("Damage"); 
            animator.SetTrigger("Roll");
        }

        float timer = 0f;
        bool heightCompressed = false;

        while (timer < dodgeTimer)
        {
            if (!heightCompressed && timer > dodgeTimer / 3f)
            {
                controller.center = new Vector3(0, 1.25f, 0);
                controller.height = 2.5f;
                heightCompressed = true;
            }

            // อ่านค่าความเร็วจากกราฟ และคูณด้วยทิศทางที่เล็งไว้
            float curveSpeed = dodgeCurve.Evaluate(timer);
            Vector3 moveDir = rollDirection * curveSpeed;
            
            controller.Move(moveDir * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        // คืนค่า Hitbox ตาม Inspector 
        controller.center = new Vector3(0, 2.45f, 0); 
        controller.height = 4.93f; 
        isDodging = false;
    }

    // ==================== Attack ====================

    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
            var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
            if (sInfo.IsTag("Roll") || nInfo.IsTag("Roll") || sInfo.IsTag("Jump") || nInfo.IsTag("Jump")) goto checkReset;

            lastClickTime = Time.time;
            if (comboStep == 0) TriggerAttack();
            else bufferCombo = true;
        }
    checkReset:
        if (comboStep > 0 && Time.time - lastClickTime > comboResetTime) ResetCombo();
    }

    // ==================== Parry ====================

    private void HandleParryInput()
    {
        if (Input.GetMouseButtonDown(1)) // Right-Click
        {
            var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
            var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;

            // ไม่ให้ Parry ถ้ากำลังกลิ้ง โดนตี หรือกำลัง Parry อยู่แล้ว
            bool isBusy = sInfo.IsTag("Roll") || nInfo.IsTag("Roll") ||
                          sInfo.IsTag("Hit") || nInfo.IsTag("Hit") ||
                          sInfo.IsTag("Parry") || nInfo.IsTag("Parry");

            if (isGrounded && !isDodging && !isBusy)
            {
                if (animator != null)
                {
                    animator.ResetTrigger("Attack"); // ยกเลิกการโจมตีที่อาจจะค้างอยู่
                    animator.SetTrigger("Parry");
                    
                    // ถ้ายกเลิกคอมโบกลางคันได้ ก็เคลียร์ ResetCombo ได้
                    ResetCombo();
                    currentDashVelocity = Vector3.zero;
                }
            }
        }
    }

    private void TriggerAttack()
    {
        float targetAngle = mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : transform.eulerAngles.y;
        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
        rotationCoroutine = StartCoroutine(SmoothRotate(targetAngle));

        comboStep++;
        if (comboStep > 3) comboStep = 1;
        alreadyAppliedForce = false;
        bufferCombo = false;

        if (animator != null)
        {
            animator.SetInteger("ComboStep", comboStep);
            animator.SetTrigger("Attack");
        }
    }

    private System.Collections.IEnumerator SmoothRotate(float targetAngle)
    {
        Quaternion target = Quaternion.Euler(0f, targetAngle, 0f);
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 720f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = target;
    }

    // ==================== Animation Logic ====================

    private void CheckAnimationLogic()
    {
        if (animator == null) return;
        var sInfo = animator.GetCurrentAnimatorStateInfo(0);
        var nInfo = animator.GetNextAnimatorStateInfo(0);
        bool isLocked = sInfo.IsTag("Attack") || nInfo.IsTag("Attack") || sInfo.IsTag("Roll") || nInfo.IsTag("Roll");
        bool justTrigger = Time.time - lastClickTime < 0.2f;

        if (isLocked || justTrigger)
        {
            float t = sInfo.normalizedTime % 1f;
            if (animator.IsInTransition(0) && (nInfo.IsTag("Attack") || nInfo.IsTag("Roll")))
                t = nInfo.normalizedTime % 1f;

            bool isRolling = sInfo.IsTag("Roll") || nInfo.IsTag("Roll");
            if (t >= forceTime && !alreadyAppliedForce && sInfo.IsTag("Attack") && !isRolling)
            {
                PerformAttackDash();
                alreadyAppliedForce = true;
            }

            float window = comboStep == 3 ? finisherWindowTime : comboWindowTime;
            if (bufferCombo && t >= window) TriggerAttack();
        }
        else
        {
            if (comboStep != 0 && !animator.IsInTransition(0) && !justTrigger) ResetCombo();
        }
    }

    private void ResetCombo()
    {
        comboStep = 0; bufferCombo = false; alreadyAppliedForce = false;
        if (animator != null) animator.SetInteger("ComboStep", 0);
    }

    // ใช้สำหรับเคลียร์คอมโบเวลาเริ่มร่ายสกิล (เรียกแบบ Public ได้)
    public void ResetComboAndDash()
    {
        ResetCombo();
        currentDashVelocity = Vector3.zero;
    }

    public void PerformAttackDash() => currentDashVelocity = transform.forward * attackDashForce;

    // ==================== Move ====================

    private void Move()
    {
        var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
        bool isLocked = animator != null && (sInfo.IsTag("Attack") || nInfo.IsTag("Attack") ||
                                             sInfo.IsTag("Roll")   || nInfo.IsTag("Roll") ||
                                             sInfo.IsTag("Hit")    || nInfo.IsTag("Hit") ||
                                             sInfo.IsTag("Parry")  || nInfo.IsTag("Parry") ||
                                             sInfo.IsTag("Skill")  || nInfo.IsTag("Skill"));

        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v).normalized;

        if (animator != null)
        {
            animator.SetFloat("FreelookSpeed", dir.magnitude, 0.05f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
        }

        Vector3 finalMove = Vector3.zero;

        // --- Attack Dash ---
        if (currentDashVelocity.magnitude > 0.1f)
        {
            finalMove += currentDashVelocity;
            currentDashVelocity = Vector3.Lerp(currentDashVelocity, Vector3.zero, dashDecay * Time.deltaTime);
        }

        // --- Hit Knockback ---
        if (currentHitVelocity.magnitude > 0.1f)
        {
            finalMove += currentHitVelocity;
            currentHitVelocity = Vector3.Lerp(currentHitVelocity, Vector3.zero, 10f * Time.deltaTime);
        }

        if (!isLocked)
        {
            if (dir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg +
                                    (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                    ref rotationVelocity, 1f / rotationSpeed);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                finalMove += Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * moveSpeed;
            }

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetTrigger("Jump");
            }
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        finalMove += verticalVelocity;

        // รวบยอดเดินทีเดียว จะช่วยแก้บัค CharacterController ไถลบน Terrain ได้มหาศาล
        controller.Move(finalMove * Time.deltaTime);
    }

    // ==================== Animation Events ====================

    /// <summary>เรียกจาก Animation Event พร้อมส่งเลข 1, 2, 3 มาด้วย</summary>
    public void PlayAttackSoundEvent(int soundIndex)
    {
        if (playerAudio != null && soundIndex >= 1 && soundIndex <= 3)
        {
            // แปลงค่า int เป็น Enum (0 = Attack1, 1 = Attack2, etc.)
            PlayerSoundType type = (PlayerSoundType)(soundIndex - 1);
            playerAudio.PlaySound(type);
        }
    }

    public void EnableInvincibility() => isInvincible = true;
    public void DisableInvincibility() => isInvincible = false;

    public void EnableParryWindow() => isParrying = true;
    public void DisableParryWindow() => isParrying = false;
    public void PlayParryCastSoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.ParryCast);
    }
    
    public void PlayJumpSoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.Jump);
    }

    public void PlayRollSoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.Roll);
    }

    public void PlayHitSoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.Hit);
    }

    public void UsingSkill1SoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.UsingSkill1);
    }

    public void PlaySkill1SoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.Skill1);
    }

    /// <summary>เรียกจาก EnemyAI หรือ Projectile ที่ชนผู้เล่น</summary>
    public void TakeDamage(int rawDmg, Vector3 attackerPosition = default)
    {
        if (isDead) return;

        // ถ้าโดนโจมตีตอนกำลังตั้งการ์ด (Parry)
        if (isParrying)
        {
            Debug.Log("<color=yellow>[Player]</color> Parry สำเร็จ!");
            if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.ParrySuccess);
            
            // แสดง VFX ตรงจุดที่ตั้งไว้ หรือจุดที่ดาบอยู่
            if (parryHitVFXPrefab != null)
            {
                // กำหนดค่าตั้งต้นกัน Error ลืมลากเอาไว้ที่หน้าผู้เล่น
                Vector3 vfxSpawnPos = transform.position + transform.forward * 1f + Vector3.up * 1.5f;

                if (parryVFXSpawnPoint != null)
                {
                    // ถ้าลากจุด Spawn ใส่มา ให้โผล่ตรงนั้นเลย (แม่นยำที่สุด)
                    vfxSpawnPos = parryVFXSpawnPoint.position;
                }

                GameObject vfx = Instantiate(parryHitVFXPrefab, vfxSpawnPos, Quaternion.identity);
                Destroy(vfx, 2f); // สมมติว่าเอฟเฟคอยู่นาน 2 วิ
            }

            // TODO: ในอนาคตสามารถใส่คำสั่งให้ศัตรูชะงัก (Stun) หรือคูลดาวน์สกิลได้ตรงนี้
            return; // ไม่รับดาเมจ
        }

        if (isInvincible) { print("ไม่โดนเว้ย (อมตะ/กลิ้ง)"); return; }
        
        // เช็คว่ากำลังร่ายสกิลอยู่หรือเปล่า (Super Armor)
        var sInfo = animator.GetCurrentAnimatorStateInfo(0);
        var nInfo = animator.GetNextAnimatorStateInfo(0);
        bool isCastingSkill = sInfo.IsTag("Skill") || nInfo.IsTag("Skill");

        if (!isCastingSkill)
        {
            if (animator != null)
            {
                animator.ResetTrigger("Attack"); // ยกเลิก Trigger ฟันที่ค้างอยู่ในคิว
                animator.SetTrigger("Damage");
            }

            // โดนตี → ยกเลิกท่าโจมตี + ปิดอาวุธ + หยุดแรงพุ่ง + กระเด็นถอยหลัง
            ResetCombo();
            currentDashVelocity = Vector3.zero;

            // บังคับปิดดาเมจของอาวุธทันที — ทั้ง 2 ทาง เพื่อความมั่นใจ
            if (weaponHandler != null) weaponHandler.DisableHitbox();
            

            // เล่นเสียงโดนฟัน (เสียงอาวุธกระแทก) คู่กับเสียงคนร้อง
            if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.HitImpact);

            // เล่น VFX ตอนโดนตี
            if (hitVFXPrefab != null)
            {
                Vector3 vfxPos = hitVFXSpawnPoint != null 
                    ? hitVFXSpawnPoint.position 
                    : transform.position + Vector3.up * 1.5f;
                GameObject vfx = Instantiate(hitVFXPrefab, vfxPos, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            // คำนวณทิศทางกระเด็น (ออกจากศูนย์กลางของคนที่ตี)
            Vector3 knockbackDir = -transform.forward; // ค่าเริ่มต้นถ้าไม่มี attackerPosition
            if (attackerPosition != default)
            {
                knockbackDir = (transform.position - attackerPosition).normalized;
                knockbackDir.y = 0; // ไม่ให้กระเด็นขึ้นฟ้า/มุดดิน
            }
            currentHitVelocity = knockbackDir * 4f; // 4f คือความแรงกระเด็น (ปรับแต่งเลขนี้ได้)
        }
        else
        {
            Debug.Log("<color=cyan>[Player]</color> ทนทานการโจมตี (Super Armor) จากสกิล!");
        }

        int actual = Mathf.Max(1, Mathf.RoundToInt(rawDmg - Defense));
        currentHP = Mathf.Max(0, currentHP - actual);
        if (healthBar != null) healthBar.value = currentHP;

        Debug.Log($"<color=red>[Player]</color> รับ {rawDmg} - Def{Defense:F0} = {actual} จริง | HP:{currentHP}");
        if (currentHP <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("<color=red>[Player]</color> ตายแล้ว!");
        
        if (animator != null) animator.SetTrigger("Die");
        if (controller != null) controller.enabled = false;
    }
}