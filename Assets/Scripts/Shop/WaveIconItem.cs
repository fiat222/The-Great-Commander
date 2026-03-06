using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveIconItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    
    public int EnemyTypeIndex { get; private set; }

    public void Setup(Sprite icon, int count, int typeIndex = -1)
    {
        this.EnemyTypeIndex = typeIndex;
        if (iconImage != null) iconImage.sprite = icon;
        if (countText != null) countText.text = "X " + count;
    }
}
