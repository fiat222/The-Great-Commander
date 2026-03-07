using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PlayerAudio;

/// <summary>
/// Archer Skill — ฝนธนู AoE
///
/// Flow:
///   กด R         → วงกลม AoE โผล่บนพื้น + UI hint ซ้าย/ขวา
///   เลื่อนเมาส์  → วงเคลื่อนตาม cursor (Raycast พื้น)
///   กด Mouse0    → ยิงฝนธนูลงในวง → วงหาย → Cooldown
///   กด R อีกรอบ → Cancel
/// </summary>
[RequireComponent(typeof(Archer))]
public class ArcherSkill : MonoBehaviour
{
    // ==================== Skill Config ====================
    [Header("Skill Config")]
    public float cooldown = 10f;
    public float damageMultiplier = 1.5f;
    public int arrowCount = 20;
    public float aoeRadius = 6f;
    public float arrowSpeed = 25f;
    public float spawnHeight = 18f;
    public float fireInterval = 0.05f;

    [Header("Arrow")]
    [Tooltip("Prefab ที่มี RainArrowProjectile — ยิงลงมาเฉียงตามกล้อง")]
    public GameObject rainArrowPrefab;

    [Header("Burning Zone")]
    [Tooltip("Prefab พื้นไฟ — ควรมี Particle System ไฟ + BurningZone script")]
    public GameObject burningZonePrefab;
    [Tooltip("หน่วงกี่วินาทีหลังกดสกิลก่อนไฟจะเริ่ม")]
    public float burnDelay = 1f;
    [Tooltip("ดาเมจต่อวินาที")]
    public int burnDamagePerSecond = 10;
    [Tooltip("ระยะเวลาไฟลุก (วินาที)")]
    public float burnDuration = 5f;

    [Header("AoE Indicator")]
    [Tooltip("Prefab วงกลมบนพื้น เช่น Plane + Circle Material")]
    public GameObject aoeIndicatorPrefab;
    [Tooltip("Layer ของพื้น สำหรับ Raycast วาง indicator")]
    public LayerMask groundLayer;

    // ==================== Skill UI Tags ====================
    [Header("Skill UI Tags")]
    [Tooltip("Tag ของ Image icon สกิล")]
    public string skillIconTag       = "SkillIcon";
    [Tooltip("Tag ของ Image Filled สำหรับ Cooldown")]
    public string cooldownFillTag    = "SkillCooldownFill";
    [Tooltip("Tag ของ TextMeshProUGUI แสดงเวลา")]
    public string statusTextTag      = "SkillStatusText";
    [Tooltip("Tag ของ Image overlay ทับ Skill icon (แสดงตอน Active)")]
    public string skillOverlayTag    = "SkillActiveOverlay";
    [Tooltip("Tag ของ Image overlay ทับ SpecialAtk icon (แสดงตอน Charge Mode)")]
    public string specialOverlayTag  = "SkillSpecialOverlay";

    [Header("Skill UI Colors")]
    public Color colorReady    = Color.white;
    public Color colorActive   = new Color(0.3f, 1f, 0.4f, 1f);
    public Color colorCooldown = new Color(0.35f, 0.35f, 0.35f, 1f);
    public Color colorOverlay  = new Color(1f, 1f, 1f, 0.35f);

    // ── UI refs (หาจาก Tag อัตโนมัติ) ──
    private Image skillIconBg;
    private Image cooldownFillImage;
    private TextMeshProUGUI statusText;
    private Image skillOverlay;
    private Image specialOverlay;
    private bool uiResolved;

    // ==================== Hint UI ====================
    [Header("Hint UI")]
    [Tooltip("SkillIndicatorUI component ที่อยู่บน Canvas")]
    public SkillIndicatorUI hintUI;

    // ==================== Runtime ====================
    private Archer archer;
    private PlayerAudioComponent playerAudio;
    private bool isSkillActive;
    private bool isOnCooldown;
    private float cooldownTimer;

    private GameObject currentIndicator;
    private Vector3 aoeTargetPos;

    // ==================== Lifecycle ====================

    private void Awake()
    {
        archer = GetComponent<Archer>();
        playerAudio = GetComponent<PlayerAudioComponent>();

        if (hintUI == null)
        {
            var obj = GameObject.FindWithTag("Indicator");
            if (obj != null) hintUI = obj.GetComponent<SkillIndicatorUI>();
        }
    }

    private void Start()
    {
        TryResolveUI();
    }

    private void Update()
    {
        if (!uiResolved) TryResolveUI();
        if (archer.IsDead) return;

        HandleCooldownTick();
        HandleInput();

        if (isSkillActive)
            UpdateIndicatorPosition();

        RefreshUI();
    }

    // ==================== UI Resolution ====================

    private void TryResolveUI()
    {
        var iconObj = GameObject.FindWithTag(skillIconTag);
        if (iconObj != null) skillIconBg = iconObj.GetComponent<Image>();

        var fillObj = GameObject.FindWithTag(cooldownFillTag);
        if (fillObj != null) cooldownFillImage = fillObj.GetComponent<Image>();

        var textObj = GameObject.FindWithTag(statusTextTag);
        if (textObj != null) statusText = textObj.GetComponent<TextMeshProUGUI>();

        var overlayObj = GameObject.FindWithTag(skillOverlayTag);
        if (overlayObj != null) skillOverlay = overlayObj.GetComponent<Image>();

        var specialObj = GameObject.FindWithTag(specialOverlayTag);
        if (specialObj != null) specialOverlay = specialObj.GetComponent<Image>();

        if (skillIconBg != null && cooldownFillImage != null && statusText != null)
        {
            uiResolved = true;
            ApplySkillIconFromSO();
            RefreshUI();
        }
    }

