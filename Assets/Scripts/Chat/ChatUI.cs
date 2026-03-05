using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChatUI : MonoBehaviour
{
    public static ChatUI Instance { get; private set; }

    [Header("References")]
    public GameObject chatBG;                  // พื้นหลัง+ScrollView ซ่อน/แสดงตาม isOpen
    public Transform floatingContainer;        // Content ลอย (แสดงตลอด)
    public Transform scrollContainer;          // Content ใน ScrollView
    public TMP_InputField inputField;
    public ScrollRect scrollRect;
    public GameObject messagePrefab;

    [Header("Settings")]
    public int maxMessages = 20;
    public float messageFadeDelay = 5f;

    private bool isOpen = false;
    private int floatingCount = 0;
    private int scrollCount = 0;

    private void Awake() => Instance = this;

    private void Start()
    {
        chatBG.SetActive(false);
        inputField.gameObject.SetActive(false);
        inputField.onDeselect.AddListener(_ => { if (isOpen) SubmitAndClose(); });
    }

    private void Update()
    {
        if (!isOpen && Input.GetKeyDown(KeyCode.Return))
            OpenChat();
        else if (isOpen && Input.GetKeyDown(KeyCode.Return))
            SubmitAndClose();
    }

    void OpenChat()
    {
        isOpen = true;
        chatBG.SetActive(true);
        inputField.gameObject.SetActive(true);
        inputField.text = "";

        // ลบ floating ทั้งหมดเมื่อเปิด chat
        foreach (Transform child in floatingContainer)
            Destroy(child.gameObject);
        floatingCount = 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ActivateInputNextFrame());
    }

    IEnumerator ActivateInputNextFrame()
    {
        yield return null;
        inputField.ActivateInputField();
    }

    void SubmitAndClose()
    {
        if (!isOpen) return;

        string msg = inputField.text.Trim();
        if (!string.IsNullOrEmpty(msg))
            ChatManager.Instance?.SendMessage(msg);

        isOpen = false;
        chatBG.SetActive(false);
        inputField.gameObject.SetActive(false);
        inputField.text = "";

        if (GameManager.Instance != null)
        {
            bool isCombat = GameManager.Instance.CurrentPhase == GamePhase.Combat;
            Cursor.lockState = isCombat ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isCombat;
        }
    }

    public void AddMessage(string message)
    {
        if (messagePrefab == null) return;

        // 1. สร้างข้อความลอย (แสดง 5 วิแล้วหาย)
        if (floatingContainer != null)
        {
            if (floatingCount >= maxMessages)
            {
                Destroy(floatingContainer.GetChild(0).gameObject);
                floatingCount--;
            }

            GameObject floatMsg = Instantiate(messagePrefab, floatingContainer);
            TMP_Text floatTmp = floatMsg.GetComponent<TMP_Text>();
            floatTmp.text = message;
            floatTmp.color = Color.white;
            floatingCount++;

            StartCoroutine(FadeAndDestroy(floatTmp, messageFadeDelay));
        }

        // 2. สร้างข้อความใน ScrollView (ถาวร เห็นตอนเปิด)
        if (scrollContainer != null)
        {
            if (scrollCount >= maxMessages)
            {
                Destroy(scrollContainer.GetChild(0).gameObject);
                scrollCount--;
            }

            GameObject scrollMsg = Instantiate(messagePrefab, scrollContainer);
            TMP_Text scrollTmp = scrollMsg.GetComponent<TMP_Text>();
            scrollTmp.text = message;
            scrollTmp.color = Color.white;
            scrollCount++;

            StartCoroutine(ScrollToBottom());
        }
    }

    IEnumerator FadeAndDestroy(TMP_Text tmp, float delay)
    {
        // รอ delay วินาที
        yield return new WaitForSeconds(delay);

        // Fade ออก 1 วินาที
        float duration = 1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (tmp == null) yield break;
            tmp.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }

        // ลบออก
        if (tmp != null) Destroy(tmp.gameObject);
        floatingCount--;
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}