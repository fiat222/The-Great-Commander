using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Unity.Netcode;

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
    private bool attackTracking; // หมุนตามเพลเยอร์ระหว่างท่าตี (ใช้กับ Combo)
    private float nextFlinchTime; // คูลดาวน์ท่าชะงัก (ป้องกันแสปมสตัน)
    private int revivesRemaining; // จำนวนครั้งที่ลุกขึ้นได้อีก
    private bool attackPreSelected; // เลือกท่าโจมตีไว้ล่วงหน้าแล้วหรือยัง

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

        // ตั้งค่าลุกจาก SO
        revivesRemaining = combatSO != null ? combatSO.reviveCount : 0;

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

    /// <summary>
    /// ดึงระยะโจมตีของท่าที่จองไว้ ถ้าไม่จองไว้หรือตั้งค่าไม่ครบ ให้กลับไปใช้ค่า Default
    /// </summary>
    public float GetCurrentAttackRange()
    {
        if (combatSO == null || !attackPreSelected || animator == null)
            return attackRange;

        int pendingIndex = animator.GetInteger("AttackIndex");
        
        if (combatSO.customAttackRanges != null && pendingIndex < combatSO.customAttackRanges.Length)
        {
            float customRange = combatSO.customAttackRanges[pendingIndex];
            if (customRange > 0f) return customRange; // ถ้าตั้งค่าเป็น 0 ถือว่าไม่ได้ตั้ง
        }

        return attackRange;
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

        bool isAnimatorAttacking = animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

        // ถ้ารับดาเมจอยู่ ให้ชะงัก (Hit Stun)
        bool isAnimatorHit = animator.GetCurrentAnimatorStateInfo(0).IsTag("Hit");

        bool isAttackState = currentState == EnemyState.AttackPlayer || 
                             currentState == EnemyState.AttackBase || 
                             currentState == EnemyState.AttackMinion;

        if (isAnimatorAttacking || isAnimatorHit)
        {
            agent.updateRotation = false;

            if (isAnimatorAttacking && attackTracking)
            {
                // กำลังไกวอาวุธอยู่ → หมุนหาเพลเยอร์ไปเรื่อยๆ (สำหรับ Combo Tracking)
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                RotateTowardsTarget();
            }
            else
            {
                // กำลังฟัน หรือกำลังโดนตีชะงักอยู่ → ล็อคทุกอย่างให้อยู่กับที่
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
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
        else if (currentState == EnemyState.AttackMinion || currentState == EnemyState.ChaseMinion) target = minionTransform;
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
        else if (currentState == EnemyState.AttackMinion || currentState == EnemyState.ChaseMinion) target = minionTransform;
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
        // ถ้า playerTransform หาย (เช่น respawn ใหม่) ให้ค้นหาใหม่
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // อัปเดตระยะ Player (ใช้ขอบ Collider แทนจุดกึ่งกลาง)
        if (playerTransform != null)
            distanceToPlayer = DistanceToEdge(playerTransform);

        // อัปเดตระยะ Base
        if (baseTransform != null)
            distanceToBase = DistanceToEdge(baseTransform);

        // ค้นหา Minion ที่ใกล้ที่สุดในระยะ detectionRange ถ้ายังไม่มีเป้าหมาย Minion
        if (minionTransform == null || !minionTransform.gameObject.activeInHierarchy)
        {
            FindClosestMinion();
        }

        // อัปเดตระยะ Minion
        if (minionTransform != null)
        {
            distanceToMinion = DistanceToEdge(minionTransform);
            if (distanceToMinion > chaseRange)
            {
                minionTransform = null;
            }
        }
    }

    /// <summary>วัดระยะจากขอบ Collider ของเราถึงขอบ Collider ของเป้าหมาย (ไม่ใช่ศูนย์กลาง)</summary>
    private float DistanceToEdge(Transform target)
    {
        Collider targetCol = target.GetComponent<Collider>();
        if (targetCol != null)
        {
            Vector3 closestPoint = targetCol.ClosestPoint(transform.position);
            return Vector3.Distance(transform.position, closestPoint);
        }
        return Vector3.Distance(transform.position, target.position);
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

        // 1. ประชิดฐาน → ตีฐานเสมอ
        if (baseTransform != null && distanceToBase <= baseAttackRange)
        {
            currentState = EnemyState.AttackBase;
            return;
        }

        // 2. จัดการตามสถานะปัจจุบัน (ล็อคเป้าจนตาย/หลุดระยะ)
        switch (currentState)
        {
            // --- กำลังสู้ Minion → อยู่กับ Minion จนตาย ---
            case EnemyState.AttackMinion:
                if (minionTransform == null) currentState = EnemyState.MoveToBase;
                else if (distanceToMinion > GetCurrentAttackRange()) currentState = EnemyState.ChaseMinion;
                else if (inCooldown)
                {
                    float threshold = 0.5f;
                    if (combatSO != null && combatSO.attackCooldown > threshold)
                        currentState = EnemyState.ChaseMinion;
                }
                break;

            case EnemyState.ChaseMinion:
                if (minionTransform == null) currentState = EnemyState.MoveToBase;
                else if (distanceToMinion > chaseRange) { minionTransform = null; currentState = EnemyState.MoveToBase; }
                else if (distanceToMinion <= GetCurrentAttackRange() * 0.8f && !inCooldown) currentState = EnemyState.AttackMinion;
                break;

            // --- กำลังสู้ Player → อยู่กับ Player จนตาย/หลุดระยะ ---
            case EnemyState.AttackPlayer:
                if (playerDead || playerTransform == null || distanceToPlayer > GetCurrentAttackRange())
                    currentState = EnemyState.ChasePlayer;
                else if (inCooldown) 
                {
                    float threshold = 0.5f;
                    if (combatSO != null && combatSO.attackCooldown > threshold)
                        currentState = EnemyState.ChasePlayer;
                }
                break;

            case EnemyState.ChasePlayer:
                if (playerDead || playerTransform == null || distanceToPlayer > chaseRange) currentState = EnemyState.MoveToBase;
                else if (distanceToPlayer <= GetCurrentAttackRange() * 0.8f && !inCooldown) currentState = EnemyState.AttackPlayer;
                break;

            // --- ตีฐาน / เดินไปฐาน → ล็อคสิ่งแรกที่เข้ามาในระยะ ---
            case EnemyState.AttackBase:
                if (baseTransform == null || distanceToBase > baseAttackRange + 1f) currentState = EnemyState.MoveToBase;
                else if (minionTransform != null && distanceToMinion <= detectionRange) currentState = EnemyState.ChaseMinion;
                else if (!playerDead && playerTransform != null && distanceToPlayer <= detectionRange) currentState = EnemyState.ChasePlayer;
                break;

            case EnemyState.MoveToBase:
                // ล็อคสิ่งแรกที่เข้ามาในระยะ (เช็คทั้ง Minion + Player)
                bool minionInRange = minionTransform != null && distanceToMinion <= detectionRange;
                bool playerInRange = !playerDead && playerTransform != null && distanceToPlayer <= detectionRange;

                if (minionInRange && playerInRange)
                {
                    // ทั้งคู่อยู่ในระยะ → เลือกตัวที่ใกล้กว่า
                    currentState = distanceToMinion <= distanceToPlayer 
                        ? EnemyState.ChaseMinion : EnemyState.ChasePlayer;
                }
                else if (minionInRange) currentState = EnemyState.ChaseMinion;
                else if (playerInRange) currentState = EnemyState.ChasePlayer;
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
        // ส่งบอก Animator ว่าตอนนี้กำลังโจมตีแบบยืนอยู่กับที่ (เช่น ตีป้อม หรือ Minion ในอนาคต)
        bool isStationaryAttack = currentState == EnemyState.AttackBase;
        animator.SetBool("IsStationaryAttack", isStationaryAttack);

        // ถ้าเพิ่งเริ่มโจมตี ให้ตั้งคูลดาวน์
        if (isAttacking && Time.time >= nextAttackTime)
        {
            // --- เช็คระยะให้ชัวร์อีกรอบก่อนจะกางกรงเล็บฟัน (ป้องกันการตีลม) ---
            bool canReach = false;
            float currentAtkRng = GetCurrentAttackRange();

            if (currentState == EnemyState.AttackPlayer && distanceToPlayer <= currentAtkRng + 0.2f) canReach = true;
            else if (currentState == EnemyState.AttackMinion && distanceToMinion <= currentAtkRng + 0.2f) canReach = true;
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

                    // ถ้าไม่ได้เลือกท่าไว้ล่วงหน้า ให้สุ่มก่อนตี (เผื่อกรณี base/ไม่ได้ผ่านเข้า phase 3)
                    if (!attackPreSelected) 
                    {
                        int variants = combatSO.attackVariants;
                        if (variants > 1)
                            animator.SetInteger("AttackIndex", Random.Range(0, variants));
                        else
                            animator.SetInteger("AttackIndex", 0);
                    }

                    animator.SetTrigger(combatSO.attackTriggerName);
                    attackPreSelected = false; // รีเซตสถานะการจองท่า
                    Debug.Log($"[Combat] ⚔️ โจมตี! ท่า={animator.GetInteger("AttackIndex")} cooldown={cooldown}s");
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
                    agent.updateRotation = true;
                    agent.speed = baseSpeed;
                    agent.SetDestination(baseTransform.position);

                    // รีเซ็ตสถานะ strafe ที่ค้างจากการต่อสู้
                    isStrafing = false;
                    hasRetreated = false;
                    targetStrafeX = 0f;
                    targetStrafeY = 0f;
                    if (animator != null) animator.SetBool("IsStrafing", false);
                }
                break;

            case EnemyState.ChasePlayer:
                if (playerTransform != null)
                {
                    agent.isStopped = false;
                    bool inCooldown = Time.time < nextAttackTime;
                    float currentAtkRng = GetCurrentAttackRange();
                    float sRange = combatSO != null ? combatSO.strafeRange : currentAtkRng + 2f;

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

                            Vector3 awayFromTarget = (transform.position - playerTransform.position).normalized;
                            Vector3 retreatPos = transform.position + awayFromTarget * 3f;

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

                            Vector3 toTarget = (playerTransform.position - transform.position).normalized;
                            Vector3 strafeDir = Vector3.Cross(toTarget, Vector3.up) * strafeDirection;
                            // รักษาระยะ ~ strafeRange
                            Vector3 targetPos = playerTransform.position + toTarget * -sRange + strafeDir * 3f;

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
                    Debug.Log($"[Combat] 🏃 วิ่งเข้าตี! dist={distanceToPlayer:F1}, atkRange={GetCurrentAttackRange():F1}");
                    isStrafing = false;
                    hasRetreated = false; // รีเซ็ต ให้ถอยได้อีกรอบหน้า
                    if (animator != null)
                    {
                        animator.SetBool("IsStrafing", false);
                        targetStrafeX = 0f;
                        targetStrafeY = 0f;

                        // สุ่มท่าโจมตีไว้ล่วงหน้าเลย (ทำทีเดียวต่อการวิ่ง 1 รอบ)
                        if (combatSO != null && !attackPreSelected)
                        {
                            int variants = combatSO.attackVariants;
                            if (variants > 1)
                                animator.SetInteger("AttackIndex", Random.Range(0, variants));
                            else
                                animator.SetInteger("AttackIndex", 0);
                            attackPreSelected = true;
                        }
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
                    bool inCooldownM = Time.time < nextAttackTime;
                    float currentAtkRngM = GetCurrentAttackRange();
                    float sRangeM = combatSO != null ? combatSO.strafeRange : currentAtkRngM + 2f;

                    if (inCooldownM && combatSO != null)
                    {
                        // ถ้ายังเล่นท่าตีอยู่ → หยุดนิ่ง
                        bool stillSwinging = animator != null && 
                            (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || 
                             animator.GetCurrentAnimatorStateInfo(0).IsTag("Atk") ||
                             animator.GetNextAnimatorStateInfo(0).IsTag("Attack") ||
                             animator.GetNextAnimatorStateInfo(0).IsTag("Atk"));
                        if (stillSwinging)
                        {
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                            return;
                        }

                        isStrafing = true;

                        // Phase 1: ถอยหลัง
                        if (!hasRetreated && distanceToMinion < sRangeM - 0.5f)
                        {
                            agent.updateRotation = false;
                            RotateTowardsTarget();
                            Vector3 awayFromTarget = (transform.position - minionTransform.position).normalized;
                            agent.speed = combatSO.strafeSpeed;
                            agent.SetDestination(transform.position + awayFromTarget * 3f);
                            if (animator != null)
                            {
                                animator.SetBool("IsStrafing", true);
                                targetStrafeX = 0f;
                                targetStrafeY = -1f;
                            }
                            return;
                        }

                        hasRetreated = true;

                        // Phase 2: เดินวน
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
                            agent.updateRotation = false;
                            RotateTowardsTarget();
                            Vector3 toTarget = (minionTransform.position - transform.position).normalized;
                            Vector3 strafeDir = Vector3.Cross(toTarget, Vector3.up) * strafeDirection;
                            Vector3 targetPos = minionTransform.position + toTarget * -sRangeM + strafeDir * 3f;
                            agent.speed = combatSO.strafeSpeed;
                            agent.SetDestination(targetPos);
                            if (animator != null)
                            {
                                animator.SetBool("IsStrafing", true);
                                targetStrafeX = strafeDirection;
                                targetStrafeY = 0f;
                            }
                            return;
                        }

                        // ยืนรอ
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

                    // Phase 3: คูลดาวน์หมด → วิ่งเข้าตี
                    isStrafing = false;
                    hasRetreated = false;
                    if (animator != null)
                    {
                        animator.SetBool("IsStrafing", false);
                        targetStrafeX = 0f;
                        targetStrafeY = 0f;

                        // สุ่มท่าโจมตีไว้ล่วงหน้าเลย
                        if (combatSO != null && !attackPreSelected)
                        {
                            int variants = combatSO.attackVariants;
                            if (variants > 1)
                                animator.SetInteger("AttackIndex", Random.Range(0, variants));
                            else
                                animator.SetInteger("AttackIndex", 0);
                            attackPreSelected = true;
                        }
                    }
                    agent.updateRotation = true;
                    agent.speed = baseSpeed * 2.5f;
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

        // --- ระบบ Hyper Armor + Flinch Cooldown ---
        var sInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        var nInfo = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
        bool isAttacking = sInfo.IsTag("Attack") || nInfo.IsTag("Attack");

        bool skipFlinch = combatSO != null && combatSO.hasHyperArmor && isAttacking;
        bool flinchOnCooldown = Time.time < nextFlinchTime; // ยังอยู่ในช่วงป้องกัน spam

        if (currentHP > 0 && animator != null && !skipFlinch && !flinchOnCooldown)
        {
            animator.SetTrigger("Damage");
            float fCooldown = combatSO.flinchCooldown;
            nextFlinchTime = Time.time + fCooldown;
        }

        Vector3 vfxPos = hitVFXPoint != null ? hitVFXPoint.position : transform.position;
        VFXManager.Instance?.Play(stats?.hitVFX, vfxPos);

        if (currentHP <= 0)
        {
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
            animator.SetBool("isDead", true);
            animator.SetTrigger("Die");
        }

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
        GetComponent<CharacterAudio>()?.PlayDeath();

        // --- ระบบ Revive ---
        if (revivesRemaining > 0)
        {
            revivesRemaining--;
            float delay = combatSO != null ? combatSO.reviveDelay : 2f;
            Invoke(nameof(Revive), delay);
            Debug.Log($"[Combat] 💀→🔄 ตายแล้วจะลุก! เหลือลุกได้อีก {revivesRemaining} ครั้ง (รอ {delay}s)");
            return; // ไม่ drop ไม่นับ wave ยัง
        }

        // --- ตายถาวร ---
        if (countsInWaveUI)
            GameManager.OnSystemEnemyDied?.Invoke(typeIndex);
            
        PowerBallDropper.Drop(transform.position, powerBallDropAmount);

        Invoke(nameof(PlayRemovalVFX), 2f);
        Destroy(gameObject, 3f);
    }

    /// <summary>ลุกขึ้นจากความตาย! (Phase 1: เล่นท่าลุก)</summary>
    private void Revive()
    {
        // isDead ยังเป็น true → ตีไม่โดนระหว่างลุก

        // ฟื้น HP ตามเปอร์เซ็นต์
        float hpPercent = combatSO != null ? combatSO.reviveHPPercent : 0.5f;
        currentHP = Mathf.Max(1f, stats != null ? stats.GetHP() * hpPercent : currentHP);
        if (healthBar != null)
        {
            healthBar.maxValue = stats != null ? stats.GetHP() : 100;
            healthBar.value = currentHP;
        }

        // เปิด Collider กลับ (ให้โดนตีได้ระหว่างลุก)
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = true;

        // เล่นท่าลุก (ยังไม่เปิด Agent → รอ Animation Event)
        if (animator != null)
            animator.SetTrigger("Revive");

        Debug.Log($"[Combat] 🔄💀 กำลังลุก... HP={currentHP}");
    }

    /// <summary>เรียกจาก Animation Event ตอนท่า Revive จบ → เปิดการเคลื่อนที่</summary>
    public void OnReviveComplete()
    {
        isDead = false; // ตอนนี้ค่อยโดนตีได้ + ขยับได้

        // เปิด NavMeshAgent
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        // รีเซ็ต state
        currentState = EnemyState.ChasePlayer;
        nextAttackTime = Time.time + 1f;

        Debug.Log($"[Combat] 🔄✅ ลุกเสร็จ! พร้อมสู้แล้ว");
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

    /// <summary>เรียกจาก Animation Event ตอนเริ่มไกวอาวุธ → เปิดการหมุนตามเพลเยอร์</summary>
    public void StartAttackTracking() => attackTracking = true;

    /// <summary>เรียกจาก Animation Event ตอนพุ่งหรือท่าจบ → หยุดหมุน</summary>
    public void StopAttackTracking() => attackTracking = false;

    /// <summary>เรียกจาก Animation Event ตอนกำลังฟัด → หยุด Tracking + พุ่งเข้าใส่</summary>
    public void AttackLunge()
    {
        // เลือกเป้าหมายตาม state ปัจจุบัน
        Transform target = null;
        if (currentState == EnemyState.AttackBase) target = baseTransform;
        else if (currentState == EnemyState.AttackMinion || currentState == EnemyState.ChaseMinion) target = minionTransform;
        else target = playerTransform;

        if (target == null) return;

        // หยุด tracking + หันหน้าฟึบ
        attackTracking = false;
        SnapRotateTowardsTarget();

        // พุ่งเข้าหาเป้าหมายแบบไวมาก (ไม่วาป)
        float dist = Vector3.Distance(transform.position, target.position);
        float lungeDistance = Mathf.Max(0f, dist - 3f); // เว้นห่าง 3 หน่วย
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;

        if (lungeDistance > 0.1f)
            StartCoroutine(LungeCoroutine(dir, lungeDistance));
    }

    private System.Collections.IEnumerator LungeCoroutine(Vector3 dir, float totalDist)
    {
        float duration = 0.12f; // ระยะเวลาพุ่ง (วินาที) ยิ่งน้อย ยิ่งไว
        float elapsed = 0f;
        float moved = 0f;

        while (elapsed < duration && agent != null && agent.isOnNavMesh)
        {
            float step = (totalDist / duration) * Time.deltaTime;
            if (moved + step > totalDist) step = totalDist - moved;

            agent.Move(dir * step);
            moved += step;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void EnableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = false;
    }
}