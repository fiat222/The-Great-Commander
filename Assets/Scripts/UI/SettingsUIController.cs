using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsUIController : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel หลักของ Settings ที่จะเปิด/ปิด")]
    public GameObject settingsPanel;
    
    [Header("Pages (เนื้อหาฝั่งขวา)")]
    public GameObject pageControls;
    public GameObject pageAudio;
    public GameObject pageBrightness;

    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Brightness")]
    public Slider brightnessSlider;
    [Tooltip("Image สีดำที่กางเต็มจอ และตั้ง Raycast Target เป็น false (ควรอยู่นอก SettingsPanel เพื่อให้มืดตลอดเวลา)")]
    public Image brightnessOverlay;

    // เก็บค่าที่เซฟไว้ เผื่อกรณีกดยกเลิกจะได้คืนค่าเดิม
    private float savedMaster;
    private float savedBgm;
    private float savedSfx;
    private float savedBrightness;

    private void Start()
    {
        // 1. โหลดค่าที่เคยเซฟไว้ (ถ้าไม่มีให้ใช้ค่า Default)
        savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        savedBgm = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        savedSfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
        savedBrightness = PlayerPrefs.GetFloat("Brightness", 1f);

        // 2. นำค่าไปใช้กับเกมทันที (เผื่อข้าม Scene แล้วไม่มีคนเซ็ต)
        ApplySettings(savedMaster, savedBgm, savedSfx, savedBrightness);

        // 3. ตั้งค่า Slider ให้ตรงกับที่โหลดมา
        if (masterVolumeSlider) masterVolumeSlider.value = savedMaster;
        if (bgmVolumeSlider) bgmVolumeSlider.value = savedBgm;
        if (sfxVolumeSlider) sfxVolumeSlider.value = savedSfx;
        if (brightnessSlider) brightnessSlider.value = savedBrightness;

        // 4. ผูก Event ให้ทำงานตอนเลื่อน Slider (Real-time Preview)
        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(val => PreviewSettings());
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener(val => PreviewSettings());
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(val => PreviewSettings());
        if (brightnessSlider) brightnessSlider.onValueChanged.AddListener(val => PreviewSettings());

        // ปิดหน้าต่าง Settings ไว้ก่อนตอนเริ่มเกม
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    /// <summary>เรียกผ่านปุ่มรูปฟันเฟือง</summary>
    public void OpenSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
        
        // รีเซ็ต Slider ให้ตรงกับค่าที่เซฟไว้ล่าสุด (เผื่อก่อนหน้านี้เลื่อนเล่นแล้วกดยกเลิก)
        if (masterVolumeSlider) masterVolumeSlider.SetValueWithoutNotify(savedMaster);
        if (bgmVolumeSlider) bgmVolumeSlider.SetValueWithoutNotify(savedBgm);
        if (sfxVolumeSlider) sfxVolumeSlider.SetValueWithoutNotify(savedSfx);
        if (brightnessSlider) brightnessSlider.SetValueWithoutNotify(savedBrightness);

        // ใช้ค่าที่เซฟไว้แสดงผล
        ApplySettings(savedMaster, savedBgm, savedSfx, savedBrightness);
        
        // ซ่อนหน้าฝั่งขวาทั้งหมดไว้ก่อน (รอผู้เล่นกดหัวข้อฝั่งซ้ายก่อนค่อยแสดงทีละหน้า)
        HideAllPages();
    }

    /// <summary>ปุ่ม "ตกลง" (Confirm / Apply)</summary>
    public void ConfirmSettings()
    {
        // 1. ดึงค่าจาก Slider ปัจจุบัน
        savedMaster = masterVolumeSlider ? masterVolumeSlider.value : savedMaster;
        savedBgm = bgmVolumeSlider ? bgmVolumeSlider.value : savedBgm;
        savedSfx = sfxVolumeSlider ? sfxVolumeSlider.value : savedSfx;
        savedBrightness = brightnessSlider ? brightnessSlider.value : savedBrightness;

        // 2. เซฟลงเครื่อง (PlayerPrefs)
        PlayerPrefs.SetFloat("MasterVolume", savedMaster);
        PlayerPrefs.SetFloat("BGMVolume", savedBgm);
        PlayerPrefs.SetFloat("SFXVolume", savedSfx);
        PlayerPrefs.SetFloat("Brightness", savedBrightness);
        PlayerPrefs.Save();

        // 3. ยืนยันการใช้งานค่านี้
        ApplySettings(savedMaster, savedBgm, savedSfx, savedBrightness);

        // ปิดหน้าต่าง
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    /// <summary>ปุ่ม "กลับสู่เกม" หรือ "ยกเลิก" (Cancel / Close)</summary>
    public void CancelAndCloseSettings()
    {
        // คืนค่ากลับเป็นค่าที่เซฟไว้ล่าสุด
        ApplySettings(savedMaster, savedBgm, savedSfx, savedBrightness);
        
        // ปิดหน้าต่าง
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    /// <summary>ปุ่ม "ยอมแพ้" (Surrender)</summary>
    public void Surrender()
    {
        Debug.Log("Surrender clicked! กดยอมแพ้ โชว์หน้า Lose");

        // ปิดหน้า Settings
        if (settingsPanel) settingsPanel.SetActive(false);

        // แสดงหน้า Lose Panel
        if (EnemyTracker.Instance == null)
        {
            Debug.LogError("[Surrender] EnemyTracker.Instance is NULL! กรุณาตรวจสอบว่าใน Scene มี EnemyTracker หรือไม่");
            return;
        }

        if (EnemyTracker.Instance.youLostUI == null)
        {
            Debug.LogError("[Surrender] EnemyTracker.Instance.youLostUI is NULL! กรุณาลาก You Lost UI ไปใส่ใน Inspector ของ EnemyTracker");
            return;
        }

        // เปิดใช้งาน You Lost UI
        EnemyTracker.Instance.youLostUI.SetActive(true);
        EnemyTracker.Instance.youLostUI.transform.SetAsLastSibling(); // เอามาไว้หน้าสุดกันโดน UI อื่นบัง

        // ปลดล็อคเมาส์เพื่อให้สามารถกดปุ่มในหน้า Lose ได้
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[Surrender] แสดงหน้าต่าง Lose ไปแล้ว!");
    }

    /// <summary>ทำงานทุกครั้งที่เลื่อน Slider</summary>
    private void PreviewSettings()
    {
        float m = masterVolumeSlider ? masterVolumeSlider.value : savedMaster;
        float b = bgmVolumeSlider ? bgmVolumeSlider.value : savedBgm;
        float s = sfxVolumeSlider ? sfxVolumeSlider.value : savedSfx;
        float br = brightnessSlider ? brightnessSlider.value : savedBrightness;
        
        ApplySettings(m, b, s, br);
    }

    /// <summary>ฟังก์ชันหัวใจหลักในการสั่งเพิ่ม/ลดเสียงและแสง</summary>
    private void ApplySettings(float master, float bgm, float sfx, float brightness)
    {
        // 1. Master Volume (ปรับเสียงทั้งเกมรวมถึง Listener)
        AudioListener.volume = master;

        // 2. BGM & SFX (ส่งค่าไปให้ AudioManager จัดการ AudioSource)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(bgm);
            AudioManager.Instance.SetSFXVolume(sfx);
        }

        // 3. Brightness (ความสว่าง)
        if (brightnessOverlay != null)
        {
            // บังคับเปิด Brightness Overlay ตลอดเวลา (เผื่อเผลอไปปิดมันไว้ใน Hierarchy)
            if (!brightnessOverlay.gameObject.activeSelf)
                brightnessOverlay.gameObject.SetActive(true);

            Color c = brightnessOverlay.color;
            // brightness = 1 (สว่างสุด) -> alpha = 0
            // brightness = 0 (มืดสุด) -> alpha = 0.95 (เผื่อให้พอมองเห็นบ้าง ไม่ดำสนิท)
            c.a = Mathf.Clamp(1f - brightness, 0f, 0.95f); 
            brightnessOverlay.color = c;
        }
    }

    // ─────────────────────────────────────────
    //  Tab Navigation (ปุ่มฝั่งซ้าย)
    // ─────────────────────────────────────────
    public void HideAllPages()
    {
        if (pageControls) pageControls.SetActive(false);
        if (pageAudio) pageAudio.SetActive(false);
        if (pageBrightness) pageBrightness.SetActive(false);
    }

    public void ShowPageControls()
    {
        HideAllPages();
        if (pageControls) pageControls.SetActive(true);
    }

    public void ShowPageAudio()
    {
        HideAllPages();
        if (pageAudio) pageAudio.SetActive(true);
    }

    public void ShowPageBrightness()
    {
        HideAllPages();
        if (pageBrightness) pageBrightness.SetActive(true);
    }
}
