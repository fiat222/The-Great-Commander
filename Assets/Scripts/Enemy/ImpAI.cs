using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ImpAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Transform baseTransform;
    private Transform minionTransform;
    private Animator animator;

    [Header("Enemy Stats SO")]
    public EnemyStatsSO stats;   // 🔥 เปลี่ยนจาก MinionData เป็น EnemyStatsSO
    public Slider healthBar;

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.MoveToBase;
    public float detectionRange = 17f;
    public float chaseRange = 28f;
    public float attackRange = 15;
    public float baseAttackRange = 9f;

    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;

    [Header("AI Settings")]
    public float updateRate = 0.2f;
    public int typeIndex;
    public bool countsInWaveUI;

    [Header("PowerBall Drop")]
    public int powerBallDropAmount = 3;

    [Header("VFX Spawn Points")]
    public Transform hitVFXPoint;
    public Transform deathVFXPoint;
    public Transform removalVFXPoint;

    private float currentHP;
    private float currentDamage;
    private float currentDefense;
    private float distanceToPlayer;
    private float distanceToBase;
    private float distanceToMinion;
    private bool isDead = false;
    public bool IsDead => isDead;
    private Vector3 pendingTargetPos;

    // ==================== EVENT SUBSCRIBE ====================

    void OnEnable()
    {
        EnemyStatsSO.OnEnemyWaveScaled += OnWaveScaled;
    }

    void OnDisable()
    {
        EnemyStatsSO.OnEnemyWaveScaled -= OnWaveScaled;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null) baseTransform = baseObj.transform;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        ApplyStatsFromSO();

        if (healthBar != null)
        {
            healthBar.maxValue = currentHP;
            healthBar.value = currentHP;
        }

        // ⭐ ถ้ามี HealthSystem ให้เชื่อม OnDie → Die()
        HealthSystem hs = GetComponent<HealthSystem>();
        if (hs != null) hs.OnDie.AddListener(Die);

        InvokeRepeating(nameof(UpdateDestination), 0f, updateRate);
    }

    // ==================== APPLY STATS ====================

    private void ApplyStatsFromSO()
    {
        if (stats == null)
        {
            Debug.LogWarning("[ImpAI] ไม่มี EnemyStatsSO!");
            currentHP = 80f;
            currentDamage = 10f;
            currentDefense = 0f;
            return;
        }

        currentHP = stats.GetHP();
        currentDamage = stats.GetDamage();
        currentDefense = stats.GetDefense();

        walkSpeed = stats.GetSpeed();
        runSpeed = walkSpeed * 1.5f;
        attackRange = stats.attackRange;

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.acceleration = 60f;
            agent.angularSpeed = 600f;
            // ให้มันหยุดเดินก่อนถึงเป้าหมายเล็กน้อย (ตามระยะโจมตี) จะได้ไม่เอาหน้าไปแนบ
            agent.stoppingDistance = Mathf.Max(2f, attackRange - 2f);
        }
    }

    private void OnWaveScaled(EnemyStatsSO changedSO)
    {
        if (changedSO != stats) return;

        float hpPercent = currentHP / healthBar.maxValue;

        ApplyStatsFromSO();

        currentHP = stats.GetHP() * hpPercent;

        if (healthBar != null)
        {
            healthBar.maxValue = stats.GetHP();
            healthBar.value = currentHP;
        }
    }

    // ==================== UPDATE ====================

    void Update()
    {
        if (isDead) return;

        UpdateTargetsAndDistances();
        HandleStateTransitions();
        HandleMovementAndRotation();
        UpdateAnimations();
    }

    private void HandleMovementAndRotation()
    {
        if (agent == null || !agent.enabled) return;

        bool isAttackingState = currentState == EnemyState.AttackPlayer ||
                                currentState == EnemyState.AttackBase ||
                                currentState == EnemyState.AttackMinion;

        // เช็คว่าแอนิเมชั่นโจมตีกำลังเล่นอยู่จริงๆ ไหม
        bool isAnimatorFiring = animator != null && (animator.GetCurrentAnimatorStateInfo(0).IsTag("Atk") || 
                                                     animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"));

        // ถ้าอยู่ในสถานะโจมตี
        if (isAttackingState)
        {
            // หยุดเดินเฉพาะตอนที่กำลัง "วาดลวดลาย" โจมตีจริงๆ เท่านั้น (กันสไลด์)
            // ถ้าเป็นช่วงรอคูลดาวน์ ให้ Agent เดินเข้าหา stoppingDistance ได้
            agent.isStopped = isAnimatorFiring;
            
            if (isAnimatorFiring) agent.velocity = Vector3.zero;

            agent.updateRotation = false;
            RotateTowardsTarget();
        }
        else
        {
            // ถ้าไล่ล่า/เดินไปฐาน
            agent.isStopped = false;
            agent.updateRotation = true;
        }
    }

    private void RotateTowardsTarget()
    {
        Transform target = null;
        if (currentState == EnemyState.AttackBase) target = baseTransform;
        else if (currentState == EnemyState.AttackMinion) target = minionTransform;
        else target = playerTransform;

        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // ล็อกแกน Y ไม่ให้ตัวละครเงยหน้า/ก้มหน้า
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    private void UpdateTargetsAndDistances()
    {
        // อัปเดตระยะ Player
        if (playerTransform != null)
            distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // อัปเดตระยะ Base
        if (baseTransform != null)
            distanceToBase = Vector3.Distance(transform.position, baseTransform.position);

        // ค้นหา Minion ที่ใกล้ที่สุด
        if (minionTransform == null || !minionTransform.gameObject.activeInHierarchy)
        {
            FindClosestMinion();
        }

        if (minionTransform != null)
        {
            distanceToMinion = Vector3.Distance(transform.position, minionTransform.position);
            if (distanceToMinion > chaseRange) minionTransform = null;
        }
    }

    private void FindClosestMinion()
    {
        GameObject[] minions = GameObject.FindGameObjectsWithTag("Minion");
        float closestDist = detectionRange;
        Transform closestMinion = null;

        foreach (GameObject m in minions)
        {
            if (m == null) continue;
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestMinion = m.transform;
            }
        }
        minionTransform = closestMinion;
    }

    // ==================== STATE MACHINE (เหมือนเดิม) ====================

    private bool IsPlayerDead()
    {
        if (playerTransform == null) return true;
        var pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null) return pc.IsDead;
        var ar = playerTransform.GetComponent<Archer>();
        if (ar != null) return ar.IsDead;
        return false;
    }

    private void HandleStateTransitions()
    {
        bool playerDead = IsPlayerDead();

        // 1. ตรวจสอบการโจมตีฐานก่อน (ถ้าประชิดฐานแล้ว)
        if (baseTransform != null && distanceToBase <= baseAttackRange)
        {
            currentState = EnemyState.AttackBase;
            return;
        }

        // 2. จัดการสถานะตามเป้าหมาย (Priority: Minion > Player > Base)
        switch (currentState)
        {
            case EnemyState.AttackMinion:
                if (minionTransform == null) currentState = EnemyState.MoveToBase;
                else if (distanceToMinion > attackRange) currentState = EnemyState.ChaseMinion;
                break;

            case EnemyState.ChaseMinion:
                if (minionTransform == null) currentState = EnemyState.MoveToBase;
                else if (distanceToMinion <= attackRange) currentState = EnemyState.AttackMinion;
                break;

            case EnemyState.AttackPlayer:
                if (minionTransform != null) currentState = EnemyState.ChaseMinion;
                else if (playerDead || playerTransform == null || distanceToPlayer > attackRange) currentState = EnemyState.ChasePlayer;
                break;

            case EnemyState.ChasePlayer:
                if (minionTransform != null) currentState = EnemyState.ChaseMinion;
                else if (playerDead || playerTransform == null || distanceToPlayer > chaseRange) currentState = EnemyState.MoveToBase;
                else if (distanceToPlayer <= attackRange) currentState = EnemyState.AttackPlayer;
                break;

            case EnemyState.AttackBase:
                if (minionTransform != null) currentState = EnemyState.ChaseMinion;
                else if (!playerDead && distanceToPlayer <= detectionRange) currentState = EnemyState.ChasePlayer;
                else if (baseTransform == null || distanceToBase > baseAttackRange + 1f) currentState = EnemyState.MoveToBase;
                break;

            case EnemyState.MoveToBase:
                if (minionTransform != null) currentState = EnemyState.ChaseMinion;
                else if (!playerDead && playerTransform != null && distanceToPlayer <= detectionRange) currentState = EnemyState.ChasePlayer;
                break;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", agent != null ? agent.velocity.magnitude / walkSpeed : 0f);

        bool isAttacking = currentState == EnemyState.AttackPlayer ||
                           currentState == EnemyState.AttackBase ||
                           currentState == EnemyState.AttackMinion;
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

            case EnemyState.ChaseMinion:
                if (minionTransform != null)
                {
                    agent.isStopped = false;
                    agent.speed = runSpeed;
                    agent.SetDestination(minionTransform.position);
                }
                break;

            case EnemyState.AttackPlayer:
                if (playerTransform != null)
                {
                    agent.SetDestination(playerTransform.position);
                }
                break;

            case EnemyState.AttackMinion:
                if (minionTransform != null)
                {
                    agent.SetDestination(minionTransform.position);
                }
                break;

            case EnemyState.AttackBase:
                if (baseTransform != null)
                {
                    agent.SetDestination(baseTransform.position);
                }
                break;
        }
    }

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

    // ==================== PROJECTILE ====================

    public void SpawnProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Transform target = null;
        if (currentState == EnemyState.AttackBase) target = baseTransform;
        else if (currentState == EnemyState.AttackMinion) target = minionTransform;
        else target = playerTransform;

        if (target == null) return;

        Vector3 targetPos = target.position + Vector3.up;

        Vector3 direction = (targetPos - firePoint.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position,
                                      Quaternion.LookRotation(direction));

        proj.GetComponent<ImpProjectile>()?.Launch(direction, projectileSpeed, (int)currentDamage);
    }

    // ==================== DAMAGE ====================

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        float actualDamage = Mathf.Max(1f, dmg - currentDefense);

        currentHP -= actualDamage;
        currentHP = Mathf.Max(currentHP, 0);

        if (healthBar != null)
            healthBar.value = currentHP;

        if (animator != null)
            animator.SetTrigger("Damage");

        Vector3 vfxPos = hitVFXPoint != null ? hitVFXPoint.position : transform.position;
        VFXManager.Instance?.Play(stats?.hitVFX, vfxPos);

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) 
        {
            animator.applyRootMotion = false;
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsAttacking", false);
            animator.SetTrigger("Die");
        }

        if (countsInWaveUI)
            GameManager.OnSystemEnemyDied?.Invoke(typeIndex);

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 vfxPos = deathVFXPoint != null ? deathVFXPoint.position : transform.position;
        VFXManager.Instance?.Play(stats?.deathVFX, vfxPos);
        PowerBallDropper.Drop(transform.position, powerBallDropAmount);

        Invoke(nameof(PlayRemovalVFX), 2f);
        Destroy(gameObject, 3f);
    }

    private void PlayRemovalVFX()
    {
        Vector3 vfxPos = removalVFXPoint != null ? removalVFXPoint.position : 
                         (hitVFXPoint != null ? hitVFXPoint.position : transform.position);
        VFXManager.Instance?.Play(stats?.removalVFX, vfxPos);
    }
}