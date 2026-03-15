using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections;
using PlayerAudio;

/// <summary>
/// โครงสร้างพื้นฐานใหม่สำหรับตัวละคร (ใช้กับ Wizard เป็นตัวแรก)
/// จะไม่กระทบกับ Warrior และ Archer ของเก่า
/// </summary>
public abstract class BaseCharacter : NetworkBehaviour
{
    [Header("Identity & Stats")]
    public PlayerStatsSO stats;
    protected int currentHP;
    protected bool isDead;

    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float rotationSpeed = 13f;
    public float gravity = -19.62f;

    [Header("References")]
    protected CharacterController controller;
    protected Animator animator;
    protected Transform mainCamera;
    protected PlayerAudioComponent playerAudio;

    [Header("UI Elements (Auto)")]
    public Slider healthBar;
    public TextMeshProUGUI hpText;

    protected Vector3 verticalVelocity;
    protected float rotationVelocity;
    protected bool isGrounded;

    public int MaxHP => stats != null ? stats.GetHP() : 100;

    protected virtual void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<PlayerAudioComponent>();
        mainCamera = Camera.main != null ? Camera.main.transform : null;

        // Auto find UI
        if (healthBar == null) healthBar = GameObject.FindWithTag("HPBar")?.GetComponent<Slider>();
        if (hpText == null) hpText = GameObject.FindWithTag("HPText")?.GetComponent<TextMeshProUGUI>();

        if (stats != null)
        {
            moveSpeed = stats.GetSpeed();
            currentHP = MaxHP;
        }
        UpdateUI();
    }

    protected void HandleStandardMovement(float speedMultiplier = 1.0f)
    {
        UpdateGrounded();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0, v).normalized;

        if (animator != null) animator.SetFloat("FreelookSpeed", dir.magnitude, 0.05f, Time.deltaTime);

        Vector3 moveDir = Vector3.zero;
        if (dir.magnitude >= 0.1f && mainCamera != null)
        {
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, 1f / rotationSpeed);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward * (moveSpeed * speedMultiplier);
        }

        if (isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2f;
        else verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move((moveDir + verticalVelocity) * Time.deltaTime);
    }

    protected void UpdateGrounded()
    {
        // Simple grounded check (ปรับปรุงได้ตามความเหมาะสมของสภาพแวดล้อม)
        isGrounded = controller.isGrounded;
        if (animator != null) animator.SetBool("isGrounded", isGrounded);
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateUI();
        if (currentHP <= 0) Die();
    }

    protected void UpdateUI()
    {
        if (healthBar != null) { healthBar.maxValue = MaxHP; healthBar.value = currentHP; }
        if (hpText != null) hpText.text = $"{currentHP}/{MaxHP}";
    }

    protected virtual void Die()
    {
        isDead = true;
        if (animator != null) animator.SetTrigger("Die");
        controller.enabled = false;
    }
}
