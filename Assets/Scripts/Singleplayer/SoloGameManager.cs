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
    private GamePhase currentPhase = GamePhase.Planning;

    [Header("Planning Phase")]
    public float planningDuration = 30f;
    private float planningTimer;

    [Header("Wave")]
    private int currentWave = 1;

    // Cursor
    private bool isManualUnlock = true;
    private bool currentModeWantsLock = false;

    public int ExpectedEnemyCount { get; private set; }
    public string systemWaveDraft { get; private set; } = "";

    [Header("Enemy Pool")]
    public EnemyStatsSO[] enemyStatsSOs;

    [Header("Spawner")]
    private EnemySpawner_Single globalSpawner;

    [Header("UI")]
    public GameObject crosshairObject;
    public GameObject skillUI;
    public GameObject nextPhaseButton;

    private bool isGameEnded = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        globalSpawner = FindFirstObjectByType<EnemySpawner_Single>();
        planningTimer = planningDuration;
        GenerateSystemWaveDraft(); // ⭐ สร้างเวฟเตรียมไว้เลยตั้งแต่รอบ Planning
        UpdateWaveUI();
        UpdatePhaseUI();
        OnPhaseChangedGlobal?.Invoke(currentPhase);
    }

    void Update()
    {
        if (isGameEnded) return;

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

        // (ลบการเช็ค Enemy.Length == 0 ออก เพราะ Spawner ทยอยเสก จะบั๊กถ้าเช็คทันที)
        // ให้ SoloEnemyTracker เป็นตัวนับจำนวนแทน
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
        SpawnWave();
        UpdatePhaseUI();
        OnPhaseChangedGlobal?.Invoke(currentPhase);
    }

    void StartPlanning()
    {
        currentPhase = GamePhase.Planning;
        currentWave++;
        planningTimer = planningDuration;
        CleanupEnemies();
        GenerateSystemWaveDraft(); // ⭐ สุ่มเวฟใหม่ไว้ล่วงหน้า
        UpdateWaveUI();
        UpdatePhaseUI();
        OnPhaseChangedGlobal?.Invoke(currentPhase);
    }

    // ================= WAVE =================

    private void GenerateSystemWaveDraft()
    {
        if (enemyStatsSOs == null || enemyStatsSOs.Length == 0) return;

        int totalToSpawn = 1 + (currentWave - 1) * 2;
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

    // ================= ENEMY SHOP (Solo) =================
    // Solo Mode ไม่มีระบบส่ง Enemy ครับ — Tab Enemy ใน Shop ถูกซ่อนไว้แล้ว

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
            else            SingleShopManager.Instance.CloseShop();
        }

        // --- Minimap ---
        if (MinimapUI.Instance != null)
            MinimapUI.Instance.SetVisible(!isPlanning); // Minimap เปิดตอน Combat
    }

    // ================= GAME END =================

    public void OnGameEnded()
    {
        isGameEnded = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReturnToMenu() => SceneManager.LoadScene("MenuScene");

    // ================= UTIL =================

    public static void SafeSetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}