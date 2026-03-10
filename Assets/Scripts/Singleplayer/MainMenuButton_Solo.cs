using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton_Solo : MonoBehaviour
{
    public void GoToMainMenu()
    {
        SoloGameManager.Instance?.ReturnToMenu();
    }
}