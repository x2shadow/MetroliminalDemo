using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Внешний компонент для управления тряской камеры через CinemachineBasicMultiChannelPerlin.
/// Вызывает плавное изменение amplitude/frequency в зависимости от горизонтальной скорости,
/// состояния бега/ходьбы и на земле ли игрок.
/// Опционально поддерживает Cinemachine Impulse для "ударных" шагов.
/// </summary>
[DisallowMultipleComponent]
public class PlayerCameraShake : MonoBehaviour
{
    [Header("CinemachineBasicMultiChannelPerlin")]
    public CinemachineBasicMultiChannelPerlin vcamNoise;

    [Header("Idle (breathing)")]
    public float idleAmplitude = 0.5f;
    public float idleFrequency = 0.3f;

    [Header("Walk")]
    public float walkAmplitude = 0.55f;
    public float walkFrequency = 4f;

    [Header("Run")]
    public float runAmplitude = 0.6f;
    public float runFrequency = 5f;

    [Tooltip("Скорость сглаживания изменения amplitude/frequency")]
    public float shakeSmoothSpeed = 8f;

    private void Awake()
    {
        vcamNoise.AmplitudeGain = idleAmplitude;
        vcamNoise.FrequencyGain = idleFrequency;
    }

    /// <summary>
    /// Вызывается извне (например из PlayerController) каждый кадр.
    /// horizVelocity — горизонтальная составляющая velocity (Vector3.xz)
    /// maxPossibleSpeed — максимальная горизонтальная скорость (например moveSpeed * sprintMultiplier)
    /// isSprinting / isGrounded — состояния для корректного выбора режима.
    /// </summary>
    public void UpdateShake(Vector3 horizVelocity, float maxPossibleSpeed, bool isSprinting, bool isGrounded)
    {
        if (vcamNoise == null) return;

        float horizSpeed = new Vector3(horizVelocity.x, 0f, horizVelocity.z).magnitude;
        float normalizedSpeed = (maxPossibleSpeed > 0f) ? Mathf.Clamp01(horizSpeed / maxPossibleSpeed) : 0f;

        const float moveThreshold = 0.05f;

        float targetAmp = idleAmplitude;
        float targetFreq = idleFrequency;

        if (isGrounded && normalizedSpeed > moveThreshold)
        {
            if (isSprinting)
            {
                targetAmp = runAmplitude * normalizedSpeed;
                targetFreq = runFrequency;
            }
            else
            {
                targetAmp = Mathf.Lerp(idleAmplitude, walkAmplitude, normalizedSpeed);
                targetFreq = Mathf.Lerp(idleFrequency, walkFrequency, normalizedSpeed);
            }
        }
        else
        {
            targetAmp = idleAmplitude;
            targetFreq = idleFrequency;
        }

        // Плавное сглаживание (умножаем на Time.deltaTime)
        vcamNoise.AmplitudeGain = Mathf.Lerp(vcamNoise.AmplitudeGain, targetAmp, Time.deltaTime * shakeSmoothSpeed);
        vcamNoise.FrequencyGain = Mathf.Lerp(vcamNoise.FrequencyGain, targetFreq, Time.deltaTime * shakeSmoothSpeed);
    }
}
