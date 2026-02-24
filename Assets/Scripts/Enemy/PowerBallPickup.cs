using UnityEngine;


public class PowerBallPickup : MonoBehaviour
{
    [Header("Value")]
    [Tooltip("เงินที่จะได้เมื่อเก็บ PowerBall 1 ลูก")]
    public int value = 1;

    [Header("Lifetime")]
    [Tooltip("เวลาก่อนที่ PowerBall จะหายไป (sec)")]
    public float lifetime = 15f;
    [Tooltip("กี่วินาทีก่อนหมดเวลา ถ้าไม่เก็บจะลอยเข้าหาผู้เล่นอัตโนมัติ")]
    public float autoCollectTime = 3f;

    [Header("Float Animation")]
    public float floatAmplitude = 0.15f;
    public float floatSpeed = 2f;

    [Header("Magnet")]
    [Tooltip("ระยะที่ผู้เล่นต้องเดินเข้ามาเองเพื่อดูด")]
    public float attractRadius = 3f;
    [Tooltip("ความเร็วดูดปกติ")]
    public float attractSpeed = 8f;

    [Header("Auto Collect")]
    [Tooltip("ความเร็วลอยเข้าหาผู้เล่นตอน auto collect")]
    public float autoCollectSpeed = 14f;

    private Transform playerTransform;
    private Vector3 startPosition;
    private float elapsed = 0f;
    private bool isAutoCollecting = false;

    void Start()
    {
        // ปิด gravity ให้ Rigidbody (ถ้ามี) เพื่อไม่ให้จมพื้น — script จะคุม movement เองทั้งหมด
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // เริ่มที่ตำแหน่งปัจจุบัน + ยกขึ้นอีกเล็กน้อยเพื่อไม่ให้ชิดพื้น
        startPosition = transform.position + Vector3.up * floatAmplitude;
        transform.position = startPosition;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // หมดเวลาแบบ manual destroy
        if (elapsed >= lifetime)
        {
            // ถ้าไม่มีผู้เล่น ก็ทำลายทิ้งตามปกติ
            if (playerTransform == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        // เริ่ม Auto Collect เมื่อเหลือเวลา autoCollectTime วินาที
        if (!isAutoCollecting && elapsed >= lifetime - autoCollectTime)
        {
            isAutoCollecting = true;
        }

        if (playerTransform == null) return;

        if (isAutoCollecting)
        {
            // ลอยเข้าหาผู้เล่น
            transform.position = Vector3.MoveTowards(
                transform.position,
                playerTransform.position,
                autoCollectSpeed * Time.deltaTime
            );

            // ถึงตัวผู้เล่นแล้วให้เก็บ
            if (Vector3.Distance(transform.position, playerTransform.position) < 0.5f)
            {
                Collect();
            }
        }
        else
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distToPlayer <= attractRadius)
            {
                // โหมดดูด
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTransform.position,
                    attractSpeed * Time.deltaTime
                );
            }
            else
            {
                // โหมดลอย
                float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    private void Collect()
    {
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.Money += value;
            PlacementManager.Instance.OnMoneyChanged?.Invoke(PlacementManager.Instance.Money);
        }

        Destroy(gameObject);
    }
}
