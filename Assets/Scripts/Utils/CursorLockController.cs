using UnityEngine;

public class CursorLockController : MonoBehaviour
{
    [Header("Settings")]
    public bool lockOnStart = true;
    public KeyCode unlockKey = KeyCode.Escape;

    private bool isLocked = false;

    void Start()
    {
        if (lockOnStart)
        {
            SetCursorLock(true);
        }
    }

    void Update()
    {
        // กดปุ่ม Escape เพื่อปลดล็อคเมาส์ (หรือล็อคกลับ)
        if (Input.GetKeyDown(unlockKey))
        {
            SetCursorLock(!isLocked);
        }

        // ถ้าคลิกซ้ายในจอขณะที่เป็นอิสระ ให้กลับมาล็อคใหม่ (สะดวกตอนเทสครับ)
        if (Input.GetMouseButtonDown(0) && !isLocked)
        {
            SetCursorLock(true);
        }
    }

    public void SetCursorLock(bool value)
    {
        isLocked = value;

        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("<color=cyan>[Cursor]</color> Mouse Locked (Press Esc to unlock)");
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("<color=yellow>[Cursor]</color> Mouse Unlocked");
        }
    }
}
