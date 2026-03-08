using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// ใส่ Script นี้บน GameObject เดียวกับ CinemachineCamera ของ Freelook
/// ปิด Cinemachine mouse input เมื่อ:
///   - แชทเปิดอยู่
///   - ผู้เล่นกด Esc ปลดล็อคเมาส์
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CameraInputBlocker : MonoBehaviour
{
    private CinemachineInputAxisController inputAxisController;

    private void Awake()
    {
        inputAxisController = GetComponent<CinemachineInputAxisController>();
    }

    private void Update()
    {
        if (inputAxisController == null) return;

        bool chatOpen     = ChatManager.Instance != null && ChatManager.Instance.IsChatOpen;
        bool mouseUnlocked = GameManager.Instance != null && GameManager.Instance.IsManualUnlock;

        inputAxisController.enabled = !chatOpen && !mouseUnlocked;
    }
}