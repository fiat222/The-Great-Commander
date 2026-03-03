using Unity.Netcode;
using UnityEngine;

public class BaseHealth : NetworkBehaviour
{
    public int health = 100;

    // ⭐ เพิ่ม: แจ้ง EnemyTracker เมื่อป้อมพัง
    public static System.Action<ulong> OnBaseDied;

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"<color=green>[Base]</color> HP : {health}");

        if (health <= 0)
        {
            Debug.LogError("ฐานพังแล้ว! จบเกม");

            // ⭐ เพิ่ม: แจ้ง Server ว่าป้อมฝั่งใดพัง
            if (IsServer)
            {
                ulong loserClientId = OwnerClientId; // ป้อมของใคร = ใครแพ้
                EnemyTracker.Instance?.ShowGameResultClientRpc(loserClientId);
            }
        }
    }
}