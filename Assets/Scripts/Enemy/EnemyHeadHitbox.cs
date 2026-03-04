using UnityEngine;

/// <summary>
/// ติดไว้บน child GameObject "HeadHitbox" ของ Enemy
/// Tag ของ GameObject นี้ต้องเป็น "EnemyHead"
/// Collider ของ GameObject นี้ต้องเป็น Trigger
/// </summary>
public class EnemyHeadHitbox : MonoBehaviour
{
    // reference กลับไปหา root ของ Enemy อัตโนมัติ
    // หรือจะลาก assign เองใน Inspector ก็ได้
    [Tooltip("ถ้าไม่ได้ assign จะหา HealthSystem / EnemyAI จาก parent อัตโนมัติ")]
    public GameObject enemyRoot;

    private void Awake()
    {
        if (enemyRoot == null)
            enemyRoot = transform.root.gameObject;
    }

    /// <summary>คืน root GameObject ของ enemy สำหรับให้ Arrow/Hitbox หา component</summary>
    public GameObject GetEnemyRoot() => enemyRoot;
}