using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;

    [SerializeField] private HealthSystem healthUI;

    private int currentHealth;
    private bool deathSequenceStarted;
    
    [SerializeField] private bool isHostBase = true;

    [Header("Death Settings")]
    [Tooltip("ถ้าติ๊ก ป้อมหลักจะถูกลบออกจากซีนหลังจากจบคัตซีน Game Over แล้ว")]
    public bool destroyOnDeath = true;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        ResolveHealthUI();
        UpdateUI(currentHealth);
    }

    private void Start()
    {
        // ⭐ NETWORK-BASED: เนื่องจาก 1 Scene มี 1 ป้อม เราจึงเช็คจาก "คนเล่น" แทนพิกัด
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // ถ้าเราเป็น Host ป้อมในเครื่องเราคือ P0(Host), ถ้าเราเป็น Client ป้อมในเครื่องเราคือ P1(Client)
            isHostBase = NetworkManager.Singleton.IsHost;
        }

        Debug.Log($"<color=cyan>[BaseHealth]</color> <b>{gameObject.name}</b> ถูกระบุเป็นทีม: <b>{(isHostBase ? "P0 (Host)" : "P1 (Client)")}</b> (IsHost: {NetworkManager.Singleton?.IsHost})");
        
        // เปลี่ยนชื่อให้ดูง่ายใน Hierarchy
        gameObject.name = $"{(isHostBase ? "[P0]" : "[P1]")} {gameObject.name}";
        
        RefreshUIVisibility();
        UpdateUI(currentHealth);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private void ResolveHealthUI()
    {
        if (healthUI != null) return;

        // ค้นหา ONLY บนตัวเองเท่านั้น (ไม่ขึ้นไปหาพ่อแม่)
        healthUI = GetComponent<HealthSystem>();

        if (healthUI != null)
            Debug.Log($"<color=lime>[BaseHealth]</color> พบ HealthSystem บน {healthUI.gameObject.name} ✅");
        else
            Debug.LogWarning($"<color=red>[BaseHealth]</color> ไม่พบ HealthSystem บนตัวเอง! ต้องเติม HealthSystem component ให้บน {gameObject.name} ❌");
    }

    private void RefreshUIVisibility()
    {
        if (healthUI == null || healthUI.healthCanvas == null) return;
        healthUI.healthCanvas.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;

        // ในเมื่อ 1 Scene มี 1 ป้อม และเราเซ็ต ID ถูกต้องแล้ว กด P ก็ให้พังป้อมใน Scene นี้ได้เลย
        Debug.Log($"<color=red>[Debug]</color> กด P สั่งพังป้อมในเครื่องนี้ (Owner ID: {(isHostBase ? "0" : "1")})");
        TakeDamage(999);
    }

    private void UpdateUI(int currentHP)
    {
        ResolveHealthUI();
        if (healthUI != null)
            healthUI.ForceSetHealth(currentHP, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateUI(currentHealth);
        Debug.Log($"<color=green>[Base]</color> {gameObject.name} โดนตี! เลือดเหลือ {currentHealth}/{maxHealth}");

        if (currentHealth <= 0 && !deathSequenceStarted)
        {
            deathSequenceStarted = true;
            
            ulong clientId = isHostBase ? 0UL : 1UL;
            Debug.Log($"<color=yellow>[BaseHealth]</color> ฐานแตก! (Owner: Player {clientId}, isHostBase: {isHostBase}) -> แจ้ง Server");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyPlayerDiedServerRpc(clientId);
            }
            
            StartCoroutine(PlayBaseDeathSequence());
        }
    }

    private System.Collections.IEnumerator PlayBaseDeathSequence()
    {
        float delay = 0f;

        if (healthUI != null)
        {
            if (healthUI.gameOverDelay > 0f)
                delay = healthUI.gameOverDelay;
        }
        else
        {
            delay = 2.5f;
        }

        // 1) โฟกัสกล้องก่อน
        if (CameraManager.Instance != null)
            CameraManager.Instance.FocusInitialView();

        // 2) รอกล้องแพนไปถึง
        float arriveDelay = (healthUI != null) ? healthUI.cameraArriveDelay : 0.8f;
        if (arriveDelay > 0f)
            yield return new WaitForSeconds(arriveDelay);

        // 3) VFX ระเบิด
        if (healthUI != null && healthUI.deathVfxPrefab != null)
        {
            GameObject vfx = Instantiate(healthUI.deathVfxPrefab, transform.position, Quaternion.identity);
            if (healthUI.deathVfxDuration > 0f)
                Destroy(vfx, healthUI.deathVfxDuration);
        }

        // 4) ป้อมจม
        if (healthUI != null)
            yield return healthUI.ExecuteSinkAnimation(transform);

        // 5) delay ก่อน Game Over
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 6) Game Over (Solo Mode)
        if (SoloEnemyTracker.Instance != null)
        {
            Debug.Log("<color=cyan>[BaseHealth]</color> Calling SoloEnemyTracker.NotifyPlayerDied()");
            SoloEnemyTracker.Instance.NotifyPlayerDied();
        }
        else if (SoloGameManager.Instance != null)
        {
            // Fallback ถ้าไม่มี Tracker
            Debug.LogWarning("<color=yellow>[BaseHealth]</color> SoloEnemyTracker not found, using SoloGameManager fallback");
            SoloGameManager.Instance.NotifyPlayerDied();
        }

        // 7) Destroy
        if (destroyOnDeath)
            Destroy(gameObject);
    }
}