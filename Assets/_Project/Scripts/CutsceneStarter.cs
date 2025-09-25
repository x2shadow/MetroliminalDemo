using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneStarter : MonoBehaviour
{
    [SerializeField] PlayableDirector playableDirector;

    private bool hasTriggered = false;      // Чтобы не запускать повторно

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        playableDirector.Play(); 
        hasTriggered = true;
    }
}
