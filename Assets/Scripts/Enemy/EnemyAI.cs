using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Animator animator;

    [Header("AI Settings")]
    public float updateRate = 0.2f; // อัปเดตเป้าหมายทุกๆ 0.2 วินาที (ประหยัด CPU)

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // หาผู้เล่นด้วย Tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            // ใช้ InvokeRepeating แทน Update เพื่อลดภาระเครื่อง
            InvokeRepeating(nameof(UpdateDestination), 0f, updateRate);
        }
        else
        {
            Debug.LogWarning("<color=red>[EnemyAI]</color> ไม่เจอวัตถุที่มี Tag 'Player' ในซีน!");
        }
    }

    void Update()
    {
        // ส่งความเร็วเคลื่อนที่ไปให้ Animator เพื่อเล่นท่าเดิน
        if (animator != null && agent != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void UpdateDestination()
    {
        if (playerTransform != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(playerTransform.position);
        }
    }

    private void OnTriggerEnter(Collider target)
    {
        if (target.CompareTag("Weapon"))
        {
            // พี่สามารถเพิ่มแอนิเมชันตายหรือ Particle เลือดตรงนี้ได้นะครับ
            print("Die!!");
            Destroy(gameObject);
        }
    }
}
