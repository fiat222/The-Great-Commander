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
    }

    public void RegisterPlayerCameras(CinemachineCamera freeLook, CinemachineCamera targetLock)
    {
        this.freelookCamera    = freeLook;
        this.targetLockCamera  = targetLock;
        if (GameManager.Instance != null)
            SetPhaseCamera(GameManager.Instance.CurrentPhase);
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