using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class WavePreviewUI : MonoBehaviour
{
    [Header("Original UI Panels")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject combatPanel;

    [Header("Original UI Settings")]
    [SerializeField] private GameObject planningIconPrefab;
    [SerializeField] private GameObject combatIconPrefab;
    [SerializeField] private Transform planningContainer;
    [SerializeField] private Transform combatContainer;

    [Header("Sent Enemies UI (Optional)")]
    [SerializeField] private GameObject sentEnemiesPanel;
    [SerializeField] private Transform sentEnemiesContainer;
    [SerializeField] private GameObject sentIconPrefab;

    private Dictionary<int, int> incomingCounts = new Dictionary<int, int>();
    private Dictionary<int, int> sentCounts = new Dictionary<int, int>();
    private Dictionary<int, int> deathCounts = new Dictionary<int, int>();
    private List<WaveIconItem> combatIcons = new List<WaveIconItem>();

    private bool hasInitialized = false;

    void OnEnable()  => TryInitialize();
    void Start()     => TryInitialize();

    void Update()
    {
        if (!hasInitialized) TryInitialize();
    }

    private void TryInitialize()
    {
        if (hasInitialized || GameManager.Instance == null) return;

        GameManager.Instance.systemWaveDraft.OnValueChanged += OnWaveDraftChanged;
        GameManager.OnSystemEnemyDied += HandleEnemyDeath;
        GameManager.OnPhaseChangedGlobal += RefreshLayoutOnPhaseChange;

        GameManager.Instance.p0SentCounts.OnListChanged += OnSentListChanged;
        GameManager.Instance.p1SentCounts.OnListChanged += OnSentListChanged;
        GameManager.OnEnemyIncoming += HandleIncomingEnemy;

        hasInitialized = true;
        RefreshLayout();
        Debug.Log($"<color=cyan>[WavePreviewUI]</color> Initialized. Phase: {GameManager.Instance.CurrentPhase}");
    }

    void OnDisable()
    {
        if (!hasInitialized || GameManager.Instance == null) return;

        GameManager.Instance.systemWaveDraft.OnValueChanged -= OnWaveDraftChanged;
        GameManager.OnSystemEnemyDied -= HandleEnemyDeath;
        GameManager.OnPhaseChangedGlobal -= RefreshLayoutOnPhaseChange;

        GameManager.Instance.p0SentCounts.OnListChanged -= OnSentListChanged;
        GameManager.Instance.p1SentCounts.OnListChanged -= OnSentListChanged;
        GameManager.OnEnemyIncoming -= HandleIncomingEnemy;

        hasInitialized = false;
    }

    private void OnSentListChanged(NetworkListEvent<int> changeEvent) => RefreshLayout();

    private void RefreshLayoutOnPhaseChange(GamePhase phase)
    {
        if (phase == GamePhase.Planning)
            deathCounts.Clear();
        RefreshLayout();
    }

    private void OnWaveDraftChanged(FixedString512Bytes oldVal, FixedString512Bytes newVal) => RefreshLayout();

    private void RefreshLayout()
    {
        if (GameManager.Instance == null) return;

        bool isPlanning = GameManager.Instance.CurrentPhase == GamePhase.Planning;

        GameManager.SafeSetActive(planningPanel,    isPlanning,  "WavePreviewUI");
        GameManager.SafeSetActive(combatPanel,      !isPlanning, "WavePreviewUI");
        GameManager.SafeSetActive(sentEnemiesPanel, isPlanning,  "WavePreviewUI");

        // --- คำนวณ Incoming ---
        incomingCounts.Clear();
        string draft = GameManager.Instance.systemWaveDraft.Value.ToString();

        if (GameManager.Instance.systemEnemyPool == null || GameManager.Instance.systemEnemyPool.Length == 0)
            Debug.LogWarning("<color=red>[WavePreviewUI]</color> systemEnemyPool is NULL or EMPTY!");

        if (!string.IsNullOrEmpty(draft))
        {
            foreach (string p in draft.Split('|'))
            {
                string[] sub = p.Split(':');
                if (sub.Length == 2)
                    incomingCounts[int.Parse(sub[0])] = int.Parse(sub[1]);
            }
        }

        // รวม Incoming จากที่เพื่อนส่งมา (Combat เท่านั้น)
        if (!isPlanning)
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;
            var opponentList = myId == 0 ? GameManager.Instance.p1SentCounts : GameManager.Instance.p0SentCounts;
            for (int i = 0; i < opponentList.Count; i++)
                AddCountToDict(incomingCounts, i, opponentList[i]);
        }

        // --- คำนวณ Sent (Planning เท่านั้น) ---
        sentCounts.Clear();
        if (isPlanning)
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;
            var myList = myId == 0 ? GameManager.Instance.p0SentCounts : GameManager.Instance.p1SentCounts;
            for (int i = 0; i < myList.Count; i++)
                AddCountToDict(sentCounts, i, myList[i]);
        }

        // หักยอดที่ตายไปแล้ว (Combat เท่านั้น)
        if (!isPlanning)
        {
            foreach (var pair in deathCounts)
            {
                if (incomingCounts.ContainsKey(pair.Key))
                {
                    incomingCounts[pair.Key] = Mathf.Max(0, incomingCounts[pair.Key] - pair.Value);
                }
            }
        }

        // --- อัปเดต UI ---
        UpdateContainerWithCounts(planningContainer,     incomingCounts, planningIconPrefab,              true);
        UpdateContainerWithCounts(combatContainer,       incomingCounts, combatIconPrefab,                false);
        UpdateContainerWithCounts(sentEnemiesContainer,  sentCounts,     sentIconPrefab ?? planningIconPrefab, false);
    }

    private void AddCountToDict(Dictionary<int, int> dict, int index, int count)
    {
        if (count <= 0) return;
        if (dict.ContainsKey(index)) dict[index] += count;
        else dict[index] = count;
    }

    private void UpdateContainerWithCounts(Transform container, Dictionary<int, int> counts, GameObject prefab, bool isPlanningContainer)
    {
        if (container == null || prefab == null) return;

        foreach (Transform child in container) Destroy(child.gameObject);
        if (!isPlanningContainer && container == combatContainer) combatIcons.Clear();

        var pool = GameManager.Instance.systemEnemyPool;
        if (pool == null) return;

        foreach (var pair in counts)
        {
            int index = pair.Key;
            int count = pair.Value;

            if (index >= pool.Length) continue;

            // ✅ ใช้ EnemyStatsSO โดยตรง — ดึง icon จาก field ที่ถูกต้อง
            EnemyStatsSO data = pool[index];
            if (data == null) continue;

            GameObject obj = Instantiate(prefab, container);
            WaveIconItem item = obj.GetComponent<WaveIconItem>();
            if (item != null)
            {
                item.Setup(data.icon, count, index);
                if (container == combatContainer) combatIcons.Add(item);
            }
        }
    }

    private void HandleIncomingEnemy(int typeIndex) => RefreshLayout();

    private void HandleEnemyDeath(int typeIndex)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        if (deathCounts.ContainsKey(typeIndex)) deathCounts[typeIndex]++;
        else deathCounts[typeIndex] = 1;

        RefreshLayout();
    }

    private void UpdateCombatUIOnly()
    {
        var pool = GameManager.Instance.systemEnemyPool;
        if (pool == null) return;

        foreach (var item in combatIcons)
        {
            if (item == null) continue;

            int typeIndex = item.EnemyTypeIndex;
            if (!incomingCounts.ContainsKey(typeIndex)) continue;

            int currentValue = incomingCounts[typeIndex];

            // ✅ ใช้ EnemyStatsSO โดยตรง
            if (typeIndex < pool.Length && pool[typeIndex] != null)
            {
                item.Setup(pool[typeIndex].icon, currentValue, typeIndex);
                if (currentValue <= 0) item.gameObject.SetActive(false);
            }
        }
    }
}