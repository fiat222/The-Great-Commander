using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UpgradeItemCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cardFrame;        // ← ลาก Frame มาใส่
    [SerializeField] private Button upgradeBtn;

    [Header("Highlight Settings")]
    [SerializeField] private GameObject highlightObj;

    [Header("Level Up Feedback")]
    [SerializeField] private GameObject levelUpPopup;
    [SerializeField] private float levelUpDisplayTime = 1.2f;

    [Header("Level Colors")]
    [SerializeField] private Color colorLevel1   = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color colorLevel2   = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color colorLevel4   = new Color(0.6f, 0.2f, 0.9f);
    [SerializeField] private Color colorLevelMax = new Color(1f, 0.84f, 0f);

    public static System.Action<UpgradeItemCard> OnAnyUpgradeCardSelected;

    private MinionData _data;
    private int _index;
    private Coroutine _levelUpCoroutine;

    private void Awake()
    {
        if (upgradeBtn == null)
            upgradeBtn = GetComponentInChildren<Button>();

        if (upgradeBtn != null)
        {
            upgradeBtn.onClick.RemoveAllListeners();
            upgradeBtn.onClick.AddListener(OnUpgradeClicked);
        }

        if (highlightObj != null)
            highlightObj.SetActive(false);

        if (levelUpPopup != null)
            levelUpPopup.SetActive(false);
    }

    private void OnEnable()
    {
        MinionData.OnMinionUpgraded += OnMinionUpgraded;

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged += OnMoneyChanged;

        OnAnyUpgradeCardSelected += CheckHighlightStatus;

        RefreshUI();
    }

    private void OnDisable()
    {
        MinionData.OnMinionUpgraded -= OnMinionUpgraded;

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged -= OnMoneyChanged;

        OnAnyUpgradeCardSelected -= CheckHighlightStatus;

        TooltipUI.Instance?.Hide();
    }

    public void Setup(MinionData data, int index)
    {
        _data = data;
        _index = index;

        if (nameText != null)
            nameText.text = data.minionName;

        // ⭐ แสดงรูป: ลองใช้ icon ก่อน ถ้าไม่มีให้ใช้ picture แทน
        if (iconImage != null)
        {
            Sprite sprite = data.icon != null ? data.icon : data.picture;
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true;
            }
        }

        if (highlightObj != null)
            highlightObj.SetActive(false);

        RefreshUI();
    }

    private void OnUpgradeClicked()
    {
        OnAnyUpgradeCardSelected?.Invoke(this);

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.UpgradeMinion(_data);
            ShowLevelUpPopup();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data == null) return;

        bool isMax = _data.IsMaxLevel;
        int cost = _data.GetUpgradeCost();

        string content;

        if (isMax)
        {
            content = $"<b>HP:</b> {_data.GetHP():F0}\n" +
                      $"<b>ATK:</b> {_data.GetDamage():F1}\n" +
                      $"<b>DEF:</b> {_data.GetDefense():F1}\n" +
                      $"<b>Speed:</b> {_data.GetSpeed():F1}\n" +
                      $"<b>Range:</b> {_data.attackrange}\n" +
                      $"<color=#FFD700>MAX LEVEL</color>";
        }
        else
        {
            float nextHP    = _data.GetHP()      * _data.hpMultiplier;
            float nextATK   = _data.GetDamage()  * _data.attackMultiplier;
            float nextDEF   = _data.GetDefense() * _data.defenseMultiplier;

            float diffHP    = nextHP    - _data.GetHP();
            float diffATK   = nextATK   - _data.GetDamage();
            float diffDEF   = nextDEF   - _data.GetDefense();

            content = $"<b>HP:</b> {_data.GetHP():F0} <color=#44FF44>+{diffHP:F0}</color>\n" +
                      $"<b>ATK:</b> {_data.GetDamage():F1} <color=#44FF44>+{diffATK:F1}</color>\n" +
                      $"<b>DEF:</b> {_data.GetDefense():F1} <color=#44FF44>+{diffDEF:F1}</color>\n" +
                      $"<b>Speed:</b> {_data.GetSpeed():F1}\n" +
                      $"<b>Range:</b> {_data.attackrange}\n" +
                      $"<color=#FFDD44>Cost: {cost} Orb</color>";
        }

        TooltipUI.Instance?.Show(_data.minionName, content, TooltipUI.TooltipSize.Small);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance?.Hide();
    }

    private void ShowLevelUpPopup()
    {
        if (levelUpPopup == null) return;

        if (_levelUpCoroutine != null)
            StopCoroutine(_levelUpCoroutine);

        _levelUpCoroutine = StartCoroutine(LevelUpRoutine());
    }

    private System.Collections.IEnumerator LevelUpRoutine()
    {
        levelUpPopup.SetActive(true);
        yield return new WaitForSeconds(levelUpDisplayTime);
        levelUpPopup.SetActive(false);
        _levelUpCoroutine = null;
    }

    private void OnMinionUpgraded(MinionData upgraded)
    {
        if (upgraded == _data)
            RefreshUI();
    }

    private void OnMoneyChanged(int _) => RefreshUI();

    private void CheckHighlightStatus(UpgradeItemCard selectedCard)
    {
        if (highlightObj != null)
            highlightObj.SetActive(selectedCard == this);
    }

    private void RefreshUI()
    {
        if (_data == null) return;

        int currentMoney = PlacementManager.Instance != null
            ? PlacementManager.Instance.Money
            : 0;

        bool isMax = _data.IsMaxLevel;
        int cost = _data.GetUpgradeCost();

        if (levelText != null)
            levelText.text = $"Lv {_data.CurrentLevel} / {_data.maxLevel}";

        if (costText != null)
            costText.text = isMax ? "MAX" : $"{cost}";

        if (upgradeBtn != null)
            upgradeBtn.interactable = !isMax && currentMoney >= cost;

        UpdateCardColor(_data.CurrentLevel, _data.maxLevel);
    }

    private void UpdateCardColor(int currentLevel, int maxLevel)
    {
        if (cardFrame == null) return;      // ← เปลี่ยนแค่ Frame

        Color targetColor;

        if (currentLevel >= maxLevel)
            targetColor = colorLevelMax;
        else if (currentLevel >= 4)
            targetColor = colorLevel4;
        else if (currentLevel >= 2)
            targetColor = colorLevel2;
        else
            targetColor = colorLevel1;

        cardFrame.color = targetColor;
    }
}