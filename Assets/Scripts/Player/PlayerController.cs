using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 9f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 4f;
    public float gravity = -19.62f;

    [Header("Combo Settings")]
    public float comboResetTime = 3.0f; 
    public float attackDashForce = 10f; 
    public float dashDecay = 10f; 
    [Range(0, 1)] public float forceTime = 0.2f; // จะพุ่งตอนเฟรมที่เท่าไหร่ (0-1)
    [Range(0, 1)] public float comboWindowTime = 0.5f; // จะเริ่มกดคอมโบต่อได้ตอนเฟรมที่เท่าไหร่
    
    private int comboStep = 0;
    private float lastClickTime;
    private Vector3 currentDashVelocity;
    private bool alreadyAppliedForce; // กันไม่ให้พุ่งซ้ำในท่าเดียว
    private bool bufferCombo; // เก็บค่าว่าผู้เล่นกดคลิกค้างไว้เพื่อต่อคอมโบไหม

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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleAttackInput();
        CheckAnimationLogic(); // ใช้หลักการที่พี่ส่งมาคุมจังหวะ
        Move();
    }

    private void HandleAttackInput()
    {
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

    private void CheckAnimationLogic()
    {
        if (animator == null) return;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        var nextStateInfo = animator.GetNextAnimatorStateInfo(0);
        
        // เช็คว่าอยู่ในสถานะ Attack หรือกำลังจะไป Attack
        bool inAttack = stateInfo.IsTag("Attack") || nextStateInfo.IsTag("Attack");

        // ถ้าเราเพิ่งกด Trigger ไป แอนิเมเตอร์อาจจะยังไม่เปลี่ยนสถานะในหลักมิลลิวินาทีแรก
        bool justTriggered = Time.time - lastClickTime < 0.2f;

        if (inAttack || justTriggered)
        {
            float normalizedTime = stateInfo.normalizedTime % 1f;
            if (animator.IsInTransition(0) && nextStateInfo.IsTag("Attack"))
            {
                normalizedTime = nextStateInfo.normalizedTime % 1f;
            }

            // --- เอาโค้ดพุ่งอัตโนมัติออกแล้วครับ เพื่อให้พี่ผูก Event ใน Animator เองได้ 100% ---

            // จังหวะ Combo Window (เช็คว่ากดต่อคอมโบได้หรือยัง)
            if (bufferCombo && normalizedTime >= comboWindowTime)
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
        // ตรวจว่ากำลังพัง (ฟัน) อยู่ไหม โดยเช็ค Tag ใน Animator
        bool isAttacking = animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

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

        // ใช้แรงพุ่งสะสมเคลื่อนที่ตัวละคร (ทำงานจากแรงที่ PerformAttackDash ส่งมา)
        if (currentDashVelocity.magnitude > 0.1f)
        {
            controller.Move(currentDashVelocity * Time.deltaTime);
            currentDashVelocity = Vector3.Slerp(currentDashVelocity, Vector3.zero, dashDecay * Time.deltaTime);
        }

        // ถ้าไม่ได้ฟันอยู่ ถึงจะเดินและกระโดดได้
        if (!isAttacking)
        {
            if (direction.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + (mainCameraTransform != null ? mainCameraTransform.eulerAngles.y : 0);
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