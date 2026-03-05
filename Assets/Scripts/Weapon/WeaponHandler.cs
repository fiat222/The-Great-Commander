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
        // เคลียร์รายชื่อศัตรูที่โดนไปแล้ว → ฟันรอบใหม่ตีโดนได้หมด
        var hitbox = weaponHitbox.GetComponent<PlayerWeaponHitbox>();
        if (hitbox != null) hitbox.ClearHitList();
    }

    public void DisableHitbox() 
    {
        weaponHitbox.enabled = false;
        var hitbox = weaponHitbox.GetComponent<PlayerWeaponHitbox>();
        if (hitbox != null) hitbox.ClearHitList();
    }
}
