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
        UpdateWaveUI();
        UpdatePhaseUI();
        OnPhaseChangedGlobal?.Invoke(currentPhase);
    }

    // ================= WAVE =================

    void SpawnWave()
    {
        if (globalSpawner == null || enemyStatsSOs == null || enemyStatsSOs.Length == 0) return;

        // อัปเดต wave ให้ EnemyStatsSO ทุกตัวก่อน
        foreach (var so in enemyStatsSOs)
            if (so != null) so.SetWave(currentWave);

        int total = 1 + (currentWave - 1) * 2;
        ExpectedEnemyCount = total; // บันทึกเพื่อให้ Tracker ดึงไปใช้

        globalSpawner.SpawnWaveBatch(enemyStatsSOs, total);
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