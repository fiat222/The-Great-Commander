using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ArcherAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public MinionData data;

    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform shootPoint;
    public float arrowSpeed = 30f;

    private Transform currentTarget;
    private float lastAttackTime;
    private bool isInRange = false;
    private bool isDead = false;

    void Start()
    {
        if (data != null)
        {
            agent.speed = data.speed;
        }
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

        Vector3 flat = currentTarget.position - transform.position;
        flat.y = 0;
        float distance = flat.magnitude;

        float range = data != null ? data.attackrange : 10f;

        if (distance > range)
        {
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
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

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

            float cooldown = data != null ? 1f / data.speed : 2f; // ใช้ speed แทน cooldown
            if (Time.time - lastAttackTime > cooldown)
            {
                lastAttackTime = Time.time;
                animator.SetTrigger("Shoot");
            }
        }
    }

    public void SpawnArrow()
    {
        if (arrowPrefab == null || shootPoint == null || currentTarget == null) return;

        Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
        Vector3 direction = (targetPos - shootPoint.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, shootPoint.position, Quaternion.LookRotation(direction));
        int dmg = data != null ? data.damage : 1;
        arrow.GetComponent<ArrowProjectile>()?.Launch(direction, arrowSpeed, dmg);
    }

    void SetAttack(bool value) => animator.SetBool("Attack", value);
    void SetRunning(bool value) => animator.SetBool("Run", value);

    public void TakeDamage(int dmg)
    {
        // TODO: ลด HP ด้วย data.hp
        // if (currentHP <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        agent.isStopped = true;
        SetAttack(false);
        SetRunning(false);
        animator.SetBool("Death", true);
    }

    void FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) { currentTarget = null; return; }

        currentTarget = enemies
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .First().transform;
    }
}