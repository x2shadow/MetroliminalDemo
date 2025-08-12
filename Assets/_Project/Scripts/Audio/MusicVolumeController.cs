using UnityEngine;
using UnityEngine.UI;

public class MusicVolumeController : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioManager audioManager;

    void Start()
    {
        // Автопоиск AudioManager если не установлен
        if (audioManager == null)
            audioManager = AudioManager.Instance;
        
        // Инициализация слайдера текущим значением
        if (audioManager != null)
        {
            volumeSlider.value = audioManager.GetMusicVolume();
        }
        else
        {
            Debug.LogWarning("AudioManager not found!");
            volumeSlider.value = 1f;
        }
        
        // Подписка на изменение слайдера
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float volume)
    {
        if (audioManager != null)
        {
            audioManager.SetMusicVolume(volume);
        }
    }

    void OnDestroy()
    {
        // Отписка при уничтожении объекта
        volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }
}