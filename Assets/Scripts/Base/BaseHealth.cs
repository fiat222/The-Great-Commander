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
            if (SoloGameManager.Instance != null)
            {
                TakeDamage(999);
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                TakeDamageServerRpc(999, NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount, ulong senderClientId)
    {
        TakeDamage(amount, senderClientId);
    }

    public void TakeDamage(int amount, ulong senderClientId = ulong.MaxValue)
    {
        // ─── Singleplayer ───
        if (SoloGameManager.Instance != null)
        {
            health -= amount;
            Debug.Log($"<color=green>[Base Singleplayer]</color> HP : {health}");

            if (health <= 0)
            {
                Debug.LogError("ฐานพังแล้ว! จบเกม (Solo)");
                if (SoloEnemyTracker.Instance != null)
                    SoloEnemyTracker.Instance.NotifyPlayerDied();
            }
            return;
        }

        // ─── Multiplayer ───
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
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

                ulong loserClientId = senderClientId == ulong.MaxValue 
                    ? NetworkManager.Singleton.LocalClientId 
                    : senderClientId;
                    
                Debug.Log($"[BaseHealth] loserClientId={loserClientId}");
                EnemyTracker.Instance?.ShowGameResultClientRpc(loserClientId);
            }
        }
    }
}