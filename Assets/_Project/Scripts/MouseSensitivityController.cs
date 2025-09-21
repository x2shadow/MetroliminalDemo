using UnityEngine;
using UnityEngine.UI;

public class MouseSensitivityController : MonoBehaviour
{
    public Slider sensitivitySlider;
    // Ключ для сохранения в PlayerPrefs
    private const string SensitivityPrefKey = "MouseSensitivity";

    // Ссылка на PlayerController; можно назначить через инспектор или искать в сцене
    public PlayerController playerController;

    private void Start()
    {
        // Получаем сохранённое значение чувствительности или устанавливаем значение по умолчанию, например 100f
        float savedSensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, 1f);

        // Применяем сохранённое значение к PlayerController
        if (playerController != null)
        {
            playerController.mouseSensitivity = savedSensitivity;
        }

        // Настраиваем слайдер
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = savedSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
    }

    private void OnSensitivityChanged(float value)
    {
        if (playerController != null)
        {
            playerController.mouseSensitivity = value;
        }
        // Сохраняем новое значение
        PlayerPrefs.SetFloat(SensitivityPrefKey, value);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }
    }
}
