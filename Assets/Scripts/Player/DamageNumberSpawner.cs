using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    public GameObject prefab;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void Show(int damage, Vector3 worldPos)
    {
        if (Instance == null || Instance.prefab == null) return;
        var go = Instantiate(Instance.prefab, worldPos, Quaternion.identity);
        go.GetComponent<DamageNumberUI>()?.Init(damage, worldPos);
    }
}