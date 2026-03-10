using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// จัดการระบบเลือกตัวละคร (Single Player)
/// วางบน GameObject ใน CharacterSelectScene
/// </summary>
public class SingleCharacterSelectManager : MonoBehaviour
{
    public static SingleCharacterSelectManager Instance { get; private set; }

    [Header("Character Database")]
    public CharacterDataSO[] characters;

    // State
    private int _selectedIndex = -1;
    private bool _isReady = false;

    public int SelectedIndex => _selectedIndex;
    public bool IsReady => _isReady;

    // Events
    public static System.Action OnSelectionChanged;
    public static System.Action OnReadyChanged;
    public static System.Action OnStarting;

    private void Awake()
    {
        Instance = this;
        CharacterSelectData.Characters = characters;
        CharacterSelectData.P1CharacterIndex = -1;
    }

    private void Start()
    {
        OnSelectionChanged?.Invoke();
        OnReadyChanged?.Invoke();
    }

    // ==================== Public API ====================

    public void SelectCharacter(int index)
    {
        if (_isReady) return;
        if (index < 0 || index >= characters.Length) return;

        _selectedIndex = index;
        CharacterSelectData.P1CharacterIndex = index;
        OnSelectionChanged?.Invoke();
    }

    public void ToggleReady()
    {
        if (!_isReady && _selectedIndex < 0) return; // ยังไม่เลือก ห้าม Ready

        _isReady = !_isReady;
        OnReadyChanged?.Invoke();

        if (_isReady)
        {
            OnStarting?.Invoke();
            Invoke(nameof(LoadGameScene), 2f);
        }
        else
        {
            CancelInvoke(nameof(LoadGameScene));
        }
    }

    public CharacterDataSO GetSelectedCharacter()
    {
        if (_selectedIndex < 0 || _selectedIndex >= characters.Length) return null;
        return characters[_selectedIndex];
    }

    // ==================== Private ====================

    private void LoadGameScene()
    {
        SceneManager.LoadScene("SoloGameScene");
    }
}