using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class MinionAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public MinionData data;

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

        Vector3 flat = currentTarget.position - transform.position;
        flat.y = 0;
        float distance = flat.magnitude;

        float range = data != null ? data.attackrange : 2f;
        float cooldown = data != null ? 1f / Mathf.Max(data.speed, 0.1f) : 1.5f;

        if (distance > range)
        {
            if (isInRange) isInRange = false;

            SetWalk(true);
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();

            Vector3 dir = currentTarget.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            if (!isInRange)
            {
                isInRange = true;
                SetWalk(false);
            }

            if (Time.time - lastAttackTime > cooldown)
            {
                lastAttackTime = Time.time;
                animator.SetTrigger("Attack");
            }
        }
    }

    public void DealDamage()
    {
        if (currentTarget == null) return;

        Vector3 flat = currentTarget.position - transform.position;
        flat.y = 0;
        float range = data != null ? data.attackrange : 2f;
        if (flat.magnitude > range * 1.2f) return;

        int dmg = data != null ? data.damage : 1;
        // TODO: currentTarget.GetComponent<BaseHealth>()?.TakeDamage(dmg);
        Debug.Log($"<color=red>[MinionAI]</color> Hit! Damage: {dmg}");
    }

    void SetWalk(bool value) => animator.SetBool("Walk", value);

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
        SetWalk(false);
        animator.SetBool("Die", true);
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