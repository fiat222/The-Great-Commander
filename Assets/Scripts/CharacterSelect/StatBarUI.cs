using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatBarUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI labelText;  // "HP", "ATK" etc.
    public Image fillBar;              // Image Type = Filled, Fill Method = Horizontal
    public TextMeshProUGUI valueText;  // "800", "60" etc.

    public void Setup(string label, float value, float maxValue)
    {
        if (labelText != null) labelText.text = label;
        if (valueText != null) valueText.text = Mathf.RoundToInt(value).ToString();
        if (fillBar != null) fillBar.fillAmount = Mathf.Clamp01(value / maxValue);
    }
}