using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    // ─── NetworkVariables (Server เขียน / ทุกคนอ่าน) ──────────────
    // จำนวน Enemy แต่ละฝั่ง (อัปเดตทุก tick จาก MinimapDataSender)
    private NetworkVariable<int>  p0EnemyCount = new NetworkVariable<int>(999);
    private NetworkVariable<int>  p1EnemyCount = new NetworkVariable<int>(999);

    // ว่าฝั่งนั้น "คลีย์แล้ว" หรือยัง
    private NetworkVariable<bool> p0Cleared    = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> p1Cleared    = new NetworkVariable<bool>(false);

    // ─── State ────────────────────────────────────────────────────
    private Coroutine activeCoroutine;
    private bool      countdownRunning   = false;
    private bool      phaseChangeQueued  = false;
    private bool      p0EverHadEnemies   = false; // ต้องเคยเจอ Enemy > 0 ก่อนถึงจะนับว่าคลีย์
    private bool      p1EverHadEnemies   = false;

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

    public override void OnNetworkSpawn()
    {
        // ฟัง NetworkVariable เพื่ออัปเดต UI ทุก Client
        p0Cleared.OnValueChanged += (_, __) => EvaluateOnClient();
        p1Cleared.OnValueChanged += (_, __) => EvaluateOnClient();
    }

    // ────────────────────────────────────────────────────────────
    //  SERVER — รับรายงาน Enemy Count จาก MinimapDataSender
    // ────────────────────────────────────────────────────────────
    [Rpc(SendTo.Server)]
    public void ReportEnemyCountServerRpc(int count, ulong senderClientId)
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        // อัปเดตค่าให้ตรงฝั่ง
        if (senderClientId == 0) p0EnemyCount.Value = count;
        else                     p1EnemyCount.Value = count;

        // ต้องเคยเจอ Enemy > 0 ก่อน ถึงจะนับว่า "คลีย์" ได้
        if (count > 0)
        {
            if (senderClientId == 0) p0EverHadEnemies = true;
            else                     p1EverHadEnemies = true;
        }

        // ตรวจ Clear
        if (senderClientId == 0 && !p0Cleared.Value && count == 0 && p0EverHadEnemies)
        {
            p0Cleared.Value = true;
            Debug.Log("<color=cyan>[EnemyTracker]</color> P0 Enemy หมดแล้ว!");
        }
        else if (senderClientId != 0 && !p1Cleared.Value && count == 0 && p1EverHadEnemies)
        {
            p1Cleared.Value = true;
            Debug.Log("<color=cyan>[EnemyTracker]</color> P1 Enemy หมดแล้ว!");
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
        if ((p0Done || p1Done) && !countdownRunning)
        {
            // winnerIsP0 = true ถ้า P0 คลีย์ก่อน
            bool winnerIsP0 = p0Done && !p1Done;
            ShowKillOpponentButtonClientRpc(winnerIsP0);
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

    /// <summary>โชว์ปุ่ม Kill Opponent เฉพาะคนที่คลีย์ Enemy ก่อน</summary>
    [ClientRpc]
    private void ShowKillOpponentButtonClientRpc(bool winnerIsP0)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool iAmWinner = (winnerIsP0 && myId == 0) || (!winnerIsP0 && myId != 0);
        SetUI(killOpponentButton, iAmWinner);
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

    [Rpc(SendTo.Server)]
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

    /// <summary>ป้อมพัง → แสดง You Win / You Lost</summary>
    [ClientRpc]
    public void ShowGameResultClientRpc(ulong loserClientId)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        SetUI(youLostUI, myId == loserClientId);
        SetUI(youWinUI,  myId != loserClientId);

        // หยุด Countdown ที่รันอยู่ (ถ้ามี)
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        SetUI(centerPanel, false);
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
    [Rpc(SendTo.Server)]
    public void ResetForNewWaveServerRpc()
    {
        p0EnemyCount.Value = 999;
        p1EnemyCount.Value = 999;
        p0Cleared.Value    = false;
        p1Cleared.Value    = false;
        countdownRunning   = false;
        phaseChangeQueued  = false;
        p0EverHadEnemies   = false;
        p1EverHadEnemies   = false;
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
            if (netObj == null || netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                continue;

            // ลองหา PlayerController ก่อน (Warrior)
            var pc = p.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(999999);
                Debug.Log("[EnemyTracker] บังคับ PlayerController ตาย");
                return;
            }

            // ถ้าไม่มี ลองหา Archer
            var archer = p.GetComponent<Archer>();
            if (archer != null)
            {
                archer.TakeDamage(999999);
                Debug.Log("[EnemyTracker] บังคับ Archer ตาย");
                return;
            }
        }
    }

    private static void SetUI(GameObject ui, bool active)
        { if (ui != null) ui.SetActive(active); }
}