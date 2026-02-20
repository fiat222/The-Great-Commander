using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class MinionAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public int damage = 3;

    private Transform currentTarget;
    private float lastAttackTime;
    private bool isInRange = false;
    private bool isDead = false;

    void Start()
    {
        SetWalk(false);
    }

    void Update()
    {
        if (isDead) return;

        FindClosestEnemy();

        if (currentTarget == null)
        {
            SetWalk(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance > attackRange)
        {
            // ===== นอกระยะ → เดินเข้าหา =====
            if (isInRange)
            {
                isInRange = false;
            }

            SetWalk(true);
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            // ===== ในระยะ → หยุดโจมตี =====
            agent.isStopped = true;

            Vector3 dir = currentTarget.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            if (!isInRange)
            {
                isInRange = true;
                SetWalk(false);
            }

            if (Time.time - lastAttackTime > attackCooldown)
            {
                lastAttackTime = Time.time;
                animator.SetTrigger("Attack");
                Debug.Log($"<color=green>[MinionAI]</color> Attack!");
            }
        }
    }

    // เรียกจาก Animation Event ใน clip Attack
    public void DealDamage()
    {
        if (currentTarget == null) return;

        // เช็คระยะอีกครั้งกันกรณี enemy หนีไปแล้ว
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > attackRange * 1.2f) return;
        
        // TODO: currentTarget.GetComponent<BaseHealth>()?.TakeDamage(damage);
        Debug.Log($"<color=red>[MinionAI]</color> Hit! Damage: {damage}");
    }

    void SetWalk(bool value) => animator.SetBool("Walk", value);

    public void TakeDamage(int dmg)
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
        SetWalk(false);
        animator.SetBool("Die", true);

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