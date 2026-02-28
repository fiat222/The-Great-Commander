using UnityEngine;
using TMPro;
using System.Collections;

public class DamageNumberUI : MonoBehaviour
{
    public float floatSpeed = 2.5f;
    public float lifetime = 0.8f;

    private TextMeshPro tmp;
    private Camera cam;

    private void Awake()
    {
        // หาทั้งบน GameObject นี้เองและ children
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null) tmp = GetComponentInChildren<TextMeshPro>();
        cam = Camera.main;
    }

    public void Init(int damage, Vector3 worldPos)
    {
        transform.position = worldPos;

        if (tmp == null)
        {
            Debug.LogError("[DamageNumberUI] ไม่พบ TextMeshPro บน Prefab!");
            Destroy(gameObject);
            return;
        }

        tmp.text = damage.ToString();
        tmp.color = Color.white;
        tmp.alpha = 1f;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float ratio = elapsed / lifetime;
            transform.position = startPos + Vector3.up * (floatSpeed * elapsed);
            tmp.alpha = Mathf.Lerp(1f, 0f, ratio);
            if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            yield return null;
        }
        Destroy(gameObject);
    }
}