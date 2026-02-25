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

    public float damage;
    public float defense;
    public float speed;
    public float hp;
    public float attackrange;
}
