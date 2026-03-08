using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance { get; private set; }

    private List<string> chatHistory = new List<string>();
    private const int MAX_MESSAGES = 50;

    // ⭐ สถานะว่าตอนนี้ Chat Input Field เปิดอยู่ไหม
    public bool IsChatOpen { get; private set; }

    private void Awake() => Instance = this;

    /// <summary>เรียกจาก ChatUI ตอนเปิด/ปิด Input Field</summary>
    public void SetChatOpen(bool open)
    {
        IsChatOpen = open;
    }

    public void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        string senderName = NetworkManager.Singleton.IsHost ? "Player1" : "Player2";
        string fullMessage = $"[{senderName}]: {message}";

        SendMessageServerRpc(fullMessage);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendMessageServerRpc(string message)
    {
        BroadcastMessageClientRpc(message);
    }

    [ClientRpc]
    void BroadcastMessageClientRpc(string message)
    {
        chatHistory.Add(message);
        if (chatHistory.Count > MAX_MESSAGES)
            chatHistory.RemoveAt(0);

        if (ChatUI.Instance != null)
            ChatUI.Instance.AddMessage(message);
    }
}