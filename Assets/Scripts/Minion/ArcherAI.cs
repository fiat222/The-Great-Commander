using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ArcherAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public MinionData data;

    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform shootPoint;
    public float arrowSpeed = 30f;

    [Header("Health Settings")]
    public Slider healthBar; // ลาก UI Slider มาใส่ที่นี่ใน Inspector

    private float currentHP;
    private Transform currentTarget;
    private float lastAttackTime;
    private bool isInRange = false;
    private bool isDead = false;

    void Start()
    {
        if (data != null)
        {
            agent.speed = data.speed;
            currentHP = data.hp;
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

        // เล็งเป้าหมาย: 1.จุดโดนตี(hitVFXPoint) 2.กลาง Collider 3.จุดอ้างอิงบวก Y
        Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
        
        // พยายามดึงฮิตบ็อกซ์หรือตําแหน่งกลางจากสคริปต์ศัตรู
        EnemyAI enemyAI = currentTarget.GetComponent<EnemyAI>();
        ImpAI impAI = currentTarget.GetComponent<ImpAI>();
        
        if (enemyAI != null && enemyAI.hitVFXPoint != null)
        {
            targetPos = enemyAI.hitVFXPoint.position;
        }
        else if (impAI != null && impAI.hitVFXPoint != null)
        {
            targetPos = impAI.hitVFXPoint.position;
        }
        else
        {
            Collider targetCol = currentTarget.GetComponent<Collider>();
            if (targetCol != null)
            {
                targetPos = targetCol.bounds.center;
            }
        }

        Vector3 direction = (targetPos - shootPoint.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, shootPoint.position, Quaternion.LookRotation(direction));
        float dmg = data != null ? data.damage : 1f;
        arrow.GetComponent<ArrowProjectile>()?.Launch(direction, arrowSpeed, (int)dmg);
    }

    void SetAttack(bool value) => animator.SetBool("Attack", value);
    void SetRunning(bool value) => animator.SetBool("Run", value);

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        // คำนวณ Damage หลังจาก Defense
        float defense = data != null ? data.defense : 0f;
        float actualDamage = Mathf.Max(1f, dmg - defense);

        currentHP -= actualDamage;
        currentHP = Mathf.Max(currentHP, 0);

        if (healthBar != null)
            healthBar.value = currentHP;

        Debug.Log($"<color=orange>[ArcherAI]</color> {gameObject.name} โดนตี {dmg} ดาเมจ (Defense: {defense}, Actual: {actualDamage}) | HP เหลือ: {currentHP}");

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        agent.isStopped = true;
        SetAttack(false);
        SetRunning(false);
        animator.SetTrigger("Die");

        // ปิด Collider และลบตัวละครทิ้งหลังจาก 1 วิ
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 1f);
    }

    void FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) { currentTarget = null; return; }

        currentTarget = enemies
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .First().transform;
    }

    private void OnDrawGizmosSelected()
    {
        float range = data.attackrange;

        // Attack Range (แดง)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}