    /// <summary>ดึง Sprite จาก SO ใส่ icon — เรียกซ้ำได้เมื่อ character เปลี่ยน</summary>
    public void ApplySkillIconFromSO()
    {
        if (archer == null || archer.stats == null) return;
        if (!uiResolved) TryResolveUI();
        if (skillIconBg == null) return;

        // ArcherSkill ใช้ specialAttackIcon (slot R)
        if (archer.stats.skillIcon != null)
            skillIconBg.sprite = archer.stats.skillIcon;
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
        {
            archer.ForceNextShotAccuracy(aoeTargetPos);
            StartCoroutine(FireRainOfArrows());
        }
    }

    // ==================== Skill Logic ====================

    private void ActivateSkill()
    {
        isSkillActive = true;

        if (aoeIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(aoeIndicatorPrefab, GetGroundPoint(), Quaternion.identity);
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
        StartCoroutine(SpawnBurningZoneDelayed(aoeTargetPos));
        if (playerAudio != null) playerAudio.PlaySound(PlayerSoundType.Skill1);

        if (rainArrowPrefab == null)
        {
            Debug.LogWarning("[ArcherSkill] ไม่มี rainArrowPrefab!");
            StartCooldown();
            yield break;
        }

        int dmg = Mathf.RoundToInt(archer.maxDamage * damageMultiplier);
        Vector3 camForwardFlat = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 arrowDir = (camForwardFlat * 0.35f + Vector3.down).normalized;

        for (int i = 0; i < arrowCount; i++)
        {
            Vector2 disk = Random.insideUnitCircle * aoeRadius;
            Vector3 groundPos = aoeTargetPos + new Vector3(disk.x, 0f, disk.y);
            Vector3 spawnPos = groundPos - arrowDir * spawnHeight;

            var arrow = Instantiate(rainArrowPrefab, spawnPos, Quaternion.identity);
            arrow.GetComponent<RainArrowProjectile>()?.Launch(arrowDir, arrowSpeed, dmg, playerAudio);

            yield return new WaitForSeconds(fireInterval);
        }

        StartCooldown();
        Debug.Log($"<color=cyan>[ArcherSkill]</color> ยิงฝนธนู {arrowCount} ลูก! Dmg/ลูก: {dmg}");
    }

    // ==================== Burning Zone ====================

    private System.Collections.IEnumerator SpawnBurningZoneDelayed(Vector3 pos)
    {
        yield return new WaitForSeconds(burnDelay);

        if (burningZonePrefab == null)
        {
            Debug.LogWarning("[ArcherSkill] ไม่มี burningZonePrefab!");
            yield break;
        }

        var zone = Instantiate(burningZonePrefab, pos, Quaternion.identity);

        // ตั้งค่าให้ BurningZone (ถ้ามี script ติดมากับ prefab)
        var burning = zone.GetComponent<BurningZone>();
        if (burning != null)
        {
            burning.radius          = aoeRadius;
            burning.damagePerSecond = burnDamagePerSecond;
            burning.duration        = burnDuration;
        }

        // destroy prefab ตาม duration เผื่อ BurningZone script ไม่ได้ติดมา
        Destroy(zone, burnDuration + 0.5f);
        Debug.Log($"<color=orange>[ArcherSkill]</color> พื้นไฟเริ่มลุก! {burnDuration}s {burnDamagePerSecond}dmg/s");
    }

    // ==================== AoE Indicator ====================

    private void UpdateIndicatorPosition()
    {
        aoeTargetPos = GetGroundPoint();
        if (currentIndicator != null)
            currentIndicator.transform.position = aoeTargetPos;
    }

    private Vector3 GetGroundPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return hit.point;
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
            skillIconBg.color = isOnCooldown  ? colorCooldown
                              : isSkillActive ? colorActive
                              : colorReady;

        if (cooldownFillImage != null)
            cooldownFillImage.fillAmount = isOnCooldown ? (1f - cooldownTimer / cooldown) : 0f;

        if (statusText != null)
        {
            if (isOnCooldown)
                statusText.text = $"{cooldownTimer:F1}s";
            else if (isSkillActive)
                statusText.text = "ACTIVE";
            else
                statusText.text = "";
        }

        // Skill slot overlay — แสดงตอน Active
        if (skillOverlay != null)
            skillOverlay.color = isSkillActive
                ? colorOverlay
                : Color.clear;

        // SpecialAtk slot overlay — แสดงตอน Charge Mode
        if (specialOverlay != null)
            specialOverlay.color = archer.IsChargeModeActive
                ? colorOverlay
                : Color.clear;
    }

    // ==================== Public ====================

    public bool IsSkillActive => isSkillActive;
    public bool IsOnCooldown  => isOnCooldown;
    public float CooldownRatio => isOnCooldown ? (cooldownTimer / cooldown) : 0f;
}