using UnityEngine;
using UnityEngine.Rendering.Universal; // 1. เพิ่ม URP Support

public class ResolutionController : MonoBehaviour
{
    private float targetAspectRatio = 16f / 9f;
    private Camera mainCam;
    private Camera uiCam;
    private GameObject uiCamObj;
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Start()
    {
        SetupCameras();
        UpdateResolution(force: true);
    }

    void Update()
    {
        // ตรวจสอบการเปลี่ยนขนาดหน้าจอเพื่อประหยัด CPU (ไม่ต้องรัน Sync ทุกเฟรมถ้าไม่เปลี่ยน)
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateResolution();
        }
    }

    private void SetupCameras()
    {
        mainCam = GetComponent<Camera>();
        if (mainCam == null) mainCam = Camera.main;

        if (mainCam == null) return;

        var mainCamData = mainCam.GetUniversalAdditionalCameraData();
        mainCamData.renderType = CameraRenderType.Base;
        
        mainCam.cullingMask &= ~(1 << 5);

        if (uiCamObj == null)
        {
            uiCamObj = new GameObject("Dedicated_UI_Camera");
            uiCamObj.transform.SetParent(mainCam.transform);
            uiCamObj.transform.localPosition = Vector3.zero;
            uiCamObj.transform.localRotation = Quaternion.identity;

            uiCam = uiCamObj.AddComponent<Camera>();
            var uiCamData = uiCam.GetUniversalAdditionalCameraData();
            uiCamData.renderType = CameraRenderType.Overlay;
            
            uiCam.cullingMask = (1 << 5); // เห็นเฉพาะ UI
            uiCam.useOcclusionCulling = false;

            if (!mainCamData.cameraStack.Contains(uiCam))
            {
                mainCamData.cameraStack.Add(uiCam);
            }
        }
    }

    void UpdateResolution(bool force = false)
    {
        if (mainCam == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspectRatio;

        Rect rect = new Rect(0, 0, 1, 1);
        float currentMatch = 0f; // 0 = Width, 1 = Height

        if (scaleHeight < 1.0f) // Pillarbox (Wide screen)
        {
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            currentMatch = 1f; // บังคับให้ขยับตามแนวตั้ง (เพราะแนวนอนเหลือขอบ)
        }
        else // Letterbox (Tall screen เช่น 16:10)
        {
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            currentMatch = 0f; // บังคับให้ขยับตามแนวนอน (เพราะแนวตั้งเหลือขอบ)
        }

        mainCam.rect = rect;
        if (uiCam != null) uiCam.rect = rect;

        SyncCanvases(currentMatch);
    }

    private void SyncCanvases(float matchValue)
    {
        if (uiCam == null) return;

        Canvas[] canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.layer != 5) canvas.gameObject.layer = 5;

            if (canvas.renderMode != RenderMode.ScreenSpaceCamera || canvas.worldCamera != uiCam)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCam;
                canvas.planeDistance = 1;
            }

            // --- ⭐ แก้ไขเรื่อง UI เพี้ยน: ปรับ CanvasScaler ให้ตรงกับสัดส่วน 16:9 ---
            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = matchValue;
            }
        }
    }

    private void OnDestroy()
    {
        if (mainCam != null)
        {
            mainCam.cullingMask |= (1 << 5);
            var mainCamData = mainCam.GetUniversalAdditionalCameraData();
            if (uiCam != null && mainCamData != null) mainCamData.cameraStack.Remove(uiCam);
        }
        if (uiCamObj != null) Destroy(uiCamObj);
    }
}

