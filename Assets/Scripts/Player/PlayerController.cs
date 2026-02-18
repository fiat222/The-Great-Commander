using UnityEngine;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float rotationSpeed = 13f;
    public float jumpHeight = 3f;
    public float gravity = -19.62f;

    [Header("Combo Settings")]
    public float comboResetTime = 3.0f; 
    public float attackDashForce = 5f; 
    public float dashDecay = 10f; 
    public float rollForce = 20f; 
    public float rollSpeed = 15f; // ความเร็วต่อเนื่องในขณะกลิ้ง (Elden Ring Style)
    public float rollDecay = 8f; 
    [Range(0, 1)] public float forceTime = 0.2f; 
    [Range(0, 1)] public float comboWindowTime = 0.5f; 
    [Range(0, 1)] public float finisherWindowTime = 0.85f; // หน้าต่างต่อคอมโบของท่าสุดท้าย (ต้องรอนานกว่าปกติเพื่อให้ดูหนักแน่น)
    
    private int comboStep = 0;
    private float lastClickTime;
    private Vector3 currentDashVelocity;
    private Vector3 rollDirection; // เก็บพิกัดที่จะกลิ้งไป
    private bool alreadyAppliedForce; 
    private bool bufferCombo; 

    [Header("Target Lock Settings")]
    public float lockRange = 15f;
    // targetCamera removed, now handled by CameraManager
    public CinemachineTargetGroup targetGroup; // (Optional) ลาก Target Group มาใส่ถ้าอยากได้สไตล์ Monster Hunter
    public Transform cameraPivot; // ลาก CamaraFocus มาใส่ที่นี่ครับ
    private Transform currentTarget;
    private bool isLockedOn;

    [Header("References")]
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    private float rotationVelocity;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private Coroutine rotationCoroutine; // สำหรับคุมการหมุนนุ่มๆ

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleTargetLockInput(); // เช็คปุ่มล็อคเป้า (เมาส์กลาง)
        HandleRollInput(); 
        HandleAttackInput();
        CheckAnimationLogic(); 
        Move();
    }

    private void LateUpdate()
    {
        // --- ระบบจัดระเบียบกล้อง (หมุน Pivot ให้ตรงแนว Enemy -> Player เสมอ) ---
        // เราทำใน LateUpdate เพื่อความชัวร์ว่าตัวละครขยับเสร็จแล้วค่อยหันกล้องครับ
        if (isLockedOn && currentTarget != null && cameraPivot != null)
        {
            Vector3 lookDir = currentTarget.position - cameraPivot.position;
            lookDir.y = 0;
            if (lookDir.magnitude > 0.1f)
            {
                // ใช้การหมุนแบบ World Space เพื่อไม่ให้สนว่า Player จะหันหน้าไปทางไหนครับ
                cameraPivot.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    private void HandleTargetLockInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) // เปลี่ยนจากเมาส์กลางเป็นปุ่ม Tab ครับ
        {
            if (isLockedOn)
            {
                UnlockTarget();
            }
            else
            {
                FindNearestTarget();
            }
        }

        // หลุดล็อคถ้าเป้าหมายตายหรืออยู่ไกลเกินไป
        if (isLockedOn && currentTarget == null) UnlockTarget();
        if (isLockedOn && currentTarget != null && Vector3.Distance(transform.position, currentTarget.position) > lockRange + 2f) UnlockTarget();
    }

    private void FindNearestTarget()
    {
        // ใช้ OverlapSphere เพื่อหาศัตรูในรัศมีรอบตัว (แม่นยำและประหยัดทรัพยากรกว่าครับ)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockRange);
        float closestDistance = lockRange;
        Transform closestEnemy = null;

        foreach (var hitCollider in hitColliders)
        {
            // เช็คว่ามี Tag เป็น Enemy ไหม
            if (hitCollider.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestEnemy = hitCollider.transform;
                }
            }
        }

        if (closestEnemy != null)
        {
            currentTarget = closestEnemy;
            isLockedOn = true;

            // --- จัดการกล้องผ่าน CameraManager ---
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetTargetLock(true);
                var vcam = CameraManager.Instance.TargetLockCamera;

                // --- สไตล์ Monster Hunter (ใช้ Target Group) ---
                if (targetGroup != null)
                {
                    // ล้างเป้าหมายเดิมออกก่อน (ถ้ามีเหลือติดมา)
                    while (targetGroup.Targets.Count > 1) 
                        targetGroup.RemoveMember(targetGroup.Targets[1].Object);
                    
                    // เพิ่มศัตรูเข้ากลุ่ม 
                    targetGroup.AddMember(currentTarget, 1f, 0f);
                    if (vcam != null) vcam.LookAt = targetGroup.transform;
                }
                else
                {
                    // แบบ Elden Ring (มองศัตรูตรงๆ)
                    if (vcam != null) vcam.LookAt = currentTarget;
                }
            }
        }
    }

    public void UnlockTarget()
    {
        isLockedOn = false;

        // เอาศัตรูออกจาก Target Group
        if (targetGroup != null && currentTarget != null)
        {
            targetGroup.RemoveMember(currentTarget);
        }

        currentTarget = null;
        if (CameraManager.Instance != null) CameraManager.Instance.SetTargetLock(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockRange);
    }

    private void HandleRollInput()
    {
        // เช็คว่ากำลังยุ่งอยู่ไหม (ฟันอยู่ หรือกำลังกลิ้งอยู่)
        bool isBusy = animator != null && (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || 
                                          animator.GetCurrentAnimatorStateInfo(0).IsTag("Roll") ||
                                          animator.GetNextAnimatorStateInfo(0).IsTag("Attack") || 
                                          animator.GetNextAnimatorStateInfo(0).IsTag("Roll"));

        // กดกลิ้งได้เฉพาะเฟสต่อสู้เท่านั้นครับ
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        if (Input.GetKeyDown(KeyCode.LeftShift) && isGrounded && !isBusy)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                rollDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                
                // หันให้นุ่มนวลเหมือนตอนฟันครับ
                if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
                rotationCoroutine = StartCoroutine(SmoothRotate(targetAngle));
            }
            else
            {
                rollDirection = transform.forward;
            }

            // ในระบบ Elden Ring เราจะไม่ใช้แรงส่งตูมเดียว แต่จะเคลื่อนที่ใน Move() ตลอดแอนิเมชัน
            // แต่ผมยังคงใส่ DashVelocity นิดหน่อยตอนเริ่มเพื่อให้มันดู "กระชาก" ตอนออกตัวครับ
            currentDashVelocity = rollDirection * (rollForce * 0.5f);

            if (animator != null)
            {
                animator.SetTrigger("Roll");
            }
        }
    }

    private void HandleAttackInput()
    {
        // โจมตีได้เฉพาะเฟสต่อสู้เท่านั้นครับ
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        if (Input.GetMouseButtonDown(0))
        {
            lastClickTime = Time.time;
            
            // ถ้าไม่ได้ฟันอยู่ ให้เริ่มฟันทันที
            if (comboStep == 0)
            {
                TriggerAttack();
            }
            else
            {
                // ถ้ากำลังฟันอยู่ ให้ "จดจำ" (Buffer) ไว้ว่าผู้เล่นอยากต่อคอมโบ
                bufferCombo = true;
            }
        }

        // รีเซ็ตคอมโบถ้าทิ้งช่วงนานเกินไป
        if (comboStep > 0 && Time.time - lastClickTime > comboResetTime)
        {
            ResetCombo();
        }
    }

    private void TriggerAttack()
    {
        // --- เพิ่มระบบหันไปตามทิศที่กดเดิน (เหมือนตอนกลิ้ง) ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // หาความชันที่อ้างอิงจากมุมกล้อง
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
            
            // เปลี่ยนจากหันทันที เป็นการสั่ง Coroutine ให้ค่อยๆ หมุน (แต่หมุนไวมาก) เพื่อไม่ให้ดูเหมือนวาร์ปครับ
            if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
            rotationCoroutine = StartCoroutine(SmoothRotate(targetAngle));
        }

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
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
        float duration = 0.1f; // หมุนให้เสร็จใน 0.1 วินาที (ไวแต่ไม่วาร์ป)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // ใช้ RotateTowards เพื่อให้การหมุนมีความเร็วสม่ำเสมอและนุ่มนวล
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    private void CheckAnimationLogic()
    {
        if (animator == null) return;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        var nextStateInfo = animator.GetNextAnimatorStateInfo(0);
        
        // เช็ค Tag ทั้ง Attack และ Roll เพื่อล็อคการเดิน
        bool isLockedState = stateInfo.IsTag("Attack") || nextStateInfo.IsTag("Attack") || 
                             stateInfo.IsTag("Roll") || nextStateInfo.IsTag("Roll");

        // ถ้าเราเพิ่งกด Trigger ไป แอนิเมเตอร์อาจจะยังไม่เปลี่ยนสถานะในหลักมิลลิวินาทีแรก
        bool justTriggered = Time.time - lastClickTime < 0.2f;

        if (isLockedState || justTriggered)
        {
            float normalizedTime = stateInfo.normalizedTime % 1f;
            if (animator.IsInTransition(0) && (nextStateInfo.IsTag("Attack") || nextStateInfo.IsTag("Roll")))
            {
                normalizedTime = nextStateInfo.normalizedTime % 1f;
            }

            // --- เอาโค้ดพุ่งอัตโนมัติออกแล้วครับ เพื่อให้พี่ผูก Event ใน Animator เองได้ 100% ---

            if (normalizedTime >= forceTime && !alreadyAppliedForce && stateInfo.IsTag("Attack"))
            {
                PerformAttackDash();
                alreadyAppliedForce = true;
            }

            // จังหวะ Combo Window (เช็คว่ากดต่อคอมโบได้หรือยัง)
            // ทริค: ถ้าเป็นท่าที่ 3 (ท่าสุดท้าย) เราจะให้รอนานกว่าปกติ (finisherWindowTime) เพื่อไม่ให้หันหน้าวาร์ปครับ
            float currentWindow = (comboStep == 3) ? finisherWindowTime : comboWindowTime;

            if (bufferCombo && normalizedTime >= currentWindow)
            {
                TriggerAttack();
            }
        }
        else
        {
            // ถ้าไม่ได้อยู่ใน Attack และ "ไม่ได้กำลังเปลี่ยนท่า" และ "ไม่ได้เพิ่งกด" ถึงจะรีเซ็ต
            if (comboStep != 0 && !animator.IsInTransition(0))
            {
                if (!justTriggered)
                {
                    ResetCombo();
                }
            }
        }
    }

    private void ResetCombo()
    {
        comboStep = 0;
        bufferCombo = false;
        alreadyAppliedForce = false;
        if (animator != null) animator.SetInteger("ComboStep", 0);
    }

    public void PerformAttackDash()
    {
        currentDashVelocity = transform.forward * attackDashForce;
    }

    private void Move()
    {
        // ตรวจสอบว่ากำลังทำ Action ที่ต้องล็อคการเดินไหม (ฟัน/กลิ้ง)
        // ปรับปรุง: เช็คทั้งสถานะปัจจุบัน และสถานะถัดไป (Transition) เพื่อให้ล็อคแน่น 100% ครับ
        var stateInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nextStateInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
        
        bool isLocked = animator != null && (stateInfo.IsTag("Attack") || nextStateInfo.IsTag("Attack") || 
                                             stateInfo.IsTag("Roll") || nextStateInfo.IsTag("Roll"));

        // ระบบเช็คพื้นแบบใหม่โดยใช้ Empty Object (SphereCast)
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (animator != null)
        {
            animator.SetFloat("FreelookSpeed", direction.magnitude, 0.05f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
        }

        // ใช้แรงพุ่งสะสมเคลื่อนที่ตัวละคร
        if (currentDashVelocity.magnitude > 0.1f)
        {
            controller.Move(currentDashVelocity * Time.deltaTime);
            float decay = isLocked && stateInfo.IsTag("Roll") ? rollDecay : dashDecay;
            currentDashVelocity = Vector3.Lerp(currentDashVelocity, Vector3.zero, decay * Time.deltaTime);
        }

        // --- ระบบเคลื่อนที่ต่อเนื่องขณะกลิ้ง (Elden Ring Style) ---
        if (isLocked && stateInfo.IsTag("Roll"))
        {
            float normalizedTime = stateInfo.normalizedTime % 1f;
            // กลิ้งไปข้างหน้าเฉพาะช่วงเวลาที่ตัวละครกำลังม้วนตัว (0% - 75% ของแอนิเมชัน)
            if (normalizedTime < 0.75f)
            {
                controller.Move(rollDirection * rollSpeed * Time.deltaTime);
            }
        }

        // เคลื่อนที่และหมุนตัว (ถ้าไม่ได้โดนล็อค Animation อยู่)
        if (!isLocked)
        {
            if (direction.magnitude >= 0.1f)
            {
                // คำนวณหาทิศทางที่จะไปอ้างอิงจากมุมกล้อง
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
                
                // หันหน้าตัวละครไปตามทิศที่จะเดิน (ไม่ว่าจะเป็น Lock-on หรือไม่ เพื่อให้เป็นสไตล์ Monster Hunter ครับ)
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, 1f / rotationSpeed);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
            }

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetTrigger("Jump");
            }
        }

        // แรงโน้มถ่วง (ทำงานตลอดเวลา)
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}