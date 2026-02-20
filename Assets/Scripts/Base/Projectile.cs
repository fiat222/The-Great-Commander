using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("VFX Settings")]
    public GameObject hitVFXPrefab;
    public float speed = 35f;

    private Transform target;
    private Transform towerTransform;
    private float towerRange;
    private int damage;
    private float distanceTraveled = 0f;

    public void Seek(Transform _target, int _damage, Transform _tower, float _range)
    {
        target = _target;
        damage = _damage;
        towerTransform = _tower;
        towerRange = _range;
        distanceTraveled = 0f;
    }

    void Update()
    {
        if (target == null || towerTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        // เช็กว่าเป้าหมายอยุ่ในขอบเขตไหม
        float distToTower = Vector3.Distance(towerTransform.position, target.position);
        if (distToTower > towerRange)
        {
            Destroy(gameObject);
            return;
        }

        // ล็อกตำแหน่งบนเส้นตรงระหว่างป้อมกับเป้าหมาย
        distanceTraveled += speed * Time.deltaTime;
        float progress = distanceTraveled / distToTower;

        transform.position = Vector3.Lerp(towerTransform.position, target.position, progress);

        if (progress >= 1.0f)
        {
            HitTarget();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // เลือกชนเฉพาะเป้าหมาย
        if (other.CompareTag("Enemy"))
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (target != null)
        {
            BaseHealth hp = target.GetComponent<BaseHealth>();
            if (hp != null) hp.TakeDamage(damage);

            if (hitVFXPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitVFXPrefab, target.position, Quaternion.identity);
                Destroy(hitEffect, 2f);
            }
        }
        Destroy(gameObject);
    }
}