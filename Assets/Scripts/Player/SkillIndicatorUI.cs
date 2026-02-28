using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI ซ้าย/ขวาที่โผล่ตอนเลือก AoE target
/// Screen Space - Overlay
///
/// Setup:
///   Canvas (Screen Space Overlay)
///     ├── LeftHint  (Image รูปเมาส์ซ้าย + TMP "Use")
///     └── RightHint (Image รูป R + TMP "Cancel")
///
///   ลาก LeftHint และ RightHint มาใส่ Inspector
///   Script นี้ ShowHint() / HideHint() จาก ArcherSkill
/// </summary>
public class SkillIndicatorUI : MonoBehaviour
{
    [Header("Hint UI")]
    public GameObject leftHint;   // เมาส์ซ้าย + "Use"
    public GameObject rightHint;  // R + "Cancel"

    private void Awake()
    {
        HideHint();
    }

    public void ShowHint()
    {
        if (leftHint != null) leftHint.SetActive(true);
        if (rightHint != null) rightHint.SetActive(true);
    }

    public void HideHint()
    {
        if (leftHint != null) leftHint.SetActive(false);
        if (rightHint != null) rightHint.SetActive(false);
    }
}