using UnityEngine;
using Unity.Cinemachine;

public class SpectatorController : MonoBehaviour
{
    public static SpectatorController Instance { get; private set; }

    [Header("Spectator Camera")]
    public CinemachineCamera spectatorVcam;

    [Header("Movement Settings")]
    public float normalSpeed = 10f;
    public float fastSpeed = 25f;
    public float verticalSpeed = 8f;
    public float smoothTime = 0.08f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;

    private bool isSpectating;
    private Vector3 velocity;
    private float yaw;
    private float pitch;
    private Transform camTransform;

    private const int SPECTATOR_PRIORITY = 30;
    private const int INACTIVE_PRIORITY = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        camTransform = spectatorVcam != null ? spectatorVcam.transform : transform;

        if (spectatorVcam != null)
            spectatorVcam.Priority.Value = INACTIVE_PRIORITY;
    }

    private void OnEnable()
    {
        GameManager.OnPhaseChangedGlobal += OnPhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal += OnPhaseChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
        SoloGameManager.OnPhaseChangedGlobal -= OnPhaseChanged;
    }

    public void EnterSpectate(Transform deadPlayerTransform = null)
    {
        if (isSpectating) return;
        isSpectating = true;

        if (deadPlayerTransform != null)
        {
            camTransform.position = deadPlayerTransform.position + Vector3.up * 2f;
            camTransform.rotation = deadPlayerTransform.rotation;
        }

        yaw = camTransform.eulerAngles.y;
        pitch = camTransform.eulerAngles.x;

        if (CameraManager.Instance != null)
            CameraManager.Instance.SetSpectatorActive(true);
        else if (spectatorVcam != null)
            spectatorVcam.Priority.Value = SPECTATOR_PRIORITY;

        if (GameManager.Instance != null)
            GameManager.Instance.SetCursorMode(true);
        else if (SoloGameManager.Instance != null)
            SoloGameManager.Instance.ApplyCursorState(false);
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log("<color=cyan>[Spectator]</color> เข้าโหมด Spectate แล้ว");
    }

    public void ExitSpectate()
    {
        if (!isSpectating) return;
        isSpectating = false;

        if (CameraManager.Instance != null)
            CameraManager.Instance.SetSpectatorActive(false);
        else if (spectatorVcam != null)
            spectatorVcam.Priority.Value = INACTIVE_PRIORITY;

        if (CameraManager.Instance != null && GameManager.Instance != null)
        {
            CameraManager.Instance.SetPhaseCamera(GameManager.Instance.CurrentPhase);
            GameManager.Instance.SetCursorMode(false);
        }
        else if (CameraManager.Instance != null && SoloGameManager.Instance != null)
        {
            CameraManager.Instance.SetPhaseCamera(SoloGameManager.Instance.CurrentPhase);
            SoloGameManager.Instance.ApplyCursorState(true);
        }

        Debug.Log("<color=cyan>[Spectator]</color> ออกจากโหมด Spectate แล้ว");
    }

    public bool IsSpectating => isSpectating;

    /// <summary>
    /// อัปเดต yaw/pitch ให้ตรงกับ rotation ปัจจุบันของกล้อง 
    /// (ใช้ตอนโดนบังคับเปลี่ยนมุมกล้องจากภายนอก เพื่อไม่ให้เมาส์ดีด)
    /// </summary>
    public void SyncViewFromTransform()
    {
        yaw = camTransform.eulerAngles.y;
        pitch = camTransform.eulerAngles.x;
        // ปรับ pitch ให้เป็นช่วง -180 ถึง 180 เพื่อให้ Clamp ทำงานถูก
        if (pitch > 180) pitch -= 360;
    }

    /// <summary>
    /// บังคับกล้องไปที่มุมมองเริ่มต้นตามที่ระบุในภาพ
    /// </summary>
    public void ResetToInitialView()
    {
        if (spectatorVcam != null)
        {
            spectatorVcam.Follow = null;
            spectatorVcam.LookAt = null;
        }

        camTransform.position = new Vector3(17.27f, 14.92f, -29.08f);
        camTransform.rotation = Quaternion.Euler(16.59f, 180f, 0f);

        SyncViewFromTransform();
        
        if (!isSpectating)
        {
            EnterSpectate();
        }
    }

    private void Update()
    {
        if (!isSpectating) return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float upDown = 0f;
        if (Input.GetKey(KeyCode.Space)) upDown = 1f;
        else if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space)) upDown = -1f;

        Vector3 forward = camTransform.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = camTransform.right; right.y = 0f; right.Normalize();

        Vector3 targetVelocity = (forward * v + right * h) * speed
                               + Vector3.up * upDown * verticalSpeed;

        velocity = Vector3.Lerp(velocity, targetVelocity,
                                1f - Mathf.Exp(-Time.deltaTime / smoothTime));
        camTransform.position += velocity * Time.deltaTime;

        // ✅ Clamp ไม่ให้ออกนอกขอบ Terrain
        camTransform.position = ClampToTerrain(camTransform.position);
    }

    private Vector3 ClampToTerrain(Vector3 pos)
    {
        var terrain = Terrain.activeTerrain;
        if (terrain == null) return pos;

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        pos.x = Mathf.Clamp(pos.x, origin.x, origin.x + size.x);
        pos.z = Mathf.Clamp(pos.z, origin.z, origin.z + size.z);
        pos.y = Mathf.Clamp(pos.y, origin.y + 10f, origin.y + 150f);

        return pos;
    }

    private void OnPhaseChanged(GamePhase newPhase)
    {
        if (!isSpectating) return;

        ExitSpectate();
    }
}