using UnityEngine;
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
    public float rollForce = 20f;
    public float rollDecay = 8f;
    [Range(0, 1)] public float forceTime = 0.2f;
    [Range(0, 1)] public float comboWindowTime = 0.5f;
    [Range(0, 1)] public float finisherWindowTime = 0.85f;

    private int comboStep;
    private float lastClickTime;
    private Vector3 currentDashVelocity;
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
    private bool isDead;
    public bool IsDead => isDead;
    private Coroutine rotationCoroutine;

    public bool isMovementLocked { get; set; }

    // ==================== Lifecycle ====================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

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
        HandleTargetLockInput();
        HandleRollInput();
        HandleAttackInput();
        CheckAnimationLogic();
        UpdateWeaponEffect();
        Move();
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
        if (!Input.GetKeyDown(KeyCode.LeftShift) || !isGrounded || isBusy) return;

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

        currentDashVelocity = rollDirection * rollForce;
        if (animator != null) animator.SetTrigger("Roll");
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

    public void PerformAttackDash() => currentDashVelocity = transform.forward * attackDashForce;

    // ==================== Move ====================

    private void Move()
    {
        var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
        bool isLocked = animator != null && (sInfo.IsTag("Attack") || nInfo.IsTag("Attack") ||
                                             sInfo.IsTag("Roll") || nInfo.IsTag("Roll"));

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

        if (currentDashVelocity.magnitude > 0.1f)
        {
            controller.Move(currentDashVelocity * Time.deltaTime);
            float decay = isLocked && sInfo.IsTag("Roll") ? rollDecay : dashDecay;
            currentDashVelocity = Vector3.Lerp(currentDashVelocity, Vector3.zero, decay * Time.deltaTime);
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
                controller.Move(Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * moveSpeed * Time.deltaTime);
            }

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetTrigger("Jump");
            }
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // ==================== Animation Events ====================

    public void EnableInvincibility() => isInvincible = true;
    public void DisableInvincibility() => isInvincible = false;

    /// <summary>เรียกจาก EnemyAI หรือ Projectile ที่ชนผู้เล่น</summary>
    public void TakeDamage(int rawDmg)
    {
        if (isDead || isInvincible) { print("ไม่โดนเว้ย"); return; }
        if (animator != null) animator.SetTrigger("Damage");

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