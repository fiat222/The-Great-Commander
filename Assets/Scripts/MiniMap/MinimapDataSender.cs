using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class MinimapDataSender : NetworkBehaviour
{
    public static MinimapDataSender Instance { get; private set; }

    [Header("Settings")]
    public float updateRate = 0.2f;
    private float timer;
    private List<MinimapUnitData> dataBuffer = new List<MinimapUnitData>();

    // ⭐ เพิ่ม: เก็บจำนวน Enemy ฝั่งตรงข้ามที่รับมาล่าสุด
    private int opponentEnemyCount = -1;

    private void Awake() => Instance = this;

    void Update()
    {
        if (!IsSpawned) return;

        timer += Time.deltaTime;
        if (timer >= updateRate)
        {
            timer = 0f;
            SendMySceneData();
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[MinimapDataSender] OnNetworkSpawn! IsServer={IsServer} IsClient={IsClient}");
    }

    void SendMySceneData()
    {
        dataBuffer.Clear();

        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            var netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId == myClientId)
            {
                dataBuffer.Add(new MinimapUnitData
                {
                    Position = new Vector2(p.transform.position.x, p.transform.position.z),
                    UnitType = 0
                });
                break;
            }
        }

        foreach (var m in GameObject.FindGameObjectsWithTag("Minion"))
            dataBuffer.Add(new MinimapUnitData
            {
                Position = new Vector2(m.transform.position.x, m.transform.position.z),
                UnitType = 1
            });

        foreach (var e in GameObject.FindGameObjectsWithTag("Enemy"))
            dataBuffer.Add(new MinimapUnitData
            {
                Position = new Vector2(e.transform.position.x, e.transform.position.z),
                UnitType = 2
            });

        if (dataBuffer.Count == 0) return;

        SendSceneDataServerRpc(dataBuffer.ToArray());
    }

    [ServerRpc(RequireOwnership = false)]
    void SendSceneDataServerRpc(MinimapUnitData[] units, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // ── โค้ดเดิม ──────────────────────────────────────────────
        var targetIds = new List<ulong>();
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            if (clientId != senderClientId)
                targetIds.Add(clientId);

        if (targetIds.Count == 0) return;

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = targetIds.ToArray() }
        };

        ReceiveOpponentDataClientRpc(units, clientRpcParams);
    }

    [ClientRpc]
    void ReceiveOpponentDataClientRpc(MinimapUnitData[] units, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[Minimap] ได้รับข้อมูล {units.Length} units จากฝั่งตรงข้าม");
        if (MinimapUI.Instance != null)
            MinimapUI.Instance.Refresh(units);
    }
}