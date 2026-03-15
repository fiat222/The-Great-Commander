using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownUI : MonoBehaviour
{
    private TextMeshProUGUI countdownText;

    private void Awake()
    {
        countdownText = GetComponent<TextMeshProUGUI>();
        gameObject.SetActive(false); // ปิดไว้ก่อนในตอนเริ่ม
    }

    public void StartCountdown(int seconds)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(CountdownRoutine(seconds));
    }

    public void CancelCountdown()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    private IEnumerator CountdownRoutine(int seconds)
    {
        int timer = seconds;
        while (timer > 0)
        {
            countdownText.text = $"Game Start In {timer}...";
            yield return new WaitForSecondsRealtime(1f);
            timer--;
        }

        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.5f);

        // หากต้องการให้หายไปหลังจาก GO!
        // gameObject.SetActive(false);
    }
}