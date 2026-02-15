using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private int damage;
    private Transform towerTransform; // เก็บตำแหน่งป้อม
    private float towerRange;          // เก็บระยะยิงของป้อม

    public float speed = 30f;

    // รับค่าเพิ่ม: ตำแหน่งป้อม และ ระยะยิง
    public void Seek(Transform _target, int _damage, Transform _tower, float _range)
    {
        target = _target;
        damage = _damage;
        towerTransform = _tower;
        towerRange = _range;
    }

    void Update()
    {
        // ถ้าเป้าหมายหายไป ให้ลบกระสุนทิ้ง
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // เช็กว่าศัตรูวิ่งออกจากวงยิงของป้อมยัง
        if (towerTransform != null)
        {
            float distanceToTower = Vector3.Distance(towerTransform.position, target.position);
            if (distanceToTower > towerRange)
            {
                Debug.Log("ศัตรูออกจากระยะ");
                Destroy(gameObject); // หยุดยิงและทำลายลูกกระสุน
                return;
            }
        }

        // การเคลื่อนที่ตามปกติ
        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
    }

    void HitTarget()
    {
        if (target != null)
        {
            BaseHealth hp = target.GetComponent<BaseHealth>();
            if (hp != null) hp.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}