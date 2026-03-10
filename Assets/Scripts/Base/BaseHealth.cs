using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;

    [SerializeField] private HealthSystem healthUI;

    private NetworkVariable<int> networkHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private bool isSubscribed;

    private void Awake()
    {
        ResolveHealthUI();
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

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
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

    // ─── RPC / Damage ─────────────────────────────────────────────────────────
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount, ulong senderClientId)
    {
        TakeDamage(amount, senderClientId);
    }

    public void TakeDamage(int amount, ulong senderClientId = ulong.MaxValue)
    {
        if (amount <= 0)
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
}