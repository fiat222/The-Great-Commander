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

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[MinimapDataSender] OnNetworkSpawn! IsServer={IsServer} IsClient={IsClient}");
    }

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

    void SendMySceneData()
    {
        dataBuffer.Clear();

        // ✅ ดึง Player ตัวเองจาก LocalPlayerSpawner (Local Instantiate ไม่มี NetworkObject)
        var myPlayer = LocalPlayerSpawner.Instance?.GetSpawnedPlayer();
        if (myPlayer != null)
        {
            dataBuffer.Add(new MinimapUnitData
            {
                Position = new Vector2(myPlayer.transform.position.x, myPlayer.transform.position.z),
                UnitType = 0 // white dot
            });
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

        // ✅ RequireOwnership = false ทำให้ Client ส่งได้ด้วย
        SendSceneDataServerRpc(dataBuffer.ToArray());
    }

    // ✅ RequireOwnership = false — สำคัญมาก ไม่งั้น Client ส่งไม่ได้
    [ServerRpc(RequireOwnership = false)]
    void SendSceneDataServerRpc(MinimapUnitData[] units, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // ส่งข้อมูลให้ฝั่งตรงข้ามเท่านั้น
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