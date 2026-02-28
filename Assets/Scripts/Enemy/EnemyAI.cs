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
    public EnemyCombatSO combatSO; // 🔥 สมองการต่อสู้แยกไฟล์

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.MoveToBase;
    public float detectionRange = 17f;
    public float chaseRange = 28f;
    public float baseAttackRange = 7f;

    [Header("Combat")]
    public GameObject weaponEffect;
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

    // --- Runtime Combat ---
    private float nextAttackTime;
    private int strafeDirection = 1;
    private float strafeEndTime;
    private bool isStrafing;
    private bool hasRetreated; // ถอยหลังแล้วรอบนี้หรือยัง
    private float targetStrafeX; // ค่าเป้าหมายสำหรับ Blend Tree (ค่อยๆ ไหล)
    private float targetStrafeY;

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
        UpdateWeaponEffect();
    }

    private void UpdateWeaponEffect()
    {
        if (weaponEffect == null || animator == null) return;

        var sInfo = animator.GetCurrentAnimatorStateInfo(0);
        var nInfo = animator.GetNextAnimatorStateInfo(0);

        bool inAttack = sInfo.IsTag("Attack") || nInfo.IsTag("Attack");

        if (weaponEffect.activeSelf != inAttack)
            weaponEffect.SetActive(inAttack);
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
            agent.isStopped = false; // ปลดเบรก ให้เดินได้

            if (isAttackState)
            {
                // อยู่ในสถานะโจมตีแต่ยังไม่เล่นท่า → ปิด NavMesh หมุน ใช้ของเราเอง
                agent.updateRotation = false;
                RotateTowardsTarget();
            }
            else if (isStrafing)
            {
                // กำลัง Strafe → หันหน้าหาเพลเยอร์ทุกเฟรม
                agent.updateRotation = false;
                RotateTowardsTarget();
            }
            else
            {
                // เดิน/วิ่งปกติ → ให้ NavMesh จัดการหมุนตามเส้นทาง
                agent.updateRotation = true;
                targetStrafeX = 0f;
                targetStrafeY = 0f;
            }

            // --- ค่อยๆ ไหล StrafeX/Y ไปหาเป้าหมาย (ป้องกันกระตุก) ---
            if (animator != null)
            {
                float dampTime = 0.15f;
                animator.SetFloat("StrafeX", targetStrafeX, dampTime, Time.deltaTime);
                animator.SetFloat("StrafeY", targetStrafeY, dampTime, Time.deltaTime);
            }
        }
    }

    /// <summary>หมุนหน้าหาเป้าหมายแบบนุ่มๆ (Slerp)</summary>
    private void RotateTowardsTarget()
    {
        Transform target = null;
        if (currentState == EnemyState.AttackBase) target = baseTransform;
        else if (currentState == EnemyState.AttackMinion) target = minionTransform;
        else target = playerTransform;

        if (target != null)
        {
            Vector3 direction = (target.position - transform.position);
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }
        }
    }

    /// <summary>หมุนหน้าหาเป้าหมายแบบฟึบทันที (Snap)</summary>
    private void SnapRotateTowardsTarget()
    {
        Transform target = null;
        if (currentState == EnemyState.AttackBase) target = baseTransform;
        else if (currentState == EnemyState.AttackMinion) target = minionTransform;
        else target = playerTransform;

        if (target != null)
        {
            Vector3 direction = (target.position - transform.position);
            direction.y = 0;
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction.normalized);
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
        bool inCooldown = Time.time < nextAttackTime;

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
                else if (playerDead || playerTransform == null || distanceToPlayer > attackRange) 
                    currentState = EnemyState.ChasePlayer;
                else if (inCooldown) 
                {
                    // ถ้าคูลดาวน์นาน (เช่น > 0.5 วิ) ให้เดินวน (Chase/Strafe)
                    // ถ้าคูลดาวน์สั้น (รัวๆ) ให้ปักหลักยืนรอที่เดิม (Attack)
                    float threshold = 0.5f;
                    if (combatSO != null && combatSO.attackCooldown > threshold)
                        currentState = EnemyState.ChasePlayer;
                }
                break;

            case EnemyState.ChasePlayer:
                if (minionTransform != null) currentState = EnemyState.ChaseMinion;
                else if (playerDead || playerTransform == null || distanceToPlayer > chaseRange) currentState = EnemyState.MoveToBase;
                else if (distanceToPlayer <= attackRange * 0.8f && !inCooldown) currentState = EnemyState.AttackPlayer;
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
        
        // ถ้าเพิ่งเริ่มโจมตี ให้ตั้งคูลดาวน์
        if (isAttacking && Time.time >= nextAttackTime)
        {
            // --- เช็คระยะให้ชัวร์อีกรอบก่อนจะกางกรงเล็บฟัน (ป้องกันการตีลม) ---
            bool canReach = false;
            if (currentState == EnemyState.AttackPlayer && distanceToPlayer <= attackRange + 0.2f) canReach = true;
            else if (currentState == EnemyState.AttackMinion && distanceToMinion <= attackRange + 0.2f) canReach = true;
            else if (currentState == EnemyState.AttackBase && distanceToBase <= baseAttackRange + 0.5f) canReach = true;

            if (canReach)
            {
                // หันหน้าหาเป้าหมายแบบฟึบก่อนฟัน
                SnapRotateTowardsTarget();
                agent.updateRotation = false;

                float cooldown = combatSO != null ? combatSO.attackCooldown : 1.5f;
                nextAttackTime = Time.time + cooldown;
                
                if (combatSO != null && !string.IsNullOrEmpty(combatSO.attackTriggerName))
                {
                    animator.ResetTrigger("Damage"); // ยกเลิกท่าโดนตี ถ้ากำลังเล่นอยู่
                    animator.SetTrigger(combatSO.attackTriggerName);
                    Debug.Log($"[Combat] ⚔️ โจมตี! cooldown={cooldown}s, nextAttack={nextAttackTime:F1}");
                }
            }
        }

        animator.SetBool("IsAttacking", isAttacking);

        // --- ล้าง Trigger ที่ค้างอยู่ (ป้องกันการตีลม) ---
        // ไม่ลบถ้ายังอยู่ในคูลดาวน์ (หมายถึงเพิ่งสั่งตี → ต้องปล่อยให้ Animator รับ Trigger ก่อน)
        bool inCooldown2 = Time.time < nextAttackTime;
        if (!isAttacking && !inCooldown2 && combatSO != null && !string.IsNullOrEmpty(combatSO.attackTriggerName))
        {
            animator.ResetTrigger(combatSO.attackTriggerName);
        }
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
                    bool inCooldown = Time.time < nextAttackTime;
                    float sRange = combatSO != null ? combatSO.strafeRange : attackRange + 2f;

                    if (inCooldown && combatSO != null)
                    {
                        // ถ้ายังเล่นท่าตีอยู่ หรือกำลัง Transition เข้าท่าตี → หยุดนิ่ง
                        bool stillSwinging = animator != null && 
                            (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || 
                             animator.GetCurrentAnimatorStateInfo(0).IsTag("Atk") ||
                             animator.GetNextAnimatorStateInfo(0).IsTag("Attack") ||
                             animator.GetNextAnimatorStateInfo(0).IsTag("Atk"));
                        if (stillSwinging)
                        {
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                            Debug.Log("[Combat] 🛑 ยังตีอยู่ รอท่าจบก่อน");
                            return;
                        }

                        isStrafing = true; // บอก CheckAttackStasis ว่าอยู่ในโหมดคุมเชิง → หันหน้าหาเพลเยอร์ตลอด

                        // === Phase 1: ถอยหลังแค่ครั้งเดียวหลังตี ===
                        if (!hasRetreated && distanceToPlayer < sRange - 0.5f)
                        {
                            Debug.Log($"[Combat] 🔙 ถอยหลัง dist={distanceToPlayer:F1} → target={sRange:F1}");
                            agent.updateRotation = false;
                            RotateTowardsTarget(); // หันหน้าหาเพลเยอร์ขณะถอย

                            Vector3 awayFromPlayer = (transform.position - playerTransform.position).normalized;
                            Vector3 retreatPos = transform.position + awayFromPlayer * 3f;

                            agent.speed = combatSO.strafeSpeed;
                            agent.SetDestination(retreatPos);

                            // ส่งค่า Animator (ใช้ StrafeX = 0 สำหรับถอย ถ้ามีท่า)
                            if (animator != null)
                            {
                                animator.SetBool("IsStrafing", true);
                                targetStrafeX = 0f;
                                targetStrafeY = -1f; // ถอยหลัง
                            }
                            return;
                        }

                        // ถอยหลังเสร็จแล้ว (อยู่ที่ strafeRange แล้ว หรือข้าม Phase 1 ไปแล้ว)
                        hasRetreated = true;

                        // === Phase 2: เดินวนซ้าย/ขวาที่ระยะ strafeRange ===
                        if (Time.time > strafeEndTime)
                        {
                            if (Random.value < combatSO.strafeChance)
                            {
                                isStrafing = true;
                                strafeDirection = Random.value < 0.5f ? -1 : 1;
                                strafeEndTime = Time.time + combatSO.strafeDuration;
                            }
                            else
                            {
                                isStrafing = false;
                                strafeEndTime = Time.time + 0.5f;
                            }
                        }

                        if (isStrafing)
                        {
                            Debug.Log($"[Combat] ↔️ เดินวน dir={strafeDirection}");
                            agent.updateRotation = false;
                            RotateTowardsTarget(); // หันหน้าหาเพลเยอร์ขณะเดินวน

                            Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
                            Vector3 strafeDir = Vector3.Cross(toPlayer, Vector3.up) * strafeDirection;
                            // รักษาระยะ ~ strafeRange
                            Vector3 targetPos = playerTransform.position + toPlayer * -sRange + strafeDir * 3f;

                            agent.speed = combatSO.strafeSpeed;
                            agent.SetDestination(targetPos);

                            if (animator != null)
                            {
                                animator.SetBool("IsStrafing", true);
                                targetStrafeX = strafeDirection;
                                targetStrafeY = 0f; // เดินข้าง
                            }
                            return;
                        }

                        // ไม่ strafe ก็ยืนรอ → หันหน้าหาเพลเยอร์
                        agent.updateRotation = false;
                        RotateTowardsTarget();
                        if (animator != null)
                        {
                            animator.SetBool("IsStrafing", true);
                            targetStrafeX = 0f;
                            targetStrafeY = 0f;
                        }
                        return;
                    }

                    // === Phase 3: คูลดาวน์หมด → วิ่งเข้าหาเพลเยอร์ ===
                    Debug.Log($"[Combat] 🏃 วิ่งเข้าตี! dist={distanceToPlayer:F1}, atkRange={attackRange:F1}");
                    isStrafing = false;
                    hasRetreated = false; // รีเซ็ต ให้ถอยได้อีกรอบหน้า
                    if (animator != null)
                    {
                        animator.SetBool("IsStrafing", false);
                        targetStrafeX = 0f;
                        targetStrafeY = 0f;
                    }
                    agent.updateRotation = true;
                    agent.speed = baseSpeed * 2.5f;
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

        // --- ระบบ Hyper Armor ---
        bool isAttacking = animator != null && (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || animator.GetCurrentAnimatorStateInfo(0).IsTag("Atk"));
        bool skipFlinch = combatSO != null && combatSO.hasHyperArmor && isAttacking;

        if (animator != null && !skipFlinch)
            animator.SetTrigger("Damage");

        Vector3 vfxPos = hitVFXPoint != null ? hitVFXPoint.position : transform.position;
        VFXManager.Instance?.Play(stats?.hitVFX, vfxPos);

        if (currentHP <= 0)
        {
            // ตาย → บังคับเล่นท่า Damage ก่อน (ไม่สน Hyper Armor) เพื่อให้ Animator ไปถึง Die ได้
            if (animator != null && skipFlinch)
                animator.SetTrigger("Damage");
            Die();
        }
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

        // วงเขียว = ระยะเดินวน (strafeRange)
        if (combatSO != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, combatSO.strafeRange);
        }
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