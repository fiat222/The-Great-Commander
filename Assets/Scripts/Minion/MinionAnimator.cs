using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MinionAnimator : MonoBehaviour
{
    private Animator anim;

    // Best Practice: ใช้ StringToHash เพื่อให้มันทำงานได้ไวขึ้น (ลดการคำนวณ String ซ้ำๆ)
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int dieHash = Animator.StringToHash("Die");
    private readonly int hitHash = Animator.StringToHash("TakeDamage");

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // 1. แอนิเมชันเดิน/วิ่ง (ตั้งค่า Speed จาก AI วิ่ง)
    public void SetSpeed(float currentSpeed)
    {
        // ส่งความเร็วเดินไปบอก Animator ว่าตอนนี้กำลังเดินอยู่หรือไม่
        anim.SetFloat(speedHash, currentSpeed);
    }

    // 2. แอนิเมชันโจมตี
    public void TriggerAttack()
    {
        anim.SetTrigger(attackHash);
    }

    // 3. แอนิเมชันตอนโดนตี (สะดุ้ง)
    public void TriggerHit()
    {
        anim.SetTrigger(hitHash);
    }

    // 4. แอนิเมชันตอนตาย
    public void TriggerDeath()
    {
        anim.SetTrigger(dieHash);
    }

    // --- ส่วนเสริม: ถ้ามี Animation Event บนโมเดลให้เรียกฟังชันนี้ ---
    // (ใช้กรณีที่อยากให้ดาเมจเกิดตอนถึงคีย์เฟรมที่ดาบฟันโดนศัตรู พอดีเป๊ะ!)
    public void OnAnimationAttackHit()
    {
        // แจ้งเตือน AI หรือ สคริปต์โจมตี ของ Minion ให้ทำการปล่อยลูกตะกั่ว หรือ ลดเลือดศัตรูตรงนี้ 
        Debug.Log("โมชันฟันโดนศัตรูแล้ว! ใส่ดาเมจตรงนี้");
    }
}
