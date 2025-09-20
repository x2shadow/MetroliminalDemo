using System.Collections;
using UnityEngine;

public class EntityFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityAI entity;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Footstep Settings")]
    [SerializeField] private AudioClip[] footstepsClips;
    [SerializeField] private float stepInterval = 0.5f;
    [SerializeField] private float pitchVariation = 0.075f;
    
    private Coroutine footstepsCoroutine;
    private bool isPlayingFootsteps = false;

    void Start()
    {
        if (entity == null)
            entity = GetComponent<EntityAI>();
            
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        bool shouldPlayFootsteps = entity.currentState == EntityAI.State.Patrol;// && entity.agent.velocity.magnitude > 0.1f;

        if (shouldPlayFootsteps && !isPlayingFootsteps)
        {
            StartFootsteps();
        }
        else if (!shouldPlayFootsteps && isPlayingFootsteps)
        {
            StopFootsteps();
        }
    }

    private void StartFootsteps()
    {
        if (footstepsCoroutine != null)
            StopCoroutine(footstepsCoroutine);
            
        footstepsCoroutine = StartCoroutine(PlayFootstepsRoutine());
        isPlayingFootsteps = true;
    }

    private void StopFootsteps()
    {
        if (footstepsCoroutine != null)
            StopCoroutine(footstepsCoroutine);
            
        isPlayingFootsteps = false;
    }

    private IEnumerator PlayFootstepsRoutine()
    {
        while (true)
        {
            if (footstepsClips.Length > 0 && audioSource != null)
            {
                PlayRandomFootstep();
            }
            
            yield return new WaitForSeconds(stepInterval);
        }
    }

    private void PlayRandomFootstep()
    {
        // Выбираем случайный звук шага
        AudioClip clip = footstepsClips[Random.Range(0, footstepsClips.Length)];
        
        // Устанавливаем случайный pitch в пределах ±pitchVariation%
        float randomPitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.pitch = randomPitch;

        // Проигрываем звук
        audioSource.volume = AudioManager.Instance.GetSoundVolume();
        audioSource.PlayOneShot(clip);
    }

    // Метод для настройки интервала шагов извне
    public void SetStepInterval(float newInterval)
    {
        stepInterval = newInterval;
        
        // Перезапускаем корутину, если она активна
        if (isPlayingFootsteps)
        {
            StopFootsteps();
            StartFootsteps();
        }
    }

    // Метод для настройки вариации pitch извне
    public void SetPitchVariation(float newVariation)
    {
        pitchVariation = Mathf.Clamp(newVariation, 0.8f, 1.2f);
    }

    void OnDisable()
    {
        StopFootsteps();
    }
}