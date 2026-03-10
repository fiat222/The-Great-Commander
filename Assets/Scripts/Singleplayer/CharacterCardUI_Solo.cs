using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI ของการ์ดเลือกตัวละคร 1 ใบ (Solo Mode)
/// ต้องสร้างเป็น Prefab แล้วลาก components ให้ครบ
/// </summary>
public class CharacterCardUI_Solo : MonoBehaviour
{
    [Header("Card Elements")]
    public Image characterIcon;
    public Button selectButton;

    [Header("Highlight Border")]
    public GameObject highlightBorder;

    private int _characterIndex;

    /// <summary>เรียกตอน Instantiate จาก CharacterSelectUI</summary>
    public void Setup(CharacterDataSO data, int index)
    {
        _characterIndex = index;

        if (characterIcon != null)
            characterIcon.sprite = data.icon != null ? data.icon : data.portrait;

        if (selectButton != null)
            selectButton.onClick.AddListener(OnClick);

        if (highlightBorder != null)
            highlightBorder.SetActive(false);
    }

    private void OnClick()
    {
        SingleCharacterSelectManager.Instance?.SelectCharacter(_characterIndex);
    }

    /// <summary>แสดง Border เมื่อเลือกการ์ดนี้อยู่</summary>
    public void SetHighlight(bool selected)
    {
        if (highlightBorder != null)
            highlightBorder.SetActive(selected);
    }

    /// <summary>เปิด/ปิดปุ่มกด (เมื่อ Ready แล้วจะปิด)</summary>
    public void SetInteractable(bool interactable)
    {
        if (selectButton != null)
            selectButton.interactable = interactable;
    }
}