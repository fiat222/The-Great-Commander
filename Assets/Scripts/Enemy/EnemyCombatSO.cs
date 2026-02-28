using UnityEngine;

[CreateAssetMenu(fileName = "NewCombatPattern", menuName = "Game/Enemy Combat SO")]
public class EnemyCombatSO : ScriptableObject
{
    [Header("--- ระยะเวลาพักหลังโจมตี (วินาที) ---")]
    [Tooltip("จังหวะพักหลังโจมตีเสร็จ (วินาที)")]
    public float attackCooldown = 1.5f;

    [Header("--- ระบบชะงัก (Hyper Armor) ---")]
    [Tooltip("ถ้าติ๊กไว้ ศัตรูจะไม่โดนขัดจังหวะ (ไม่เล่นท่า Damage) ขณะกำลังโจมตี")]
    public bool hasHyperArmor = true;

    [Header("--- เดินวนระหว่างรอ (Strafe) ---")]
    [Range(0f, 1f)]
    [Tooltip("โอกาสที่จะเดินวนรอบๆ เพลเยอร์ในช่วงคูลดาวน์")]
    public float strafeChance = 0.6f;
    [Tooltip("ระยะห่างที่ศัตรูจะรักษาขณะเดินวน (ต้องมากกว่า attackRange)")]
    public float strafeRange = 5f;
    [Tooltip("ระยะเวลาที่เดินวนต่อครั้ง (วินาที)")]
    public float strafeDuration = 2f;
    [Tooltip("ความเร็วขณะเดินวน (มักจะให้ช้ากว่าปกติ)")]
    public float strafeSpeed = 2f;

    [Header("Animation")]
    [Tooltip("ชื่อ Animator Trigger ที่ใช้สั่งโจมตี")]
    public string attackTriggerName = "Attack";

    [Tooltip("จำนวนท่าโจมตีที่มี (ใช้สุ่ม AttackIndex 0 ถึง N-1)")]
    public int attackVariants = 1;
}
