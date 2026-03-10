using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI สำหรับ Character Select (Single Player)
/// </summary>
public class SingleCharacterSelectUI : MonoBehaviour
{
    [Header("Character Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI classText;
    public GameObject readyIndicator;
    public Transform statContainer;
    public GameObject statBarPrefab;

    [Header("Stat Max Values")]
    public float maxHP = 1000f;
    public float maxATK = 100f;
    public float maxDEF = 50f;
    public float maxSPD = 20f;

    [Header("Character Cards")]
    public Transform cardContainer;
    public GameObject characterCardPrefab;

    [Header("Bottom UI")]
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI countdownText;

    private CharacterCardUI_Solo[] _cardInstances;

    private void OnEnable()
    {
        SingleCharacterSelectManager.OnSelectionChanged += RefreshUI;
        SingleCharacterSelectManager.OnReadyChanged += RefreshReadyUI;
        SingleCharacterSelectManager.OnStarting += ShowCountdown;
    }

    private void OnDisable()
    {
        SingleCharacterSelectManager.OnSelectionChanged -= RefreshUI;
        SingleCharacterSelectManager.OnReadyChanged -= RefreshReadyUI;
        SingleCharacterSelectManager.OnStarting -= ShowCountdown;
    }

    private void Start()
    {
        SetupCards();
        readyButton.onClick.AddListener(() => SingleCharacterSelectManager.Instance?.ToggleReady());
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    // ==================== Setup ====================

    private void SetupCards()
    {
        if (SingleCharacterSelectManager.Instance == null)
        {
            Invoke(nameof(SetupCards), 0.5f);
            return;
        }

        var characters = SingleCharacterSelectManager.Instance.characters;
        _cardInstances = new CharacterCardUI_Solo[characters.Length];

        for (int i = 0; i < characters.Length; i++)
        {
            var go = Instantiate(characterCardPrefab, cardContainer);
            var card = go.GetComponent<CharacterCardUI_Solo>();
            card.Setup(characters[i], i);
            _cardInstances[i] = card;
        }
    }

    // ==================== Refresh ====================

    private void RefreshUI()
    {
        var mgr = SingleCharacterSelectManager.Instance;
        if (mgr == null) return;

        var charData = mgr.GetSelectedCharacter();

        // ลบ StatBar เก่า
        if (statContainer != null)
            foreach (Transform child in statContainer)
                Destroy(child.gameObject);

        if (charData != null)
        {
            if (nameText != null) nameText.text = charData.characterName;
            if (classText != null) classText.text = charData.className;

            if (statContainer != null && statBarPrefab != null)
            {
                CreateStatBar("HP", charData.GetHP(), maxHP);
                CreateStatBar("ATK", charData.GetATK(), maxATK);
                CreateStatBar("DEF", charData.GetDEF(), maxDEF);
                CreateStatBar("SPD", charData.GetSpeed(), maxSPD);
            }
        }
        else
        {
            if (nameText != null) nameText.text = "???";
            if (classText != null) classText.text = "Choose a Character...";
        }

        UpdateCardHighlights();

        if (SingleCharacterDisplaySpawner.Instance != null)
            SingleCharacterDisplaySpawner.Instance.RefreshDisplay();
    }

    private void RefreshReadyUI()
    {
        var mgr = SingleCharacterSelectManager.Instance;
        if (mgr == null) return;

        if (readyIndicator != null) readyIndicator.SetActive(mgr.IsReady);

        if (readyButtonText != null)
            readyButtonText.text = mgr.IsReady ? "Cancel" : "Ready!";

        // ถ้า Ready แล้ว ปิด Card ทั้งหมด
        SetCardsInteractable(!mgr.IsReady);
    }

    // ==================== Helpers ====================

    private void CreateStatBar(string label, float value, float max)
    {
        var go = Instantiate(statBarPrefab, statContainer);
        go.GetComponent<StatBarUI>()?.Setup(label, value, max);
    }

    private void UpdateCardHighlights()
    {
        var mgr = SingleCharacterSelectManager.Instance;
        if (mgr == null || _cardInstances == null) return;

        for (int i = 0; i < _cardInstances.Length; i++)
            _cardInstances[i].SetHighlight(mgr.SelectedIndex == i);
    }

    private void SetCardsInteractable(bool interactable)
    {
        if (_cardInstances == null) return;
        foreach (var card in _cardInstances)
            card.SetInteractable(interactable);
    }

    private void ShowCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "Game Start In 3...";
        }
    }
}