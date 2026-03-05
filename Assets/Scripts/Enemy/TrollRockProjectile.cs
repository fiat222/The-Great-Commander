using UnityEngine;

public class TrollRockProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float lifetime = 5f;

    [Header("VFX")]
    [Tooltip("Effect ตอนหินกระทบเป้าหมาย หรือตกพื้น")]
    public GameObject impactVFX;

    private float speed;
    private int damage;
    private Vector3 velocity;
    private bool isFlying = false;

    private void Awake()
    {
        // ลบโค้ดหา modelTransform ออกไป
    }

    public void Launch(Vector3 direction, float launchSpeed, int launchDamage)
    {
        speed = launchSpeed;
        damage = launchDamage;
        velocity = direction.normalized * speed;
        isFlying = true;

        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isFlying) return;

        // พุ่งไปข้างหน้า
        transform.position += velocity * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;

        // ไม่โดนตัวเองหรือ Enemy ด้วยกัน (Troll ปาหินไม่โดนพวกเดียวกัน)
        if (other.CompareTag("Enemy")) return;

        bool hitSomething = false;

        if (other.CompareTag("Player"))
        {
            // สร้างตำแหน่งผู้โจมตีจำลอง โดยถอยไปด้านหลังในทิศที่หินพุ่งมา
            // เพื่อให้ทิศกระเด็น (Knockback) พุ่งตามทิศของก้อนหิน ไม่ใช่พุ่งเข้าหา Troll
            Vector3 fakeAttackerPos = transform.position - velocity.normalized * 10f;

            other.GetComponent<PlayerController>()?.TakeDamage(damage, fakeAttackerPos);
            other.GetComponent<Archer>()?.TakeDamage(damage, fakeAttackerPos);
            hitSomething = true;
        }
        else if (other.CompareTag("Minion"))
        {
            other.GetComponent<MinionAI>()?.TakeDamage(damage);
            other.GetComponent<ArcherAI>()?.TakeDamage(damage);
            hitSomething = true;
        }
        else if (other.CompareTag("Base"))
        {
            other.GetComponent<BaseHealth>()?.TakeDamage(damage);
            hitSomething = true;
        }
        else
        {
            // ชนอย่างอื่น (พื้น, กำแพง, ปราการ ฯลฯ)
            hitSomething = true;
        }

        if (hitSomething)
        {
            // เล่น Effect ตอนหินแตก
            if (impactVFX != null)
            {
                // ถ้ามี VFXManager จะดีมาก ถ้าไม่มีก็ Instantiate ตรงๆ
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.Play(impactVFX, transform.position);
                }
                else
                {
                    Instantiate(impactVFX, transform.position, Quaternion.identity);
                }
            }

            isFlying = false;
            Destroy(gameObject);
        }
    }
}
