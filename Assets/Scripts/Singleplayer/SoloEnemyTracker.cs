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
    [Tooltip("Panel กลางจอสำหรับแสดงข้อความ Wave Clear / นับถอยหลัง")]
    public GameObject      centerPanel;
    public TextMeshProUGUI centerText;

    [Header("Enemy Counter UI")]
    public TextMeshProUGUI enemyCounterText;

    [Header("Win / Lose UI")]
    public GameObject youWinUI;
    public GameObject youLostUI;

    [Header("Back to Menu Buttons")]
    [Tooltip("ปุ่มกลับเมนูใน YouWin UI")]
    public Button winBackToMenuButton;
    [Tooltip("ปุ่มกลับเมนูใน YouLose UI")]
    public Button loseBackToMenuButton;

    // ─── Settings ─────────────────────────────────────────────────────
    [Header("Settings")]
    public float waveClearDelay    = 15f; // วิที่รอก่อนเปลี่ยน Phase หลัง Wave Clear
    [Tooltip("VFX สายฟ้าตอน Player ถูกบังคับตาย")]
    public GameObject deathLightningVFX;

    // ─── Private State ────────────────────────────────────────────────
    private int  _remainingEnemies    = 0;
    private bool _hasCountedStart     = false;
    private bool _phaseChangeQueued   = false;
    private bool _gameResultShown     = false;  
    private Coroutine _activeCoroutine;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        SetUI(centerPanel, false);
        SetUI(youWinUI,    false);
        SetUI(youLostUI,   false);
    }

    private void OnEnable()
    {
        SoloGameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        SoloGameManager.OnSystemEnemyDied    += HandleEnemyDied;

        if (winBackToMenuButton  != null) winBackToMenuButton.onClick.AddListener(OnBackToMenu);
        if (loseBackToMenuButton != null) loseBackToMenuButton.onClick.AddListener(OnBackToMenu);
    }

    private void OnDisable()
    {
        SoloGameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
        SoloGameManager.OnSystemEnemyDied    -= HandleEnemyDied;

        if (winBackToMenuButton  != null) winBackToMenuButton.onClick.RemoveListener(OnBackToMenu);
        if (loseBackToMenuButton != null) loseBackToMenuButton.onClick.RemoveListener(OnBackToMenu);
    }

    // ─────────────────────────────────────────────────────────────────
    //  PHASE CHANGED
    // ─────────────────────────────────────────────────────────────────
    private void HandlePhaseChanged(GamePhase newPhase)
{
    if (newPhase == GamePhase.Combat)
    {
        _phaseChangeQueued = false;
        _gameResultShown = false; // รีเซ็ตเมื่อเริ่มสู้ใหม่ (ถ้าเกมมีหลายรอบ)
        CalculateTotalEnemies();
    }
    else
    {
        _hasCountedStart   = false;
        _remainingEnemies  = 0;
        _phaseChangeQueued = false;
        _gameResultShown   = false; // รีเซ็ตเมื่อกลับไปวางแผน
        SetUI(centerPanel, false);
        UpdateCounterUI();
    }
}

    // ─────────────────────────────────────────────────────────────────
    //  ENEMY COUNT
    // ─────────────────────────────────────────────────────────────────
    private void CalculateTotalEnemies()
    {
        if (SoloGameManager.Instance == null) return;

        // ดึงจำนวนเต็มๆ จาก SoloGameManager
        int total = SoloGameManager.Instance.ExpectedEnemyCount;

        _remainingEnemies = total;
        _hasCountedStart  = true;

        Debug.Log($"[SoloEnemyTracker] Combat started — Total enemies: {total}");
        UpdateCounterUI();

        // เวฟนี้ไม่มี Enemy เลย → Wave Clear ทันที
        if (total == 0) StartWaveClear();
    }

    private void HandleEnemyDied(int typeIndex)
    {
        if (!_hasCountedStart) return;
        if (SoloGameManager.Instance == null ||
            SoloGameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        _remainingEnemies--;
        Debug.Log($"[SoloEnemyTracker] Enemy died. Remaining: {_remainingEnemies}");
        UpdateCounterUI();

        if (_remainingEnemies <= 0)
        {
            _hasCountedStart = false;
            StartWaveClear();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  WAVE CLEAR
    // ─────────────────────────────────────────────────────────────────
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

        // เปลี่ยนเฟสกลับ Planning
        SoloGameManager.Instance?.ChangePhase();
    }

    // ─────────────────────────────────────────────────────────────────
    //  WIN / LOSE
    // ─────────────────────────────────────────────────────────────────

    /// <summary>เรียกจากภายนอก (เช่น PlayerController) เมื่อ Player ตาย</summary>
    public void NotifyPlayerDied()
    {
        // ตรวจสอบว่าเคยแสดงผลไปหรือยัง ถ้าเคยแล้วให้ Return ออกไปเลย ไม่ต้องเล่นเสียงซ้ำ
        if (_gameResultShown) return; 
        _gameResultShown = true; 

        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        StopAllCoroutines();

        SetUI(centerPanel, false);
        SetUI(youLostUI,   true);
        SetUI(youWinUI,    false);

        // เสียงจะถูกเรียกเพียงครั้งเดียวแน่นอน
        AudioManager.Instance?.PlayLose(); 
        SoloGameManager.Instance?.OnGameEnded();
    }

    /// <summary>เรียกเพื่อแสดง Win UI (เช่น ผ่านครบ Wave ที่กำหนด)</summary>
    public void ShowWin()
    {
        // ตรวจสอบว่าเคยแสดงผลไปหรือยัง
        if (_gameResultShown) return; 
        _gameResultShown = true;

        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        StopAllCoroutines();

        SetUI(centerPanel, false);
        SetUI(youWinUI,    true);
        SetUI(youLostUI,   false);

        // เสียงจะถูกเรียกเพียงครั้งเดียวแน่นอน
        AudioManager.Instance?.PlayWin();
        SoloGameManager.Instance?.OnGameEnded();
    }

    // ─────────────────────────────────────────────────────────────────
    //  BUTTON CALLBACKS
    // ─────────────────────────────────────────────────────────────────
    private void OnBackToMenu()
    {
        SoloGameManager.Instance?.ReturnToMenu();
    }

    // ─────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────
    
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
