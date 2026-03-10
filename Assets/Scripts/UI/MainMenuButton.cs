using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;

public class MainMenuButton : MonoBehaviour
{
    public void GoToMainMenu()
    {
        // บอก GameManager ว่าตั้งใจออก
        GameManager.Instance?.SetIntentionalDisconnect();

        if (NetworkManager.Singleton == null)
        {
            SceneManager.LoadScene("MenuScene");
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            var tracker = EnemyTracker.Instance;
            if (tracker != null)
                tracker.ForceClientsToMenuClientRpc();

            StartCoroutine(LoadMenuAfterShutdown());
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            StartCoroutine(LoadMenuAfterShutdown());
        }
    }

    private IEnumerator LoadMenuAfterShutdown()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("MenuScene");
    }
}