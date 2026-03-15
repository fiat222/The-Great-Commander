using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UpgradePlayerCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum PlayerType { Warrior, Archer }

    [Header("Player Type (Fallback Solo Only)")]
    [SerializeField] private PlayerType playerType = PlayerType.Warrior;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cardFrame;
    [SerializeField] private Button upgradeButton;

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

    public static System.Action<UpgradePlayerCard> OnAnyPlayerCardSelected;

    private Coroutine _levelUpCoroutine;

    // ==================== Lifecycle ====================

    private void Awake()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        if (highlightObj != null)
            highlightObj.SetActive(false);

        if (levelUpPopup != null)
            levelUpPopup.SetActive(false);
    }

    private void OnEnable()
    {
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged += OnMoneyChanged;

        OnAnyPlayerCardSelected += CheckHighlightStatus;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnMoneyChanged -= OnMoneyChanged;

        OnAnyPlayerCardSelected -= CheckHighlightStatus;
        TooltipUI.Instance?.Hide();
    }

    // ==================== Get Data ====================

    /// <summary>
    /// Solo → return 0 เสมอ
    /// Duo  → Host = 0, Client = 1
    /// </summary>
    private int GetPlayerIndex()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null
            && Unity.Netcode.NetworkManager.Singleton.IsConnectedClient)
        {
            return Unity.Netcode.NetworkManager.Singleton.IsHost ? 0 : 1;
        }

        return 0; // Solo
    }

    private CharacterDataSO GetCharacter()
    {
        return CharacterSelectData.GetCharacter(GetPlayerIndex());
    }

    private PlayerStatsSO GetStats()
    {
        if (UpgradeManager.Instance == null) return null;

        CharacterDataSO character = GetCharacter();

        if (character != null && character.statsSO != null)
            return character.statsSO;

        // Fallback
        return playerType == PlayerType.Warrior
            ? UpgradeManager.Instance.warriorStats
            : UpgradeManager.Instance.archerStats;
    }

    // ==================== Button ====================

    private void OnUpgradeClicked()
    {
        if (UpgradeManager.Instance == null) return;

        OnAnyPlayerCardSelected?.Invoke(this);

        PlayerStatsSO stats = GetStats();
        UpgradeManager.Instance.UpgradePlayerByStats(stats);

        ShowLevelUpPopup();
        RefreshUI();
    }

    // ==================== Hover Tooltip ====================

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayerStatsSO stats = GetStats();
        if (stats == null) return;

        CharacterDataSO character = GetCharacter();
        string typeName = character != null ? character.characterName
                        : (playerType == PlayerType.Warrior ? "Warrior" : "Archer");

        bool isMax = stats.IsMaxLevel;
        int cost = stats.GetUpgradeCost();
        string content;

        if (isMax)
        {
            content = $"<b>HP:</b> {stats.GetHP()}\n" +
                      $"<b>ATK:</b> {stats.GetDamage():F1}\n" +
                      $"<b>DEF:</b> {stats.GetDefense():F1}\n" +
                      $"<b>Speed:</b> {stats.GetSpeed():F1}\n" +
                      $"<color=#FFD700>MAX LEVEL</color>";
        }
        else
        {
            int nextHP      = Mathf.RoundToInt(stats.baseHP     * Mathf.Pow(stats.hpMultiplier,     stats.CurrentLevel + 1));
            float nextATK   = stats.baseDamage  * Mathf.Pow(stats.damageMultiplier,  stats.CurrentLevel + 1);
            float nextDEF   = stats.baseDefense * Mathf.Pow(stats.defenseMultiplier, stats.CurrentLevel + 1);
            float nextSpeed = stats.baseSpeed   * Mathf.Pow(stats.speedMultiplier,   stats.CurrentLevel + 1);

            int   diffHP    = nextHP    - stats.GetHP();
            float diffATK   = nextATK   - stats.GetDamage();
            float diffDEF   = nextDEF   - stats.GetDefense();
            float diffSpeed = nextSpeed - stats.GetSpeed();

            content = $"<b>HP:</b> {stats.GetHP()} <color=#44FF44>+{diffHP}</color>\n" +
                      $"<b>ATK:</b> {stats.GetDamage():F1} <color=#44FF44>+{diffATK:F1}</color>\n" +
                      $"<b>DEF:</b> {stats.GetDefense():F1} <color=#44FF44>+{diffDEF:F1}</color>\n" +
                      $"<b>Speed:</b> {stats.GetSpeed():F1} <color=#44FF44>+{diffSpeed:F1}</color>\n" +
                      $"<color=#FFDD44>Cost: {cost} Orb</color>";
        }

        TooltipUI.Instance?.Show(typeName, content, TooltipUI.TooltipSize.Small);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance?.Hide();
    }

    // ==================== Level Up Popup ====================

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

    // ==================== Callbacks ====================

    private void OnMoneyChanged(int _) => RefreshUI();

    private void CheckHighlightStatus(UpgradePlayerCard selectedCard)
    {
        if (highlightObj != null)
            highlightObj.SetActive(selectedCard == this);
    }

    // ==================== UI Refresh ====================

    public void RefreshUI()
    {
        PlayerStatsSO stats = GetStats();
        if (stats == null) return;

        CharacterDataSO character = GetCharacter();

        int currentMoney = PlacementManager.Instance != null
            ? PlacementManager.Instance.Money
            : 0;

        bool isMax = stats.IsMaxLevel;
        int cost = stats.GetUpgradeCost();

        if (nameText != null)
            nameText.text = character != null ? character.characterName
                          : (playerType == PlayerType.Warrior ? "Warrior" : "Archer");

        if (levelText != null)
            levelText.text = $"Lv {stats.CurrentLevel} / {stats.maxLevel}";

        if (costText != null)
            costText.text = isMax ? "MAX" : $"{cost}";

        if (upgradeButton != null)
            upgradeButton.interactable = !isMax && currentMoney >= cost;

        if (iconImage != null)
        {
            if (character != null && character.icon != null)
                iconImage.sprite = character.icon;
            else if (stats.icon != null)
                iconImage.sprite = stats.icon;
        }

        UpdateCardColor(stats.CurrentLevel, stats.maxLevel);
    }

    // ==================== Card Color ====================

    private void UpdateCardColor(int currentLevel, int maxLevel)
    {
        if (cardFrame == null) return;

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