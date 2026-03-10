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

            if (currentHealth <= 0)
            {
                Debug.LogError("ฐานพังแล้ว! จบเกม (Solo)");
                SoloEnemyTracker.Instance?.NotifyPlayerDied();
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

        if (networkHealth.Value <= 0)
        {
            Debug.LogError("ฐานพังแล้ว! จบเกม");

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
}