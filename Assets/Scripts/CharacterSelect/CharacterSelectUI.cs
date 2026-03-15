using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Player 1")]
    public TextMeshProUGUI p1NameText;
    public TextMeshProUGUI p1ClassText;
    public GameObject p1ReadyIndicator;
    public Transform p1StatContainer;

    [Header("Player 2")]
    public TextMeshProUGUI p2NameText;
    public TextMeshProUGUI p2ClassText;
    public GameObject p2ReadyIndicator;
    public Transform p2StatContainer;

    [Header("Stat Bar Prefabs")]
    public GameObject p1StatBarPrefab;  // หลอดสีน้ำเงิน
    public GameObject p2StatBarPrefab;  // หลอดสีแดง

    [Header("Stat Max Values")]
    public float maxHP = 1000f;
    public float maxATK = 100f;
    public float maxDEF = 50f;
    public float maxSPD = 20f;

    [Header("VS + Cards")]
    public TextMeshProUGUI vsText;
    public Transform cardContainer;
    public GameObject characterCardPrefab;

    [Header("Bottom UI")]
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;
    public CountdownUI countdownController;

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
        // สั่งปิดผ่าน Controller
        if (countdownController != null) countdownController.gameObject.SetActive(false);
    }

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

    private void RefreshUI()
    {
        var mgr = CharacterSelectManager.Instance;
        if (mgr == null) return;

        RefreshPlayerUI(
            mgr.GetSelectedCharacter(0),
            p1NameText, p1ClassText, p1StatContainer, p1StatBarPrefab
        );
        RefreshPlayerUI(
            mgr.GetSelectedCharacter(1),
            p2NameText, p2ClassText, p2StatContainer, p2StatBarPrefab
        );

        UpdateCardHighlights();

        if (CharacterDisplaySpawner.Instance != null)
            CharacterDisplaySpawner.Instance.RefreshDisplays();
    }

    private void RefreshPlayerUI(
        CharacterDataSO charData,
        TextMeshProUGUI nameText,
        TextMeshProUGUI classText,
        Transform statContainer,
        GameObject statPrefab)
    {
        // ลบ StatBar เก่าออก
        if (statContainer != null)
            foreach (Transform child in statContainer)
                Destroy(child.gameObject);

        if (charData != null)
        {
            nameText.text = charData.characterName;
            if (classText != null) classText.text = charData.className;

            // แสดง StatBar เฉพาะตอนเลือกแล้วเท่านั้น
            if (statContainer != null && statPrefab != null)
            {
                CreateStatBar(statContainer, statPrefab, "HP", charData.GetHP(), maxHP);
                CreateStatBar(statContainer, statPrefab, "ATK", charData.GetATK(), maxATK);
                CreateStatBar(statContainer, statPrefab, "DEF", charData.GetDEF(), maxDEF);
                CreateStatBar(statContainer, statPrefab, "SPD", charData.GetSpeed(), maxSPD);
            }
        }
        else
        {
            // ยังไม่เลือก — แสดงแค่ชื่อ ไม่มี StatBar
            nameText.text = "???";
            if (classText != null) classText.text = "Choose a Charactor...";
            // ไม่สร้าง StatBar เลย
        }
    }

    private void CreateStatBar(Transform container, GameObject prefab, string label, float value, float max)
    {
        var go = Instantiate(prefab, container);
        go.GetComponent<StatBarUI>()?.Setup(label, value, max);
    }

    private void RefreshReadyUI()
    {
        var mgr = CharacterSelectManager.Instance;
        if (mgr == null) return;

        if (p1ReadyIndicator != null) p1ReadyIndicator.SetActive(mgr.p1Ready.Value);
        if (p2ReadyIndicator != null) p2ReadyIndicator.SetActive(mgr.p2Ready.Value);

        bool myReady = mgr.AmIHost ? mgr.p1Ready.Value : mgr.p2Ready.Value;
        if (readyButtonText != null)
            readyButtonText.text = myReady ? "Cancel" : "Ready!";

        UpdateCardInteractable(!myReady);

        // --- เพิ่มเติม: ถ้าใครกดยกเลิก ให้สั่งหยุดนับถอยหลัง ---
        if (!mgr.p1Ready.Value || !mgr.p2Ready.Value)
        {
            if (countdownController != null) countdownController.CancelCountdown();
        }
    }

    private void UpdateCardHighlights()
    {
        var mgr = CharacterSelectManager.Instance;
        if (mgr == null || cardInstances == null) return;
        for (int i = 0; i < cardInstances.Length; i++)
            cardInstances[i].SetHighlight(
                mgr.p1Selection.Value == i,
                mgr.p2Selection.Value == i
            );
    }

    private void UpdateCardInteractable(bool interactable)
    {
        if (cardInstances == null) return;
        foreach (var card in cardInstances)
            card.SetInteractable(interactable);
    }

    private void ShowCountdown()
    {
        // เรียกใช้งานผ่าน Controller แทนการเขียน Logic เอง
        if (countdownController != null)
        {
            countdownController.StartCountdown(3);
        }
    }
}