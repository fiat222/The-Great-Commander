using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI ของการ์ดเลือกตัวละคร 1 ใบ
/// ต้องสร้างเป็น Prefab แล้วลาก components ให้ครบ
/// </summary>
public class CharacterCardUI : MonoBehaviour
{
    [Header("Card Elements")]
    public Image characterIcon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI classText;
    public Button selectButton;

    [Header("Highlight Borders")]
    [Tooltip("สีฟ้า = P1 กำลังเลือกการ์ดนี้")]
    public GameObject p1HighlightBorder;
    [Tooltip("สีแดง = P2 กำลังเลือกการ์ดนี้")]
    public GameObject p2HighlightBorder;

    private int characterIndex;

    /// <summary>เรียกตอน Instantiate จาก CharacterSelectUI</summary>
    public void Setup(CharacterDataSO data, int index)
    {
        characterIndex = index;

        // ใช้ icon ก่อน ถ้าไม่มีค่อยใช้ portrait
        if (characterIcon != null)
            characterIcon.sprite = data.icon != null ? data.icon : data.portrait;

        if (nameText != null)
            nameText.text = data.characterName;

        if (classText != null)
            classText.text = data.className;

        if (selectButton != null)
            selectButton.onClick.AddListener(OnClick);

        // ซ่อน Highlight ตอนเริ่ม
        if (p1HighlightBorder != null) p1HighlightBorder.SetActive(false);
        if (p2HighlightBorder != null) p2HighlightBorder.SetActive(false);
    }

    private void OnClick()
    {
        CharacterSelectManager.Instance?.SelectCharacter(characterIndex);
    }

    /// <summary>แสดง Border ว่าใครเลือกการ์ดนี้อยู่</summary>
    public void SetHighlight(bool p1Selected, bool p2Selected)
    {
        if (p1HighlightBorder != null) p1HighlightBorder.SetActive(p1Selected);
        if (p2HighlightBorder != null) p2HighlightBorder.SetActive(p2Selected);
    }

    /// <summary>เปิด/ปิดปุ่มกด (เมื่อ Player Ready แล้วจะปิด)</summary>
    public void SetInteractable(bool interactable)
    {
        if (selectButton != null)
            selectButton.interactable = interactable;
    }
}
