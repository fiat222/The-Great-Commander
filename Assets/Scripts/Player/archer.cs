using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class Archer : MonoBehaviour
{
    // ==================== SO ====================
    [Header("Stats SO")]
    [Tooltip("ลาก PlayerStatsSO ตัวเดียวกับ PlayerController มาใส่ได้เลยครับ")]
    public PlayerStatsSO stats;

    // ==================== Movement ====================
    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float rotationSpeed = 13f;
    public float jumpHeight = 3f;
    public float gravity = -19.62f;

    [Header("Aim Settings")]
    public float aimMoveSpeed = 5f;
    public float aimRotationSpeed = 8f;

    // ==================== Arrow ====================
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public AimCrosshair crosshair;
    public float spreadMax = 15f;
    public float spreadMin = 0.5f;

    [Header("Arrow Power Settings")]
    public float minArrowSpeed = 15f;
    public float maxArrowSpeed = 40f;
    [Range(1, 50)] public int minDamage = 3;
    [Range(1, 50)] public int maxDamage = 15;
    public float maxSpreadAngle = 10f;

    // ==================== Roll ====================
    [Header("Roll Settings")]
    public float rollForce = 20f;
    public float rollSpeed = 15f;
    public float rollDecay = 8f;
    public float dashDecay = 10f;
    [Range(0, 1)] public float forceTime = 0.2f;

    // ==================== Target Lock ====================
    [Header("Target Lock Settings")]
    public float lockRange = 15f;
    public CinemachineTargetGroup targetGroup;
    public Transform cameraPivot;
    private Transform currentTarget;
    private bool isLockedOn;

    // ==================== References ====================
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
    private bool isDead;
    private Vector3 currentDashVelocity;
    private Vector3 rollDirection;
    private float rotationVelocity;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private bool isInvincible;
    private Coroutine rotationCoroutine;

    private bool isAiming;
    private bool hasFiredThisAim;
    private float lastAccuracy;

    // ==================== Lifecycle ====================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        ApplyStats(isFirstInit: true);

        if (crosshair == null)
        {
            var obj = GameObject.FindWithTag("Crosshair");
            if (obj != null) crosshair = obj.GetComponent<AimCrosshair>();
        }
        if (crosshair == null)
            Debug.LogWarning("<color=red>[Archer]</color> ยังไม่มีการผูก Crosshair!");
    }

    /// <summary>
    /// ดึงค่าจาก SO มาใช้
    /// minDamage/maxDamage จะ scale ตาม AttackDamage อัตโนมัติ (20%–100%)
    /// </summary>
    public void ApplyStats(bool isFirstInit = false)
    {
        if (stats != null)
        {
            maxHP = stats.GetHP();
            moveSpeed = stats.GetSpeed() * 2.4f;
            aimMoveSpeed = stats.GetSpeed() * 1.0f;
            AttackDamage = stats.GetDamage();
            Defense = stats.GetDefense();

            minDamage = Mathf.Max(1, Mathf.RoundToInt(AttackDamage * 0.20f));
            maxDamage = Mathf.RoundToInt(AttackDamage);
        }

        if (isFirstInit)
        {
            currentHP = maxHP;
            if (healthBar != null) { healthBar.maxValue = maxHP; healthBar.value = maxHP; }
        }

        Debug.Log($"[Archer] Stats Lv{(stats != null ? stats.CurrentLevel : 0)} | HP:{maxHP} Spd:{moveSpeed:F1} Def:{Defense:F1} MinDmg:{minDamage} MaxDmg:{maxDamage}");
    }

    private void Update()
    {
        if (isDead) return;
        HandleTargetLockInput();
        HandleRollInput();
        HandleAimAndShootInput();
        CheckAnimationLogic();
        Move();
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
        bool isBusy = animator != null &&
                      (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") ||
                       animator.GetCurrentAnimatorStateInfo(0).IsTag("Roll") ||
                       animator.GetNextAnimatorStateInfo(0).IsTag("Attack") ||
                       animator.GetNextAnimatorStateInfo(0).IsTag("Roll") || isAiming);
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

        currentDashVelocity = rollDirection * (rollForce * 0.5f);
        if (animator != null) animator.SetTrigger("Roll");
    }

    // ==================== Aim & Shoot ====================

    private void HandleAimAndShootInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            bool isBusy = animator != null &&
                          (animator.GetCurrentAnimatorStateInfo(0).IsTag("Roll") ||
                           animator.GetNextAnimatorStateInfo(0).IsTag("Roll"));
            if (!isBusy) StartAiming();
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (isAiming && !hasFiredThisAim) Shoot();
            StopAiming();
        }

        bool isPlayingAttack = animator != null &&
                               (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") ||
                                animator.GetNextAnimatorStateInfo(0).IsTag("Attack"));
        if (isAiming || isPlayingAttack) { UpdateAimBlendTree(); FaceCamera(); }
    }

    private void StartAiming()
    {
        isAiming = true; hasFiredThisAim = false;
        if (animator != null) { animator.SetBool("isAiming", true); animator.SetTrigger("DrawArrow"); }
        if (crosshair != null) crosshair.StartAim();
    }

    private void StopAiming()
    {
        isAiming = false;
        if (animator != null) animator.SetBool("isAiming", false);
        if (crosshair != null) crosshair.StopAim();
    }

    private void Shoot()
    {
        hasFiredThisAim = true;
        lastAccuracy = crosshair != null ? crosshair.GetAccuracy() : 1f;
        if (animator != null) animator.SetTrigger("Shoot");
    }

    public void SpawnArrow()
    {
        if (arrowPrefab == null || arrowSpawnPoint == null) return;

        // ใช้ ViewportPointToRay เพื่อความแม่นยำสูงสุดที่จุดกึ่งกลางหน้าจอ
        Ray ray = mainCameraTransform != null ? 
                  Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)) : 
                  new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

        // เล็งกระแทกทุกอย่าง ยกเว้น Player และ Minion เพื่อให้จุดเล็ง (targetPt) ถูกต้องเสมอ
        int mask = ~(LayerMask.GetMask("Player") | LayerMask.GetMask("Minion"));

        Vector3 targetPt = Physics.Raycast(ray, out RaycastHit hit, 200f, mask)
            ? hit.point : ray.GetPoint(200f);

        Vector3 dir = (targetPt - arrowSpawnPoint.position).normalized;
        float finalSpd = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, lastAccuracy);
        int finalDmg = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, lastAccuracy));

        Debug.Log($"<color=cyan>[Archer]</color> ยิง! Acc:{lastAccuracy:P0} Spd:{finalSpd:F1} Dmg:{finalDmg}");
        var arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.LookRotation(dir));
        arrow.GetComponent<ArrowProjectile>()?.Launch(dir, finalSpd, finalDmg);
    }

    private void UpdateAimBlendTree()
    {
        if (animator == null) return;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 world = mainCameraTransform != null
            ? Quaternion.Euler(0, mainCameraTransform.eulerAngles.y, 0) * new Vector3(h, 0, v)
            : new Vector3(h, 0, v);
        Vector3 local = transform.InverseTransformDirection(world);
        animator.SetFloat("Right", local.x, 0.05f, Time.deltaTime);
        animator.SetFloat("Forward", local.z, 0.05f, Time.deltaTime);
    }

    private void FaceCamera()
    {
        if (mainCameraTransform == null) return;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y,
                                            mainCameraTransform.eulerAngles.y,
                                            ref rotationVelocity, 1f / aimRotationSpeed);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
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

    private void CheckAnimationLogic() { }

    // ==================== Move ====================

    private void Move()
    {
        var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;

        bool isRolling = animator != null && (sInfo.IsTag("Roll") || nInfo.IsTag("Roll"));
        bool isPlayingAttack = animator != null && (sInfo.IsTag("Attack") || nInfo.IsTag("Attack"));
        bool isLocked = isRolling;

        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float spd = (isAiming || isPlayingAttack) ? aimMoveSpeed : moveSpeed;
        Vector3 dir = new Vector3(h, 0f, v).normalized;

        if (animator != null)
        {
            if (!isAiming && !isPlayingAttack)
                animator.SetFloat("FreelookSpeed", dir.magnitude, 0.05f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
        }

        if (currentDashVelocity.magnitude > 0.1f)
        {
            controller.Move(currentDashVelocity * Time.deltaTime);
            float decay = isRolling ? rollDecay : dashDecay;
            currentDashVelocity = Vector3.Lerp(currentDashVelocity, Vector3.zero, decay * Time.deltaTime);
        }

        if (isRolling && sInfo.normalizedTime % 1f < 0.75f)
            controller.Move(rollDirection * rollSpeed * Time.deltaTime);

        if (!isLocked)
        {
            if (isAiming || isPlayingAttack)
            {
                if (dir.magnitude >= 0.1f)
                {
                    float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg +
                                  (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                    controller.Move(Quaternion.Euler(0f, angle, 0f) * Vector3.forward * spd * Time.deltaTime);
                }
            }
            else
            {
                if (dir.magnitude >= 0.1f)
                {
                    float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg +
                                        (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                        ref rotationVelocity, 1f / rotationSpeed);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    controller.Move(Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * spd * Time.deltaTime);
                }
                if (Input.GetButtonDown("Jump") && isGrounded)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (animator != null) animator.SetTrigger("Jump");
                }
            }
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // ==================== Animation Events ====================

    public void EnableInvincibility() => isInvincible = true;
    public void DisableInvincibility() => isInvincible = false;

    public void TakeDamage(int rawDmg)
    {
        if (isDead || isInvincible) { print("ไม่โดนเว้ย"); return; }
        if (animator != null) animator.SetTrigger("Damage");

        int actual = Mathf.Max(1, Mathf.RoundToInt(rawDmg - Defense));
        currentHP = Mathf.Max(0, currentHP - actual);
        if (healthBar != null) healthBar.value = currentHP;

        Debug.Log($"<color=red>[Archer]</color> รับ {rawDmg} - Def{Defense:F0} = {actual} จริง | HP:{currentHP}");
        if (currentHP <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("<color=red>[Archer]</color> ตายแล้ว!");
        if (animator != null) animator.SetTrigger("Die");
    }
}