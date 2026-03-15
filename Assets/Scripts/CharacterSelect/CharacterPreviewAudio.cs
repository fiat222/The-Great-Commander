using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CharacterPreviewAudio : MonoBehaviour
{
    [SerializeField] private AudioClip selectionVoice;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        // ตั้งค่าให้เล่นเป็น 2D เพื่อความชัดเจนในหน้าเมนู
        _audioSource.spatialBlend = 0f;
        _audioSource.playOnAwake = false;
    }

    private void Start()
    {
        // เมื่อ Prefab ถูก Spawn และ Start ทำงาน ให้เล่นเสียงทันที
        if (selectionVoice != null)
        {
            _audioSource.PlayOneShot(selectionVoice);
        }
    }
}