using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using PlayerAudio;
using Unity.Netcode;

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
    [Range(1, 100)] public int maxDamage = 25;
    public float maxSpreadAngle = 10f;

    [Header("Quick Shot Settings")]
    [Tooltip("ดาเมจยิงเร็ว (คลิกซ้ายครั้งเดียว โหมดปกติ)")]
    public int quickShotDamage = 10;
    [Tooltip("ความเร็วลูกดอกยิงเร็ว")]
    public float quickShotSpeed = 35f;

    // ==================== Roll / Dodge ====================
    [Header("Roll / Dodge Settings")]
    public AnimationCurve dodgeCurve = AnimationCurve.Linear(0f, 15f, 0.5f, 0f);
    private bool isDodging;
    private float dodgeTimer;
    private float dodgeCooldownTimer;

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
    private PlayerAudioComponent playerAudio;

    [Header("Hit VFX")]
    public GameObject hitVFXPrefab;
    public Transform hitVFXSpawnPoint;

    [Header("Attack VFX")]
    public GameObject chargingVFXPrefab;
    public GameObject fullChargeVFXPrefab;
    public GameObject shootVFXPrefab;
    private GameObject activeChargingVFX;
    private GameObject activeFullChargeVFX;

    [Header("Player Cameras")]
    public CinemachineCamera freelookCamera;
    public CinemachineCamera targetLockCamera;

    // ==================== Ground Check ====================
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public float groundCheckRadius = 0.4f;
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
    public bool IsDead => isDead;
    private Vector3 currentHitVelocity;
    private Vector3 rollDirection;
    private float rotationVelocity;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private bool isInvincible;
    private Coroutine rotationCoroutine;

    private bool isAiming;
    private bool hasFiredThisAim;
    private float lastAccuracy;
    private bool forceNextShot100Accuracy;
    private bool hasForcedTarget;
    private Vector3 forcedTargetPosition;

    private bool isChargeModeActive;
    public bool IsChargeModeActive => isChargeModeActive;
    private bool pendingQuickShot;
    private bool bufferedShot; // ⭐ สำหรับเก็บ Input คลิกซ้ายตอนกำลังกลิ้ง

    private bool inputEnabled = true;
    private bool mouseEnabled => Cursor.lockState == CursorLockMode.Locked;
    private bool isChatOpen => ChatManager.Instance != null && ChatManager.Instance.IsChatOpen;

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

        if (healthBar == null)
        {
            var hpBarObj = GameObject.FindWithTag("HPBar");
            if (hpBarObj != null)
                healthBar = hpBarObj.GetComponent<Slider>();
            else
                Debug.LogWarning("[Archer] ไม่พบ GameObject ที่มี Tag 'HPBar'");
        }

        ApplyStats(isFirstInit: true);

        if (crosshair == null)
        {
            var obj = GameObject.FindWithTag("Crosshair");
            if (obj != null) crosshair = obj.GetComponent<AimCrosshair>();
        }
        if (crosshair == null)
            Debug.LogWarning("<color=red>[Archer]</color> ยังไม่มีการผูก Crosshair!");
    }

    public void ApplyStats(bool isFirstInit = false)
    {
        if (stats != null)
        {
            int oldMaxHP = maxHP;  // เก็บค่าเดิมก่อน

            maxHP = stats.GetHP();
            moveSpeed = stats.GetSpeed();
            aimMoveSpeed = stats.GetSpeed();
            AttackDamage = stats.GetDamage();
            Defense = stats.GetDefense();

            minDamage = Mathf.Max(1, Mathf.RoundToInt(AttackDamage * 0.20f));
            maxDamage = Mathf.RoundToInt(AttackDamage * 2f);
            quickShotDamage = Mathf.RoundToInt(AttackDamage);

            ApplySkillIcons();

            // เพิ่ม currentHP ตาม diff ที่ max เพิ่มขึ้น
            if (!isFirstInit && oldMaxHP > 0)
            {
                int diff = maxHP - oldMaxHP;
                currentHP = Mathf.Min(currentHP + diff, maxHP);
            }
        }

        if (isFirstInit)
        {
            currentHP = maxHP;
            if (healthBar != null) { healthBar.maxValue = maxHP; healthBar.value = maxHP; }
            return;
        }

        if (healthBar != null)
        {
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }

        Debug.Log($"[Archer] Stats Lv{(stats != null ? stats.CurrentLevel : 0)} | HP:{currentHP}/{maxHP} Spd:{moveSpeed:F1} Def:{Defense:F1} MinDmg:{minDamage} MaxDmg:{maxDamage}");
    }

    /// <summary>
    /// หา Image ที่ติด Tag "SkillNormal" และ "SkillSpecial" แล้วใส่ icon จาก SO อัตโนมัติ
    /// Tag ให้ติดที่ Image component บน NormalAtk/SpecialAtk ใน Hierarchy
    /// </summary>
    private void ApplySkillIcons()
    {
        if (stats == null) return;
        SetIconByTag("SkillIcon",    stats.skillIcon);
        SetIconByTag("SkillNormal",  stats.normalAttackIcon);
        SetIconByTag("SkillSpecial", stats.specialAttackIcon);

        // แจ้ง ArcherSkill ให้รีเฟรช icon ด้วยเมื่อ character เปลี่ยน
        GetComponent<ArcherSkill>()?.ApplySkillIconFromSO();
    }

    private void SetIconByTag(string tag, Sprite icon)
    {
        if (icon == null) return;
        var obj = GameObject.FindWithTag(tag);
        if (obj == null) { Debug.LogWarning($"[Archer] ไม่พบ Tag '{tag}'"); return; }
        var img = obj.GetComponent<Image>();
        if (img != null) img.sprite = icon;
    }

    private void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal += OnPhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal += OnPhaseChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        inputEnabled = (phase == GamePhase.Combat);
        if (!inputEnabled) StopAiming();

        if (phase == GamePhase.Combat)
        {
            ApplySkillIcons();
            if (crosshair == null)
            {
                var obj = GameObject.FindWithTag("Crosshair");
                if (obj != null) crosshair = obj.GetComponent<AimCrosshair>();
                if (crosshair != null) crosshair.SetQuickShotMode(!isChargeModeActive);
            }
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            inputEnabled = (GameManager.Instance.CurrentPhase == GamePhase.Combat);
        else if (SoloGameManager.Instance != null)
            inputEnabled = (SoloGameManager.Instance.CurrentPhase == GamePhase.Combat);

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.RegisterPlayerCameras(freelookCamera, targetLockCamera);
        }

        if (crosshair != null)
        {
            crosshair.SetQuickShotMode(!isChargeModeActive);
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (dodgeCooldownTimer > 0) dodgeCooldownTimer -= Time.deltaTime;

        if (!inputEnabled || isChatOpen)
        {
            if (!isDodging) ApplyGravityOnly();
            return;
        }

        HandleTargetLockInput();
        HandleRollInput();
        if (mouseEnabled)
            HandleAimAndShootInput();
        CheckAnimationLogic();

        if (!isDodging)
            Move();
        else
            ApplyGravityDuringDodge();
    }

    private void ApplyGravityOnly()
    {
        UpdateGroundedStatus();
        if (isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2f;
        else verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void ApplyGravityDuringDodge()
    {
        UpdateGroundedStatus();

        if (isGrounded && verticalVelocity.y < 0)
            verticalVelocity.y = -2f;
        else
            verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move(verticalVelocity * Time.deltaTime);
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

        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundDistance);
        }
    }

    // ==================== Roll ====================

    private void HandleRollInput()
    {
        var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
        bool isBusy = animator != null && (sInfo.IsTag("Roll") || nInfo.IsTag("Roll"));

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
        dodgeCooldownTimer = dodgeTimer + 0.15f;

        if (animator != null)
        {
            animator.ResetTrigger("Damage");
            animator.ResetTrigger("DrawArrow");
            animator.ResetTrigger("Shoot");
            animator.SetTrigger("Roll");
        }

        float timer = 0f;
        bool heightCompressed = false;

        while (timer < dodgeTimer)
        {
            if (!heightCompressed && timer > dodgeTimer / 3f)
            {
                controller.center = new Vector3(0, 0.45f, 0);
                controller.height = 0.9f;
                heightCompressed = true;
            }

            float curveSpeed = dodgeCurve.Evaluate(timer);
            controller.Move(rollDirection * curveSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        controller.center = new Vector3(0, 0.9f, 0);
        controller.height = 1.8f;
        isDodging = false;
    }

    // ==================== Aim & Shoot ====================

    private void HandleAimAndShootInput()
    {
        bool isPlayingRoll = animator != null && (animator.GetCurrentAnimatorStateInfo(0).IsTag("Roll") || animator.GetNextAnimatorStateInfo(0).IsTag("Roll"));
        bool isPlayingAttack = animator != null && (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || animator.GetNextAnimatorStateInfo(0).IsTag("Attack"));
        bool isTransitioning = animator != null && animator.IsInTransition(0);
        
        // isBusy จะใช้สำหรับกันการ "เริ่ม" ท่าทางใหม่เท่านั้น
        bool isBusy = isPlayingRoll || isPlayingAttack || isTransitioning;

        // --- 🖱️ ระบบสลับโหมด (ชาร์จ / ยิงเร็ว) ---
        if (Input.GetMouseButtonDown(1))
        {
            if (isAiming) StopAiming();
            isChargeModeActive = !isChargeModeActive;
            if (crosshair != null) crosshair.SetQuickShotMode(!isChargeModeActive);
            Debug.Log($"<color=yellow>[Archer]</color> โหมด: {(isChargeModeActive ? "ชาร์จ" : "ยิงเร็ว")}");
        }

        // --- 🏹 ระบบ Input Buffering ---
        if (Input.GetMouseButtonDown(0) && isBusy)
        {
            bufferedShot = true;
            animator.SetBool("hasBuffer", true);

            if (isPlayingRoll)
            {
                if (isChargeModeActive) 
                {
                    // ⭐ [เเก้ไข] เริ่มระบบ logic การชาร์จทันที (วงจะได้บีบเเละเริ่มนับค่าพลัง)
                    animator.SetTrigger("DrawArrow");
                    StartAiming();
                }
                else animator.SetTrigger("QuickShot");
            }
        }

        // --- 🎯 ระบบควบคุมการทำงาน (Logic) ---
        
        // 1. จัดการเรื่อง Buffer เมื่อหายกลิ้ง (เปลี่ยนจาก isBusy เป็น !isPlayingRoll เพื่อความแม่นยำ)
        if (!isPlayingRoll && bufferedShot)
        {
            bufferedShot = false;
            animator.SetBool("hasBuffer", false);
            if (isChargeModeActive) 
            {
                if (!isAiming) StartAiming();
                if (!Input.GetMouseButton(0)) { Shoot(); StopAiming(); }
            }
            else
            {
                pendingQuickShot = true;
                lastAccuracy = 1f;
                animator.SetTrigger("QuickShot");
            }
        }

        // 2. จัดการการยิง/ง้าง (แยกออกจาก isBusy เพื่อให้ปล่อยเมาส์ได้ทุกเมื่อ)
        if (isChargeModeActive)
        {
            // การ "เริ่ม" ง้าง ต้องว่างก่อน
            if (!isBusy && Input.GetMouseButton(0) && !isAiming && !bufferedShot)
            {
                StartAiming();
            }

            // การ "ปล่อย" หรือ "ชาร์จ" ต้องทำได้ตลอดถ้า isAiming เป็น true
            if (isAiming)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    // ⭐ [เเก้ไข] ถ้ายังกลิ้งอยู่ ห้ามยิงเด็ดขาด (ป้องกันการยิงดาเมจ 0)
                    if (!isPlayingRoll)
                    {
                        if (!hasFiredThisAim) Shoot();
                        StopAiming();
                    }
                }
            }

            // ระบบความปลอดภัย: หยุดง้างถ้ากลิ้ง (ยกเว้นตอนกำลังจองท่า)
            if (isAiming && isPlayingRoll && !bufferedShot) StopAiming();
        }
        else
        {
            // โหมดยิงเร็ว: ต้องว่างถึงจะยิงได้
            if (!isBusy && Input.GetMouseButtonDown(0))
            {
                pendingQuickShot = true;
                lastAccuracy = 1f;
                animator.SetTrigger("QuickShot");
            }
        }

        if (isAiming || isPlayingAttack) { UpdateAimBlendTree(); FaceCamera(); }

        // --- ระบบสปาวน์ Charging & Full Charge VFX ---
        if (isAiming && !isQuickShotModeActive())
        {
            float acc = crosshair != null ? crosshair.GetAccuracy() : 0f;
            
            if (acc >= 1f)
            {
                // เข้าสู่สถานะชาร์จเต็ม
                if (activeFullChargeVFX == null && fullChargeVFXPrefab != null)
                {
                    activeFullChargeVFX = Instantiate(fullChargeVFXPrefab, arrowSpawnPoint);
                }
                if (activeChargingVFX != null) Destroy(activeChargingVFX);
            }
            else if (acc > 0.1f) // เริ่มแสดงผลเมื่อมีการชาร์จไปสักพัก (เลี่ยงแวบๆ ตอนคลิกไว)
            {
                // กำลังชาร์จ
                if (activeChargingVFX == null && chargingVFXPrefab != null)
                {
                    activeChargingVFX = Instantiate(chargingVFXPrefab, arrowSpawnPoint);
                }
                if (activeFullChargeVFX != null) Destroy(activeFullChargeVFX);
            }
        }
    }

    private bool isQuickShotModeActive()
    {
        return !isChargeModeActive;
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

        if (activeChargingVFX != null) Destroy(activeChargingVFX);
        if (activeFullChargeVFX != null) Destroy(activeFullChargeVFX);
    }

    private void Shoot()
    {
        hasFiredThisAim = true;

        if (forceNextShot100Accuracy)
        {
            lastAccuracy = 1f;
            forceNextShot100Accuracy = false;
        }
        else
        {
            lastAccuracy = crosshair != null ? crosshair.GetAccuracy() : 1f;
        }

        if (animator != null) animator.SetTrigger("Shoot");

        // --- ระบบสปาวน์ Shoot VFX ---
        if (shootVFXPrefab != null && arrowSpawnPoint != null)
        {
            GameObject vfx = Instantiate(shootVFXPrefab, arrowSpawnPoint.position, transform.rotation);
            Destroy(vfx, 2f);
        }

        if (activeChargingVFX != null) Destroy(activeChargingVFX);
        if (activeFullChargeVFX != null) Destroy(activeFullChargeVFX);
    }

    public void ForceNextShotAccuracy(Vector3? targetPos = null)
    {
        forceNextShot100Accuracy = true;
        if (targetPos.HasValue)
        {
            hasForcedTarget = true;
            forcedTargetPosition = targetPos.Value;
        }
    }

    public void SpawnArrow()
    {
        if (arrowPrefab == null || arrowSpawnPoint == null) return;

        Vector3 targetPt;

        if (hasForcedTarget)
        {
            targetPt = forcedTargetPosition;
            hasForcedTarget = false;
        }
        else
        {
            Ray ray = mainCameraTransform != null
                ? Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))
                : new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

            int mask = ~(LayerMask.GetMask("Player") | LayerMask.GetMask("Minion"));
            targetPt = Physics.Raycast(ray, out RaycastHit hit, 200f, mask)
                ? hit.point : ray.GetPoint(200f);
        }

        Vector3 dir = (targetPt - arrowSpawnPoint.position).normalized;

        var arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.LookRotation(dir));
        var proj = arrow.GetComponent<ArrowProjectile>();

        if (pendingQuickShot)
        {
            pendingQuickShot = false;
            proj?.LaunchStraight(dir, quickShotSpeed, quickShotDamage, crosshair, playerAudio);
            Debug.Log($"<color=cyan>[Archer]</color> Quick Shot! Spd:{quickShotSpeed:F1} Dmg:{quickShotDamage}");
        }
        else
        {
            float finalSpd = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, lastAccuracy);
            int finalDmg = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, lastAccuracy));
            proj?.Launch(dir, finalSpd, finalDmg, crosshair, playerAudio);
            Debug.Log($"<color=cyan>[Archer]</color> Charge Shot! Acc:{lastAccuracy:P0} Spd:{finalSpd:F1} Dmg:{finalDmg}");
        }
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
        bool isHit = animator != null && (sInfo.IsTag("Hit") || nInfo.IsTag("Hit"));
        bool isPlayingAttack = animator != null && (sInfo.IsTag("Attack") || nInfo.IsTag("Attack"));
        bool isLocked = isRolling || isHit || isDead ||
                        (animator != null && animator.IsInTransition(0) && nInfo.IsTag("Roll"));

        UpdateGroundedStatus();
        if (isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float spd = (isAiming || isPlayingAttack) ? aimMoveSpeed : moveSpeed;
        Vector3 dir = new Vector3(h, 0f, v).normalized;

        if (isLocked) dir = Vector3.zero;

        if (animator != null)
        {
            if (!isAiming && !isPlayingAttack)
                animator.SetFloat("FreelookSpeed", dir.magnitude, 0.05f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
        }

        Vector3 finalMove = Vector3.zero;

        if (currentHitVelocity.magnitude > 0.1f)
        {
            finalMove += currentHitVelocity;
            currentHitVelocity = Vector3.Lerp(currentHitVelocity, Vector3.zero, 10f * Time.deltaTime);
        }

        if (!isLocked)
        {
            if (isAiming || isPlayingAttack)
            {
                if (dir.magnitude >= 0.1f)
                {
                    float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg +
                                  (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                    finalMove += Quaternion.Euler(0f, angle, 0f) * Vector3.forward * spd;
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
                    finalMove += Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * spd;
                }

                if (Input.GetButtonDown("Jump") && isGrounded)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (animator != null) animator.SetTrigger("Jump");
                }
            }
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        finalMove += verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    private void UpdateGroundedStatus()
    {
        if (groundCheck == null) return;

        float castRadius = groundCheckRadius;
        float castDistance = groundDistance;
        Vector3 origin = groundCheck.position + Vector3.up * castRadius;

        isGrounded = Physics.SphereCast(origin, castRadius, Vector3.down, out _, castDistance, groundMask);
    }

    // ==================== Animation Events ====================

    public void PlayShootSoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.Attack1);
    }

    public void PlayChargeBowSoundEvent()
    {
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.ChargeBow);
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

    public void EnableInvincibility() => isInvincible = true;
    public void DisableInvincibility() => isInvincible = false;

    public void TakeDamage(int rawDmg, Vector3 attackerPosition = default)
    {
        if (isDead) return;

        if (rawDmg >= 999999)
        {
            currentHP = 0;
            if (healthBar != null) healthBar.value = 0;
            Debug.Log("<color=red>[Archer]</color> โดนสั่งตายทันที (System Kill)!");
            Die();
            return;
        }

        if (isInvincible) { print("ไม่โดนเว้ย"); return; }
        if (animator != null) animator.SetTrigger("Damage");

        StopAiming();

        Vector3 knockbackDir = -transform.forward;
        if (attackerPosition != default)
        {
            knockbackDir = (transform.position - attackerPosition).normalized;
            knockbackDir.y = 0;
        }
        currentHitVelocity = knockbackDir * 4f;

        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.HitImpact);

        if (hitVFXPrefab != null)
        {
            Vector3 vfxPos = hitVFXSpawnPoint != null
                ? hitVFXSpawnPoint.position
                : transform.position + Vector3.up * 1.5f;
            GameObject vfx = Instantiate(hitVFXPrefab, vfxPos, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        int actual = Mathf.Max(1, Mathf.RoundToInt(rawDmg - Defense));
        currentHP = Mathf.Max(0, currentHP - actual);
        if (healthBar != null) healthBar.value = currentHP;

        Debug.Log($"<color=orange>[Archer]</color> {gameObject.name} took {actual} damage. HP: {currentHP}/{maxHP}");
        if (currentHP <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (healthBar != null) healthBar.value = currentHP;
        Debug.Log($"<color=lime>[Archer]</color> ฮีล +{amount} | HP:{currentHP}/{maxHP}");
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log($"<color=red>[Archer]</color> {gameObject.name} Die() called! IsOwner={GetComponent<NetworkObject>()?.IsOwner}, IsServer={NetworkManager.Singleton.IsServer}");
        if (animator != null) animator.SetTrigger("Die");

        SpectatorController.Instance?.EnterSpectate(transform);

        // แจ้งการตายไปยังส่วนกลาง (GameManager)
        if (GameManager.Instance != null && NetworkManager.Singleton != null)
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;
            Debug.Log($"<color=red>[Archer]</color> {gameObject.name} sending death notification for LocalClientId: {myId}");
            GameManager.Instance.NotifyPlayerDiedServerRpc(myId);
        }
        else
        {
            Debug.LogWarning($"<color=red>[Archer]</color> {gameObject.name} Failed to sync death: GameManager={GameManager.Instance}, NetMgr={NetworkManager.Singleton}");
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
            Gizmos.DrawSphere(groundCheck.position, groundDistance);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockRange);
    }
}