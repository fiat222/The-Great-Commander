using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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

    [Header("Health Settings")]
    public MinionData data;            // ลาก MinionData ScriptableObject มาใส่ใน Inspector
    public Slider healthBar;           // ลาก UI Slider มาใส่ใน Inspector

    [Header("AI Settings")]
    public float updateRate = 0.2f;
    public int typeIndex;           // เพิ่มเพื่อระบุว่าเป็นตัวไหน
    public bool countsInWaveUI;      // เพิ่มเพื่อระบุว่าเป็นมอนสเตอร์ระบบที่ต้องนับใน UI

    [Header("PowerBall Drop")]
    [Tooltip("จำนวน PowerBall ที่ drop เมื่อตาย (0 = ไม่ drop)")]
    public int powerBallDropAmount = 5;

    private float distanceToPlayer;
    private float distanceToBase;
    private bool isDead = false;
    private float currentHP;

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
            currentHP = 100f; // ค่า default ถ้าไม่มี MinionData
        }

        if (healthBar != null)
        {
            healthBar.maxValue = currentHP;
            healthBar.value = currentHP;
        }

        // ปิด Collider ไว้ก่อนตอนเริ่มเกม
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
        // --- กฎเหล็ก: ถ้าถึงป้อมแล้ว ต้องตีป้อมอย่างเดียว ไม่สนเพลเยอร์ ---
        if (baseTransform != null && distanceToBase <= baseAttackRange)
        {
            if (currentState != EnemyState.AttackBase)
            {
                currentState = EnemyState.AttackBase;
                Debug.Log("<color=red>[EnemyAI]</color> Base reached! Ignoring player and attacking base...");
            }
            return;
        }

        switch (currentState)
        {
            case EnemyState.MoveToBase:
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = EnemyState.ChasePlayer;
                    Debug.Log("<color=orange>[EnemyAI]</color> Player detected! Chasing...");
                }
                break;

            case EnemyState.ChasePlayer:
                if (distanceToPlayer > chaseRange)
                {
                    currentState = EnemyState.MoveToBase;
                    Debug.Log("<color=yellow>[EnemyAI]</color> Player lost! Returning to base...");
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
                agent.isStopped = true;
                if (playerTransform != null)
                {
                    Vector3 lookPos = playerTransform.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);
                }
                break;

            case EnemyState.AttackBase:
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

    // ==================== ANIMATION EVENTS ====================

    public void EnableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = false;
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

        Debug.Log($"<color=orange>[EnemyAI]</color> {gameObject.name} โดนตี {dmg} ดาเมจ (Defense: {defense}, Actual: {actualDamage}) | HP เหลือ: {currentHP}");

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

        Debug.Log("<color=red>[EnemyAI]</color> <b>Die!!</b>");

        // 1. เล่นอนิเมชันตาย
        if (animator != null) animator.SetTrigger("Die");

        // 2. แจ้งระบบ UI ว่ามอนสเตอร์รายทางตัวนี้ตายแล้ว
        if (countsInWaveUI)
        {
            GameManager.OnSystemEnemyDied?.Invoke(typeIndex);
        }

        // 3. หยุดเดินและปิด AI
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 4. ปิด Collider เพื่อไม่ให้ขวางทาง
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;

        DisableWeaponCollider();

        // 5. Drop PowerBall ณ ตำแหน่งที่ตาย
        PowerBallDropper.Drop(transform.position, powerBallDropAmount);

        // 6. ทำลายทิ้งหลังจาก 3 วินาที
        Destroy(gameObject, 3f);
    }
}