using UnityEngine;
using System.Collections;
using TMPro;

public class HealPackSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject healPackPrefab;
    public float cooldown = 30f;

    [Header("UI")]
    public GameObject cooldownCanvas;
    public TextMeshProUGUI cooldownText;
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 2f;

    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private Vector3 initialCanvasLocalPos;

    private void Awake()
    {
        if (cooldownCanvas != null)
        {
            initialCanvasLocalPos = cooldownCanvas.transform.localPosition;
            cooldownCanvas.SetActive(false);
        }
    }

    private void Start()
    {
        SpawnPack();
    }

    private void Update()
    {
        if (!isOnCooldown)
        {
            if (cooldownCanvas != null && cooldownCanvas.activeSelf) cooldownCanvas.SetActive(false);
            return;
        }

        if (cooldownCanvas != null && !cooldownCanvas.activeSelf) cooldownCanvas.SetActive(true);

        cooldownTimer -= Time.deltaTime;

        // อัปเดตตัวเลข
        if (cooldownText != null)
        {
            cooldownText.text = Mathf.CeilToInt(cooldownTimer).ToString();
        }

        // อนิเมชั่นลอยขึ้นลง
        if (cooldownCanvas != null)
        {
            float newY = initialCanvasLocalPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            cooldownCanvas.transform.localPosition = new Vector3(initialCanvasLocalPos.x, newY, initialCanvasLocalPos.z);
        }

        if (cooldownTimer <= 0f)
        {
            isOnCooldown = false;
            if (cooldownCanvas != null) cooldownCanvas.SetActive(false);
            SpawnPack();
        }
    }

    public void OnPackCollected()
    {
        isOnCooldown = true;
        cooldownTimer = cooldown;
        if (cooldownCanvas != null) cooldownCanvas.SetActive(true);
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