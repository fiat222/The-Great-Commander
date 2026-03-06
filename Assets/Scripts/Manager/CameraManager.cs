using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Phase Cameras")]
    [SerializeField] private CinemachineCamera planningCamera;
    [SerializeField] private CinemachineCamera freelookCamera;
    [SerializeField] private CinemachineCamera targetLockCamera;

    public CinemachineCamera TargetLockCamera => targetLockCamera;

    [Header("Priority Settings")]
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // Sync initial state incase OnNetworkSpawn triggered before CameraManager was ready
            SetPhaseCamera(GameManager.Instance.CurrentPhase);
        }
    }

    public void RegisterPlayerCameras(CinemachineCamera freeLook, CinemachineCamera targetLock)
    {
        this.freelookCamera = freeLook;
        this.targetLockCamera = targetLock;
        if (GameManager.Instance != null)
        {
            SetPhaseCamera(GameManager.Instance.CurrentPhase);
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
            // Default to freelook when entering combat
            if (freelookCamera != null) freelookCamera.Priority.Value = activePriority;
        }
    }

    public void SetTargetLock(bool isLockedOn)
    {
        if (isLockedOn)
        {
            if (targetLockCamera != null) targetLockCamera.Priority.Value = activePriority + 5; // Higher than freelook
            if (freelookCamera != null) freelookCamera.Priority.Value = activePriority;
        }
        else
        {
            if (targetLockCamera != null) targetLockCamera.Priority.Value = inactivePriority;
            if (freelookCamera != null) freelookCamera.Priority.Value = activePriority;
        }
    }

    private void ResetAllPriorities()
    {
        if (planningCamera != null) planningCamera.Priority.Value = inactivePriority;
        if (freelookCamera != null) freelookCamera.Priority.Value = inactivePriority;
        if (targetLockCamera != null) targetLockCamera.Priority.Value = inactivePriority;
    }
}
