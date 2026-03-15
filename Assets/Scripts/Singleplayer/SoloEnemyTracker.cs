using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// นับ Enemy, ตรวจสอบ Wave Clear, จัดการ Win/Lose UI สำหรับ SoloGameScene
/// วางบน GameObject "SoloEnemyTracker" ใน SoloGameScene
/// </summary>
public class SoloEnemyTracker : MonoBehaviour
{
    public static SoloEnemyTracker Instance { get; private set; }

    // ─── UI ───────────────────────────────────────────────────────────
    [Header("Wave Clear / Center UI")]
    public GameObject centerPanel;
    public TextMeshProUGUI centerText;

    [Header("Enemy Counter UI")]
    public TextMeshProUGUI enemyCounterText;

    [Header("Win / Lose UI")]
    public GameObject youWinUI;
    public GameObject youLostUI;

    [Header("Menus to Close on Give Up")]
    [Tooltip("ลาก SettingCanvas มาใส่ที่นี่ เพื่อให้มันปิดตัวลงเมื่อกดยอมแพ้")]
    public GameObject settingCanvas; // ⭐ เพิ่มใหม่: สำหรับปิดหน้า Setting

    [Header("Back to Menu Buttons")]
    public Button winBackToMenuButton;
    public Button loseBackToMenuButton;

    // ─── Settings ─────────────────────────────────────────────────────
    [Header("Settings")]
    public float waveClearDelay = 15f;
    public GameObject deathLightningVFX;

    // ─── Private State ────────────────────────────────────────────────
    private int _remainingEnemies = 0;
    private bool _hasCountedStart = false;
    private bool _phaseChangeQueued = false;
    private bool _gameResultShown = false;
    private Coroutine _activeCoroutine;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        SetUI(centerPanel, false);
        SetUI(youWinUI, false);
        SetUI(youLostUI, false);
    }

    private void OnEnable()
    {
        SoloGameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        SoloGameManager.OnSystemEnemyDied += HandleEnemyDied;

        if (winBackToMenuButton != null) winBackToMenuButton.onClick.AddListener(OnBackToMenu);
        if (loseBackToMenuButton != null) loseBackToMenuButton.onClick.AddListener(OnBackToMenu);
    }

    private void OnDisable()
    {
        SoloGameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
        SoloGameManager.OnSystemEnemyDied -= HandleEnemyDied;

        if (winBackToMenuButton != null) winBackToMenuButton.onClick.RemoveListener(OnBackToMenu);
        if (loseBackToMenuButton != null) loseBackToMenuButton.onClick.RemoveListener(OnBackToMenu);
    }

    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.Combat)
        {
            _phaseChangeQueued = false;
            _gameResultShown = false;
            CalculateTotalEnemies();
        }
        else
        {
            _hasCountedStart = false;
            _remainingEnemies = 0;
            _phaseChangeQueued = false;
            _gameResultShown = false;
            SetUI(centerPanel, false);
            UpdateCounterUI();
        }
    }

    private void CalculateTotalEnemies()
    {
        if (SoloGameManager.Instance == null) return;
        int total = SoloGameManager.Instance.ExpectedEnemyCount;
        _remainingEnemies = total;
        _hasCountedStart = true;
        UpdateCounterUI();
        if (total == 0) StartWaveClear();
    }

    private void HandleEnemyDied(int typeIndex)
    {
        if (!_hasCountedStart) return;
        if (SoloGameManager.Instance == null || SoloGameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        _remainingEnemies--;
        UpdateCounterUI();

        if (_remainingEnemies <= 0)
        {
            _hasCountedStart = false;
            StartWaveClear();
        }
    }

    private void StartWaveClear()
    {
        if (_phaseChangeQueued) return;
        _phaseChangeQueued = true;
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(WaveClearRoutine());
    }

    private IEnumerator WaveClearRoutine()
    {
        SetUI(centerPanel, true);
        for (float t = waveClearDelay; t > 0f; t -= 1f)
        {
            if (centerText != null)
                centerText.text = $"Wave Clear!\nNext wave in {Mathf.CeilToInt(t)}s";
            yield return new WaitForSeconds(1f);
        }
        SetUI(centerPanel, false);
        SoloGameManager.Instance?.ChangePhase();
    }

    // ─────────────────────────────────────────────────────────────────
    //  WIN / LOSE / GIVE UP
    // ─────────────────────────────────────────────────────────────────

    public void GiveUp()
    {
        if (_gameResultShown) return;

        Debug.Log("[SoloEnemyTracker] Player Surrendered!");

        // ⭐ ปิดหน้าจอ Setting ทันทีที่กดยอมแพ้
        if (settingCanvas != null)
        {
            settingCanvas.SetActive(false);
        }

        NotifyPlayerDied();
    }

    public void NotifyPlayerDied()
    {
        if (_gameResultShown) return;
        _gameResultShown = true;

        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        StopAllCoroutines();

        SetUI(centerPanel, false);
        SetUI(youLostUI, true);
        SetUI(youWinUI, false);

        AudioManager.Instance?.PlayLose();
        SoloGameManager.Instance?.OnGameEnded();
    }

    public void ShowWin()
    {
        if (_gameResultShown) return;
        _gameResultShown = true;

        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        StopAllCoroutines();

        SetUI(centerPanel, false);
        SetUI(youWinUI, true);
        SetUI(youLostUI, false);

        AudioManager.Instance?.PlayWin();
        SoloGameManager.Instance?.OnGameEnded();
    }

    private void OnBackToMenu()
    {
        SoloGameManager.Instance?.ReturnToMenu();
    }

    private void UpdateCounterUI()
    {
        if (enemyCounterText != null)
        {
            if (_hasCountedStart && SoloGameManager.Instance != null && SoloGameManager.Instance.CurrentPhase == GamePhase.Combat)
                enemyCounterText.text = $"Enemies Left: {_remainingEnemies}";
            else
                enemyCounterText.text = "";
        }
    }

    private static void SetUI(GameObject ui, bool active)
    {
        SoloGameManager.SafeSetActive(ui, active);
    }
}