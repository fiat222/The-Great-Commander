using UnityEngine;

public class CharacterDisplaySpawner : MonoBehaviour
{
    public static CharacterDisplaySpawner Instance { get; private set; }

    [Header("Slots")]
    public Transform p1Slot; // ฝั่งซ้าย
    public Transform p2Slot; // ฝั่งขวา

    [Header("Rotation")]
    public Vector3 p1Rotation = new Vector3(0, 30, 0);  // หันเข้ากล้องนิดนึง
    public Vector3 p2Rotation = new Vector3(0, -30, 0); // หันเข้ากล้องนิดนึง

    // [Header("Preview Layer")]
    // public string previewLayerName = "CharacterPreview";

    private GameObject p1Instance;
    private GameObject p2Instance;
    private int previewLayer;

    private void Awake()
    {
        Instance = this;
        // previewLayer = LayerMask.NameToLayer(previewLayerName);
    }

    private void OnEnable()
    {
        CharacterSelectManager.OnSelectionChanged += RefreshDisplays;
    }

    private void OnDisable()
    {
        CharacterSelectManager.OnSelectionChanged -= RefreshDisplays;
    }

    private void Start()
    {
        RefreshDisplays();
    }

    public void RefreshDisplays()
    {
        var mgr = CharacterSelectManager.Instance;
        if (mgr == null) return;

        SpawnCharacter(ref p1Instance, p1Slot, mgr.p1Selection.Value, p1Rotation);
        SpawnCharacter(ref p2Instance, p2Slot, mgr.p2Selection.Value, p2Rotation);
    }

    void SpawnCharacter(ref GameObject instance, Transform slot, int index, Vector3 rotation)
    {
        // ยังไม่เลือก → ลบออก
        if (index < 0 || CharacterSelectData.Characters == null
            || index >= CharacterSelectData.Characters.Length)
        {
            if (instance != null)
            {
                Destroy(instance);
                instance = null;
            }
            return;
        }

        var charData = CharacterSelectData.Characters[index];
        if (charData?.playerPrefab == null) return;

        // ถ้า Spawn ตัวเดิมอยู่แล้วไม่ต้อง Spawn ใหม่
        if (instance != null && instance.name == charData.playerPrefab.name + "(Clone)")
            return;

        // ลบตัวเก่าก่อน
        if (instance != null) Destroy(instance);

        // Spawn ใหม่
        instance = Instantiate(
            charData.playerPrefab,
            slot.position,
            Quaternion.Euler(rotation)
        );
        instance.name = charData.playerPrefab.name + "(Clone)";

        // ลบ Component ที่ไม่ต้องการใน Preview
        RemoveGameplayComponents(instance);

        // ตั้ง Layer ให้ CharacterCamera ถ่ายได้ (ถ้าต้องการ)
        // SetLayerRecursive(instance, previewLayer);

        // เล่น Idle Animation
        var animator = instance.GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsIdle", true);
    }

    void RemoveGameplayComponents(GameObject obj)
    {
        // ลบ Component ที่ไม่ต้องการใน Preview Scene
        foreach (var type in new System.Type[]
        {
            typeof(CharacterController),
            typeof(Rigidbody),
            //typeof(NetworkObject) // ถ้ามี
        })
        {
            var comp = obj.GetComponent(type);
            if (comp != null) Destroy(comp);
        }

        // ลบ Script พวก Player Controller
        var scripts = obj.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            string typeName = script.GetType().Name;
            if (typeName.Contains("Controller") || typeName.Contains("Warrior")
                || typeName.Contains("Archer") || typeName.Contains("Player"))
            {
                Destroy(script);
            }
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}