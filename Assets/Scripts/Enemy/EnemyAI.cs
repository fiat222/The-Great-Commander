using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    NavMeshAgent agent;
    private Transform playerTransform;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // หาผู้เล่นด้วย Tag "Player" และเก็บ Reference ไว้ใช้ยาวๆ
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("<color=red>[EnemyAI]</color> ไม่เจอวัตถุที่มี Tag 'Player' ในซีน!");
        }
    }

    void Update()
    {
        // อัปเดตตำแหน่งเป้าหมายทุกเฟรมเพื่อให้เดินตามตลอดเวลา
        if (playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }
}
