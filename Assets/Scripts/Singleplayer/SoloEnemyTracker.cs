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
        Debug.Log("<color=cyan>[SoloEnemyTracker]</color> Awake! Checking UI references...");
        Debug.Log($"  - centerPanel: {(centerPanel != null ? centerPanel.name : "NULL")}, active: {(centerPanel != null ? centerPanel.activeSelf : "N/A")}");
        Debug.Log($"  - youWinUI: {(youWinUI != null ? youWinUI.name : "NULL")}, active: {(youWinUI != null ? youWinUI.activeSelf : "N/A")}");
        Debug.Log($"  - youLostUI: {(youLostUI != null ? youLostUI.name : "NULL")}, active: {(youLostUI != null ? youLostUI.activeSelf : "N/A")}");
        
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
        
        // ⭐ เปลี่ยนไปเฟส Planning ก่อน เพื่อให้ IsGameWon ได้เช็คคำว่า
        SoloGameManager.Instance?.ChangePhase();
        
        // ⭐ ตรวจสอบว่าชนะเกมหรือไม่ (จบทุก Wave แล้ว)
        if (SoloGameManager.Instance != null && SoloGameManager.Instance.IsGameWon)
        {
            Debug.Log($"<color=yellow>[SoloEnemyTracker]</color> Game Won! All waves completed.");
            yield return new WaitForSeconds(0.5f);
            ShowWin();
        }
        else if (SoloGameManager.Instance != null)
        {
            Debug.Log($"<color=cyan>[SoloEnemyTracker]</color> Wave cleared. Current Wave: {SoloGameManager.Instance.CurrentWave}");
        }
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
        if (_gameResultShown)
        {
            Debug.Log("<color=yellow>[SoloEnemyTracker]</color> NotifyPlayerDied called but _gameResultShown is already true, returning.");
            return;
        }
        
        _gameResultShown = true;
        Debug.Log("<color=red>[SoloEnemyTracker]</color> ===== NotifyPlayerDied CALLED =====");

        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        StopAllCoroutines();

        Debug.Log("<color=red>[SoloEnemyTracker]</color> Player Lost! Showing lose UI.");
        Debug.Log($"  - youLostUI exists: {youLostUI != null}");
        
        SetUI(centerPanel, false);
        Debug.Log("  - centerPanel hidden");
        
        SetUI(youLostUI, true);
        Debug.Log($"  - youLostUI active set to true. Current state: {(youLostUI != null ? youLostUI.activeSelf : "N/A")}");
        
        SetUI(youWinUI, false);
        Debug.Log("  - youWinUI hidden");
        
        // ⭐ ยกขึ้นมาหน้าสุด เพื่อไม่ให้ UI อื่นบัง
        if (youLostUI != null && youLostUI.transform.parent != null)
        {
            youLostUI.transform.SetAsLastSibling();
            Debug.Log("<color=red>[SoloEnemyTracker]</color> You Lost UI moved to front (sibling index: " + youLostUI.transform.GetSiblingIndex() + ")");
        }
        else if (youLostUI == null)
        {
            Debug.LogError("<color=red>[SoloEnemyTracker ERROR]</color> youLostUI is NULL! Please assign it in Inspector.");
        }
        else
        {
            Debug.LogWarning("<color=yellow>[SoloEnemyTracker]</color> youLostUI.transform.parent is NULL!");
        }

        AudioManager.Instance?.PlayLose();
        SoloGameManager.Instance?.OnGameEnded();
        Debug.Log("<color=red>[SoloEnemyTracker]</color> ===== NotifyPlayerDied FINISHED =====");
    }

    public void ShowWin()
    {
        if (_gameResultShown) return;
        _gameResultShown = true;

        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        StopAllCoroutines();

        Debug.Log("<color=green>[SoloEnemyTracker]</color> Player Won! Showing win UI.");
        
        SetUI(centerPanel, false);
        SetUI(youWinUI, true);
        SetUI(youLostUI, false);
        
        // ⭐ ยกขึ้นมาหน้าสุด เพื่อไม่ให้ UI อื่นบัง
        if (youWinUI != null && youWinUI.transform.parent != null)
        {
            youWinUI.transform.SetAsLastSibling();
            Debug.Log("<color=green>[SoloEnemyTracker]</color> You Win UI moved to front!");
        }
        else if (youWinUI == null)
        {
            Debug.LogError("<color=green>[SoloEnemyTracker ERROR]</color> youWinUI is NULL! Please assign it in Inspector.");
        }

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
        if (ui == null)
        {
            Debug.LogError("<color=red>[SoloEnemyTracker SetUI ERROR]</color> UI GameObject is NULL!");
            return;
        }
        
        bool wasActive = ui.activeSelf;
        SoloGameManager.SafeSetActive(ui, active);
        bool isNowActive = ui.activeSelf;
        
        Debug.Log($"<color=cyan>[SoloEnemyTracker SetUI]</color> {ui.name}: {wasActive} → {isNowActive} (requested: {active})");
        
        // 检查父对象是否激活
        if (active && !isNowActive && ui.transform.parent != null)
        {
            bool parentActive = ui.transform.parent.gameObject.activeSelf;
            Debug.LogWarning($"<color=yellow>[SoloEnemyTracker SetUI]</color> Parent '{ui.transform.parent.name}' is {(parentActive ? "active" : "INACTIVE")}!");
        }
    }
}