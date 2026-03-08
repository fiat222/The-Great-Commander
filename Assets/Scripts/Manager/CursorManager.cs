using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    private bool isCursorLocked = true;

    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        // Toggle cursor with G key
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            ToggleCursor();
        }
    }

    void ToggleCursor()
    {
        isCursorLocked = !isCursorLocked;

        if (isCursorLocked)
            LockCursor();
        else
            UnlockCursor();
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
