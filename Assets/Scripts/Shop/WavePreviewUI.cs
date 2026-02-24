using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class WavePreviewUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject combatPanel;

    [Header("Settings")]
    [SerializeField] private GameObject planningIconPrefab;
    [SerializeField] private GameObject combatIconPrefab;
    [SerializeField] private Transform planningContainer;
    [SerializeField] private Transform combatContainer;

    // เก็บจำนวนศัตรูที่เหลืออยู่ปัจจุบันแยกตาม Index
    private Dictionary<int, int> currentCounts = new Dictionary<int, int>();
    private List<WaveIconItem> combatIcons = new List<WaveIconItem>();

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.systemWaveDraft.OnValueChanged += OnWaveDraftChanged;
            GameManager.OnSystemEnemyDied += HandleEnemyDeath;
            GameManager.OnPhaseChangedGlobal += RefreshLayoutOnPhaseChange;

            // ✨ เพิ่มการดักจับเมื่อมีการส่งมอนสเตอร์มาเพิ่มแบบ Real-time
            GameManager.Instance.p0Type0Count.OnValueChanged += RefreshLayoutOnCountChanged;
            GameManager.Instance.p0Type1Count.OnValueChanged += RefreshLayoutOnCountChanged;
            GameManager.Instance.p1Type0Count.OnValueChanged += RefreshLayoutOnCountChanged;
            GameManager.Instance.p1Type1Count.OnValueChanged += RefreshLayoutOnCountChanged;
        }
        
        // เริ่มต้นให้แสดงผลตามข้อมูลที่มีอยู่
        RefreshLayout();
    }

    private void RefreshLayoutOnCountChanged(int oldVal, int newVal)
    {
        // อัปเดตเฉพาะในเฟส Combat เพราะใน Planning เราโชว์แค่ร่างเวฟระบบครับ
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Combat)
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

        string draft = GameManager.Instance.systemWaveDraft.Value.ToString();
        
        // สลับ Panel ตามเฟส
        bool isPlanning = GameManager.Instance.CurrentPhase == GamePhase.Planning;
        if (planningPanel != null) planningPanel.SetActive(isPlanning);
        if (combatPanel != null) combatPanel.SetActive(!isPlanning);

        // อัปเดตข้อมูลใน Dictionary
        currentCounts.Clear();

        // 1. ดึงข้อมูลจาก System Wave (รายทาง)
        if (!string.IsNullOrEmpty(draft))
        {
            string[] parts = draft.Split('|');
            foreach (string p in parts)
            {
                string[] sub = p.Split(':');
                if (sub.Length == 2)
                    currentCounts[int.Parse(sub[0])] = int.Parse(sub[1]);
            }
        }

        // 2. ✨ รวมยอดกับศัตรูที่ "เพื่อนส่งมาหาเรา" (เฉพาะในเฟส Combat)
        if (!isPlanning)
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;
            if (myId == 0) // เราคือ Host -> นับที่ Client ส่งมา (p1)
            {
                AddSentCountToTotal(0, GameManager.Instance.p1Type0Count.Value);
                AddSentCountToTotal(1, GameManager.Instance.p1Type1Count.Value);
            }
            else // เราคือ Client -> นับที่ Host ส่งมา (p0)
            {
                AddSentCountToTotal(0, GameManager.Instance.p0Type0Count.Value);
                AddSentCountToTotal(1, GameManager.Instance.p0Type1Count.Value);
            }
        }

        // สร้าง UI ทั้งสองฝั่ง
        UpdateContainer(planningContainer, true);
        UpdateContainer(combatContainer, false);
    }

    private void AddSentCountToTotal(int index, int count)
    {
        if (count <= 0) return;
        if (currentCounts.ContainsKey(index))
            currentCounts[index] += count;
        else
            currentCounts[index] = count;
    }

    private void UpdateContainer(Transform container, bool isPlanning)
    {
        GameObject prefabToUse = isPlanning ? planningIconPrefab : combatIconPrefab;
        if (container == null || prefabToUse == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (!isPlanning) combatIcons.Clear();

        foreach (var pair in currentCounts)
        {
            int index = pair.Key;
            int count = pair.Value;

            if (GameManager.Instance.systemEnemyPool != null && index < GameManager.Instance.systemEnemyPool.Length)
            {
                MinionData data = GameManager.Instance.systemEnemyPool[index];
                GameObject obj = Instantiate(prefabToUse, container);
                WaveIconItem item = obj.GetComponent<WaveIconItem>();
                if (item != null)
                {
                    item.Setup(data.picture != null ? data.picture : data.icon, count);
                    if (!isPlanning) combatIcons.Add(item);
                }
            }
        }
    }

    private void HandleEnemyDeath(int typeIndex)
    {
        // ⚠️ เราจะลดจำนวนเฉพาะในเฟส Combat เท่านั้นครับ
        if (GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        if (currentCounts.ContainsKey(typeIndex))
        {
            if (currentCounts[typeIndex] > 0)
            {
                currentCounts[typeIndex]--;
                
                // อัปเดต UI เฉพาะใน Combat Panel (เพื่อให้ดูเรียลไทม์)
                UpdateCombatUIOnly();
            }
        }
    }

    private void UpdateCombatUIOnly()
    {
        // แทนที่จะสร้างใหม่หมด เราจะแค่ Update ตัวเลขใน CombatIcons ครับ
        int i = 0;
        foreach (var pair in currentCounts)
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

            GameManager.Instance.p0Type0Count.OnValueChanged -= RefreshLayoutOnCountChanged;
            GameManager.Instance.p0Type1Count.OnValueChanged -= RefreshLayoutOnCountChanged;
            GameManager.Instance.p1Type0Count.OnValueChanged -= RefreshLayoutOnCountChanged;
            GameManager.Instance.p1Type1Count.OnValueChanged -= RefreshLayoutOnCountChanged;
        }
    }
}
