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

    [Header("Ground Detection")]
    [Tooltip("Layer ของพื้น/Terrain ที่ใช้ Raycast หาความสูง (ต้องเลือกเฉพาะ Ground layer เท่านั้น)")]
    public LayerMask groundLayer;

    void Awake()
    {
        Instance = this;
    }

    public static void Drop(Vector3 pos, int amount)
    {
        if (Instance == null || Instance.powerBallPrefab == null || amount <= 0) return;

        // Raycast จากด้านบนสูงๆ ลงมา เพื่อหาพื้นจริง
        // ใช้ LayerMask groundLayer เพื่อไม่ชน enemy collider หรือ VFX
        float groundY = 0f; // fallback เป็น y=0
        Vector3 rayOrigin = new Vector3(pos.x, pos.y + 50f, pos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, Instance.groundLayer))
        {
            groundY = hit.point.y;
        }
        else
        {
            // ถ้า Raycast ไม่โดน (groundLayer ไม่ได้ตั้ง) ให้ fallback หา collider ทุก layer
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hitFallback, 100f))
                groundY = hitFallback.point.y;
        }

        for (int i = 0; i < amount; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * Instance.scatterRadius;
            Vector3 spawnPos = new Vector3(pos.x + scatter.x, groundY + 0.5f, pos.z + scatter.y);
            Instantiate(Instance.powerBallPrefab, spawnPos, Quaternion.identity);
        }
    }
}

