using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Collider weaponHitbox;
    [SerializeField] private GameObject weaponEffect;

    // --- ✨ หมวดจัดการ Effect (ความสวยงาม) ---
    public void SetEffectActive(bool active)
    {
        if (weaponEffect != null)
            weaponEffect.SetActive(active);
    }

    // --- ⚔️ หมวดจัดการ Collider (ดาเมจ) ---
    public void EnableHitbox()
    {
        weaponHitbox.enabled = true;
    }

    public void DisableHitbox() 
    {
        weaponHitbox.enabled = false;
    }
}
