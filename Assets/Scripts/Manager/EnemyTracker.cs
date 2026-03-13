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
    public GameObject      centerPanel;
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
    // จำนวน Enemy แต่ละฝั่ง (อัปเดตทุก tick จาก MinimapDataSender)
    private NetworkVariable<int>  p0EnemyCount = new NetworkVariable<int>(999);
    private NetworkVariable<int>  p1EnemyCount = new NetworkVariable<int>(999);

    // ว่าฝั่งนั้น "คลีย์แล้ว" หรือยัง
    private NetworkVariable<bool> p0Cleared    = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> p1Cleared    = new NetworkVariable<bool>(false);

    // ─── State ────────────────────────────────────────────────────
    // ─── State ────────────────────────────────────────────────────
    private Coroutine activeCoroutine;
    private bool      countdownRunning   = false;
    private bool      phaseChangeQueued  = false;

    // การนับศัตรูในเครื่องตัวเองแบบแม่นยำเป๊ะๆ 100%
    private int       localRemainingEnemies = 0;
    private bool      localHasCountedStart  = false;

    // ────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        SetUI(centerPanel,          false);
        SetUI(killOpponentButton,   false);
        SetUI(youWinUI,             false);
        SetUI(youLostUI,            false);
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
        // กด K เพื่อกดปุ่ม Kill Opponent (เฉพาะตอนที่ปุ่ม Active อยู่)
        if (Input.GetKeyDown(KeyCode.K) &&
            killOpponentButton != null &&
            killOpponentButton.activeSelf)
        {
            OnKillOpponentPressed();
        }
    }

    public override void OnNetworkSpawn()
    {
        // ฟัง NetworkVariable เพื่ออัปเดต UI ทุก Client
        p0Cleared.OnValueChanged += (_, __) => EvaluateOnClient();
        p1Cleared.OnValueChanged += (_, __) => EvaluateOnClient();

        GameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        GameManager.OnSystemEnemyDied += HandleLocalEnemyDied;
    }

    public void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.p0Dead.OnValueChanged += (_, __) => EvaluatePlayerDeathUI();
            GameManager.Instance.p1Dead.OnValueChanged += (_, __) => EvaluatePlayerDeathUI();
        }
    }

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

    // ────────────────────────────────────────────────────────────
    //  LOCAL COUNTING (นับจำนวนแบบเป๊ะๆ แทนการใช้ Minimap)
    // ────────────────────────────────────────────────────────────
    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.Combat)
        {
            CalculateLocalTotalEnemies();
        }
        else
        {
            localHasCountedStart = false;
        }
    }

    private void CalculateLocalTotalEnemies()
    {
        if (GameManager.Instance == null) return;

        int total = 0;

        // 1. รับจำนวน System Enemies รันตาม Draft
        string draft = GameManager.Instance.systemWaveDraft.Value.ToString();
        if (!string.IsNullOrEmpty(draft))
        {
            string[] parts = draft.Split('|');
            foreach (string p in parts)
            {
                string[] sub = p.Split(':');
                if (sub.Length == 2)
                    total += int.Parse(sub[1]);
            }
        }

        // 2. รับจำนวน Sent Enemies จากเพื่อน
        ulong myId = NetworkManager.Singleton.LocalClientId;
        if (myId == 0) // เราคือ Host -> ดูโควต้าที่ Client (p1) ส่งมาหาเรา
        {
            foreach (int count in GameManager.Instance.p1SentCounts)
                total += count;
        }
        else // เราคือ Client -> ดูโควต้าที่ Host (p0) ส่งมาหาเรา
        {
            foreach (int count in GameManager.Instance.p0SentCounts)
                total += count;
        }

        localRemainingEnemies = total;
        localHasCountedStart = true;
        Debug.Log($"<color=cyan>[EnemyTracker]</color> Calculated exact enemies for Combat: {total}");

        // ถ้าบังเอิญเวฟนี้ไม่มีมอนสเตอร์เลยสักตัว ก็ถือว่าเคลียร์ทันที
        if (total == 0)
        {
            ReportWaveClearedServerRpc(myId);
        }
    }

    private void HandleLocalEnemyDied(int typeIndex)
    {
        if (!localHasCountedStart) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        localRemainingEnemies--;
        Debug.Log($"<color=cyan>[EnemyTracker]</color> Enemy Died! Remaining: {localRemainingEnemies}");

        if (localRemainingEnemies <= 0)
        {
            localHasCountedStart = false; // นับเสร็จแล้ว
            ReportWaveClearedServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [Rpc(SendTo.Server)]
    private void ReportWaveClearedServerRpc(ulong senderClientId)
    {
        if (senderClientId == 0 && !p0Cleared.Value)
        {
            p0Cleared.Value = true;
            Debug.Log("<color=cyan>[EnemyTracker]</color> P0 Enemy หมดแล้ว! (Local Count Confirmed)");
        }
        else if (senderClientId != 0 && !p1Cleared.Value)
        {
            p1Cleared.Value = true;
            Debug.Log("<color=cyan>[EnemyTracker]</color> P1 Enemy หมดแล้ว! (Local Count Confirmed)");
        }

        EvaluateOnServer();
    }

    // ────────────────────────────────────────────────────────────
    //  SERVER — ตัดสินว่าจะทำอะไรต่อ
    // ────────────────────────────────────────────────────────────
    private void EvaluateOnServer()
    {
        if (!IsServer || phaseChangeQueued) return;

        bool p0Done = p0Cleared.Value;
        bool p1Done = p1Cleared.Value;

        // 🎉 ทั้งสองฝั่งหมด → Wave Clear + เปลี่ยนเฟส
        if (p0Done && p1Done)
        {
            phaseChangeQueued = true;
            HideKillOpponentButtonClientRpc();  // ซ่อนปุ่มก่อน
            ShowWaveClearClientRpc();
            return;
        }

        // ⏳ ฝั่งใดหมดก่อน → โชว์ปุ่ม Kill Opponent ให้คนที่คลีย์
        if (p0Done || p1Done)
        {
            if (countdownRunning) return; // ถ้าเริ่มนับถอยหลังไปแล้ว ไม่ต้องหาคนชนะใหม่

            // winnerIsP0 = true ถ้า P0 คลีย์ก่อน
            bool winnerIsP0 = p0Done && !p1Done;
            ShowKillOpponentButtonClientRpc(winnerIsP0);
        }
        else
        {
            // ถ้ายังไม่มีใครเคลียร์เลย (เช่น โดนส่งศัตรูมาเพิ่มจนไม่เคลียร์แล้ว)
            HideKillOpponentButtonClientRpc();
            countdownRunning = false; // หยุดคูลดาวน์ (ถ้ายังไม่ทันเริ่ม RPC ฝ่ายตรงข้าม)
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void NotifyMidCombatSpawnRpc(ulong targetedClientId, int count, int typeIndex)
    {
        if (!IsServer) return;

        if (targetedClientId == 0) p0Cleared.Value = false;
        else p1Cleared.Value = false;

        NotifyMidCombatSpawnClientRpc(targetedClientId, count, typeIndex);
        EvaluateOnServer();
    }

    [ClientRpc]
    private void NotifyMidCombatSpawnClientRpc(ulong targetedClientId, int count, int typeIndex)
    {
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
            Debug.Log($"<color=cyan>[EnemyTracker]</color> Received {count} mid-combat enemies (Type {typeIndex}). New remaining: {localRemainingEnemies}");
            
            // ⭐ แจ้ง UI ว่ามีมอนสเตอร์มาใหม่
            GameManager.OnEnemyIncoming?.Invoke(typeIndex);
        }
    }

    // ────────────────────────────────────────────────────────────
    //  CLIENT — ฟัง NetworkVariable (ไม่จำเป็นต้องทำอะไรเพิ่ม
    //            เพราะ Server ส่ง RPC มาตรงๆ อยู่แล้ว)
    // ────────────────────────────────────────────────────────────
    private void EvaluateOnClient() { /* ไว้ขยายในอนาคต */ }

    // ────────────────────────────────────────────────────────────
    //  KILL OPPONENT BUTTON
    // ────────────────────────────────────────────────────────────

    private void EvaluatePlayerDeathUI()
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        
        if (GameManager.Instance == null) return;
        
        bool opponentDead = (myId == 0) ? GameManager.Instance.p1Dead.Value : GameManager.Instance.p0Dead.Value;
        
        // ถ้าฝั่งตรงข้ามตายแล้ว ให้ซ่อนปุ่มของฝั่งเราทันที
        if (opponentDead && killOpponentButton != null)
        {
            SetUI(killOpponentButton, false);
            Debug.Log("<color=cyan>[EnemyTracker]</color> Opponent is dead. Hiding Kill Opponent button.");
        }
    }

    /// <summary>โชว์ปุ่ม Kill Opponent เฉพาะคนที่คลีย์ Enemy ก่อน</summary>
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
                if (opponentDead) return; // ฝั่งตรงข้ามตายแล้วไม่ต้องโชว์
            }
            
            SetUI(killOpponentButton, true);
        }
        else
        {
            SetUI(killOpponentButton, false);
        }
    }

    /// <summary>ซ่อนปุ่มทุก Client</summary>
    [ClientRpc]
    private void HideKillOpponentButtonClientRpc()
    {
        SetUI(killOpponentButton, false);
    }

    /// <summary>ผู้เล่นกดปุ่ม Kill Opponent → แจ้ง Server เริ่ม Countdown ฝั่งตรงข้าม</summary>
    private void OnKillOpponentPressed()
    {
        SetUI(killOpponentButton, false); // ซ่อนปุ่มทันทีหลังกด
        RequestKillOpponentServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestKillOpponentServerRpc(ulong senderClientId)
    {
        // ต้องอยู่ใน Combat เท่านั้น + ต้องมีฝั่งใดฝั่งหนึ่งคลีย์จริงๆ
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentPhase != GamePhase.Combat) return;
        if (countdownRunning || phaseChangeQueued) return;
        if (!p0Cleared.Value && !p1Cleared.Value) return;

        countdownRunning = true;
        HideKillOpponentButtonClientRpc();

        // ฝั่งตรงข้ามต้องตาย — targetIsP0 = true ถ้าคนกดเป็น P1 (ก็คือ P0 ต้องตาย)
        bool targetIsP0 = senderClientId != 0;
        StartDeathCountdownClientRpc(targetIsP0);
    }

    // ────────────────────────────────────────────────────────────
    //  CLIENT RPCs
    // ────────────────────────────────────────────────────────────

    /// <summary>ทั้งสองฝั่ง Enemy หมด → แสดง Wave Clear + รอ 15 วิ + เปลี่ยนเฟส</summary>
    [ClientRpc]
    private void ShowWaveClearClientRpc()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(WaveClearRoutine());
    }

    /// <summary>ฝั่งใดฝั่งหนึ่ง Enemy หมดก่อน → บังคับฝั่งตรงข้ามตายใน 15 วิ</summary>
    [ClientRpc]
    private void StartDeathCountdownClientRpc(bool targetIsP0)
    {
        ulong myId      = NetworkManager.Singleton.LocalClientId;
        bool  iAmTarget = (targetIsP0 && myId == 0) || (!targetIsP0 && myId != 0);
        if (!iAmTarget) return; // ไม่ใช่ฝั่งเรา ไม่ต้องทำอะไร

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(DeathCountdownRoutine());
    }

    [ClientRpc]
    public void ShowGameResultClientRpc(ulong loserClientId)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        StopAllCoroutines();
        bool iWon = (myId != loserClientId);
        if (youWinUI != null)  youWinUI.SetActive(iWon);
        if (youLostUI != null) youLostUI.SetActive(!iWon);
        if (centerPanel != null)        centerPanel.SetActive(false);
        if (killOpponentButton != null) killOpponentButton.SetActive(false);
        GameManager.Instance?.OnGameEnded();

        // เล่นเสียงชนะ/แพ้
        if (iWon) AudioManager.Instance?.PlayWin();
        else      AudioManager.Instance?.PlayLose();
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
        if (centerText  != null) centerText.text = "Host has disconnected...";
        yield return new WaitForSeconds(1.5f);
        NetworkManager.Singleton.Shutdown();
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("MenuSceneTest");
    }
    // ────────────────────────────────────────────────────────────
    //  COROUTINES
    // ────────────────────────────────────────────────────────────

    // ฝั่งตรงข้ามคลีย์ก่อน → แสดงข้อความ + นับถอยหลัง → ตาย
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

    // ทั้งสองฝั่งหมดพร้อมกัน → แสดงข้อความ + รอ → เปลี่ยนเฟส
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

    // ────────────────────────────────────────────────────────────
    //  RESET (เรียกจาก GameManager ตอนเปลี่ยนเป็น Planning)
    // ────────────────────────────────────────────────────────────
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ResetForNewWaveServerRpc()
    {
        p0EnemyCount.Value = 999;
        p1EnemyCount.Value = 999;
        p0Cleared.Value    = false;
        p1Cleared.Value    = false;
        countdownRunning   = false;
        phaseChangeQueued  = false;
        HideKillOpponentButtonClientRpc();
    }

    // ────────────────────────────────────────────────────────────
    //  HELPERS
    // ────────────────────────────────────────────────────────────
    private void KillLocalPlayer()
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            var netObj = p.GetComponent<NetworkObject>();
            
            // ถ้ามีบอกว่าเป็นของคนอื่นให้ข้ามไป แต่ถ้า "ไม่มี NetworkObject" ให้ถือว่าเป็นตัวละครของเครื่องนี้เลย
            if (netObj != null && netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                continue;

            // ลองหา PlayerController ก่อน (Warrior)
            var pc = p.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (deathLightningVFX != null)
                {
                    // เสกจากจุดที่สูงขึ้นไป 5 เมตรจากตัวผู้เล่นแบบรักษาการหมุนตาม Prefab ต้นฉบับ
                    GameObject vfx = Instantiate(deathLightningVFX, p.transform.position + Vector3.up * 8f, deathLightningVFX.transform.rotation);
                    Destroy(vfx, 2f);
                }

                pc.TakeDamage(999999);
                Debug.Log("[EnemyTracker] บังคับ PlayerController ตาย");
                return;
            }

            // ถ้าไม่มี ลองหา Archer
            var archer = p.GetComponent<Archer>();
            if (archer != null)
            {
                if (deathLightningVFX != null)
                {
                    // เสกจากจุดที่สูงขึ้นไป 5 เมตรแบบรักษาการหมุนตาม Prefab ต้นฉบับ
                    GameObject vfx = Instantiate(deathLightningVFX, p.transform.position + Vector3.up * 8f, deathLightningVFX.transform.rotation);
                    Destroy(vfx, 2f);
                }

                archer.TakeDamage(999999);
                Debug.Log("[EnemyTracker] บังคับ Archer ตาย");
                return;
            }
        }
    }

    private static void SetUI(GameObject ui, bool active)
        { GameManager.SafeSetActive(ui, active, "EnemyTracker"); }
}