using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // เพิ่มเพื่อให้ใช้งาน IEnumerator ได้

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

        // เช็กการยกเลิก: ถ้าผู้เล่นกด Cancel ให้หยุดการนับถอยหลังทันที
        if (!mgr.IsReady)
        {
            StopAllCoroutines();
            if (countdownText != null) countdownText.gameObject.SetActive(false);
        }

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

    // ==================== Countdown Logic ====================

    private void ShowCountdown()
    {
        if (countdownText != null)
        {
            StopAllCoroutines(); // เคลียร์เผื่อมีการเรียกซ้ำ
            StartCoroutine(CountdownRoutine());
        }
    }

    private IEnumerator CountdownRoutine()
    {
        countdownText.gameObject.SetActive(true);
        int timer = 3;

        while (timer > 0)
        {
            countdownText.text = $"Game Start In {timer}...";
            // ใช้ Realtime เพื่อให้ชัวร์ว่าเวลานับแม่นยำแม้จะมีการ Pause เกม
            yield return new WaitForSecondsRealtime(1f);
            timer--;
        }

        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.5f);

        // ตรงนี้คุณสามารถใส่คำสั่งเริ่มเกม เช่น SceneManager.LoadScene ได้เลย
    }
}