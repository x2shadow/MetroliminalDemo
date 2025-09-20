using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityChaseSound : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityAI entity;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chaseClip;
    
    private Coroutine chaseSoundCoroutine;
    private bool isPlayingChaseSound = false;

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
        HandleChaseSound();
    }

    private void HandleChaseSound()
    {
        bool shouldPlayFootsteps = entity.currentState == EntityAI.State.Chase;// && entity.agent.velocity.magnitude > 0.1f;

        if (shouldPlayFootsteps && !isPlayingChaseSound)
        {
            StartChaseSound();
        }
        else if (!shouldPlayFootsteps && isPlayingChaseSound)
        {
            StopChaseSound();
        }
    }

    private void StartChaseSound()
    {
        if (chaseSoundCoroutine != null)
            StopCoroutine(chaseSoundCoroutine);
            
        // Проигрываем звук
        audioSource.volume = AudioManager.Instance.GetSoundVolume();
        audioSource.PlayOneShot(chaseClip);
        isPlayingChaseSound = true;
    }

    private void StopChaseSound()
    {
        if (chaseSoundCoroutine != null)
            StopCoroutine(chaseSoundCoroutine);
            
        isPlayingChaseSound = false;
    }

    void OnDisable()
    {
        StopChaseSound();
    }
}
