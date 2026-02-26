using UnityEngine;

/// <summary>
/// ตัวกลางสำหรับ spawn VFX ทั่วทั้งเกม
/// วางไว้บน GameObject ชื่อ "Manager" หรือ "VFXManager" ใน Scene
///
/// วิธีใช้:
///   VFXManager.Instance?.Play(prefab, position);
///   VFXManager.Instance?.Play(prefab, position, lifetime: 2f);
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    void Awake()
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

    /// <summary>
    /// Spawn VFX prefab ที่ตำแหน่ง pos แล้ว Destroy อัตโนมัติหลัง lifetime วินาที
    /// ถ้า prefab เป็น null จะไม่ทำอะไรและไม่ error
    /// </summary>
    public void Play(GameObject prefab, Vector3 pos, float lifetime = 3f)
    {
        if (prefab == null) return;

        var fx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(fx, lifetime);
    }
}
