using UnityEngine;

/// <summary>
/// SO สำหรับ Character แต่ละตัว ใช้ในหน้าเลือกตัวละคร
/// สร้างผ่าน Create → Game → Character Data
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("Identity")]
    public string characterName;

    [Tooltip("รูปแสดงในหน้า Selection (Portrait ใหญ่)")]
    public Sprite portrait;

    [Tooltip("ไอคอนแสดงในการ์ดเลือกตัวละคร")]
    public Sprite icon;

    [Header("Prefab")]
    [Tooltip("Player Prefab ที่จะ Spawn ในเกม (ต้อง Register ใน NetworkManager)")]
    public GameObject playerPrefab;

    [Header("Stats Reference")]
    [Tooltip("ลิงก์ PlayerStatsSO เพื่อโชว์ Stats Preview")]
    public PlayerStatsSO statsSO;

    [Header("Display")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Class type เช่น Warrior, Archer, Mage")]
    public string className;
}
