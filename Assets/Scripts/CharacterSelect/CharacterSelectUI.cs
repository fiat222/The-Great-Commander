using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// จัดการ UI ของหน้าเลือกตัวละคร
/// วางบน Canvas ใน CharacterSelectScene
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Player 1 (Left Side)")]
    public Image p1Portrait;
    public TextMeshProUGUI p1NameText;
    public TextMeshProUGUI p1ClassText;
    public GameObject p1ReadyIndicator; // เช่น ✅ icon

    [Header("Player 2 (Right Side)")]
    public Image p2Portrait;
    public TextMeshProUGUI p2NameText;
    public TextMeshProUGUI p2ClassText;
    public GameObject p2ReadyIndicator;

    [Header("VS")]
    public TextMeshProUGUI vsText;

    [Header("Character Cards")]
    [Tooltip("Parent ที่จะ Spawn CharacterCard เข้าไป (ควรมี HorizontalLayoutGroup)")]
    public Transform cardContainer;
    public GameObject characterCardPrefab; // Prefab ของการ์ดเลือก

    [Header("Bottom UI")]
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI countdownText;

    [Header("Placeholder Sprites")]
    [Tooltip("รูปแสดงเมื่อยังไม่เลือกตัวละคร")]
    public Sprite unknownPortrait;

    private CharacterCardUI[] cardInstances;

    private void OnEnable()
    {
        CharacterSelectManager.OnSelectionChanged += RefreshUI;
        CharacterSelectManager.OnReadyChanged += RefreshReadyUI;
        CharacterSelectManager.OnAllReadyAndStarting += ShowCountdown;
    }

    private void OnDisable()
    {
        CharacterSelectManager.OnSelectionChanged -= RefreshUI;
        CharacterSelectManager.OnReadyChanged -= RefreshReadyUI;
        CharacterSelectManager.OnAllReadyAndStarting -= ShowCountdown;
    }

    private void Start()
    {
        SetupCards();
        readyButton.onClick.AddListener(() => CharacterSelectManager.Instance?.ToggleReady());
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    /// <summary>สร้างการ์ด Character จาก Database</summary>
    private void SetupCards()
    {
        if (CharacterSelectManager.Instance == null)
        {
            Invoke(nameof(SetupCards), 0.5f);
            return;
        }

        var characters = CharacterSelectManager.Instance.characters;
        cardInstances = new CharacterCardUI[characters.Length];

        for (int i = 0; i < characters.Length; i++)
        {
            var go = Instantiate(characterCardPrefab, cardContainer);
            var card = go.GetComponent<CharacterCardUI>();
            card.Setup(characters[i], i);
            cardInstances[i] = card;
        }
    }

    /// <summary>อัพเดต Portrait และชื่อทั้ง 2 ฝั่ง</summary>
    private void RefreshUI()
    {
        var mgr = CharacterSelectManager.Instance;
        if (mgr == null) return;

        // ========== Player 1 (Left) ==========
        var p1Char = mgr.GetSelectedCharacter(0);
        if (p1Char != null)
        {
            p1Portrait.sprite = p1Char.portrait;
            p1Portrait.color = Color.white;
            p1NameText.text = p1Char.characterName;
            if (p1ClassText != null) p1ClassText.text = p1Char.className;
        }
        else
        {
            if (unknownPortrait != null) p1Portrait.sprite = unknownPortrait;
            p1Portrait.color = new Color(1, 1, 1, 0.3f);
            p1NameText.text = "Player 1";
            if (p1ClassText != null) p1ClassText.text = "เลือกตัวละคร...";
        }

        // ========== Player 2 (Right) ==========
        var p2Char = mgr.GetSelectedCharacter(1);
        if (p2Char != null)
        {
            p2Portrait.sprite = p2Char.portrait;
            p2Portrait.color = Color.white;
            p2NameText.text = p2Char.characterName;
            if (p2ClassText != null) p2ClassText.text = p2Char.className;
        }
        else
        {
            if (unknownPortrait != null) p2Portrait.sprite = unknownPortrait;
            p2Portrait.color = new Color(1, 1, 1, 0.3f);
            p2NameText.text = "Player 2";
            if (p2ClassText != null) p2ClassText.text = "เลือกตัวละคร...";
        }

        // อัพเดต Highlight บนการ์ด
        UpdateCardHighlights();
    }

    /// <summary>อัพเดตสถานะ Ready บน UI</summary>
    private void RefreshReadyUI()
    {
        var mgr = CharacterSelectManager.Instance;
        if (mgr == null) return;

        if (p1ReadyIndicator != null) p1ReadyIndicator.SetActive(mgr.p1Ready.Value);
        if (p2ReadyIndicator != null) p2ReadyIndicator.SetActive(mgr.p2Ready.Value);

        // Ready Button
        bool myReady = mgr.AmIHost ? mgr.p1Ready.Value : mgr.p2Ready.Value;
        if (readyButtonText != null) readyButtonText.text = myReady ? "❌ ยกเลิก" : "✅ พร้อม!";

        // ถ้า Ready แล้วปิดปุ่มการ์ด (ห้ามเปลี่ยน)
        UpdateCardInteractable(!myReady);
    }

    /// <summary>Highlight การ์ดที่แต่ละ Player เลือก</summary>
    private void UpdateCardHighlights()
    {
        var mgr = CharacterSelectManager.Instance;
        if (mgr == null || cardInstances == null) return;

        for (int i = 0; i < cardInstances.Length; i++)
        {
            bool p1Selected = mgr.p1Selection.Value == i;
            bool p2Selected = mgr.p2Selection.Value == i;
            cardInstances[i].SetHighlight(p1Selected, p2Selected);
        }
    }

    private void UpdateCardInteractable(bool interactable)
    {
        if (cardInstances == null) return;
        foreach (var card in cardInstances)
            card.SetInteractable(interactable);
    }

    private void ShowCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "🎮 เริ่มเกมใน 2 วินาที...";
        }
    }
}
