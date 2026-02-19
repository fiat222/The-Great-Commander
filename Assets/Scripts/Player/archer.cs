using UnityEngine;

public class Archer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float rotationSpeed = 13f;
    public float jumpHeight = 3f;
    public float gravity = -19.62f;

    [Header("Aim Settings")]
    public float aimMoveSpeed = 5f;
    public float aimRotationSpeed = 8f;

    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public AimCrosshair crosshair;
    public float spreadMax = 15f;
    public float spreadMin = 0.5f;

    [Header("Arrow Power Settings")]
    public float minArrowSpeed = 15f;
    public float maxArrowSpeed = 40f;
    [Range(1, 5)]
    public int minDamage = 1;
    [Range(1, 5)]
    public int maxDamage = 5;
    public float maxSpreadAngle = 10f;

    [Header("Roll Settings")]
    public float rollForce = 20f;
    public float rollSpeed = 15f;
    public float rollDecay = 8f;
    public float dashDecay = 10f;
    [Range(0, 1)] public float forceTime = 0.2f;

    [Header("References")]
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    // --- Internal State ---
    private Vector3 currentDashVelocity;
    private Vector3 rollDirection;
    private float rotationVelocity;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private Coroutine rotationCoroutine;

    // Aim / Shoot State
    private bool isAiming = false;
    private bool hasFiredThisAim = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleRollInput();
        HandleAimAndShootInput();
        Move();
    }

    // ==================== ROLL ====================

    private void HandleRollInput()
    {
        bool isBusy = animator != null &&
                      (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") ||
                       animator.GetCurrentAnimatorStateInfo(0).IsTag("Roll") ||
                       animator.GetNextAnimatorStateInfo(0).IsTag("Attack") ||
                       animator.GetNextAnimatorStateInfo(0).IsTag("Roll") ||
                       isAiming);

        if (Input.GetKeyDown(KeyCode.LeftShift) && isGrounded && !isBusy)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg +
                                    (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                rollDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
                rotationCoroutine = StartCoroutine(SmoothRotate(targetAngle));
            }
            else
            {
                rollDirection = transform.forward;
            }

            currentDashVelocity = rollDirection * (rollForce * 0.5f);

            if (animator != null)
                animator.SetTrigger("Roll");
        }
    }

    // ==================== AIM & SHOOT ====================

    private void HandleAimAndShootInput()
    {
        // กดค้าง Left Mouse → เล็ง
        if (Input.GetMouseButtonDown(0))
        {
            bool isBusy = animator != null &&
                          (animator.GetCurrentAnimatorStateInfo(0).IsTag("Roll") ||
                           animator.GetNextAnimatorStateInfo(0).IsTag("Roll"));
            if (!isBusy)
            {
                StartAiming();
            }
        }

        // ปล่อย Left Mouse → ยิง
        if (Input.GetMouseButtonUp(0))
        {
            if (isAiming && !hasFiredThisAim)
            {
                Shoot();
            }
            StopAiming();
        }

        // อัปเดต BlendTree ตอนเล็ง (Forward/Right อ้างอิง camera-relative input)
        if (isAiming)
        {
            UpdateAimBlendTree();
            FaceCamera(); // หันหน้าตามกล้องขณะเล็ง
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        hasFiredThisAim = false;

        if (animator != null)
        {
            animator.SetBool("isAiming", true);
            animator.SetTrigger("DrawArrow");
        }

        if (crosshair != null)
            crosshair.StartAim();
    }

    private void StopAiming()
    {
        isAiming = false;

        if (animator != null)
            animator.SetBool("isAiming", false);

        if (crosshair != null)
            crosshair.StopAim();
    }

    private void Shoot()
    {
        hasFiredThisAim = true;

        if (animator != null)
            animator.SetTrigger("Shoot");

        // ลูกธนูจะถูก spawn ผ่าน Animation Event ใน clip Shoot แทน
    }

    // เรียกจาก Animation Event ใน clip Shoot
    public void SpawnArrow()
    {
        if (arrowPrefab == null || arrowSpawnPoint == null) return;

        float accuracy = crosshair != null ? crosshair.GetAccuracy() : 1f;

        // 🎯 Spread
        float spreadAngle = Mathf.Lerp(maxSpreadAngle, 0f, accuracy);

        Vector3 baseDirection = mainCameraTransform != null
            ? mainCameraTransform.forward
            : transform.forward;

        baseDirection.Normalize();

        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        );

        Vector3 finalDirection = spreadRotation * baseDirection;

        // ⚡ Speed
        float finalSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, accuracy);

        // 💥 Damage
        int finalDamage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, accuracy));

        GameObject arrow = Instantiate(
            arrowPrefab,
            arrowSpawnPoint.position,
            Quaternion.LookRotation(finalDirection)
        );

        arrow.GetComponent<ArrowProjectile>()?.Launch(finalDirection, finalSpeed, finalDamage);
    }

    /// <summary>
    /// อัปเดต Forward / Right Parameters สำหรับ BlendTree ตอนเล็ง
    /// โดยคำนวณทิศ input เทียบกับทิศที่ตัวละครหัน (camera-relative → character-relative)
    /// </summary>
    private void UpdateAimBlendTree()
    {
        if (animator == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // แปลง input ตาม camera โลก แล้วค่อย inverse transform เทียบกับตัวละคร
        Vector3 worldMove = mainCameraTransform != null
            ? Quaternion.Euler(0, mainCameraTransform.eulerAngles.y, 0) * new Vector3(horizontal, 0f, vertical)
            : new Vector3(horizontal, 0f, vertical);

        // ใช้ InverseTransformDirection เพื่อให้ได้ทิศเทียบกับ facing ของตัวละครจริงๆ
        Vector3 localMove = transform.InverseTransformDirection(worldMove);

        animator.SetFloat("Right", localMove.x, 0.05f, Time.deltaTime);
        animator.SetFloat("Forward", localMove.z, 0.05f, Time.deltaTime);
    }

    /// <summary>
    /// ขณะเล็ง ตัวละครหันตามทิศกล้องเสมอ
    /// </summary>
    private void FaceCamera()
    {
        if (mainCameraTransform == null) return;

        float targetAngle = mainCameraTransform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, targetAngle,
            ref rotationVelocity, 1f / aimRotationSpeed);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    // ==================== UTILITIES ====================

    private System.Collections.IEnumerator SmoothRotate(float targetAngle)
    {
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, 720f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    // ==================== MOVE ====================

    private void Move()
    {
        var stateInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nextStateInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;

        bool isRolling = animator != null &&
                         (stateInfo.IsTag("Roll") || nextStateInfo.IsTag("Roll"));

        bool isLocked = isRolling; // Attack lock ถูกถอดออก (ธนูไม่มีคอมโบระยะประชิด)

        // Ground check
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && verticalVelocity.y < 0)
            verticalVelocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(h, 0f, v).normalized;

        // ความเร็วที่ใช้ (ปกติ vs เล็ง)
        float currentSpeed = isAiming ? aimMoveSpeed : moveSpeed;

        if (animator != null)
        {
            // FreelookSpeed ใช้สำหรับ blend idle/run ตอนไม่เล็ง
            if (!isAiming)
                animator.SetFloat("FreelookSpeed", direction.magnitude, 0.05f, Time.deltaTime);

            animator.SetBool("isGrounded", isGrounded);
        }

        // Dash velocity (Roll)
        if (currentDashVelocity.magnitude > 0.1f)
        {
            controller.Move(currentDashVelocity * Time.deltaTime);
            float decay = isRolling ? rollDecay : dashDecay;
            currentDashVelocity = Vector3.Lerp(currentDashVelocity, Vector3.zero, decay * Time.deltaTime);
        }

        // Roll movement (Elden Ring style)
        if (isRolling)
        {
            float normalizedTime = stateInfo.normalizedTime % 1f;
            if (normalizedTime < 0.75f)
                controller.Move(rollDirection * rollSpeed * Time.deltaTime);
        }

        // ==================== FREE MOVEMENT ====================
        if (!isLocked)
        {
            if (isAiming)
            {
                // ตอนเล็ง: เดินได้ทุกทิศ แต่ความเร็วช้าลง, หันตาม camera ผ่าน FaceCamera()
                if (direction.magnitude >= 0.1f)
                {
                    float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg +
                                        (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                    Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                    controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
                }
            }
            else
            {
                // ตอนปกติ: เดินหันตาม input
                if (direction.magnitude >= 0.1f)
                {
                    float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg +
                                        (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                    float angle = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y, targetAngle,
                        ref rotationVelocity, 1f / rotationSpeed);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);

                    Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                    controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
                }

                // Jump
                if (Input.GetButtonDown("Jump") && isGrounded)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (animator != null) animator.SetTrigger("Jump");
                }
            }
        }

        // Gravity
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}