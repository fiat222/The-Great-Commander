using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class EnemyTracker : NetworkBehaviour
{
    public static EnemyTracker Instance { get; private set; }

    // ─── UI (ลาก assign ใน Inspector) ─────────────────────────────
    [Header("Wave / Death Countdown UI")]
    [Tooltip("Panel กลางจอ — ใช้ร่วมกันทั้ง Wave Clear และ Death Countdown")]
    public GameObject centerPanel;
    public TextMeshProUGUI centerText;

    [Header("Kill Opponent Button")]
    [Tooltip("ปุ่ม Kill Opponent — โผล่เมื่อ Enemy ฝั่งเราหมด กดแล้วนับถอยหลัง 15 วิฝั่งตรงข้าม")]
    public GameObject killOpponentButton;

    [Header("Win / Lose UI")]
    public GameObject youWinUI;
    public GameObject youLostUI;

    [Header("Settings")]
    public float countdownDuration = 15f;
    [Tooltip("เอฟเฟคสายฟ้าเวลาโดนระบบสั่งตายตอนจบ 15 วิ")]
    public GameObject deathLightningVFX;

    // ─── NetworkVariables (Server เขียน / ทุกคนอ่าน) ──────────────
    private NetworkVariable<int> p0EnemyCount = new NetworkVariable<int>(999);
    private NetworkVariable<int> p1EnemyCount = new NetworkVariable<int>(999);
    private NetworkVariable<bool> p0Cleared = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> p1Cleared = new NetworkVariable<bool>(false);

    // ─── State ────────────────────────────────────────────────────
    private Coroutine activeCoroutine;
    private bool countdownRunning = false;
    private bool phaseChangeQueued = false;
    private bool _gameResultShown = false; // ✅ เพิ่มเพื่อกันเสียงและ UI แสดงซ้ำ

    // การนับศัตรูในเครื่องตัวเองแบบแม่นยำเป๊ะๆ 100%
    private int localRemainingEnemies = 0;
    private bool localHasCountedStart = false;

    // ────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        Debug.Log("<color=cyan>[EnemyTracker]</color> Awake! Checking UI references...");
        Debug.Log($"  - centerPanel: {(centerPanel != null ? centerPanel.name : "NULL")}, active: {(centerPanel != null ? centerPanel.activeSelf : "N/A")}");
        Debug.Log($"  - youWinUI: {(youWinUI != null ? youWinUI.name : "NULL")}, active: {(youWinUI != null ? youWinUI.activeSelf : "N/A")}");
        Debug.Log($"  - youLostUI: {(youLostUI != null ? youLostUI.name : "NULL")}, active: {(youLostUI != null ? youLostUI.activeSelf : "N/A")}");
        
        SetUI(centerPanel, false);
        SetUI(killOpponentButton, false);
        SetUI(youWinUI, false);
        SetUI(youLostUI, false);
    }

    private void OnEnable()
    {
        if (killOpponentButton != null)
        {
            var btn = killOpponentButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnKillOpponentPressed);
        }
    }

    private void OnDisable()
    {
        if (killOpponentButton != null)
        {
            var btn = killOpponentButton.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(OnKillOpponentPressed);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) &&
            killOpponentButton != null &&
            killOpponentButton.activeSelf)
        {
            OnKillOpponentPressed();
        }
    }

    public override void OnNetworkSpawn()
    {
        p0Cleared.OnValueChanged += (_, __) => EvaluateOnClient();
        p1Cleared.OnValueChanged += (_, __) => EvaluateOnClient();

        GameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        GameManager.OnSystemEnemyDied += HandleLocalEnemyDied;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.p0Dead.OnValueChanged += (_, __) => EvaluatePlayerDeathUI();
            GameManager.Instance.p1Dead.OnValueChanged += (_, __) => EvaluatePlayerDeathUI();
            
            // ⭐ เช็คสถานะปัจจุบันทันที เผื่อว่าพังไปก่อนที่ลูกเรือจะขึ้นเรือมาฟังครับ
            EvaluatePlayerDeathUI();
        }
    }

    public void Start() { }

    public override void OnNetworkDespawn()
    {
        GameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
        GameManager.OnSystemEnemyDied -= HandleLocalEnemyDied;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.p0Dead.OnValueChanged -= (_, __) => EvaluatePlayerDeathUI();
            GameManager.Instance.p1Dead.OnValueChanged -= (_, __) => EvaluatePlayerDeathUI();
        }
    }

    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.Combat)
        {
            CalculateLocalTotalEnemies();
            _gameResultShown = false; // ✅ รีเซ็ตสถานะเมื่อเริ่มสู้
        }
        else
        {
            localHasCountedStart = false;
            _gameResultShown = false; // ✅ รีเซ็ตสถานะเมื่อกลับไปวางแผน
        }
    }

    private void CalculateLocalTotalEnemies()
    {
        if (GameManager.Instance == null) return;
        int total = 0;
        string draft = GameManager.Instance.systemWaveDraft.Value.ToString();
        if (!string.IsNullOrEmpty(draft))
        {
            string[] parts = draft.Split('|');
            foreach (string p in parts)
            {
                string[] sub = p.Split(':');
                if (sub.Length == 2) total += int.Parse(sub[1]);
            }
        }

        ulong myId = NetworkManager.Singleton.LocalClientId;
        if (myId == 0)
        {
            foreach (int count in GameManager.Instance.p1SentCounts) total += count;
        }
        else
        {
            foreach (int count in GameManager.Instance.p0SentCounts) total += count;
        }

        localRemainingEnemies = total;
        localHasCountedStart = true;
        Debug.Log($"<color=cyan>[EnemyTracker]</color> Calculated exact enemies for Combat: {total}");

        if (total == 0) ReportWaveClearedServerRpc(myId);
    }

    private void HandleLocalEnemyDied(int typeIndex)
    {
        if (!localHasCountedStart) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        localRemainingEnemies--;
        Debug.Log($"<color=cyan>[EnemyTracker]</color> Enemy Died! Remaining: {localRemainingEnemies}");

        if (localRemainingEnemies <= 0)
        {
            localHasCountedStart = false;
            ReportWaveClearedServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [Rpc(SendTo.Server)]
    private void ReportWaveClearedServerRpc(ulong senderClientId)
    {
        if (senderClientId == 0 && !p0Cleared.Value)
        {
            p0Cleared.Value = true;
        }
        else if (senderClientId != 0 && !p1Cleared.Value)
        {
            p1Cleared.Value = true;
        }
        EvaluateOnServer();
    }

    private void EvaluateOnServer()
    {
        if (!IsServer || phaseChangeQueued) return;
        bool p0Done = p0Cleared.Value;
        bool p1Done = p1Cleared.Value;

        if (p0Done && p1Done)
        {
            phaseChangeQueued = true;
            HideKillOpponentButtonClientRpc();
            ShowWaveClearClientRpc();
            return;
        }

        if (p0Done || p1Done)
        {
            if (countdownRunning) return;

            // ⭐ เช็คก่อนว่า "เป้าหมาย" (คนที่ยังไม่เคลียร์) ยังมีชีวิตอยู่ไหม? ถ้าตายแล้วไม่ต้องขึ้นปุ่ม
            bool winnerIsP0 = p0Done && !p1Done;
            bool targetDead = winnerIsP0 ? GameManager.Instance.p1Dead.Value : GameManager.Instance.p0Dead.Value;
            
            if (targetDead)
            {
                Debug.Log("<color=yellow>[EnemyTracker]</color> One side cleared, but opponent is already DEAD. Not showing Kill button.");
                return;
            }

            ShowKillOpponentButtonClientRpc(winnerIsP0);
        }
        else
        {
            // ⭐ กรณีที่ไม่มีใครเคลียร์เลย (เช่น โดนส่งมอนสเตอร์มาขัดจังหวะ) 
            // ให้สั่งหยุดการนับถอยหลังทั่วทั้งเซิร์ฟเวอร์
            HideKillOpponentButtonClientRpc();
            if (countdownRunning)
            {
                countdownRunning = false;
                CancelCountdownClientRpc();
            }
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void NotifyMidCombatSpawnRpc(ulong targetedClientId, int count, int typeIndex)
    {
        if (!IsServer) return;

        Debug.Log($"<color=orange>[EnemyTracker]</color> Mid-combat spawn detected for Client {targetedClientId}. Resetting clear status.");

        if (targetedClientId == 0) p0Cleared.Value = false;
        else p1Cleared.Value = false;

        // ⭐ รีเซ็ตสถานะการจบเวฟเพื่อให้ระบบเริ่ม Evaluate ใหม่ และหยุดการนับถ่อยหลัง 15 วิ (ถ้ามี)
        countdownRunning = false;
        phaseChangeQueued = false;
        
        NotifyMidCombatSpawnClientRpc(targetedClientId, count, typeIndex);
        EvaluateOnServer();
    }

    [ClientRpc]
    private void NotifyMidCombatSpawnClientRpc(ulong targetedClientId, int count, int typeIndex)
    {
        // ⭐ สำคัญ: เมื่อมีการส่งมอนสเตอร์ให้กัน (ไม่ว่าจะเครื่องไหน)
        // ให้หยุดการนับถ่อยหลัง 15 วิที่กำลังรันอยู่ในฉากทุกเครื่องทันที!
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
            SetUI(centerPanel, false);
            Debug.Log("<color=orange>[EnemyTracker]</color> Mid-combat spawn detected -> Stopping Countdown (Sync)!");
        }

        if (NetworkManager.Singleton.LocalClientId == targetedClientId)
        {
            if (!localHasCountedStart) 
            { 
                localRemainingEnemies = count; 
                localHasCountedStart = true; 
            }
            else 
            { 
                localRemainingEnemies += count; 
            }

            Debug.Log($"<color=orange>[EnemyTracker]</color> New mid-combat enemy for ME! Total Remaining: {localRemainingEnemies}");
            GameManager.OnEnemyIncoming?.Invoke(typeIndex);
        }
    }

    [ClientRpc]
    private void CancelCountdownClientRpc()
    {
        Debug.Log("<color=orange>[EnemyTracker]</color> Server requested Countdown Cancel.");
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        SetUI(centerPanel, false);
    }

    private void EvaluateOnClient() { }

    private void EvaluatePlayerDeathUI()
    {
        if (_gameResultShown) return;
        
        ulong myId = NetworkManager.Singleton.LocalClientId;
        if (GameManager.Instance == null) return;
        
        bool iAmDead = (myId == 0) ? GameManager.Instance.p0Dead.Value : GameManager.Instance.p1Dead.Value;
        bool opponentDead = (myId == 0) ? GameManager.Instance.p1Dead.Value : GameManager.Instance.p0Dead.Value;
        
        Debug.Log($"<color=cyan>[EnemyTracker]</color> Evaluation: MyID={myId}, iAmDead={iAmDead}, opponentDead={opponentDead} (p0Dead={GameManager.Instance.p0Dead.Value}, p1Dead={GameManager.Instance.p1Dead.Value})");
        
        if (opponentDead && killOpponentButton != null) 
        {
            SetUI(killOpponentButton, false);
        }
        
        // ⭐ ลำดับความสำคัญ: ถ้าเราตาย (iAmDead) คือแพ้แน่นอน
        if (iAmDead)
        {
            Debug.Log("<color=red>[EnemyTracker]</color> Result: I am DEAD -> LOSE");
            _gameResultShown = true;
            StopAllCoroutines();
            ShowResultUI(false); 
        }
        // ถ้าเรายังไม่ตาย แต่ฝ่ายตรงข้ามตายแล้ว คือชนะ
        else if (opponentDead)
        {
            Debug.Log("<color=green>[EnemyTracker]</color> Result: OPPONENT dead -> WIN");
            _gameResultShown = true;
            StopAllCoroutines();
            ShowResultUI(true); 
        }
    }

    [ClientRpc]
    private void ShowKillOpponentButtonClientRpc(bool winnerIsP0)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool iAmWinner = (winnerIsP0 && myId == 0) || (!winnerIsP0 && myId != 0);
        if (iAmWinner)
        {
            if (GameManager.Instance != null)
            {
                bool opponentDead = (myId == 0) ? GameManager.Instance.p1Dead.Value : GameManager.Instance.p0Dead.Value;
                if (opponentDead) return;
            }
            SetUI(killOpponentButton, true);
        }
        else { SetUI(killOpponentButton, false); }
    }

    [ClientRpc]
    private void HideKillOpponentButtonClientRpc() { SetUI(killOpponentButton, false); }

    private void OnKillOpponentPressed()
    {
        SetUI(killOpponentButton, false);
        RequestKillOpponentServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestKillOpponentServerRpc(ulong senderClientId)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Combat) return;
        if (countdownRunning || phaseChangeQueued) return;
        if (!p0Cleared.Value && !p1Cleared.Value) return;

        countdownRunning = true;
        HideKillOpponentButtonClientRpc();
        bool targetIsP0 = senderClientId != 0;
        StartDeathCountdownClientRpc(targetIsP0);
    }

    [ClientRpc]
    private void ShowWaveClearClientRpc()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(WaveClearRoutine());
    }

    [ClientRpc]
    private void StartDeathCountdownClientRpc(bool targetIsP0)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool iAmTarget = (targetIsP0 && myId == 0) || (!targetIsP0 && myId != 0);
        if (!iAmTarget) return;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(DeathCountdownRoutine());
    }

    [ClientRpc]
    public void ShowGameResultClientRpc(ulong loserClientId)
    {
        // ✅ ตรวจสอบ Flag เพื่อป้องกันการทำงานซ้ำ (เหมือนใน SoloEnemyTracker)
        if (_gameResultShown) return;
        _gameResultShown = true;

        ulong myId = NetworkManager.Singleton.LocalClientId;
        StopAllCoroutines();
        bool iWon = (myId != loserClientId);
        
        if (iWon)
        {
            // ฝั่งชนะ: ขึ้น Victory ทันที ไม่ต้องรอคัตซีนป้อมพัง
            ShowResultUI(true);
            Debug.Log($"<color=cyan>[EnemyTracker]</color> Game Result Shown. I Won: {iWon}");
        }
        else
        {
            // ฝั่งแพ้: เล่นคัตซีนป้อมพังก่อนแล้วค่อยขึ้น Game Over
            activeCoroutine = StartCoroutine(PlayLoseCinematicThenUI());
        }
    }

    /// <summary>
    /// แสดงผลลัพธ์ Win/Lose, ปิด UI อื่น, แจ้ง GameManager และเล่นเสียง
    /// </summary>
    private void ShowResultUI(bool iWon)
    {
        Debug.Log($"<color=cyan>[EnemyTracker]</color> ShowResultUI called - iWon: {iWon}");
        
        if (youWinUI != null) youWinUI.SetActive(iWon);
        if (youLostUI != null) youLostUI.SetActive(!iWon);
        if (centerPanel != null) centerPanel.SetActive(false); // ✅ ปิดหน้าต่างนับถอยหลังด้วยเมื่อจบเกม
        if (killOpponentButton != null) killOpponentButton.SetActive(false);

        // ⭐ ยกขึ้นมาหน้าสุด เพื่อไม่ให้ UI อื่นบัง
        GameObject resultUI = iWon ? youWinUI : youLostUI;
        if (resultUI != null && resultUI.transform.parent != null)
        {
            resultUI.transform.SetAsLastSibling();
            Debug.Log($"<color=cyan>[EnemyTracker]</color> Result UI moved to front: {resultUI.name}");
        }
        else if (resultUI != null && resultUI.transform.parent == null)
        {
            Debug.LogWarning($"<color=yellow>[EnemyTracker]</color> Result UI parent is null: {resultUI.name}");
        }
        else if (resultUI == null)
        {
            Debug.LogError($"<color=red>[EnemyTracker ERROR]</color> Result UI is NULL! {(iWon ? "youWinUI" : "youLostUI")} not assigned.");
        }

        GameManager.Instance?.OnGameEnded();

        if (iWon) AudioManager.Instance?.PlayWin();
        else AudioManager.Instance?.PlayLose();
    }

    /// <summary>
    /// สำหรับฝั่งที่แพ้: โฟกัสกล้องไปที่ฐานของตัวเอง, เล่นเอฟเฟกต์จมป้อม (ถ้ามี) แล้วค่อยขึ้น Game Over
    /// </summary>
    private IEnumerator PlayLoseCinematicThenUI()
    {
        Debug.Log("<color=red>[EnemyTracker]</color> ===== PlayLoseCinematicThenUI STARTED =====");
        
        // หา BaseHealth ที่เป็นฐานของฝั่งเรา (เลือกตัวที่อยู่ใกล้ Player ของเรามากที่สุด)
        BaseHealth myBase = null;
        var bases = Object.FindObjectsByType<BaseHealth>(FindObjectsSortMode.None);
        if (bases != null && bases.Length > 0)
        {
            // หา Player ของเรา
            Transform myPlayer = null;
            var players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in players)
            {
                var netObj = p.GetComponent<NetworkObject>();
                if (netObj != null && netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                {
                    myPlayer = p.transform;
                    break;
                }
            }

            if (myPlayer != null)
            {
                float bestDist = float.MaxValue;
                foreach (var b in bases)
                {
                    float d = Vector3.Distance(myPlayer.position, b.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        myBase = b;
                    }
                }
            }
            else
            {
                // ถ้าไม่เจอ Player ให้เลือก Base ตัวแรกเป็นค่าเริ่มต้น
                myBase = bases[0];
            }
        }

        HealthSystem baseHealthSystem = null;
        if (myBase != null)
        {
            baseHealthSystem = myBase.GetComponentInChildren<HealthSystem>();

            // 1) โฟกัสกล้องไปที่มุมมองเริ่มต้น (หุ่นนริศ) ก่อนเป็นอันดับแรก
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.FocusInitialView();
            }

            // 2) รอกล้องแพนไปถึง
            float arriveDelay = (baseHealthSystem != null) ? baseHealthSystem.cameraArriveDelay : 0.8f;
            if (arriveDelay > 0f)
            {
                yield return new WaitForSeconds(arriveDelay);
            }

            // 3) สร้างเอฟเฟกต์ระเบิด/พังหลังจากกล้องมาถึงแล้ว
            if (baseHealthSystem != null && baseHealthSystem.deathVfxPrefab != null)
            {
                GameObject vfxInstance = Instantiate(
                    baseHealthSystem.deathVfxPrefab,
                    myBase.transform.position,
                    Quaternion.identity
                );

                if (baseHealthSystem.deathVfxDuration > 0f)
                {
                    Destroy(vfxInstance, baseHealthSystem.deathVfxDuration);
                }
            }

            // 3) ถ้ามี HealthSystem และเปิด sinkOnDeath ให้เล่นอนิเมชันจมลง
            yield return baseHealthSystem.ExecuteSinkAnimation(myBase.transform);

            // 4) ดีเลย์ก่อนขึ้น Game Over ตามที่ตั้งใน HealthSystem
            if (baseHealthSystem != null && baseHealthSystem.gameOverDelay > 0f)
            {
                yield return new WaitForSeconds(baseHealthSystem.gameOverDelay);
            }
        }

        // แสดงผลแพ้พร้อมเล่นเสียง/ล็อคเกม
        Debug.Log("<color=red>[EnemyTracker]</color> About to call ShowResultUI(false)");
        ShowResultUI(false);
        Debug.Log("<color=red>[EnemyTracker]</color> ===== PlayLoseCinematicThenUI FINISHED =====");
    }

    [ClientRpc]
    public void ForceClientsToMenuClientRpc()
    {
        if (NetworkManager.Singleton.IsHost) return;
        StartCoroutine(ClientForceReturnRoutine());
    }

    private IEnumerator ClientForceReturnRoutine()
    {
        if (centerPanel != null) centerPanel.SetActive(true);
        if (centerText != null) centerText.text = "Host has disconnected...";
        
        // ⭐ ปลดล็อคเมาส์เพื่อให้ใช้งานในหน้าเมนูได้ทันที
        GameManager.Instance?.ForceUnlockCursor();
        
        yield return new WaitForSeconds(1.5f);
        NetworkManager.Singleton.Shutdown();
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("MenuSceneTest");
    }

    private IEnumerator DeathCountdownRoutine()
    {
        SetUI(centerPanel, true);
        for (float t = countdownDuration; t > 0f; t -= 1f)
        {
            if (centerText != null)
                centerText.text = $"Other Wave Clear!\nYou die in {Mathf.CeilToInt(t)}s";
            yield return new WaitForSeconds(1f);
        }
        SetUI(centerPanel, false);
        KillLocalPlayer();
    }

    private IEnumerator WaveClearRoutine()
    {
        SetUI(centerPanel, true);
        for (float t = countdownDuration; t > 0f; t -= 1f)
        {
            if (centerText != null)
                centerText.text = $"Wave Clear!\nNext wave in {Mathf.CeilToInt(t)}s";
            yield return new WaitForSeconds(1f);
        }
        SetUI(centerPanel, false);
        if (IsServer) GameManager.Instance?.RequestNextPhase();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ResetForNewWaveServerRpc()
    {
        p0EnemyCount.Value = 999;
        p1EnemyCount.Value = 999;
        p0Cleared.Value = false;
        p1Cleared.Value = false;
        countdownRunning = false;
        phaseChangeQueued = false;
        // การรีเซ็ต _gameResultShown จะทำผ่าน HandlePhaseChanged ในแต่ละ Client
        HideKillOpponentButtonClientRpc();
    }

    private void KillLocalPlayer()
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            var netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                continue;

            var pc = p.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (deathLightningVFX != null)
                {
                    GameObject vfx = Instantiate(deathLightningVFX, p.transform.position + Vector3.up * 8f, deathLightningVFX.transform.rotation);
                    Destroy(vfx, 2f);
                }
                pc.TakeDamage(999999);
                return;
            }

            var archer = p.GetComponent<Archer>();
            if (archer != null)
            {
                if (deathLightningVFX != null)
                {
                    GameObject vfx = Instantiate(deathLightningVFX, p.transform.position + Vector3.up * 8f, deathLightningVFX.transform.rotation);
                    Destroy(vfx, 2f);
                }
                archer.TakeDamage(999999);
                return;
            }
        }
    }

    private static void SetUI(GameObject ui, bool active)
    { GameManager.SafeSetActive(ui, active, "EnemyTracker"); }
}