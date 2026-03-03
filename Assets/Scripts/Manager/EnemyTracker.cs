using Unity.Netcode;
using UnityEngine;
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
    private bool      countdownRunning  = false;
    private bool      phaseChangeQueued = false;

    // ────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        SetUI(centerPanel, false);
        SetUI(youWinUI,    false);
        SetUI(youLostUI,   false);
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

        // ตรวจ Clear
        if (senderClientId == 0 && !p0Cleared.Value && count == 0)
        {
            p0Cleared.Value = true;
            Debug.Log("<color=cyan>[EnemyTracker]</color> P0 Enemy หมดแล้ว!");
        }
        else if (senderClientId != 0 && !p1Cleared.Value && count == 0)
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
            ShowWaveClearClientRpc();
            return;
        }

        // ⏳ ฝั่งใดหมดก่อน → บังคับฝั่งตรงข้ามตาย
        // p1Done=true, p0ยังไม่หมด → p0 ต้องตาย (targetIsP0=true)
        // p0Done=true, p1ยังไม่หมด → p1 ต้องตาย (targetIsP0=false)
        if ((p0Done || p1Done) && !countdownRunning)
        {
            countdownRunning = true;
            bool targetIsP0 = p1Done && !p0Done;
            StartDeathCountdownClientRpc(targetIsP0);
        }
    }

    // ────────────────────────────────────────────────────────────
    //  CLIENT — ฟัง NetworkVariable (ไม่จำเป็นต้องทำอะไรเพิ่ม
    //            เพราะ Server ส่ง RPC มาตรงๆ อยู่แล้ว)
    // ────────────────────────────────────────────────────────────
    private void EvaluateOnClient() { /* ไว้ขยายในอนาคต */ }

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