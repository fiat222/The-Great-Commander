using UnityEngine;

public class HealPack : MonoBehaviour
{
    [Header("Heal")]
    public int healAmount = 50;

    [Header("VFX")]
    [Tooltip("ParticleSystem loop ตอนแพ็คอยู่ในโลก — ลากจาก child ใน Prefab")]
    public ParticleSystem idleVFX;
    [Tooltip("Prefab ParticleSystem ตอนเก็บ — loop=false, PlayOnAwake=false")]
    public GameObject pickupVFXPrefab;

    [HideInInspector] public HealPackSpawner spawner;

    private bool collected = false;

    private void Start()
    {
        if (idleVFX != null && !idleVFX.isPlaying)
            idleVFX.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        // ดักจับทันทีเพื่อป้องกัน Collider อื่นๆ ของผู้เล่นเข้าซ้ำในจังหวะเดียวกัน
        collected = true;

        // ฮีล
        bool healed = TryHeal<Archer>(other, healAmount)
                   || TryHeal<PlayerController>(other, healAmount);

        if (!healed)
        {
            Debug.LogWarning("[HealPack] ไม่พบ Archer/PlayerController!");
            collected = false; // ถ้าฮีลไม่ได้จริงๆ ค่อยเปิดให้กดใหม่ (หรือจะปล่อยให้หายไปเลยก็ได้)
            return;
        }

        // แสดงตัวเลข
        DamageNumberSpawner.Show(healAmount, transform.position + Vector3.up);

        // เล่นเสียงฮีล
        var audioComp = other.GetComponentInParent<PlayerAudio.PlayerAudioComponent>();
        if (audioComp != null)
        {
            audioComp.PlaySound(PlayerAudio.PlayerSoundType.Heal);
        }

        // หยุด idle VFX
        if (idleVFX != null) idleVFX.Stop();

        // Spawn pickup VFX จาก Prefab แยก (ไม่ใช่ child) → Destroy อัตโนมัติ
        if (pickupVFXPrefab != null)
        {
            var vfx = Instantiate(pickupVFXPrefab, transform.position, Quaternion.identity);
            var ps = vfx.GetComponent<ParticleSystem>();
            float duration = ps != null ? ps.main.duration + 0.5f : 2f;
            Destroy(vfx, duration);
        }

        if (spawner != null) spawner.OnPackCollected();

        Destroy(gameObject);
    }

    private bool TryHeal<T>(Collider col, int amount) where T : MonoBehaviour
    {
        T target = col.GetComponent<T>() ?? col.GetComponentInParent<T>();
        if (target == null) return false;

        var method = typeof(T).GetMethod("Heal",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(target, new object[] { amount });
            Debug.Log($"<color=lime>[HealPack]</color> ฮีล {typeof(T).Name} +{amount} HP");
            return true;
        }
        return false;
    }
}