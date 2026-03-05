using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// แสดงข้อความทีละตัวอักษร (Typewriter Effect) วนซ้ำจนกว่าจะหยุด
/// ใช้กับ Status Text บน HostPanel และ ClientPanel
///
/// วิธีใช้:
///   TypewriterStatus.Instance.Play("Waiting for player...");    // เริ่มวน
///   TypewriterStatus.Instance.PlayOnce("Copied!");              // แสดงครั้งเดียว
///   TypewriterStatus.Instance.Stop("Connected!");               // หยุด + แสดงข้อความสุดท้าย
///   TypewriterStatus.Instance.Stop();                           // หยุด + ล้างข้อความ
/// </summary>
public class TypewriterStatus : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TMP Text ที่จะแสดงผล")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [Tooltip("ความเร็วพิมพ์ (วินาที/ตัวอักษร)")]
    [SerializeField] private float charInterval = 0.07f;
    [Tooltip("หยุดค้างก่อนวนรอบใหม่ (วินาที)")]
    [SerializeField] private float loopDelay = 0.5f;

    private Coroutine _coroutine;

    // ─────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────

    /// <summary>พิมพ์ข้อความทีละตัว แล้ววนซ้ำ (ใช้ตอนรอ connect)</summary>
    public void Play(string message)
    {
        StopEffect();
        gameObject.SetActive(true);
        _coroutine = StartCoroutine(LoopRoutine(message));
    }

    /// <summary>พิมพ์ข้อความทีละตัวครั้งเดียว ไม่วน</summary>
    public void PlayOnce(string message)
    {
        StopEffect();
        gameObject.SetActive(true);
        _coroutine = StartCoroutine(TypeRoutine(message, loop: false));
    }

    /// <summary>หยุดการวน + แสดงข้อความสุดท้ายทันที (เช่น "Connected!" หรือ Error)</summary>
    public void Stop(string finalMessage = "")
    {
        StopEffect();
        if (statusText != null)
            statusText.text = finalMessage;
    }

    // ─────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────

    private IEnumerator LoopRoutine(string message)
    {
        while (true)
        {
            yield return TypeRoutine(message, loop: true);
            yield return new WaitForSeconds(loopDelay);
            if (statusText != null) statusText.text = "";
        }
    }

    private IEnumerator TypeRoutine(string message, bool loop)
    {
        if (statusText == null) yield break;
        statusText.text = "";

        foreach (char c in message)
        {
            statusText.text += c;
            yield return new WaitForSeconds(charInterval);
        }
    }

    private void StopEffect()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private void OnDisable()
    {
        StopEffect();
        if (statusText != null) statusText.text = "";
    }
}
