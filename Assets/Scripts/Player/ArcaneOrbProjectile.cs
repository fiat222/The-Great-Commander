using UnityEngine;

public class ArcaneOrbProjectile : MonoBehaviour
{
    public float speed = 25f;
    public int damage = 15;
    public float lifeTime = 3f;
    public GameObject hitVFX;

    private void Start() => Destroy(gameObject, lifeTime);

    private void Update() => transform.Translate(Vector3.forward * speed * Time.deltaTime);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.isTrigger) return;

        // ทำดาเมจศัตรู
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<HealthSystem>()?.TakeDamage(damage);
            other.GetComponent<EnemyAI>()?.TakeDamage(damage, false);
        }

        if (hitVFX != null)
        {
            GameObject vfx = Instantiate(hitVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        Destroy(gameObject);
    }
}
