using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Phase Cameras")]
    [SerializeField] private CinemachineCamera planningCamera;
    [SerializeField] private CinemachineCamera freelookCamera;
    [SerializeField] private CinemachineCamera targetLockCamera;

    // ⭐ เพิ่ม: ลาก SpectatorCamera Vcam มาใส่ตรงนี้
    [Header("Spectator Camera")]
    [SerializeField] private CinemachineCamera spectatorCamera;

    public CinemachineCamera TargetLockCamera => targetLockCamera;

    [Header("Priority Settings")]
    [SerializeField] private int activePriority   = 20;
    [SerializeField] private int inactivePriority = 10;

    // Spectator ต้องสูงกว่า TargetLock (activePriority+5) เสมอ
    private const int SPECTATOR_PRIORITY = 30;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            SetPhaseCamera(GameManager.Instance.CurrentPhase);
        else if (SoloGameManager.Instance != null)
            SetPhaseCamera(SoloGameManager.Instance.CurrentPhase);
    }

    private void OnEnable()
    {
        // ⭐ Global override: ให้ Cinemachine อ่านค่าผ่านฟังก์ชันของเรา เพื่อให้เราสั่งหยุดหมุนได้ตอนปลดล็อกเมาส์
        CinemachineCore.GetInputAxis = HandleCinemachineInput;
    }

    private void OnDisable()
    {
        CinemachineCore.GetInputAxis = null;
    }

    private float HandleCinemachineInput(string axisName)
    {
        // ถ้าเมาส์ไม่ได้ถูกล็อก (เช่น กด Esc หรืออยู่ในเฟส Planning) ให้คืนค่า 0 เพื่อไม่ให้กล้องหมุน
        if (Cursor.lockState != CursorLockMode.Locked)
            return 0;

        return Input.GetAxis(axisName);
    }

    private void Update()
    {
        bool isLocked = Cursor.lockState == CursorLockMode.Locked;
        
        // --- ส่วนที่ 1: บล็อก InputProvider ทั่วทั้งซีน (เพื่อความชัวร์ที่สุด) ---
        // บางครั้ง InputProvider อาจจะไม่ได้อยู่ที่ตัวกล้องโดยตรง แต่อยู่ที่ Brain หรือ Player
        var allProviders = Object.FindObjectsByType<CinemachineInputProvider>(FindObjectsSortMode.None);
        foreach (var provider in allProviders)
        {
            if (provider.enabled != isLocked)
            {
                provider.enabled = isLocked;
            }
        }

        // --- ส่วนที่ 2: บล็อก Legacy Axes (ผ่าน HandleCinemachineInput ที่เรา override ไว้ใน OnEnable) ---
        // โค้ดส่วนนี้ทำงานอัตโนมัติผ่าน OnEnable/OnDisable
    }

    public void RegisterPlayerCameras(CinemachineCamera freeLook, CinemachineCamera targetLock)
    {
        this.freelookCamera    = freeLook;
        this.targetLockCamera  = targetLock;
        
        if (GameManager.Instance != null)
            SetPhaseCamera(GameManager.Instance.CurrentPhase);
        else if (SoloGameManager.Instance != null)
            SetPhaseCamera(SoloGameManager.Instance.CurrentPhase);
    }

    /// <summary>⭐ เปิด/ปิด Spectator Camera — เรียกจาก SpectatorController</summary>
    public void SetSpectatorActive(bool active)
    {
        if (spectatorCamera == null)
        {
            Debug.LogWarning("[CameraManager] ยังไม่ได้ลาก Spectator Camera มาใส่ Inspector!");
            return;
        }

        if (active)
        {
            // ยก Spectator ขึ้นสูงสุด ให้ Brain เลือกตัวนี้
            spectatorCamera.Priority.Value = SPECTATOR_PRIORITY;
        }
        else
        {
            // ปิด Spectator กลับไปต่ำสุด
            spectatorCamera.Priority.Value = inactivePriority;
        }
    }

    /// <summary>
    /// โฟกัสกล้อง Spectator ไปที่เป้าหมาย แล้วเปิดใช้งานด้วย Priority สูงสุด
    /// ใช้สำหรับตอนป้อมพัง / จบเกม เพื่อให้กล้องล็อคมุมมองแบบ Cinematic
    /// </summary>
    /// <param name="target">Transform ของวัตถุที่ต้องการให้กล้องโฟกัส</param>
    public void FocusSpectator(Transform target)
    {
        if (spectatorCamera == null)
        {
            Debug.LogWarning("[CameraManager] FocusSpectator ถูกเรียก แต่ยังไม่ได้เซ็ต spectatorCamera");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[CameraManager] FocusSpectator ถูกเรียก แต่ target เป็น null");
            return;
        }

        spectatorCamera.Follow = target;
        spectatorCamera.LookAt = target;
        SetSpectatorActive(true);
    }

    public void SetPhaseCamera(GamePhase phase)
    {
        ResetAllPriorities();

        if (phase == GamePhase.Planning)
        {
            if (planningCamera != null) planningCamera.Priority.Value = activePriority;
        }
        else if (phase == GamePhase.Combat)
        {
            if (freelookCamera != null) freelookCamera.Priority.Value = activePriority;
        }
    }

    public void SetTargetLock(bool isLockedOn)
    {
        if (isLockedOn)
        {
            if (targetLockCamera != null) targetLockCamera.Priority.Value = activePriority + 5;
            if (freelookCamera    != null) freelookCamera.Priority.Value   = activePriority;
        }
        else
        {
            if (targetLockCamera != null) targetLockCamera.Priority.Value = inactivePriority;
            if (freelookCamera    != null) freelookCamera.Priority.Value   = activePriority;
        }
    }

    private void ResetAllPriorities()
    {
        if (planningCamera    != null) planningCamera.Priority.Value    = inactivePriority;
        if (freelookCamera    != null) freelookCamera.Priority.Value    = inactivePriority;
        if (targetLockCamera  != null) targetLockCamera.Priority.Value  = inactivePriority;
        // ไม่ Reset spectatorCamera ที่นี่ — ให้ SetSpectatorActive จัดการเอง
    }
}