using UnityEngine;

public class PowerBallDropper : MonoBehaviour
{
    public static PowerBallDropper Instance { get; private set; }

    [Header("PowerBall Prefab")]
    [Tooltip("ลาก PowerBall Prefab ที่มี PowerBallPickup.cs มาใส่ช่องนี้")]
    public GameObject powerBallPrefab;

    [Header("Spawn Scatter")]
    [Tooltip("รัศมีการกระจายของ PowerBall ที่ drop ออกมา")]
    public float scatterRadius = 0.6f;

    void Awake()
    {
        Instance = this;
    }

 
    // เรียกจาก EnemyAI เพื่อ drop PowerBall จำนวน xx ตัว ณ ตำแหน่ง pos
  
    public static void Drop(Vector3 pos, int amount)
    {
        if (Instance == null || Instance.powerBallPrefab == null || amount <= 0) return;

        // Raycast หาพื้นจริงๆ ที่ตำแหน่ง XZ ของ enemy (กันปัญหา enemy จมพื้นตอนตาย)
        float groundY = pos.y;
        if (Physics.Raycast(new Vector3(pos.x, pos.y + 5f, pos.z), Vector3.down, out RaycastHit hit, 15f))
        {
            groundY = hit.point.y;
        }

        for (int i = 0; i < amount; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * Instance.scatterRadius;
            Vector3 spawnPos = new Vector3(pos.x + scatter.x, groundY + 0.5f, pos.z + scatter.y);
            Instantiate(Instance.powerBallPrefab, spawnPos, Quaternion.identity);
        }
    }
}
