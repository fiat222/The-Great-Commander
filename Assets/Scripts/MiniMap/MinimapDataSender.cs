using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class MinimapDataSender : NetworkBehaviour
{
    private float timer;
    public float updateRate = 0.2f; // ส่งข้อมูลทุก 0.2 วินาที (ไม่แลค)

    void Update()
    {
        if (!IsOwner) return;

        timer += Time.deltaTime;
        if (timer >= updateRate)
        {
            SendMinimapData();
            timer = 0;
        }
    }

    void SendMinimapData()
    {
        List<MinimapUnitData> data = new List<MinimapUnitData>();

        // 1. ใส่พิกัดตัวเอง (Player 2)
        data.Add(new MinimapUnitData { Position = new Vector2(transform.position.x, transform.position.z), UnitType = 0 });

        // 2. ใส่พิกัด Minion (ต้องตั้ง Tag "Minion" ที่มินเนี่ยน)
        foreach (var m in GameObject.FindGameObjectsWithTag("Minion"))
            data.Add(new MinimapUnitData { Position = new Vector2(m.transform.position.x, m.transform.position.z), UnitType = 1 });

        // 3. ใส่พิกัด Enemy (ต้องตั้ง Tag "Enemy" ที่มอนสเตอร์)
        foreach (var e in GameObject.FindGameObjectsWithTag("Enemy"))
            data.Add(new MinimapUnitData { Position = new Vector2(e.transform.position.x, e.transform.position.z), UnitType = 2 });

        UpdateEnemyMinimapServerRpc(data.ToArray());
    }

    [ServerRpc]
    void UpdateEnemyMinimapServerRpc(MinimapUnitData[] units) => UpdateEnemyMinimapClientRpc(units);

    [ClientRpc]
    void UpdateEnemyMinimapClientRpc(MinimapUnitData[] units)
    {
        if (!IsOwner && MinimapUI.Instance != null)
            MinimapUI.Instance.Refresh(units);
    }
}