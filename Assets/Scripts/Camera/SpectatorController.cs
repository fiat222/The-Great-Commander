using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// ระบบ Spectator สไตล์ Minecraft
/// ใส่ Script นี้บน GameObject ใหม่ (เช่น "SpectatorCamera") ที่มี CinemachineCamera แนบอยู่
/// 
/// การทำงาน:
///   - เมื่อ Player ตาย → เรียก SpectatorController.Instance.EnterSpectate(playerTransform)
///   - เมื่อขึ้นเฟสใหม่ (Combat) → Spectate จะหยุดอัตโนมัติผ่าน GameManager.OnPhaseChangedGlobal
/// </summary>
public class SpectatorController : MonoBehaviour
{
    public static SpectatorController Instance { get; private set; }

    [Header("Spectator Camera")]
    [Tooltip("CinemachineCamera ที่ใช้ตอน Spectate — CameraManager จะจัดการ Priority ให้")]
    public CinemachineCamera spectatorVcam;

    [Header("Movement Settings")]
    public float normalSpeed = 10f;
    public float fastSpeed = 25f;   // กด Shift
    public float verticalSpeed = 8f;
    public float smoothTime = 0.08f; // ความลื่นของการเคลื่อนที่

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private bool isSpectating;
    private Vector3 velocity;
    private float yaw;
    private float pitch;

    // ── Cached refs ──────────────────────────────────────────────────────────
    private Transform camTransform;

    // Priority ที่ใช้ตอน Spectate — สูงกว่า TargetLock (activePriority+5=25) ใน CameraManager
    private const int SPECTATOR_PRIORITY = 30;
    private const int INACTIVE_PRIORITY = 0;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        camTransform = spectatorVcam != null ? spectatorVcam.transform : transform;

        // ซ่อน Spectator Vcam ตั้งต้น
        if (spectatorVcam != null) spectatorVcam.Priority.Value = INACTIVE_PRIORITY;
    }

    private void OnEnable()
    {
        // ฟัง Event เปลี่ยนเฟสจาก GameManager
        GameManager.OnPhaseChangedGlobal += OnPhaseChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>เรียกตอน Player ตาย — ส่ง transform ของ Player ที่ตายมาเพื่อเริ่ม Spectate ณ จุดนั้น</summary>
    public void EnterSpectate(Transform deadPlayerTransform = null)
    {
        if (isSpectating) return;
        isSpectating = true;

        // วาง Camera ไว้ที่ตำแหน่ง Player ที่ตาย
        if (deadPlayerTransform != null)
        {
            camTransform.position = deadPlayerTransform.position + Vector3.up * 2f;
            camTransform.rotation = deadPlayerTransform.rotation;
        }

        // อ่าน rotation ปัจจุบันเพื่อต่อเนื่องจาก Camera เดิม
        yaw = camTransform.eulerAngles.y;
        pitch = camTransform.eulerAngles.x;

        // บอก CameraManager ให้ปิด Camera อื่นๆ ก่อน แล้วยก Spectator ขึ้น
        // (Priority 30 > TargetLock 25 > Freelook 20 ใน CameraManager)
        if (CameraManager.Instance != null) CameraManager.Instance.SetSpectatorActive(true);
        else if (spectatorVcam != null) spectatorVcam.Priority.Value = SPECTATOR_PRIORITY;

        // บอก GameManager ว่าเราเข้าโหมดที่ต้องการล็อคเมาส์
        if (GameManager.Instance != null) GameManager.Instance.SetCursorMode(true);
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

        Debug.Log("<color=cyan>[Spectator]</color> เข้าโหมด Spectate แล้ว");
    }

    /// <summary>เรียกตอนออกจาก Spectate (Respawn / ขึ้นเฟสใหม่)</summary>
    public void ExitSpectate()
    {
        if (!isSpectating) return;
        isSpectating = false;

        // ปิด Spectator Vcam ผ่าน CameraManager
        if (CameraManager.Instance != null) CameraManager.Instance.SetSpectatorActive(false);
        else if (spectatorVcam != null) spectatorVcam.Priority.Value = INACTIVE_PRIORITY;

        // คืน Camera ให้ตรงกับเฟสปัจจุบัน และบอก GameManager ว่าเราออกจากความต้องการล็อคเมาส์
        if (CameraManager.Instance != null && GameManager.Instance != null)
        {
            CameraManager.Instance.SetPhaseCamera(GameManager.Instance.CurrentPhase);
            GameManager.Instance.SetCursorMode(false);
        }

        Debug.Log("<color=cyan>[Spectator]</color> ออกจากโหมด Spectate แล้ว");
    }

    public bool IsSpectating => isSpectating;

    // ─────────────────────────────────────────────────────────────────────────
    //  Update — WASD + Space/Shift + Mouse Look
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isSpectating) return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        // ป้องกันกล้องหมุนเมื่อเมาส์ถูกปลดล็อก (เช่น ตอนกด Esc)
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        camTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;

        // แกนนอน: WASD
        float h = Input.GetAxisRaw("Horizontal");  // A/D
        float v = Input.GetAxisRaw("Vertical");    // W/S

        // แกนตั้ง: Space = ขึ้น, Shift (อย่างเดียว) = ลง
        float upDown = 0f;
        if (Input.GetKey(KeyCode.Space)) upDown = 1f;
        else if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space)) upDown = -1f;

        // คำนวณทิศทางตาม Camera
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;

        // ล็อกแกน Y สำหรับการเดินหน้า/ข้าง ให้ลอยได้อิสระ
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 targetVelocity = (forward * v + right * h) * speed
                                + Vector3.up * upDown * verticalSpeed;

        // Smooth
        velocity = Vector3.Lerp(velocity, targetVelocity, 1f - Mathf.Exp(-Time.deltaTime / smoothTime));

        camTransform.position += velocity * Time.deltaTime;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnPhaseChanged(GamePhase newPhase)
    {
        if (!isSpectating) return;

        // ออกจาก Spectate ทั้ง Combat (respawn) และ Planning (ขึ้นเฟสใหม่)
        ExitSpectate();

        // ถ้ากลับมา Planning → บังคับ CameraManager สลับไป planningCamera ทันที
        if (newPhase == GamePhase.Planning && CameraManager.Instance != null)
            CameraManager.Instance.SetPhaseCamera(GamePhase.Planning);
    }
}