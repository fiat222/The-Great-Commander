using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;

    [SerializeField] private HealthSystem healthUI;

    private readonly NetworkVariable<int> networkHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private int currentHealth;
    private bool isSubscribed;
    private bool deathSequenceStarted;

    [Header("Death Settings")]
    [Tooltip("ถ้าติ๊ก ป้อมหลักจะถูกลบออกจากซีนหลังจากจบคัตซีน Game Over แล้ว")]
    public bool destroyOnDeath = true;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;

        ResolveHealthUI();
        UpdateUI(currentHealth);
    }

    private void Start()
    {
        if (!IsUsingNetworkGameplay())
        {
            UpdateUI(currentHealth);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        maxHealth = Mathf.Max(1, maxHealth);

        if (IsServer)
            networkHealth.Value = maxHealth;

        if (!isSubscribed)
        {
            networkHealth.OnValueChanged += OnHealthChanged;
            isSubscribed = true;
        }

        UpdateUI(networkHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (isSubscribed)
        {
            networkHealth.OnValueChanged -= OnHealthChanged;
            isSubscribed = false;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P))
            return;

        if (SoloGameManager.Instance != null)
        {
            TakeDamage(999);
        }
        else if (IsUsingNetworkGameplay())
        {
            TakeDamageServerRpc(999, NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private void ResolveHealthUI()
    {
        if (healthUI != null)
            return;

        healthUI = GetComponent<HealthSystem>()
            ?? GetComponentInParent<HealthSystem>()
            ?? GetComponentInChildren<HealthSystem>();

        if (healthUI != null)
            Debug.Log($"<color=lime>[BaseHealth]</color> พบ HealthSystem บน {healthUI.gameObject.name} ✅");
        else
            Debug.LogWarning("<color=red>[BaseHealth]</color> ไม่พบ HealthSystem! UI จะไม่ลด ❌");
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        UpdateUI(newVal);
    }

    private void UpdateUI(int currentHP)
    {
        ResolveHealthUI();

        if (healthUI != null)
            healthUI.ForceSetHealth(currentHP, maxHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount, ulong senderClientId)
    {
        TakeDamage(amount, senderClientId);
    }

    public void TakeDamage(int amount, ulong senderClientId = ulong.MaxValue)
    {
        if (amount <= 0)
            return;

        if (SoloGameManager.Instance != null)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            UpdateUI(currentHealth);

            Debug.Log($"<color=green>[Base Singleplayer]</color> HP : {currentHealth}/{maxHealth}");

            if (currentHealth <= 0 && !deathSequenceStarted)
            {
                Debug.LogError("ฐานพังแล้ว! จบเกม (Solo)");
                deathSequenceStarted = true;
                StartCoroutine(PlayBaseDeathSequence(true, ulong.MaxValue));
            }

            return;
        }

        if (!IsUsingNetworkGameplay())
            return;

        if (!IsServer)
        {
            TakeDamageServerRpc(amount, NetworkManager.Singleton.LocalClientId);
            return;
        }

        networkHealth.Value = Mathf.Max(0, networkHealth.Value - amount);
        UpdateUI(networkHealth.Value);

        Debug.Log($"<color=green>[Base]</color> HP : {networkHealth.Value}/{maxHealth}");

        // โหมด Network / PvP: ไม่ต้องเล่นคัตซีนป้อมพัง ให้ EnemyTracker จัดการผลแพ้ชนะทันที
        if (networkHealth.Value <= 0)
        {
            Debug.LogError("ฐานพังแล้ว! จบเกม (Network)");

            ulong loserClientId = senderClientId == ulong.MaxValue
                ? NetworkManager.Singleton.LocalClientId
                : senderClientId;

            Debug.Log($"[BaseHealth] loserClientId={loserClientId}");
            EnemyTracker.Instance?.ShowGameResultClientRpc(loserClientId);
        }
    }

    private static bool IsUsingNetworkGameplay()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    /// <summary>
    /// ลำดับตอนฐานหลักตาย:
    /// 1) เล่นเอฟเฟกต์ระเบิด/พัง (ถ้ามีตั้งใน HealthSystem)
    /// 2) โฟกัสกล้อง Spectator ไปที่ฐานหลัก (Solo/Network ฝั่งแพ้ — ทำเสมอถ้ามี CameraManager)
    /// 3) รอให้ผู้เล่นชมฉากสักพัก
    /// 4) เรียก Game Over ตามโหมด (Solo / Network)
    /// </summary>
    private const float DefaultGameOverDelaySolo = 2.5f;

    private System.Collections.IEnumerator PlayBaseDeathSequence(bool isSolo, ulong loserClientId)
    {
        float delay = 0f;

        if (healthUI != null)
        {
            if (healthUI.gameOverDelay > 0f)
                delay = healthUI.gameOverDelay;
        }
        else if (isSolo)
        {
            delay = DefaultGameOverDelaySolo;
        }

        // 1) โฟกัสกล้อง Spectator ไปที่มุมมองเริ่มต้น (หุ่นนริศ) — ทำเป็นอันดับแรก
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.FocusInitialView();
        }

        // 2) รอกล้องแพนไปถึง (ใช้เวลาจาก healthUI ถ้ามี)
        float arriveDelay = (healthUI != null) ? healthUI.cameraArriveDelay : 0.8f;
        if (arriveDelay > 0f)
        {
            yield return new WaitForSeconds(arriveDelay);
        }

        // 3) เล่นเอฟเฟกต์ระเบิด (VFX)
        if (healthUI != null && healthUI.deathVfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(
                healthUI.deathVfxPrefab,
                transform.position,
                Quaternion.identity
            );

            if (healthUI.deathVfxDuration > 0f)
            {
                Destroy(vfxInstance, healthUI.deathVfxDuration);
            }
        }

        // (ลบทิ้งเพราะย้ายไปไว้ข้างบนแล้ว)

        // 3) ทำเหตุการณ์ป้อมค่อยๆ จมลง (Sink Animation)
        if (healthUI != null)
        {
            yield return healthUI.ExecuteSinkAnimation(transform);
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 4) เรียก Game Over ตามโหมดที่ใช้อยู่
        if (isSolo)
        {
            SoloEnemyTracker.Instance?.NotifyPlayerDied();
        }
        else
        {
            // PvP / Network Mode ใช้ EnemyTracker แสดงผลแพ้ชนะปกติ
            if (EnemyTracker.Instance != null)
            {
                EnemyTracker.Instance.ShowGameResultClientRpc(loserClientId);
            }
        }

        // 5) ลบป้อมออกจากซีนหลังจากจบคัตซีน (เฉพาะฝั่ง Server ในโหมด Network หรือโหมด Solo ปกติ)
        if (destroyOnDeath)
        {
            // ในโหมด Network ให้ให้ Server เป็นคน Destroy เพื่อให้ NetworkObject Despawn ถูกต้อง
            if (!IsUsingNetworkGameplay() || IsServer)
            {
                Destroy(gameObject);
            }
        }
    }
}