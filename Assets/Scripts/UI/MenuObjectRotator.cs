using UnityEngine;

/// <summary>
/// หมุน Object ตลอดเวลา ใช้สำหรับ Decorative Object บนหน้า Main Menu
/// </summary>
public class MenuObjectRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("ความเร็วการหมุนในแต่ละแกน (องศา/วินาที)")]
    public Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);

    [Header("Floating Effect (Optional)")]
    [Tooltip("เปิด/ปิด เอฟเฟกต์ลอยขึ้น-ลง")]
    public bool enableFloating = true;
    [Tooltip("ความสูงที่ลอยขึ้น-ลง")]
    public float floatAmplitude = 0.1f;
    [Tooltip("ความเร็วการลอย")]
    public float floatSpeed = 1f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // หมุนรอบแกนที่กำหนด
        transform.Rotate(rotationSpeed * Time.deltaTime);

        // เอฟเฟกต์ลอยขึ้น-ลง
        if (enableFloating)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }
}
