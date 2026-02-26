using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ImpAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Transform baseTransform;
    private Animator animator;

    [Header("Enemy Stats SO")]
    public EnemyStatsSO stats;   // 🔥 เปลี่ยนจาก MinionData เป็น EnemyStatsSO
    public Slider healthBar;

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.MoveToBase;
    public float detectionRange = 17f;
    public float chaseRange = 28f;
    public float attackRange = 12f;
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

    private float currentHP;
    private float currentDamage;
    private float currentDefense;
    private float distanceToPlayer;
    private float distanceToBase;
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
        if (isDead || playerTransform == null) return;

        distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (baseTransform != null)
            distanceToBase = Vector3.Distance(transform.position, baseTransform.position);

        HandleStateTransitions();
        UpdateAnimations();
    }

    // ==================== STATE MACHINE (เหมือนเดิม) ====================

    private void HandleStateTransitions()
    {
        if (baseTransform != null && distanceToBase <= baseAttackRange)
        {
            currentState = EnemyState.AttackBase;
            return;
        }

        switch (currentState)
        {
            case EnemyState.MoveToBase:
                if (distanceToPlayer <= detectionRange)
                    currentState = EnemyState.ChasePlayer;
                break;

            case EnemyState.ChasePlayer:
                if (distanceToPlayer > chaseRange)
                    currentState = EnemyState.MoveToBase;
                else if (distanceToPlayer <= attackRange)
                    currentState = EnemyState.AttackPlayer;
                break;

            case EnemyState.AttackPlayer:
                if (distanceToPlayer > attackRange)
                    currentState = EnemyState.ChasePlayer;
                break;

            case EnemyState.AttackBase:
                if (distanceToBase > baseAttackRange + 1f)
                    currentState = EnemyState.MoveToBase;
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
            case EnemyState.AttackBase:
                agent.isStopped = true;
                break;
        }
    }

    // ==================== PROJECTILE ====================

    public void SpawnProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 targetPos =
            currentState == EnemyState.AttackBase && baseTransform != null
            ? baseTransform.position + Vector3.up
            : playerTransform.position + Vector3.up;

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

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");

        if (countsInWaveUI)
            GameManager.OnSystemEnemyDied?.Invoke(typeIndex);

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        PowerBallDropper.Drop(transform.position, powerBallDropAmount);

        Destroy(gameObject, 3f);
    }
}