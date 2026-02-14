using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownCameraController : MonoBehaviour
{
    public float moveSpeed = 200f;
    public float zoomSpeed = 8000f;
    public float rotateSpeed = 100f;

    public float minY = 10f;
    public float maxY = 60f;

    public Vector2 minBounds;
    public Vector2 maxBounds;

    void Update()
    {
        // ---------- PAN (Middle Mouse Drag) ----------
        if (Mouse.current.middleButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // ทำให้เลื่อนตามพื้นเสมอ
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 move =
                (-right * mouseDelta.x + -forward * mouseDelta.y)
                * moveSpeed * 0.02f * Time.deltaTime;

            transform.position += move;
        }

        // ---------- ZOOM (Scroll) ----------
        float scroll = Mouse.current.scroll.ReadValue().y;

        Vector3 pos = transform.position;
        pos.y -= scroll * zoomSpeed * 0.01f * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;

        // ---------- ROTATE (Q / E) ----------
        if (Keyboard.current.qKey.isPressed)
            transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime, Space.World);

        if (Keyboard.current.eKey.isPressed)
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // ---------- LIMIT AREA ----------
        pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.z = Mathf.Clamp(pos.z, minBounds.y, maxBounds.y);
        transform.position = pos;
    }
}
