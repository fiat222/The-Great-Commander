using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveIconItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    public void Setup(Sprite icon, int count)
    {
        if (iconImage != null) iconImage.sprite = icon;
        if (countText != null) countText.text = "X " + count;
    }
}
