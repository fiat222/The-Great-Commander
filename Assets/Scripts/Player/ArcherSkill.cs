using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Archer Skill — ฝนธนู AoE
///
/// Flow:
///   กด R         → วงกลม AoE โผล่บนพื้น + UI hint ซ้าย/ขวา
///   เลื่อนเมาส์  → วงเคลื่อนตาม cursor (Raycast พื้น)
///   กด Mouse0    → ยิงฝนธนูลงในวง → วงหาย → Cooldown
///   กด R อีกรอบ → Cancel
///
/// Setup:
///   1. [Skill Config] ใส่ค่าต่างๆ
///   2. [AoE Indicator] ลาก Prefab วงกลม (Plane + Material วงกลม)
///      — Scale ตาม aoeRadius อัตโนมัติ
///   3. [Skill UI] ลาก skillIconBg, cooldownFillImage, statusText
///   4. [Hint UI] ลาก SkillIndicatorUI component
///   5. [Arrow] ลาก rainArrowPrefab (ใส่ RainArrowProjectile)
/// </summary>
[RequireComponent(typeof(Archer))]
public class ArcherSkill : MonoBehaviour
{
    // ==================== Skill Config ====================
    [Header("Skill Config")]
    public float cooldown = 10f;
    public float damageMultiplier = 1.5f;  // คูณจาก maxDamage ของ Archer
    public int arrowCount = 20;     // จำนวนธนูที่ยิง
    public float aoeRadius = 6f;     // รัศมีวง AoE
    public float arrowSpeed = 25f;
    public float spawnHeight = 18f;    // ความสูงที่ spawn ธนู (จากจุดเป้าหมาย)
    public float fireInterval = 0.05f;  // ระยะห่างระหว่างธนูแต่ละลูก

    [Header("Arrow")]
    [Tooltip("Prefab ที่มี RainArrowProjectile — ยิงลงมาเฉียงตามกล้อง")]
    public GameObject rainArrowPrefab;

    [Header("AoE Indicator")]
    [Tooltip("Prefab วงกลมบนพื้น เช่น Plane + Circle Material (Scale จะถูกตั้งอัตโนมัติ)")]
    public GameObject aoeIndicatorPrefab;

    [Tooltip("Layer ของพื้น สำหรับ Raycast วาง indicator")]
    public LayerMask groundLayer;

    // ==================== Skill UI (Icon + Cooldown) ====================
    [Header("Skill UI")]
    public Image skillIconBg;
    public Image cooldownFillImage;
    public TextMeshProUGUI statusText;

    [Header("Skill UI Colors")]
    public Color colorReady = Color.white;
    public Color colorActive = new Color(0.3f, 1f, 0.4f, 1f);
    public Color colorCooldown = new Color(0.35f, 0.35f, 0.35f, 1f);

    // ==================== Hint UI (ซ้าย Use / ขวา Cancel) ====================
    [Header("Hint UI")]
    [Tooltip("SkillIndicatorUI component ที่อยู่บน Canvas")]
    public SkillIndicatorUI hintUI;

    // ==================== Runtime ====================
    private Archer archer;
    private bool isSkillActive = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;

    private GameObject currentIndicator; // instance ของ AoE วงกลม
    private Vector3 aoeTargetPos;     // ตำแหน่งที่เลือก

    // ==================== Lifecycle ====================

    private void Awake()
    {
        archer = GetComponent<Archer>();
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (archer.IsDead) return;

        HandleCooldownTick();
        HandleInput();

        if (isSkillActive)
            UpdateIndicatorPosition();

        RefreshUI();
    }

