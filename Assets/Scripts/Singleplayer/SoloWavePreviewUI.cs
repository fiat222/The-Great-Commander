using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI แสดงผลล่วงหน้าสำหรับโหมด Singleplayer 
/// แสดง 1. Minion ที่เราซื้อและวางบนกระดาน 
/// แสดง 2. ศัตรูที่จะบุกมาใน Wave ถัดไป (ดึงจาก SoloGameManager)
/// </summary>
public class SoloWavePreviewUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject planningPanel;
    [SerializeField] private GameObject combatPanel;
    [Header("UI Containers & Prefabs")]
    [SerializeField] private Transform planningIncomingContainer;
    [SerializeField] private Transform combatIncomingContainer;

    [SerializeField] private GameObject incomingIconPrefab;

    // Data Storage
    private Dictionary<int, int> incomingCounts = new Dictionary<int, int>();
    private Dictionary<int, int> deathCounts = new Dictionary<int, int>();
    private List<WaveIconItem> combatIcons = new List<WaveIconItem>();

    private bool hasInitialized = false;

    private void OnEnable() => TryInitialize();
    private void Start() => TryInitialize();

    private void Update()
    {
        if (!hasInitialized) TryInitialize();
    }

    private void TryInitialize()
    {
        if (hasInitialized || SoloGameManager.Instance == null) return;

        SoloGameManager.OnPhaseChangedGlobal += HandlePhaseChanged;
        SoloGameManager.OnSystemEnemyDied += HandleEnemyDeath;
        
        // ถ้า PlacementManager มี Event ตอนลบ/สร้างลูกน้อง ค่อยเอามาผูกเพิ่มเพื่อให้ UI อัปเดตทันที
        // สมมติว่าผูกแบบ Update ทุกๆ วิเอาถ้าไม่มี Event
        
        hasInitialized = true;
        RefreshLayout();
    }

    private void OnDisable()
    {
        SoloGameManager.OnPhaseChangedGlobal -= HandlePhaseChanged;
        SoloGameManager.OnSystemEnemyDied -= HandleEnemyDeath;
        hasInitialized = false;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Planning)
        {
            deathCounts.Clear();
        }
        Invoke(nameof(RefreshLayout), 0.1f); // รอให้ Spawner/Manager จัดการ Wave ให้เสร็จก่อนนิดนึง
    }

    private void HandleEnemyDeath(int typeIndex)
    {
        if (SoloGameManager.Instance == null || SoloGameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        if (deathCounts.ContainsKey(typeIndex)) deathCounts[typeIndex]++;
        else deathCounts[typeIndex] = 1;

        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (SoloGameManager.Instance == null) return;

        bool isPlanning = SoloGameManager.Instance.CurrentPhase == GamePhase.Planning;

        // 1. เปิดปิด Panel
        SafeSetActive(planningPanel, isPlanning);
        SafeSetActive(combatPanel, !isPlanning);

        // 2. คำนวณ Incoming Enemies (จำนวนศัตรูที่จะเกิดจากสุ่ม)
        // ในโหมด Solo, SoloGameManager แจ้ง ExpectedEnemyCount แต่ไม่ได้บอกว่ากี่ตัวชนิดไหนแน่ชัด 
        // อาศัยจาก EnemyStatsSO ที่รับ Wave ใหม่
        incomingCounts.Clear();
        
        int totalExpected = SoloGameManager.Instance.ExpectedEnemyCount;
        var enemyPool = SoloGameManager.Instance.enemyStatsSOs;

        if (enemyPool != null && enemyPool.Length > 0 && totalExpected > 0)
        {
            // (เรายังไม่รู้ชัวร์ๆ ว่า Spawner จะแจกจ่าย Type ไหนกี่ตัวเป๊ะๆ เพราะมันสุ่มตอนเกิด)
            // แต่สำหรับความสวยงาม ให้ถัวเฉลี่ย หรือแสดงแค่ประเภทตัวแรกเพื่อให้เห็นไอคอน
            // หรือถ้ามีระบบเดาคงที่ ก็คำนวณเอาตรงนี้ได้
            // **ตัวอย่าง:** สมมติว่าเป็น Type 0 ล้วนๆ ไปก่อน
            incomingCounts[0] = totalExpected; 
        }

        // 4. หักลบตัวที่ตาย (Combat)
        if (!isPlanning)
        {
            foreach (var pair in deathCounts)
            {
                if (incomingCounts.ContainsKey(pair.Key))
                {
                    incomingCounts[pair.Key] -= pair.Value;
                    if (incomingCounts[pair.Key] < 0) incomingCounts[pair.Key] = 0;
                }
            }
        }

        // 5. อัปเดต UI 
        UpdateContainer(planningIncomingContainer, incomingCounts, incomingIconPrefab, true);
        UpdateContainer(combatIncomingContainer, incomingCounts, incomingIconPrefab, false);
    }

    private void AddCountToDict(Dictionary<int, int> dict, int index, int count)
    {
        if (count <= 0) return;
        if (dict.ContainsKey(index)) dict[index] += count;
        else dict[index] = count;
    }

    private void UpdateContainer(Transform container, Dictionary<int, int> counts, GameObject prefab, bool isPlanning)
    {
        if (container == null || prefab == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (!isPlanning && container == combatIncomingContainer)
            combatIcons.Clear();

        foreach (var pair in counts)
        {
            int index = pair.Key;
            int count = pair.Value;

            if (count <= 0) continue;

            // พยายามดึงภาพจาก pool ถ้ามี (สำหรับ Solo, ข้อมูลอาจจะอยู่ใน SingleShopManager หรือ EnemyStatsSOs)
            Sprite iconToUse = null;
            if (SoloGameManager.Instance.enemyStatsSOs != null && index < SoloGameManager.Instance.enemyStatsSOs.Length)
            {
                iconToUse = SoloGameManager.Instance.enemyStatsSOs[index].icon;
            }

            GameObject obj = Instantiate(prefab, container);
            WaveIconItem item = obj.GetComponent<WaveIconItem>();
            if (item != null)
            {
                item.Setup(iconToUse, count, index);
                if (!isPlanning && container == combatIncomingContainer)
                    combatIcons.Add(item);
            }
        }
    }

    private void SafeSetActive(GameObject obj, bool state)
    {
        if (obj != null) obj.SetActive(state);
    }
}
