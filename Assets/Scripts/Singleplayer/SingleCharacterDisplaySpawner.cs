using UnityEngine;

/// <summary>
/// Spawn โมเดล 3D ของตัวละครที่เลือกใน Preview Slot (Single Player)
/// </summary>
public class SingleCharacterDisplaySpawner : MonoBehaviour
{
    public static SingleCharacterDisplaySpawner Instance { get; private set; }

    [Header("Preview Slot")]
    public Transform previewSlot;

    [Header("Rotation")]
    public Vector3 previewRotation = new Vector3(0, 30, 0);

    private GameObject _currentInstance;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SingleCharacterSelectManager.OnSelectionChanged += RefreshDisplay;
    }

    private void OnDisable()
    {
        SingleCharacterSelectManager.OnSelectionChanged -= RefreshDisplay;
    }

    private void Start()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        var mgr = SingleCharacterSelectManager.Instance;
        if (mgr == null) return;

        SpawnCharacter(mgr.SelectedIndex);
    }

    private void SpawnCharacter(int index)
    {
        // ยังไม่เลือก → ลบ Preview ออก
        if (index < 0 || CharacterSelectData.Characters == null
            || index >= CharacterSelectData.Characters.Length)
        {
            if (_currentInstance != null)
            {
                Destroy(_currentInstance);
                _currentInstance = null;
            }
            return;
        }

        var charData = CharacterSelectData.Characters[index];
        if (charData?.playerPrefab == null) return;

        // ตัวเดิมอยู่แล้ว ไม่ต้อง Spawn ใหม่
        if (_currentInstance != null && _currentInstance.name == charData.playerPrefab.name + "(Clone)")
            return;

        // ลบตัวเก่าก่อน
        if (_currentInstance != null) Destroy(_currentInstance);

        // Spawn ใหม่
        _currentInstance = Instantiate(
            charData.playerPrefab,
            previewSlot.position,
            Quaternion.Euler(previewRotation)
        );
        _currentInstance.name = charData.playerPrefab.name + "(Clone)";

        RemoveGameplayComponents(_currentInstance);

        // เล่น Idle Animation
        var animator = _currentInstance.GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsIdle", true);
    }

    private void RemoveGameplayComponents(GameObject obj)
    {
        foreach (var type in new System.Type[]
        {
            typeof(CharacterController),
            typeof(Rigidbody),
        })
        {
            var comp = obj.GetComponent(type);
            if (comp != null) Destroy(comp);
        }

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
}