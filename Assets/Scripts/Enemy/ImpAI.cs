using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ImpAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Transform baseTransform;
    private Animator animator;

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.MoveToBase;
    public float detectionRange = 17f; // ระยะตรวจเจอ Player ครั้งแรก
    public float chaseRange = 28f; // ระยะหลุด (ตอนไล่ล่า)
    public float attackRange = 12f; // ระยะโจมตี (ไกลกว่า melee)
    public float baseAttackRange = 9f;  // ระยะโจมตีป้อม

    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab; // ลาก Prefab กระสุน (ImpProjectile) มาใส่
    public Transform firePoint;         // ลาก Transform ที่มือ Imp มาใส่
    public float projectileSpeed = 20f; // ความเร็วกระสุน

    [Header("Health Settings")]
    public MinionData data;    // ลาก MinionData ScriptableObject มาใส่
    public Slider healthBar;   // ลาก UI Slider มาใส่

    [Header("AI Settings")]
    public float updateRate = 0.2f;
    public int typeIndex;
    public bool countsInWaveUI;

    [Header("PowerBall Drop")]
    [Tooltip("จำนวน PowerBall ที่ drop เมื่อตาย (0 = ไม่ drop)")]
    public int powerBallDropAmount = 3;

    private float distanceToPlayer;
    private float distanceToBase;
    private bool isDead = false;
    private float currentHP;
    private Vector3 pendingTargetPos;

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

        // ตั้งค่า HP จาก MinionData
        if (data != null)
        {
            currentHP = data.hp;
            agent.speed = walkSpeed;
        }
        else
        {
            currentHP = 80f; // ค่า default ถ้าไม่มี MinionData
        }

        if (healthBar != null)
        {
            healthBar.maxValue = currentHP;
            healthBar.value = currentHP;
        }

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

    // ==================== STATE MACHINE ====================

    private void HandleStateTransitions()
    {
        // --- กฎเหล็ก: ถ้าถึงป้อมแล้ว ต้องตีป้อมอย่างเดียว ไม่สนเพลเยอร์ ---
        if (baseTransform != null && distanceToBase <= baseAttackRange)
        {
            if (currentState != EnemyState.AttackBase)
            {
                currentState = EnemyState.AttackBase;
                Debug.Log("<color=red>[ImpAI]</color> Base reached! Ignoring player and attacking base...");
            }
            return;
        }

        switch (currentState)
        {
            case EnemyState.MoveToBase:
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = EnemyState.ChasePlayer;
                    Debug.Log("<color=orange>[ImpAI]</color> Player detected! Chasing...");
                }
                break;

            case EnemyState.ChasePlayer:
                if (distanceToPlayer > chaseRange)
                {
                    currentState = EnemyState.MoveToBase;
                    Debug.Log("<color=yellow>[ImpAI]</color> Player lost! Returning to base...");
                }
                else if (distanceToPlayer <= attackRange)
                {
                    currentState = EnemyState.AttackPlayer;
                }
                break;

            case EnemyState.AttackPlayer:
                if (distanceToPlayer > attackRange)
                {
                    currentState = EnemyState.ChasePlayer;
                }
                break;

            case EnemyState.AttackBase:
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

        animator.SetFloat("Speed", agent != null ? agent.velocity.magnitude : 0f);

        bool isAttacking = currentState == EnemyState.AttackPlayer ||
                           currentState == EnemyState.AttackBase;
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
                    agent.speed = walkSpeed;
                    agent.SetDestination(baseTransform.position);
                }
                break;

            case EnemyState.ChasePlayer:
                if (playerTransform != null)
                {
                    agent.isStopped = false;
                    agent.speed = runSpeed;
                    agent.SetDestination(playerTransform.position);
                }
                break;

            case EnemyState.AttackPlayer:
                // หยุดเดินขณะโจมตี
                agent.isStopped = true;
                if (playerTransform != null)
                {
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
                    Vector3 lookPos = baseTransform.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);
                }
                break;
        }
    }

    // ==================== ANIMATION EVENTS ====================

    /// <summary>
    /// เรียกจาก Animator Event ตอนปล่อย Projectile (แทน EnableWeaponCollider ของ EnemyAI)
    /// </summary>
    public void SpawnProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;

        // เลือก target ตาม state ปัจจุบัน
        Vector3 targetPos;
        if (currentState == EnemyState.AttackBase && baseTransform != null)
            targetPos = baseTransform.position + Vector3.up * 1f;
        else if (playerTransform != null)
            targetPos = playerTransform.position + Vector3.up * 1f;
        else
            targetPos = pendingTargetPos;

        Vector3 direction = (targetPos - firePoint.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position,
                                      Quaternion.LookRotation(direction));

        int dmg = (int)((data != null) ? data.damage : 10f);
        proj.GetComponent<ImpProjectile>()?.Launch(direction, projectileSpeed, dmg);
    }

    // ==================== HEALTH ====================

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        // คำนวณ Damage หลังจาก Defense
        float defense = data != null ? data.defense : 0f;
        float actualDamage = Mathf.Max(1f, dmg - defense);

        currentHP -= actualDamage;
        currentHP = Mathf.Max(currentHP, 0);

        // อัพเดท Health Bar Slider
        if (healthBar != null)
            healthBar.value = currentHP;

        // เล่น Animation โดนตี
        if (animator != null)
            animator.SetTrigger("Damage");

        Debug.Log($"<color=orange>[ImpAI]</color> {gameObject.name} โดนตี {dmg} ดาเมจ (Defense: {defense}, Actual: {actualDamage}) | HP เหลือ: {currentHP}");

        if (currentHP <= 0)
            Die();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        // รับดาเมจจาก Tag "Weapon" และ "Minion" เท่านั้น
        if (other.CompareTag("Weapon") || other.CompareTag("Minion"))
        {
            TakeDamage(50);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("<color=red>[ImpAI]</color> <b>Die!!</b>");

        // 1. เล่นอนิเมชันตาย
        if (animator != null) animator.SetTrigger("Die");

        // 2. แจ้งระบบ UI ว่ามอนสเตอร์ตายแล้ว
        if (countsInWaveUI)
            GameManager.OnSystemEnemyDied?.Invoke(typeIndex);

        // 3. หยุดเดินและปิด AI
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 4. ปิด Collider เพื่อไม่ให้ขวางทาง
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 5. Drop PowerBall ณ ตำแหน่งที่ตาย
        PowerBallDropper.Drop(transform.position, powerBallDropAmount);

        // 6. ทำลายทิ้งหลังจาก 3 วินาที
        Destroy(gameObject, 3f);
    }

    // ==================== GIZMOS ====================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, baseAttackRange);
    }
}