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
    public Sprite portrait;
    public Sprite icon;

    [Header("Prefab")]
    [Tooltip("Prefab สำหรับแสดงใน Character Select (3D Preview)")]
    public GameObject playerPrefab;

    [Tooltip("Prefab สำหรับเล่นจริงใน GameScene")]
    public GameObject playablePrefab;

    [Header("Stats Reference")]
    public PlayerStatsSO statsSO;

    [Header("Display")]
    [TextArea(2, 4)]
    public string description;
    public string className;

    // ดึงค่า Stats จาก statsSO โดยตรง
    public int GetHP() => statsSO != null ? statsSO.GetHP() : 0;
    public float GetATK() => statsSO != null ? statsSO.GetDamage() : 0;
    public float GetDEF() => statsSO != null ? statsSO.GetDefense() : 0;
    public float GetSpeed() => statsSO != null ? statsSO.GetSpeed() : 0;
}
