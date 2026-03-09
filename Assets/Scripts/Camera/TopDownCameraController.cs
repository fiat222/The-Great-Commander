using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class TopDownCameraController : MonoBehaviour
{
    [Header("Default Planning Position & Rotation")]
    public Vector3 defaultPosition = new Vector3(18f, 86.4f, -8.3f);
    public Vector3 defaultRotation = new Vector3(90f, -180f, 0f);

    [Header("Free Fly Settings")]
    public float flyNormalSpeed   = 10f;
    public float flyFastSpeed     = 25f;
    public float flyVerticalSpeed = 8f;
    public float flySmoothTime    = 0.08f;
    public float mouseSensitivity = 0.15f;

    // ==================== Runtime ====================
    public bool IsFreeFly => true; // ใช้ FreeFly เสมอ — CameraHintsUI อ่านค่านี้
    private Vector3 flyVelocity = Vector3.zero;
    private float   flyYaw;
    private float   flyPitch;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal += OnPhaseChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        // ⭐ เมื่อกลับมาเฟส Planning ให้รีเซ็ตตำแหน่งกล้อง
        if (phase == GamePhase.Planning)
        {
            ResetToDefault();
        }

        // เมื่อเปลี่ยนเฟส ให้คืน cursor ให้ GameManager จัดการ
        if (GameManager.Instance != null)
            GameManager.Instance.SetCursorMode(false);
    }

    public void ResetToDefault()
    {
        transform.position = defaultPosition;
        transform.rotation = Quaternion.Euler(defaultRotation);

        flyYaw   = defaultRotation.y;
        flyPitch = defaultRotation.x;
        flyVelocity = Vector3.zero;

        // ⭐ บังคับ Cinemachine ให้ Snap ทันที
        var vcam = GetComponent<CinemachineCamera>();
        if (vcam != null)
        {
            vcam.ForceCameraPosition(defaultPosition, Quaternion.Euler(defaultRotation));
        }
    }

    private void Start()
    {
        transform.position = defaultPosition;
        transform.rotation = Quaternion.Euler(defaultRotation);

        flyYaw   = defaultRotation.y;
        flyPitch = defaultRotation.x;

        if (GameManager.Instance != null)
            GameManager.Instance.SetCursorMode(true);
    }

    private void Update()
    {
        UpdateFreeFly();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Free Fly Mode
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateFreeFly()
    {
        // ---------- Ctrl: กลับจุดเริ่มต้น ----------
        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            transform.position = defaultPosition;
            transform.rotation = Quaternion.Euler(defaultRotation);
            flyYaw      = defaultRotation.y;
            flyPitch    = defaultRotation.x;
            flyVelocity = Vector3.zero;
            return;
        }

        // ---------- Mouse Look ----------
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            var delta = Mouse.current.delta.ReadValue();
            flyYaw   += delta.x * mouseSensitivity;
            flyPitch -= delta.y * mouseSensitivity;
            flyPitch  = Mathf.Clamp(flyPitch, -89f, 89f);
        }
        transform.rotation = Quaternion.Euler(flyPitch, flyYaw, 0f);

        // ---------- Movement ----------
        bool  shift = Keyboard.current.leftShiftKey.isPressed;
        float speed = shift ? flyFastSpeed : flyNormalSpeed;

        float h = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        float v = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                - (Keyboard.current.sKey.isPressed ? 1f : 0f);

        float upDown = 0f;
        if (Keyboard.current.spaceKey.isPressed)                        upDown =  1f;
        else if (shift && !Keyboard.current.spaceKey.isPressed)         upDown = -1f;

        Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = transform.right;   right.y   = 0f; right.Normalize();

        Vector3 targetVel = (forward * v + right * h) * speed
                          + Vector3.up * upDown * flyVerticalSpeed;

        flyVelocity = Vector3.Lerp(flyVelocity, targetVel,
                                   1f - Mathf.Exp(-Time.deltaTime / flySmoothTime));
        transform.position += flyVelocity * Time.deltaTime;
    }
}