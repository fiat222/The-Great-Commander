using UnityEngine;
using System.Collections;

public class HealPackSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject healPackPrefab;
    public float cooldown = 30f;

    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private int lastShownSecond = -1;

    private void Start()
    {
        SpawnPack();
    }

    private void Update()
    {
        if (!isOnCooldown) return;

        cooldownTimer -= Time.deltaTime;

        // แสดงตัวเลข cooldown ลอยขึ้นทุก 1 วินาที เหมือน damage number
        int currentSecond = Mathf.CeilToInt(cooldownTimer);
        if (currentSecond != lastShownSecond && currentSecond > 0)
        {
            lastShownSecond = currentSecond;
            DamageNumberSpawner.Show(currentSecond, transform.position + Vector3.up);
        }

        if (cooldownTimer <= 0f)
        {
            isOnCooldown = false;
            SpawnPack();
        }
    }

    public void OnPackCollected()
    {
        isOnCooldown = true;
        cooldownTimer = cooldown;
        lastShownSecond = -1;
        Debug.Log($"<color=yellow>[HealPackSpawner]</color> Cooldown {cooldown}s");
    }

    private void SpawnPack()
    {
        if (healPackPrefab == null) return;
        var go = Instantiate(healPackPrefab, transform.position, transform.rotation);
        var pack = go.GetComponent<HealPack>();
        if (pack != null) pack.spawner = this;
        Debug.Log("<color=lime>[HealPackSpawner]</color> Spawn HealPack!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}