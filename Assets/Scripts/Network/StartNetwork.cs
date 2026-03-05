using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class StartNetwork : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void StartHost()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = ApproveConnection;

        if (!nm.StartHost())
        {
            Debug.LogError("[StartNetwork] StartHost failed — port 7777 may be in use.");
            return;
        }

        nm.SceneManager.LoadScene("CharacterSelectScene", LoadSceneMode.Single);
    }

    public void StartClient()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.NetworkConfig.ConnectionApproval = true;
        nm.StartClient();
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }
}
