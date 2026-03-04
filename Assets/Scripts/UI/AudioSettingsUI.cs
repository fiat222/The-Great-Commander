using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ควบคุม Popup ปรับเสียง BGM และ SFX
/// ผูก Script นี้กับ AudioPanel GameObject
///
/// โครงสร้าง AudioPanel:
///   Slider_BGM       → bgmSlider
///   Slider_SFX       → sfxSlider
///   Text_BGMValue    → bgmValueText  (optional, แสดง %)
///   Text_SFXValue    → sfxValueText  (optional, แสดง %)
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Value Labels (Optional)")]
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    private void OnEnable()
    {
        // โหลดค่า Volume ปัจจุบันจาก AudioManager ทุกครั้งที่เปิด Popup
        if (AudioManager.Instance == null) return;

        if (bgmSlider != null)
        {
            bgmSlider.value = AudioManager.Instance.BgmVolume;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance.SfxVolume;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        UpdateLabels();
    }

    private void OnBGMChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolume(value);
        UpdateLabels();
    }

    private void OnSFXChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if (bgmValueText != null && bgmSlider != null)
            bgmValueText.text = Mathf.RoundToInt(bgmSlider.value * 100) + "%";

        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100) + "%";
    }

    /// <summary>ปิด AudioPanel — ผูกกับปุ่ม Back/Close บน AudioPanel ได้โดยตรง</summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
