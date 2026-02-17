using UnityEngine;

[CreateAssetMenu(fileName = "NewMinion", menuName = "Minion/Minion Data")]
public class MinionData : ScriptableObject
{
    public string minionName;
    public GameObject prefab;
    public Sprite icon;
    public int cost;

    public int defense;
    public int damage;
    public int hp;
    public int speed;
    public float attackrange;
}
