using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { MoveToBase, ChasePlayer, AttackPlayer, AttackBase }

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Transform baseTransform;
    private Animator animator;

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.MoveToBase;
    public float detectionRange = 17f; // ระยะตรวจเจอครั้งแรก
    public float chaseRange = 28f;     // ระยะหลุด (เพิ่มขึ้นตอนไล่ล่า)
    public float attackRange = 5f;      // ระยะโจมตี
    public float baseAttackRange = 7f;  // ระยะโจมตีป้อม

    [Header("Movement Settings")]
    public float walkSpeed = 4.5f;     // ความเร็วตอนเดินหาป้อม
    public float runSpeed = 7.5f;      // ความเร็วตอนไล่กวดผู้เล่น

    [Header("Combat Settings")]
    public Collider weaponCollider;    // ลาก Collider ของหอกมาใส่ตรงนี้ครับ

    [Header("AI Settings")]
    public float updateRate = 0.2f;
    public int typeIndex;           // ⭐ เพิ่มเพื่อระบุว่าเป็นตัวไหน
    public bool countsInWaveUI;      // ⭐ เพิ่มเพื่อระบุว่าเป็นมอนสเตอร์ระบบที่ต้องนับใน UI
    private float distanceToPlayer;
    private float distanceToBase;
    private bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // หา Base ด้วย Tag
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null) baseTransform = baseObj.transform;

        // หาผู้เล่นด้วย Tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // ปิด Collider ไว้ก่อนตอนเริ่มเกมครับ
        DisableWeaponCollider();

        InvokeRepeating(nameof(UpdateDestination), 0f, updateRate);
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (baseTransform != null)
            distanceToBase = Vector3.Distance(transform.position, baseTransform.position);
        
        HandleStateTransitions();
        UpdateAnimations();
    }

    private void HandleStateTransitions()
    {
        // --- กฎเหล็ก: ถ้าถึงป้อมแล้ว ต้องตีป้อมอย่างเดียว ไม่สนเพลเยอร์ครับ ---
        if (baseTransform != null && distanceToBase <= baseAttackRange)
        {
            if (currentState != EnemyState.AttackBase)
            {
                currentState = EnemyState.AttackBase;
                Debug.Log("<color=red>[Enemy]</color> Base reached! Ignoring player and attacking base...");
            }
            return; // จบตรงนี้เลย ไม่เช็ค Player ต่อครับ
        }

        switch (currentState)
        {
            case EnemyState.MoveToBase:
                // ถ้า Player เข้ามาในระยะ Detect ให้เปลี่ยนเป็น Chase
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = EnemyState.ChasePlayer;
                    Debug.Log("<color=orange>[Enemy]</color> Player detected! Chasing...");
                }
                break;

            case EnemyState.ChasePlayer:
                // ถ้า Player หนีพ้นระยะ Chase ให้กลับไปหา Base
                if (distanceToPlayer > chaseRange)
                {
                    currentState = EnemyState.MoveToBase;
                    Debug.Log("<color=yellow>[Enemy]</color> Player lost! Returning to base...");
                }
                // ถ้าประชิดตัว ให้โจมตี
                else if (distanceToPlayer <= attackRange)
                {
                    currentState = EnemyState.AttackPlayer;
                }
                break;

            case EnemyState.AttackPlayer:
                // ถ้า Player ถอยห่างเกินระยะโจมตี ให้กลับไปไล่ล่า
                if (distanceToPlayer > attackRange)
                {
                    currentState = EnemyState.ChasePlayer;
                }
                break;
            
            case EnemyState.AttackBase:
                // ถ้าหลุดจากระยะป้อม (เช่น โดนแรงกระแทก) ให้กลับไปหาป้อมใหม่
                if (distanceToBase > baseAttackRange + 1f)
                {
                    currentState = EnemyState.MoveToBase;
                }
                break;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // ความเร็วเดิน
        animator.SetFloat("Speed", agent != null ? agent.velocity.magnitude : 0f);
        
        // สถานะโจมตี (ใช้ท่าโจมตีเดียวกันทั้งตอนตี Player และตีป้อมครับ)
        bool isAttacking = currentState == EnemyState.AttackPlayer || currentState == EnemyState.AttackBase;
        animator.SetBool("IsAttacking", isAttacking);
    }

    void UpdateDestination()
    {
        if (isDead || agent == null || !agent.isOnNavMesh) return;

        switch (currentState)
        {
            case EnemyState.MoveToBase:
                if (baseTransform != null)
                {
                    agent.isStopped = false;
                    agent.speed = walkSpeed; // เดินชิลๆ ไปหาป้อม
                    agent.SetDestination(baseTransform.position);
                }
                break;

            case EnemyState.ChasePlayer:
                if (playerTransform != null)
                {
                    agent.isStopped = false;
                    agent.speed = runSpeed; // รีบวิ่งไปหาผู้เล่น!
                    agent.SetDestination(playerTransform.position);
                }
                break;

            case EnemyState.AttackPlayer:
                // หยุดเดินขณะโจมตี
                agent.isStopped = true;
                if (playerTransform != null)
                {
                    // หันหน้าหาผู้เล่น
                    Vector3 lookPos = playerTransform.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);
                }
                break;

            case EnemyState.AttackBase:
                // หยุดเดินขณะโจมตีป้อม
                agent.isStopped = true;
                if (baseTransform != null)
                {
                    // หันหน้าหาป้อม
                    Vector3 lookPos = baseTransform.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);
                }
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วาดระยะต่างๆ เพื่อให้เช็คใน Scene ง่ายๆ ครับ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, baseAttackRange);
    }

    // ==================== ANIMATION EVENTS ====================

    // เรียกใช้จาก Animator Event ตอนเริ่มจ้วงหอกครับ
    public void EnableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = true;
    }

    // เรียกใช้จาก Animator Event ตอนจบดึงหอกกลับครับ
    public void DisableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider target)
    {
        if (isDead) return;

        if (target.CompareTag("Weapon"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("<color=red>[Enemy]</color> <b>Die!!</b>");

        // 1. เล่นอนิเมชันตาย
        if (animator != null) animator.SetTrigger("Die");

        // --- 🌊 แจ้งระบบ UI ว่ามอนสเตอร์รายทางตัวนี้ตายแล้ว ---
        if (countsInWaveUI)
        {
            GameManager.OnSystemEnemyDied?.Invoke(typeIndex);
        }

        // 2. หยุดเดินและปิด AI
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 3. ปิด Collider เพื่อไม่ให้ขวางทาง
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;
        
        // ปิดอาวุธด้วยครับ
        DisableWeaponCollider();

        // 4. ทำลายทิ้งหลังจากเวลาผ่านไป (เช่น 5 วินาที) 
        // หรือจะใช้ CleanupEnemy() ผูกกับ Animator Event ท้ายท่าตายก็ได้ครับ
        Destroy(gameObject, 3f);
    }
}
