using Unity.Netcode;
using UnityEngine;

public class BaseHealth : NetworkBehaviour
{
    public int health = 100;
    public static System.Action<ulong> OnBaseDied;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            // ✅ ส่ง LocalClientId ของเครื่องตัวเองมาด้วย
            TakeDamageServerRpc(999, NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount, ulong senderClientId)
    {
        TakeDamage(amount, senderClientId);
    }

    public void TakeDamage(int amount, ulong senderClientId = ulong.MaxValue)
    {
        if (!IsServer)
        {
            TakeDamageServerRpc(amount, NetworkManager.Singleton.LocalClientId);
            return;
        }

        health -= amount;
        Debug.Log($"<color=green>[Base]</color> HP : {health}");

        if (health <= 0)
        {
            Debug.LogError("ฐานพังแล้ว! จบเกม");

            // ✅ ใช้ senderClientId แทน LocalClientId
            ulong loserClientId = senderClientId == ulong.MaxValue 
                ? NetworkManager.Singleton.LocalClientId 
                : senderClientId;
                
            Debug.Log($"[BaseHealth] loserClientId={loserClientId}");
            EnemyTracker.Instance?.ShowGameResultClientRpc(loserClientId);
        }
    }
}