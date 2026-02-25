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

    // เก็บจำนวนศัตรูแยกตามประเภท
    private Dictionary<int, int> incomingCounts = new Dictionary<int, int>();
    private Dictionary<int, int> sentCounts = new Dictionary<int, int>();
    private List<WaveIconItem> combatIcons = new List<WaveIconItem>();

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.systemWaveDraft.OnValueChanged += OnWaveDraftChanged;
            GameManager.OnSystemEnemyDied += HandleEnemyDeath;
            GameManager.OnPhaseChangedGlobal += RefreshLayoutOnPhaseChange;

            // ✨ เพิ่มการดักจับเมื่อมีการส่งมอนสเตอร์มาเพิ่มแบบ Real-time (ใช้ OnListChanged สำหรับ NetworkList)
            GameManager.Instance.p0SentCounts.OnListChanged += OnSentListChanged;
            GameManager.Instance.p1SentCounts.OnListChanged += OnSentListChanged;
        }
        
        // เริ่มต้นให้แสดงผลตามข้อมูลที่มีอยู่
        RefreshLayout();
    }

    private void OnSentListChanged(NetworkListEvent<int> changeEvent)
    {
        RefreshLayout();
    }

    private void RefreshLayoutOnPhaseChange(GamePhase phase)
    {
        RefreshLayout();
    }

    private void OnWaveDraftChanged(FixedString512Bytes oldVal, FixedString512Bytes newVal)
    {
        RefreshLayout();
    }

    private void RefreshLayout()
    {
        if (GameManager.Instance == null) return;

        bool isPlanning = GameManager.Instance.CurrentPhase == GamePhase.Planning;
        
        // 1. จัดการ PanelVisibility
        if (planningPanel != null) planningPanel.SetActive(isPlanning);
        if (combatPanel != null) combatPanel.SetActive(!isPlanning);
        if (sentEnemiesPanel != null) sentEnemiesPanel.SetActive(isPlanning); // โชว์ตัวที่เราส่งเฉพาะตอนวางแผน

        // 2. คำนวณ Incoming (ศัตรูที่จะมาบุกเรา)
        incomingCounts.Clear();
        string draft = GameManager.Instance.systemWaveDraft.Value.ToString();
        if (!string.IsNullOrEmpty(draft))
        {
            string[] parts = draft.Split('|');
            foreach (string p in parts)
            {
                string[] sub = p.Split(':');
                if (sub.Length == 2)
                    incomingCounts[int.Parse(sub[0])] = int.Parse(sub[1]);
            }
        }

        // รวม Incoming จากที่เพื่อนส่งมา (เฉพาะใน Combat)
        if (!isPlanning)
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;
            if (myId == 0) // เราคือ Host (P0) -> ดูที่ Client (P1) ส่งมา
            {
                for (int i = 0; i < GameManager.Instance.p1SentCounts.Count; i++)
                    AddCountToDict(incomingCounts, i, GameManager.Instance.p1SentCounts[i]);
            }
            else // เราคือ Client (P1) -> ดูที่ Host (P0) ส่งมา
            {
                for (int i = 0; i < GameManager.Instance.p0SentCounts.Count; i++)
                    AddCountToDict(incomingCounts, i, GameManager.Instance.p0SentCounts[i]);
            }
        }

        // 3. คำนวณ Sent (ศัตรูที่ "เรา" ส่งไปหาเพื่อน)
        sentCounts.Clear();
        if (isPlanning)
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;
            if (myId == 0) // เราคือ Host (P0) -> โชว์กองทัพ P0
            {
                for (int i = 0; i < GameManager.Instance.p0SentCounts.Count; i++)
                    AddCountToDict(sentCounts, i, GameManager.Instance.p0SentCounts[i]);
            }
            else // เราคือ Client (P1) -> โชว์กองทัพ P1
            {
                for (int i = 0; i < GameManager.Instance.p1SentCounts.Count; i++)
                    AddCountToDict(sentCounts, i, GameManager.Instance.p1SentCounts[i]);
            }
        }

        // 4. อัปเดต UI Containers
        UpdateContainerWithCounts(planningContainer, incomingCounts, planningIconPrefab, true);
        UpdateContainerWithCounts(combatContainer, incomingCounts, combatIconPrefab, false);
        UpdateContainerWithCounts(sentEnemiesContainer, sentCounts, sentIconPrefab ?? planningIconPrefab, false);
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

        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (isPlanningContainer == false && container == combatContainer) 
            combatIcons.Clear();

        foreach (var pair in counts)
        {
            int index = pair.Key;
            int count = pair.Value;

            if (GameManager.Instance.systemEnemyPool != null && index < GameManager.Instance.systemEnemyPool.Length)
            {
                MinionData data = GameManager.Instance.systemEnemyPool[index];
                GameObject obj = Instantiate(prefab, container);
                WaveIconItem item = obj.GetComponent<WaveIconItem>();
                if (item != null)
                {
                    item.Setup(data.picture != null ? data.picture : data.icon, count);
                    if (container == combatContainer) combatIcons.Add(item);
                }
            }
        }
    }

    private void HandleEnemyDeath(int typeIndex)
    {
        // ⚠️ เราจะลดจำนวนเฉพาะในเฟส Combat เท่านั้นครับ
        if (GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        if (incomingCounts.ContainsKey(typeIndex))
        {
            if (incomingCounts[typeIndex] > 0)
            {
                incomingCounts[typeIndex]--;
                
                // อัปเดต UI เฉพาะใน Combat Panel (เพื่อให้ดูเรียลไทม์)
                UpdateCombatUIOnly();
            }
        }
    }

    private void UpdateCombatUIOnly()
    {
        // แทนที่จะสร้างใหม่หมด เราจะแค่ Update ตัวเลขใน CombatIcons ครับ
        int i = 0;
        foreach (var pair in incomingCounts)
        {
            if (i < combatIcons.Count)
            {
                // เราต้องการแค่เปลี่ยน Text ไม่ต้องเซ็ต Sprite ใหม่ก็ได้ครับ (ประหยัด Performance)
                // แต่ถ้าจะเอาชัวร์ก็เรียก Setup ใหม่ได้
                MinionData data = GameManager.Instance.systemEnemyPool[pair.Key];
                combatIcons[i].Setup(data.picture != null ? data.picture : data.icon, pair.Value);
                
                // ถ้าเหลือ 0 อาจจะทำให้มันจางลง หรือหายไปก็ได้ครับ (พี่เลือกตามชอบเลย)
                if (pair.Value <= 0) combatIcons[i].gameObject.SetActive(false);
            }
            i++;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.systemWaveDraft.OnValueChanged -= OnWaveDraftChanged;
            GameManager.OnSystemEnemyDied -= HandleEnemyDeath;
            GameManager.OnPhaseChangedGlobal -= RefreshLayoutOnPhaseChange;

            GameManager.Instance.p0SentCounts.OnListChanged -= OnSentListChanged;
            GameManager.Instance.p1SentCounts.OnListChanged -= OnSentListChanged;
        }
    }
}
