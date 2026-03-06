using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// จัดการระบบเลือกตัวละคร (Network-synced)
/// วางบน GameObject ที่มี NetworkObject ใน CharacterSelectScene
/// </summary>
public class CharacterSelectManager : NetworkBehaviour
{
    public static CharacterSelectManager Instance { get; private set; }

    [Header("Character Database")]
    public CharacterDataSO[] characters;

    // Network State: -1 = ยังไม่เลือก, 0+ = index
    public NetworkVariable<int> p1Selection = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> p2Selection = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> p1Ready = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> p2Ready = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Events
    public static System.Action OnSelectionChanged;
    public static System.Action OnReadyChanged;
    public static System.Action OnAllReadyAndStarting;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        CharacterSelectData.Characters = characters;

        p1Selection.OnValueChanged += (_, newVal) =>
        {
            CharacterSelectData.P1CharacterIndex = newVal;
            OnSelectionChanged?.Invoke();
        };
        p2Selection.OnValueChanged += (_, newVal) =>
        {
            CharacterSelectData.P2CharacterIndex = newVal;
            OnSelectionChanged?.Invoke();
        };
        p1Ready.OnValueChanged += (_, _) => OnReadyChanged?.Invoke();
        p2Ready.OnValueChanged += (_, _) => OnReadyChanged?.Invoke();

        CharacterSelectData.P1CharacterIndex = p1Selection.Value;
        CharacterSelectData.P2CharacterIndex = p2Selection.Value;

        OnSelectionChanged?.Invoke();
        OnReadyChanged?.Invoke();
    }

    // ==================== Public API ====================

    public void SelectCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characters.Length) return;
        SelectCharacterServerRpc(NetworkManager.Singleton.LocalClientId, characterIndex);
    }

    public void ToggleReady()
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool currentReady = myId == 0 ? p1Ready.Value : p2Ready.Value;
        SetReadyServerRpc(myId, !currentReady);
    }

    public CharacterDataSO GetSelectedCharacter(ulong clientId)
    {
        int idx = clientId == 0 ? p1Selection.Value : p2Selection.Value;
        if (idx < 0 || idx >= characters.Length) return null;
        return characters[idx];
    }

    public bool AmIHost => NetworkManager.Singleton.LocalClientId == 0;

    // ==================== Server RPCs ====================

    [Rpc(SendTo.Server)]
    private void SelectCharacterServerRpc(ulong clientId, int characterIndex)
    {
        if (clientId == 0 && p1Ready.Value) return;
        if (clientId != 0 && p2Ready.Value) return;

        if (clientId == 0)
            p1Selection.Value = characterIndex;
        else
            p2Selection.Value = characterIndex;
    }

    [Rpc(SendTo.Server)]
    private void SetReadyServerRpc(ulong clientId, bool ready)
    {
        if (ready)
        {
            int sel = clientId == 0 ? p1Selection.Value : p2Selection.Value;
            if (sel < 0) return;
        }

        if (clientId == 0)
            p1Ready.Value = ready;
        else
            p2Ready.Value = ready;

        // Solo: รอแค่ p1 Ready / Duo: รอทั้งคู่ Ready
        bool allReady = StartNetworkTest.IsSolo
            ? p1Ready.Value
            : p1Ready.Value && p2Ready.Value;

        if (allReady)
        {
            OnAllReadyAndStarting?.Invoke();
            Invoke(nameof(LoadGameScene), 2f);
        }
    }

    private void LoadGameScene()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}
