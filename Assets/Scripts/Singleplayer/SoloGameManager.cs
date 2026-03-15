using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class SoloGameManager : MonoBehaviour
{
    public static SoloGameManager Instance;

    public static Action<GamePhase> OnPhaseChangedGlobal;
    public static Action<int> OnSystemEnemyDied;

    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI waveText;

    public GamePhase CurrentPhase => currentPhase;
    public int CurrentWave => currentWave;
    private GamePhase currentPhase = GamePhase.Planning;

    [Header("Planning Phase")]
    public float planningDuration = 30f;
    private float planningTimer;

    [Header("Wave")]
    private int currentWave = 1;
    [Header("Victory Condition")]
    [Tooltip("จำนวน Wave สูงสุดเพื่อชนะเกม")]
    public int maxWave = 10;

    // Cursor
    private bool isManualUnlock = true;
    private bool currentModeWantsLock = false;

    public int ExpectedEnemyCount { get; private set; }
    public string systemWaveDraft { get; private set; } = "";
    public bool IsGameWon => currentPhase == GamePhase.Planning && currentWave > maxWave;

    [Header("Enemy Pool")]
    public EnemyStatsSO[] enemyStatsSOs;

    [Header("Spawner")]
    private EnemySpawner_Single globalSpawner;

    [Header("UI")]
    public GameObject crosshairObject;
    public GameObject skillUI;
    public GameObject nextPhaseButton;

    // ==================== ⭐ Wave Income System ====================
    [Header("Wave Income")]
    [Tooltip("เงินที่ให้ตอนเริ่มเกม (Wave 1)")]
    public int startingMoney = 200;
    [Tooltip("เงินฐานที่ให้ตอนเริ่ม Wave 2")]
    public int baseWaveIncome = 100;
    [Tooltip("เงินที่เพิ่มขึ้นทุก Wave (Wave2=100, Wave3=150, Wave4=200 ...)")]
    public int incomeIncreasePerWave = 50;

    private bool isGameEnded = false;

    // ⭐ ติดตามว่า Player ตายใน Wave ที่แล้วหรือไม่
    private bool playerDiedLastWave = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // ⭐ Reset Stats ทุกครั้งกี่เริ่มเกมใหม่ (Solo)
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ResetAllStats();

        globalSpawner = FindFirstObjectByType<EnemySpawner_Single>();
        planningTimer = planningDuration;
        GenerateSystemWaveDraft(); // ⭐ สร้างเวฟเตรียมไว้เลยตั้งแต่รอบ Planning
        UpdateWaveUI();
        UpdatePhaseUI();
        OnPhaseChangedGlobal?.Invoke(currentPhase);

        // ⭐ ให้เงินเริ่มต้น Wave 1
        GiveIncome(startingMoney);
    }

    void Update()
    {
        if (isGameEnded) return; // ⭐ เมื่อจบเกม/ยอมแพ้ Update จะหยุดทำงานทันที ทำให้เล่นต่อไม่ได้

        HandleCursorToggle();
        HandleClickToRelockCursor();

        if (currentPhase == GamePhase.Planning)
        {
            planningTimer -= Time.deltaTime;

            if (planningTimer <= 0)
                ChangePhase();

            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(planningTimer) + " S";
        }
    }

    // ================= INPUT =================

    private void HandleCursorToggle()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;

        if (currentPhase == GamePhase.Planning)
        {
            currentModeWantsLock = !currentModeWantsLock;
            isManualUnlock = !currentModeWantsLock;
        }
        else
        {
            isManualUnlock = !isManualUnlock;
        }

        UpdateCursorState(currentPhase);
    }

    private void HandleClickToRelockCursor()
    {
        if (!isManualUnlock && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            bool shouldLock = (currentPhase == GamePhase.Combat)
                           || (currentPhase == GamePhase.Planning && currentModeWantsLock);
            if (shouldLock) ApplyCursorState(true);
        }
    }

    private void UpdateCursorState(GamePhase phase)
    {
        if (phase == GamePhase.Planning)
            ApplyCursorState(currentModeWantsLock);
        else
            ApplyCursorState(true);
    }

    public void ApplyCursorState(bool shouldLock)
    {
        if (isManualUnlock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }
    }

    // ================= PHASE =================

    public void RequestNextPhase() => ChangePhase();

    public void ChangePhase()
    {
        if (currentPhase == GamePhase.Planning)
            StartCombat();
        else
            StartPlanning();
    }

    void StartCombat()
    {
        currentPhase = GamePhase.Combat;
        // ⭐ รีเซ็ตสถานะตายก่อนเข้า Combat ใหม่
        playerDiedLastWave = false;
        SpawnWave();
        UpdatePhaseUI();
        OnPhaseChangedGlobal?.Invoke(currentPhase);
    }

    void StartPlanning()
    {
        bool survived = !playerDiedLastWave;

        currentPhase = GamePhase.Planning;
        currentWave++;
        planningTimer = planningDuration;
        CleanupEnemies();
        GenerateSystemWaveDraft(); // ⭐ สุ่มเวฟใหม่ไว้ล่วงหน้า

        // ⭐ ให้เงิน Wave Income: Wave2=100, Wave3=150, Wave4=200 ...
        int waveIncome = baseWaveIncome + (currentWave - 2) * incomeIncreasePerWave;
        waveIncome = Mathf.Max(0, waveIncome);
        GiveIncome(waveIncome);
        Debug.Log($"<color=cyan>[SoloGameManager]</color> Wave {currentWave} Income: +{waveIncome} Gold");

        // ⭐ ถ้าไม่ตายในรอบที่แล้ว → Heal เต็ม
        if (survived)
        {
            HealLocalPlayer();
            Debug.Log($"<color=green>[SoloGameManager]</color> Player survived! HP restored to full.");
        }
        
        // ⭐ ตรวจสอบว่าชนะเกมหรือไม่
        if (IsGameWon)
        {
            Debug.Log($"<color=yellow>[SoloGameManager]</color> Victory condition met! Wave {currentWave} > Max Wave {maxWave}. EnemyTracker will show win UI.");
        }

        UpdateWaveUI();
        UpdatePhaseUI();
        OnPhaseChangedGlobal?.Invoke(currentPhase);
    }

    // ================= ⭐ INCOME & HEAL =================

    /// <summary>ให้เงินผ่าน PlacementManager (Single Player)</summary>
    private void GiveIncome(int amount)
    {
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.Money += amount;
            PlacementManager.Instance.OnMoneyChanged?.Invoke(PlacementManager.Instance.Money);
            Debug.Log($"<color=cyan>[SoloGameManager]</color> Income: +{amount} Gold (Total: {PlacementManager.Instance.Money})");
        }
    }

    /// <summary>⭐ เรียกเมื่อ Player ตายเพื่อบันทึกสถานะ</summary>
    public void NotifyPlayerDied()
    {
        playerDiedLastWave = true;
        Debug.Log($"<color=red>[SoloGameManager]</color> Player died this wave.");
    }

    /// <summary>⭐ Heal Local Player เต็มหลอด (ทั้ง PlayerController และ Archer)</summary>
    private void HealLocalPlayer()
    {
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            pc.HealToFull();
            return;
        }
        foreach (var ac in FindObjectsByType<Archer>(FindObjectsSortMode.None))
        {
            ac.HealToFull();
            return;
        }
    }

    // ================= WAVE =================

    private void GenerateSystemWaveDraft()
    {
        if (enemyStatsSOs == null || enemyStatsSOs.Length == 0) return;

        int totalToSpawn = 4 + (currentWave - 1) * 2;
        ExpectedEnemyCount = totalToSpawn; // บันทึกเพื่อให้ Tracker ดึงไปใช้
        int[] counts = new int[enemyStatsSOs.Length];

        // สุ่มแจกจ่ายจำนวนให้ครบ totalToSpawn
        for (int i = 0; i < totalToSpawn; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, enemyStatsSOs.Length);
            counts[randIndex]++;
        }

        // แปลงเป็น String: "0:3|1:2"
        string draft = "";
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] > 0)
            {
                if (draft != "") draft += "|";
                draft += $"{i}:{counts[i]}";
            }
        }

        systemWaveDraft = draft;
        Debug.Log($"<color=orange>[SoloGameManager]</color> Generated Wave {currentWave}: {draft} (Total: {totalToSpawn})");

        // แจ้งเตือนมอนสเตอร์ให้อัปเดต Stats ตามคลื่นปัจจุบัน
        foreach (var so in enemyStatsSOs)
            if (so != null) so.SetWave(currentWave);
    }

    void SpawnWave()
    {
        if (globalSpawner == null || string.IsNullOrEmpty(systemWaveDraft)) return;

        globalSpawner.SpawnWaveFromDraft(systemWaveDraft, enemyStatsSOs);
    }

    // ================= CLEANUP =================

    void CleanupEnemies()
    {
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
            Destroy(e);
    }

    // ================= UI =================

    void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = "Wave " + currentWave;
    }

    void UpdatePhaseUI()
    {
        bool isPlanning = currentPhase == GamePhase.Planning;
        SafeSetActive(crosshairObject, !isPlanning);
        SafeSetActive(skillUI, !isPlanning);
        SafeSetActive(nextPhaseButton, isPlanning);

        if (countdownText != null) SafeSetActive(countdownText.gameObject, isPlanning);
        if (waveText != null) SafeSetActive(waveText.gameObject, isPlanning);

        // --- Camera & Cursor ---
        if (CameraManager.Instance != null)
            CameraManager.Instance.SetPhaseCamera(currentPhase);

        currentModeWantsLock = false;
        isManualUnlock = isPlanning;
        UpdateCursorState(currentPhase);

        // --- Shop ---
        if (SingleShopManager.Instance != null)
        {
            if (isPlanning) SingleShopManager.Instance.OpenShop();
            else SingleShopManager.Instance.CloseShop();
        }

        // --- Minimap ---
        if (MinimapUI.Instance != null)
            MinimapUI.Instance.SetVisible(!isPlanning); // Minimap เปิดตอน Combat
    }

    // ================= GAME END & SURRENDER =================

    /// <summary>
    /// ⭐ ฟังก์ชันกดยอมแพ้: สั่งงานผ่าน SoloEnemyTracker เพื่อแสดง UI และจบเกม
    /// </summary>
    public void GiveUp()
    {
        if (isGameEnded) return;

        // สั่งให้ Tracker แสดงหน้าจอแพ้ (ซึ่ง Tracker จะมาเรียก OnGameEnded ในนี้อีกที)
        if (SoloEnemyTracker.Instance != null)
        {
            SoloEnemyTracker.Instance.GiveUp();
        }
        else
        {
            // Fallback กรณีหา Tracker ไม่เจอ
            OnGameEnded();
        }
    }

    public void OnGameEnded()
    {
        isGameEnded = true;

        // ปลดล็อกเมาส์ถาวรเพื่อให้กดปุ่มเมนูได้
        isManualUnlock = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ซ่อน UI พื้นฐานที่ใช้เล่น
        SafeSetActive(crosshairObject, false);
        SafeSetActive(skillUI, false);
        SafeSetActive(nextPhaseButton, false);
    }

    public void ReturnToMenu() => SceneManager.LoadScene("MenuSceneTest");

    // ================= UTIL =================

    public static void SafeSetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}