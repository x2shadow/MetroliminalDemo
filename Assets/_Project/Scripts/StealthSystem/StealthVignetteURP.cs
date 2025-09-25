using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Управляет интенсивностью Vignette в URP Volume на основе состояния PlayerStealth.
/// Требует URP volume в сцене (Global Volume) с Profile, содержащим Vignette override.
/// Если Vignette не найден — скрипт попытается добавить его в профиль (будет изменён Asset).
/// </summary>
[DisallowMultipleComponent]
public class StealthVignetteURP : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ссылка на Volume с Vignette (Global Volume). Если не заполнено — будет найден первый Volume в сцене.")]
    public Volume volume;

    [Tooltip("Компонент PlayerStealth на игроке (auto-find по тегу Player если пусто).")]
    public PlayerStealth playerStealth;

    [Header("Intensity settings (0..1)")]
    [Range(0f, 1f)] public float intensityDefault = 0.2f;
    [Range(0f, 1f)] public float intensityHidden = 0.3f;       // присед на свету / стоя в темноте
    [Range(0f, 1f)] public float intensityFullyHidden = 0.35f;   // присед в темноте

    [Header("Animation")]
    [Tooltip("Скорость сглаживания (больше = быстрее)")]
    public float fadeSpeed = 0.5f;

    [Header("Optional Vignette parameters")]
    [Range(0f, 1f)] public float smoothness = 0.5f;

    // internal
    private Vignette vignetteOverride;
    private VolumeProfile runtimeProfile;

    private void Awake()
    {
        // Try to auto-find playerStealth if not assigned
        if (playerStealth == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerStealth = p.GetComponent<PlayerStealth>();
        }

        // Try to auto-find volume if not assigned
        if (volume == null)
        {
            volume = FindObjectOfType<Volume>();
            if (volume == null)
            {
                Debug.LogWarning("[StealthVignetteURP] Volume not assigned and none found in scene. Create a Global Volume with a Profile that contains Vignette.");
                enabled = false;
                return;
            }
        }

        // Avoid modifying shared asset directly at runtime when possible:
        // if Volume has a shared profile asset, clone it to avoid permanent asset modification.
        if (Application.isPlaying && volume.sharedProfile != null)
        {
            runtimeProfile = Instantiate(volume.sharedProfile);
            volume.profile = runtimeProfile;
        }
        else
        {
            runtimeProfile = volume.profile;
        }

        if (runtimeProfile == null)
        {
            Debug.LogWarning("[StealthVignetteURP] Volume has no profile. Create one and add Vignette override in the Inspector.");
            enabled = false;
            return;
        }

        // Try to get Vignette override
        if (!runtimeProfile.TryGet<Vignette>(out vignetteOverride))
        {
            // If not present, add one (this modifies the profile)
            vignetteOverride = runtimeProfile.Add<Vignette>(true);
            // set sensible defaults
            vignetteOverride.intensity.overrideState = true;
            vignetteOverride.intensity.value = intensityDefault;
            vignetteOverride.smoothness.overrideState = true;
            vignetteOverride.smoothness.value = smoothness;
        }

        // Ensure overrideState is enabled so we can control it
        vignetteOverride.intensity.overrideState = true;
        vignetteOverride.smoothness.overrideState = true;

        // initialize to zero
        vignetteOverride.intensity.value = intensityDefault;
    }

    private void Update()
    {
        if (vignetteOverride == null || playerStealth == null) return;

        float target = 0f;
        int d = playerStealth.DarknessLevel;
        bool crouch = playerStealth.IsCrouching;

        // Logic per spec:
        if (d >= 2 && crouch)
            target = intensityFullyHidden;
        else if (d >= 1 || crouch)
            target = intensityHidden;
        else
            target = intensityDefault;

        // Smoothly interpolate intensity
        float current = vignetteOverride.intensity.value;
        float next = Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime);
        vignetteOverride.intensity.value = next;

        // Optionally update other vignette params (smoothness/roundness) smoothly if you want
        vignetteOverride.smoothness.value = Mathf.MoveTowards(vignetteOverride.smoothness.value, smoothness, fadeSpeed * 0.25f * Time.deltaTime);
        //vignetteOverride.roundness.value = Mathf.MoveTowards(vignetteOverride.roundness.value, roundness, fadeSpeed * 0.25f * Time.deltaTime);
    }

    private void OnDestroy()
    {
        // If we cloned a runtimeProfile, optionally destroy it to avoid leaking
#if UNITY_EDITOR
        // In editor, don't destroy instantiated asset to inspect it; in playmode destroy
        if (Application.isPlaying && runtimeProfile != null)
        {
            Destroy(runtimeProfile);
        }
#else
        if (runtimeProfile != null)
        {
            Destroy(runtimeProfile);
        }
#endif
    }

#if UNITY_EDITOR
    // Обновляем shared профиль сразу при правке в инспекторе
    void OnValidate()
    {
        if (Application.isPlaying) return;
        ResetSharedVignetteToDefault();
    }

    void ResetSharedVignetteToDefault()
    {
        if (volume == null) volume = FindObjectOfType<Volume>();
        if (volume == null) return;
        var prof = volume.sharedProfile ?? volume.profile;
        if (prof == null) return;
        if (prof.TryGet<Vignette>(out var v))
        {
            v.intensity.overrideState = true;
            v.intensity.value = intensityDefault;
            v.smoothness.overrideState = true;
            v.smoothness.value = smoothness;
            EditorUtility.SetDirty(prof);
        }
    }
#endif
}
