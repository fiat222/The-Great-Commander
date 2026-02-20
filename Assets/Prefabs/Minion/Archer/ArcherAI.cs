using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ArcherAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float attackCooldown = 2f;
    public float arrowSpeed = 30f;
    public int arrowDamage = 2;

    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform shootPoint;

    private Transform currentTarget;
    private float lastAttackTime;
    private bool isInRange = false;
    private bool isDead = false;

    void Start()
    {
        SetAttack(false);
        SetRunning(false);
    }

    void Update()
    {
        if (isDead) return;

        FindClosestEnemy();

        if (currentTarget == null)
        {
            SetAttack(false);
            SetRunning(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance > attackRange)
        {
            // ===== นอกระยะ → วิ่งเข้าหา =====
            if (isInRange)
            {
                isInRange = false;
                SetAttack(false);
                animator.ResetTrigger("Shoot");
            }

            SetRunning(true);
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            // ===== ในระยะ → หยุดยิง =====
            agent.isStopped = true;

            Vector3 dir = currentTarget.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            if (!isInRange)
            {
                isInRange = true;
                SetRunning(false);
                SetAttack(false);
                animator.ResetTrigger("Shoot");
                SetAttack(true);
            }

            if (Time.time - lastAttackTime > attackCooldown)
            {
                lastAttackTime = Time.time;
                animator.SetTrigger("Shoot");
            }
        }
    }

    // เรียกจาก Animation Event ใน clip Shoot
    public void SpawnArrow()
    {
        if (arrowPrefab == null || shootPoint == null || currentTarget == null) return;

        // เล็งไปที่ center mass ของ enemy (เพิ่ม Y นิดนึงกันยิงพื้น)
        Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
        Vector3 direction = (targetPos - shootPoint.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, shootPoint.position, Quaternion.LookRotation(direction));
        arrow.GetComponent<ArrowProjectile>()?.Launch(direction, arrowSpeed, arrowDamage);
    }

    void SetAttack(bool value) => animator.SetBool("Attack", value);
    void SetRunning(bool value) => animator.SetBool("Run", value);

    public void TakeDamage(int damage)
    {
        // TODO: ลด HP
        //animator.SetTrigger("Damage");
        // if (currentHP <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        agent.isStopped = true;
        SetAttack(false);
        SetRunning(false);
        animator.SetBool("Death",true);

        // TODO: Destroy หรือ return to pool
    }

    void FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
        {
            currentTarget = null;
            return;
        }

        currentTarget = enemies
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .First()
            .transform;
    }
}