    // ==================== Input ====================

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isSkillActive)
                CancelSkill();
            else if (!isOnCooldown)
                ActivateSkill();
            else
                Debug.Log($"<color=yellow>[ArcherSkill]</color> Cooldown อีก {cooldownTimer:F1}s");
        }

        if (isSkillActive && Input.GetMouseButtonDown(0))
            StartCoroutine(FireRainOfArrows());
    }

    // ==================== Skill Logic ====================

    private void ActivateSkill()
    {
        isSkillActive = true;

        // Spawn indicator ที่ตำแหน่งปัจจุบัน
        if (aoeIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(aoeIndicatorPrefab, GetGroundPoint(), Quaternion.identity);
            // Scale plane ให้ตรงกับ aoeRadius (Plane default = 10 units → หาร 10 แล้วคูณ 2 เพราะ radius)
            float s = (aoeRadius * 2f) / 10f;
            currentIndicator.transform.localScale = new Vector3(s, 1f, s);
        }

        if (hintUI != null) hintUI.ShowHint();

        Debug.Log("<color=lime>[ArcherSkill]</color> เลือกตำแหน่ง AoE — Mouse0: ยิง, R: ยกเลิก");
    }

    private void CancelSkill()
    {
        isSkillActive = false;
        DestroyIndicator();
        if (hintUI != null) hintUI.HideHint();
        Debug.Log("<color=orange>[ArcherSkill]</color> Skill Cancelled");
    }

    private IEnumerator FireRainOfArrows()
    {
        isSkillActive = false;
        DestroyIndicator();
        if (hintUI != null) hintUI.HideHint();

        if (rainArrowPrefab == null)
        {
            Debug.LogWarning("[ArcherSkill] ไม่มี rainArrowPrefab!");
            StartCooldown();
            yield break;
        }

        int dmg = Mathf.RoundToInt(archer.maxDamage * damageMultiplier);
        // ทิศเฉียงตามกล้อง — ลงมาในมุม 70° จากแนวนอน
        Vector3 camForwardFlat = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 arrowDir = (camForwardFlat * 0.35f + Vector3.down).normalized; // เฉียง ~70°

        for (int i = 0; i < arrowCount; i++)
        {
            // Random จุด spawn ภายในวง (uniform disk distribution)
            Vector2 disk = Random.insideUnitCircle * aoeRadius;
            Vector3 groundPos = aoeTargetPos + new Vector3(disk.x, 0f, disk.y);
            // spawn สูงขึ้นไปตาม arrowDir ย้อนกลับ
            Vector3 spawnPos = groundPos - arrowDir * spawnHeight;

            var arrow = Instantiate(rainArrowPrefab, spawnPos, Quaternion.identity);
            arrow.GetComponent<RainArrowProjectile>()?.Launch(arrowDir, arrowSpeed, dmg);

            yield return new WaitForSeconds(fireInterval);
        }

        StartCooldown();
        Debug.Log($"<color=cyan>[ArcherSkill]</color> ยิงฝนธนู {arrowCount} ลูก! Dmg/ลูก: {dmg}");
    }

    // ==================== AoE Indicator ====================

    /// <summary>
    /// ย้าย indicator ตาม cursor Raycast บนพื้น
    /// </summary>
    private void UpdateIndicatorPosition()
    {
        aoeTargetPos = GetGroundPoint();
        if (currentIndicator != null)
            currentIndicator.transform.position = aoeTargetPos;
    }

    /// <summary>
    /// Raycast จากเมาส์ไปยังพื้น — fallback คือหน้า player 5m
    /// </summary>
    private Vector3 GetGroundPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return hit.point;

        // fallback: จุดหน้า player บนพื้น
        return transform.position + transform.forward * 5f;
    }

    private void DestroyIndicator()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
    }

    // ==================== Cooldown ====================

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = cooldown;
    }

    private void HandleCooldownTick()
    {
        if (!isOnCooldown) return;
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = 0f;
            isOnCooldown = false;
            Debug.Log("<color=lime>[ArcherSkill]</color> Skill พร้อมแล้ว!");
        }
    }

    // ==================== UI ====================

    private void RefreshUI()
    {
        if (skillIconBg != null)
            skillIconBg.color = isOnCooldown ? colorCooldown
                              : isSkillActive ? colorActive
                              : colorReady;

        if (cooldownFillImage != null)
            cooldownFillImage.fillAmount = isOnCooldown ? (cooldownTimer / cooldown) : 0f;

        if (statusText != null)
        {
            if (isOnCooldown)
                statusText.text = $"{cooldownTimer:F1}s";
            else if (isSkillActive)
                statusText.text = "ACTIVE";
            else
                statusText.text = "";
        }
    }

    // ==================== Public ====================

    public bool IsSkillActive => isSkillActive;
    public bool IsOnCooldown => isOnCooldown;
    public float CooldownRatio => isOnCooldown ? (cooldownTimer / cooldown) : 0f;
}