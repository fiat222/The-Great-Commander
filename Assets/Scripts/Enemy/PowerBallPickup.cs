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

    [Header("Launch)")]
    [Tooltip("สูงแค่ไหน")]
    public float launchUpSpeed = 5f;
    [Tooltip("กระจายออกด้านข้าง")]
    public float launchSideSpeed = 3f;
    [Tooltip("ตกเร็วแค่ไหน")]
    public float gravity = 12f;

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
    [Tooltip("ความสูงเหนือตำแหน่ง Player ที่ลูกบอลจะบินเข้าหา (กึ่งกลางตัวผู้เล่น)")]
    public float collectHeight = 1f;

    // ── state ──
    private Transform playerTransform;
    private Vector3 startPosition;     // ตำแหน่งลอยหลังลงพื้นแล้ว
    private float elapsed = 0f;
    private bool isAutoCollecting = false;

    // ── launch ──
    private bool isLaunching = true;
    private Vector3 launchVelocity;
    private float groundY;

    void Start()
    {
        // ปิด Rigidbody gravity — script คุม movement เองทั้งหมด
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.useGravity = false; rb.isKinematic = true; }

        // ใช้ตำแหน่ง Y ปัจจุบันเป็น groundY 
        groundY = transform.position.y;

        // สุ่มทิศทาง launch ออกด้านข้าง
        Vector2 side = Random.insideUnitCircle.normalized * launchSideSpeed;
        launchVelocity = new Vector3(side.x, launchUpSpeed, side.y);

        // หาผู้เล่น
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        // Launch
        if (isLaunching)
        {
            launchVelocity.y -= gravity * Time.deltaTime;
            transform.position += launchVelocity * Time.deltaTime;

            // ออกจาก launch เฉพาะตอนกำลังตกลงมา (velocity.y < 0)
            // ถ้าไม่เช็ค velocity ลูกบอลจะหยุดทันทีตอนพุ่งขึ้นผ่าน groundY
            if (launchVelocity.y < 0 && transform.position.y <= groundY + floatAmplitude)
            {
                startPosition = new Vector3(transform.position.x, groundY + floatAmplitude, transform.position.z);
                transform.position = startPosition;
                isLaunching = false;
            }
            return; // ยังอยู่ launch phase ไม่นับเวลา lifetime
        }

        // Float / Attract / AutoCollect
        elapsed += Time.deltaTime;

        if (elapsed >= lifetime)
        {
            if (playerTransform == null) { Destroy(gameObject); return; }
        }

        if (!isAutoCollecting && elapsed >= lifetime - autoCollectTime)
            isAutoCollecting = true;

        if (playerTransform == null) return;

        // จุดเป้าหมายที่กึ่งกลางตัวผู้เล่น (ไม่ใช่ที่เท้า)
        Vector3 collectTarget = playerTransform.position + Vector3.up * collectHeight;

        if (isAutoCollecting)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, collectTarget, autoCollectSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, collectTarget) < 0.5f)
                Collect();
        }
        else
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distToPlayer <= attractRadius)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, collectTarget, attractSpeed * Time.deltaTime);
            }
            else
            {
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
