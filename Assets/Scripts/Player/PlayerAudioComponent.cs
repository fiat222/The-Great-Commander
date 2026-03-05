using UnityEngine;
using System.Collections.Generic;

namespace PlayerAudio
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayerAudioComponent : MonoBehaviour
    {
        [Header("Audio Data")]
        [Tooltip("แผ่นป้ายเสียงของตัวละครนี้ (เช่น WarriorAudioSO)")]
        public PlayerAudioSO audioDataSO;

        private AudioSource myAudioSource;
        private Dictionary<PlayerSoundType, PlayerAudioSO.SoundEntry> audioDict;

        private void Awake()
        {
            myAudioSource = GetComponent<AudioSource>();
            audioDict = new Dictionary<PlayerSoundType, PlayerAudioSO.SoundEntry>();

            if (audioDataSO != null)
            {
                // ดึงข้อมูลจากแผ่น SO มาสร้างเป็นดิกชันนารีเพื่อความรวดเร็วในการค้นหาตอนเล่นจริง
                foreach (var entry in audioDataSO.audioEntries)
                {
                    if (entry.clip != null && !audioDict.ContainsKey(entry.soundType))
                    {
                        audioDict.Add(entry.soundType, entry);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerAudioComponent] ยังไม่ได้ใส่แผ่นเสียง AudioSO ให้กับ {gameObject.name}");
            }
        }

        /// <summary>
        /// เรียกเล่นเสียงตามประเภทที่ระบุ
        /// </summary>
        public void PlaySound(PlayerSoundType type)
        {
            if (audioDict.TryGetValue(type, out var entry))
            {
                // ใช้ PlayOneShot เพื่อให้เสียงทับซ้อนกันได้ (เช่นฟันดาบระรัว)
                myAudioSource.PlayOneShot(entry.clip, entry.volume);
            }
            else
            {
                Debug.LogWarning($"[PlayerAudioComponent] ไม่พบเสียง {type} ในแผ่น SO ของ {gameObject.name}");
            }
        }
    }
}
