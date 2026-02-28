using UnityEngine;

public class FloatingRotate : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatAmplitude = 0.5f;   // ความสูงที่ลอยขึ้นลง
    public float floatSpeed = 2f;         // ความเร็วการลอย

    [Header("Rotation Settings")]
    public float rotationSpeed = 90f;     // ความเร็วการหมุน (องศาต่อวินาที)
    public Vector3 rotationAxis = Vector3.up; // แกนที่หมุน

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // ลอยขึ้นลงด้วย Sin wave
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // หมุน
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}