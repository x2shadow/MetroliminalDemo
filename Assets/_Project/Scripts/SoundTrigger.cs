using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTrigger : MonoBehaviour
{
    [SerializeField] string soundName;

    private bool hasTriggered = false;      // Чтобы не запускать повторно

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        AudioManager.Instance.PlaySound(soundName);
        hasTriggered = true;
    }
}
