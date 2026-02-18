using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float gravity = 5f;
    public float lifetime = 5f;

    private float speed;
    private int damage;

    private Vector3 velocity;
    private bool isFlying = false;

    private readonly Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f);

    public void Launch(Vector3 direction, float launchSpeed, int launchDamage)
    {
        speed = launchSpeed;
        damage = launchDamage;

        velocity = direction.normalized * speed;
        isFlying = true;

        transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;

        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity) * rotationOffset;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;
        if (other.CompareTag("Enemy")) return;

        isFlying = false;

        // ตัวอย่างถ้าจะใช้ damage
        // other.GetComponent<EnemyHealth>()?.TakeDamage(damage);

        transform.SetParent(other.transform);
        GetComponent<Collider>().enabled = false;

        Destroy(gameObject, 3f);
    }
}
