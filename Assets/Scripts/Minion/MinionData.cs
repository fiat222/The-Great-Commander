using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewMinion", menuName = "Minion/Minion Data")]
public class MinionData : ScriptableObject
{
    public string minionName;
    public GameObject prefab;
    
    [FormerlySerializedAs("icon")]
    public Sprite picture; // สำหรับรูปตัวละคร (เดิมชื่อ icon)
    
    public Sprite icon;    // สำหรับไอคอนจริงๆ
    
    public int cost;

    public int defense;
    public int damage;
    public int hp;
    public int speed;
    public float attackrange;
}
