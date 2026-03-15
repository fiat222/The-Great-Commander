using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PlayerController), typeof(Animator))]
public class WarriorSkill : MonoBehaviour
{
    // ==================== Skill 1 (Ice Smash) ====================
    [Header("Skill 1 (Ice Smash) Settings")]
    public GameObject iceSmashPrefab;
    [Tooltip("จุดที่พรีแฟบก้อนน้ำแข็งจะโผล่ขึ้นมา")]
    public Transform skillSpawnPoint;
    [Tooltip("ชดเชยองศาการหันของเอฟเฟค เช่น เบี้ยวขวาให้ใส่ Y = -90")]
    public Vector3 vfxRotationOffset = Vector3.zero;
    [Tooltip("ตัวคูณดาเมจของสกิล")]
    public float damageMultiplier = 1.5f;
    public float skill1Cooldown = 5f;
    [Tooltip("ระยะเวลาที่ก้อนน้ำแข็งจะคงอยู่ก่อนหายไป")]
    public float vfxLifetime = 1f;
    [Tooltip("กราฟควบคุมความเร็วและระยะเวลาในการพุ่งไปข้างหน้าตอนใช้สกิล")]
    public AnimationCurve skillMoveCurve = AnimationCurve.Linear(0f, 15f, 0.5f, 0f);

    // ==================== Skill UI Tags ====================
    [Header("Skill UI Tags")]
    [Tooltip("Tag ของ Image icon สกิล")]
    public string skillIconTag      = "SkillIcon";
    [Tooltip("Tag ของ Image Filled สำหรับ Cooldown")]
    public string cooldownFillTag   = "SkillCooldownFill";
    [Tooltip("Tag ของ TextMeshProUGUI แสดงเวลา")]
    public string statusTextTag     = "SkillStatusText";
    [Tooltip("Tag ของ Image overlay ทับ Skill icon (แสดงตอน Active)")]
    public string skillOverlayTag   = "SkillActiveOverlay";
    [Tooltip("Tag ของ Image overlay ทับ SpecialAtk icon (แสดงตอน Parry)")]
    public string specialOverlayTag = "SkillSpecialOverlay";

    [Header("Skill UI Colors")]
    public Color colorReady    = Color.white;
    public Color colorActive   = new Color(0.3f, 1f, 0.4f, 1f);
    public Color colorCooldown = new Color(0.35f, 0.35f, 0.35f, 1f);
    public Color colorOverlay  = new Color(1f, 1f, 1f, 0.35f);
    public Color colorClear  = new Color(0f, 0f, 0f, 0f);
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
    private float skill1CooldownTimer;
    private bool isCastingSkill;
    private bool isOnCooldown;

    private PlayerController player;
    private CharacterController controller;
    private Animator animator;

    // ==================== Lifecycle ====================

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

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
        if (player.IsDead) return;

