using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Collider weaponHitbox;

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
