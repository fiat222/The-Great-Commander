using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum EnemyState { MoveToBase, ChasePlayer, AttackPlayer, AttackBase, ChaseMinion, AttackMinion }

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Transform baseTransform;
    private Transform minionTransform;
    private Animator animator;

    [Header("Enemy Stats SO")]
    public EnemyStatsSO stats;   // 🔥 ใช้ตัวนี้แทน MinionData

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.MoveToBase;
    public float detectionRange = 17f;
    public float chaseRange = 28f;
    public float baseAttackRange = 7f;

    [Header("Combat")]
    public Collider weaponCollider;

    [Header("Health UI")]
    public Slider healthBar;

    [Header("AI Settings")]
    public float updateRate = 0.2f;
    public int typeIndex;
    public bool countsInWaveUI;

    [Header("PowerBall Drop")]
    public int powerBallDropAmount = 5;

    [Header("VFX Spawn Points")]
    public Transform hitVFXPoint;
    public Transform deathVFXPoint;
    public Transform removalVFXPoint;

    private float baseSpeed;
    private float attackRange;
    private float currentHP;
    private float currentDamage;
    private float currentDefense;

    private float distanceToPlayer;
    private float distanceToBase;
    private float distanceToMinion;
    private bool isDead = false;
    public bool IsDead => isDead;

    // ==================== UNITY ====================

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

        DisableWeaponCollider();
        GetComponent<CharacterAudio>()?.PlayRoar();

        // ⭐ ถ้ามี HealthSystem ให้เชื่อม OnDie → Die() เพื่อรับ kill จากป้อมด้วย
        HealthSystem hs = GetComponent<HealthSystem>();
        if (hs != null) hs.OnDie.AddListener(Die);

        InvokeRepeating(nameof(UpdateDestination), 0f, updateRate);
    }

    // ==================== APPLY STATS ====================

    private void ApplyStatsFromSO()
    {
        if (stats == null)
        {
            Debug.LogWarning("[EnemyAI] ไม่มี EnemyStatsSO!");
            return;
        }

        currentHP = stats.GetHP();
        currentDamage = stats.GetDamage();
        currentDefense = stats.GetDefense();
        baseSpeed = stats.GetSpeed();
        attackRange = stats.attackRange;

        if (agent != null)
        {
            agent.speed = baseSpeed;
            agent.acceleration = 60f;
            agent.angularSpeed = 600f;
            agent.stoppingDistance = 0f;
        }
    }

    private void OnWaveScaled(EnemyStatsSO changedSO)
    {
        if (changedSO != stats) return;

        float hpPercent = currentHP / healthBar.maxValue;

        ApplyStatsFromSO();

        // รักษา % HP เดิมไว้
        currentHP = stats.GetHP() * hpPercent;

        if (healthBar != null)
        {
            healthBar.maxValue = stats.GetHP();
            healthBar.value = currentHP;
        }

        Debug.Log($"[EnemyAI] {stats.enemyName} รีเฟรช stat ตาม Wave {stats.CurrentWave}");
    }

    // ==================== UPDATE ====================

    void Update()
    {
        if (isDead) return;

        CheckAttackStasis();
        UpdateTargetsAndDistances();
        HandleStateTransitions();
        UpdateAnimations();
    }

    private void CheckAttackStasis()
    {
        if (animator == null || agent == null || !agent.enabled) return;

        bool isAnimatorAttacking = animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || 
                                   animator.GetCurrentAnimatorStateInfo(0).IsTag("Atk");

        bool isAttackState = currentState == EnemyState.AttackPlayer || 
                             currentState == EnemyState.AttackBase || 
                             currentState == EnemyState.AttackMinion;

        if (isAnimatorAttacking)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;
        }
        else
        {
            agent.updateRotation = true;
            
            // ถ้าอยู่ในสถานะโจมตี แต่ไม่ได้เล่นท่า (ช่วงคูลดาวน์) ให้ค่อยๆ หันหาเป้าหมาย
            if (isAttackState)
            {
                RotateTowardsTarget();
            }
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
            direction.y = 0; 
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

        // ค้นหา Minion ที่ใกล้ที่สุดในระยะ detectionRange ถ้ายังไม่มีเป้าหมาย Minion
        if (minionTransform == null || !minionTransform.gameObject.activeInHierarchy)
        {
            FindClosestMinion();
        }

        // อัปเดตระยะ Minion
        if (minionTransform != null)
        {
            distanceToMinion = Vector3.Distance(transform.position, minionTransform.position);
            // ถ้าหลุดระยะไล่ล่า ให้เลิกสนใจ
            if (distanceToMinion > chaseRange)
            {
                minionTransform = null;
            }
        }
    }

    private void FindClosestMinion()
    {
        GameObject[] minions = GameObject.FindGameObjectsWithTag("Minion");
        float closestDist = detectionRange;
        Transform closestMinion = null;

        foreach (GameObject m in minions)
        {
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestMinion = m.transform;
            }
        }

        minionTransform = closestMinion;
    }

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

        // 1. ตรวจสอบการโจมตีฐานก่อนเสมอ (ถ้าประชิดฐานแล้ว)
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
                else if (!playerDead && distanceToPlayer <= detectionRange) currentState = EnemyState.ChasePlayer; // แทรกคิว Player ถ้าเข้าใกล้
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

        float currentSpeed = agent != null ? agent.velocity.magnitude : 0f;
        float normalizedSpeed = baseSpeed > 0 ? currentSpeed / baseSpeed : 0f;

        animator.SetFloat("Speed", normalizedSpeed);

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
                    agent.updateRotation = true; // เปิดการหมุนขณะวิ่ง
                    agent.speed = baseSpeed;
                    agent.SetDestination(baseTransform.position);
                }
                break;

            case EnemyState.ChasePlayer:
                if (playerTransform != null)
                {
                    agent.isStopped = false;
                    agent.updateRotation = true; // เปิดการหมุนขณะไล่ล่า
                    agent.speed = baseSpeed * 1.5f;
                    agent.SetDestination(playerTransform.position);
                }
                break;

            case EnemyState.ChaseMinion:
                if (minionTransform != null)
                {
                    agent.isStopped = false;
                    agent.updateRotation = true; // เปิดการหมุนขณะไล่ล่า
                    agent.speed = baseSpeed * 1.5f;
                    agent.SetDestination(minionTransform.position);
                }
                break;

            case EnemyState.AttackPlayer:
            case EnemyState.AttackBase:
            case EnemyState.AttackMinion:
                agent.isStopped = true;
                agent.velocity = Vector3.zero; // ป้องกันการสไลด์
                break;
        }
    }

    // ==================== DAMAGE ====================

    public void TakeDamage(float dmg)
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

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;

        DisableWeaponCollider();

        Vector3 vfxPos = deathVFXPoint != null ? deathVFXPoint.position : transform.position;
        VFXManager.Instance?.Play(stats?.deathVFX, vfxPos);
        PowerBallDropper.Drop(transform.position, powerBallDropAmount);
        GetComponent<CharacterAudio>()?.PlayDeath();

        Invoke(nameof(PlayRemovalVFX), 2f);
        Destroy(gameObject, 3f);
    }

    private void PlayRemovalVFX()
    {
        Vector3 vfxPos = removalVFXPoint != null ? removalVFXPoint.position : 
                         (hitVFXPoint != null ? hitVFXPoint.position : transform.position);
        VFXManager.Instance?.Play(stats?.removalVFX, vfxPos);
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

    // ==================== Animation Events ====================

    /// <summary>เรียกจาก Animation Event ตอน Frame ที่อาวุธปะทะเป้าหมาย</summary>
    public void PlayAttackSound() => GetComponent<CharacterAudio>()?.PlayAttack();

    public void EnableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = false;
    }
}