        HandleCooldownTick();
        HandleSkillInput();
        RefreshUI();
    }

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

    // ==================== Icon from SO ====================

    /// <summary>
    /// ดึง Sprite จาก PlayerStatsSO แล้วใส่ให้ skillIconBg อัตโนมัติ
    /// เรียกตอน Start และเรียกซ้ำได้เมื่อ character เปลี่ยน
    /// </summary>
    public void ApplySkillIconFromSO()
    {
        if (player == null || player.stats == null) return;
        if (!uiResolved) TryResolveUI();
        if (skillIconBg == null) return;

        // WarriorSkill ใช้ skillIcon (slot R)
        if (player.stats.skillIcon != null)
        {
            skillIconBg.sprite = player.stats.skillIcon;
            if (cooldownFillImage != null)
                cooldownFillImage.sprite = player.stats.skillIcon;
        }
    }

    // ==================== Input ====================

    private void HandleSkillInput()
    {
        if (ChatManager.Instance != null && ChatManager.Instance.IsChatOpen) return;
        
        if (Input.GetKeyDown(KeyCode.R) && !isOnCooldown)
        {
            var sInfo = animator.GetCurrentAnimatorStateInfo(0);
            var nInfo = animator.GetNextAnimatorStateInfo(0);

            bool isBusy = sInfo.IsTag("Roll")  || nInfo.IsTag("Roll")  ||
                          sInfo.IsTag("Hit")   || nInfo.IsTag("Hit")   ||
                          sInfo.IsTag("Parry") || nInfo.IsTag("Parry") ||
                          sInfo.IsTag("Skill") || nInfo.IsTag("Skill");

            bool isGrounded = animator.GetBool("isGrounded");

            if (isGrounded && !player.IsDodging && !isBusy && !isCastingSkill)
            {
                ExecuteSkillExternal();
            }
        }
        else if (Input.GetKeyDown(KeyCode.R) && isOnCooldown)
        {
            Debug.Log($"<color=yellow>[WarriorSkill]</color> Cooldown อีก {skill1CooldownTimer:F1}s");
        }
    }

    public void ExecuteSkillExternal()
    {
        if (isOnCooldown || isCastingSkill) return;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Skill1");

        player.ResetComboAndDash();

        StartCooldown();
        StartCoroutine(SkillMovementRoutine());
    }

    // ==================== Cooldown ====================

    private void StartCooldown()
    {
        isOnCooldown = true;
        skill1CooldownTimer = skill1Cooldown;
    }

    private void HandleCooldownTick()
    {
        if (!isOnCooldown) return;
        skill1CooldownTimer -= Time.deltaTime;
        if (skill1CooldownTimer <= 0f)
        {
            skill1CooldownTimer = 0f;
            isOnCooldown = false;
            Debug.Log("<color=lime>[WarriorSkill]</color> Skill พร้อมแล้ว!");
        }
    }

    // ==================== UI ====================

    private void RefreshUI()
    {
        if (skillIconBg != null)
            skillIconBg.color = isOnCooldown  ? colorCooldown
                              : isCastingSkill ? colorActive
                              : colorReady;

        if (cooldownFillImage != null)
            cooldownFillImage.fillAmount = isOnCooldown
                ? (1f - skill1CooldownTimer / skill1Cooldown)
                : 0f;

        if (statusText != null)
        {
            if (isOnCooldown)
                statusText.text = $"{skill1CooldownTimer:F1}s";
            else if (isCastingSkill)
                statusText.text = "ACTIVE";
            else
                statusText.text = "";
        }

        // Skill slot overlay — แสดงตอน Casting
        if (skillOverlay != null)
            skillOverlay.color = isCastingSkill
                ? colorOverlay
                : colorClear;

        // SpecialAtk slot overlay — แสดงตอน Parry window เปิดอยู่
        if (specialOverlay != null)
            specialOverlay.color = player.IsParrying
                ? colorOverlay
                : colorClear;
    }

    // ==================== Public ====================

    public bool IsCastingSkill => isCastingSkill;
    public bool IsOnCooldown   => isOnCooldown;
    public float CooldownRatio => isOnCooldown ? (skill1CooldownTimer / skill1Cooldown) : 0f;

    // ==================== Skill Movement ====================

    private float skillTurnSpeed = 8f;

    private System.Collections.IEnumerator SkillMovementRoutine()
    {
        isCastingSkill = true;

        float duration = 0.5f;
        if (skillMoveCurve != null && skillMoveCurve.length > 0)
            duration = skillMoveCurve[skillMoveCurve.length - 1].time;

        float timer = 0f;
        Transform camTransform = Camera.main.transform;

        while (timer < duration)
        {
            if (player.IsDead) break;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v).normalized;

            if (inputDir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, skillTurnSpeed * Time.deltaTime);
            }

            float curveSpeed = skillMoveCurve.Evaluate(timer);
            controller?.Move(transform.forward * curveSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        isCastingSkill = false;
    }

    // ==================== Animation Events ====================

    public void SpawnIceSmashVFX()
    {
        if (iceSmashPrefab == null)
        {
            Debug.LogWarning("<color=yellow>[WarriorSkill]</color> ยังไม่ได้ใส่ Prefab ของ IceSmashVFX!");
            return;
        }
        if (skillSpawnPoint == null)
        {
            Debug.LogWarning("<color=yellow>[WarriorSkill]</color> ยังไม่ได้ลากจุด Spawn Point!");
            return;
        }

        Quaternion spawnRot = skillSpawnPoint.rotation * Quaternion.Euler(vfxRotationOffset);
        GameObject vfx = Instantiate(iceSmashPrefab, skillSpawnPoint.position, spawnRot);

        PlayerWeaponHitbox hitbox = vfx.GetComponent<PlayerWeaponHitbox>()
                                 ?? vfx.GetComponentInChildren<PlayerWeaponHitbox>();
        if (hitbox != null)
        {
            hitbox.SetDamage(player.AttackDamage * damageMultiplier);
            hitbox.ClearHitList();
        }

        Destroy(vfx, vfxLifetime);
    }
}