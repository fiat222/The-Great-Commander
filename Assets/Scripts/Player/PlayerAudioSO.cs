using UnityEngine;
using System.Collections.Generic;

namespace PlayerAudio
{
    [CreateAssetMenu(fileName = "NewPlayerAudioData", menuName = "Audio/Player Audio Data", order = 1)]
    public class PlayerAudioSO : ScriptableObject
    {
        [System.Serializable]
        public class SoundEntry
        {
            public PlayerSoundType soundType;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Header("SFX Library")]
        public SoundEntry[] audioEntries;

        // ไม่ต้องใช้ Dictionary ใน SO ก็ได้ เพราะข้อมูลมันตายตัว 
        // ให้ Component เป็นคนสร้าง Dictionary ตอนเริ่มเกมเอาจะปลอดภัยกว่า
    }
}
