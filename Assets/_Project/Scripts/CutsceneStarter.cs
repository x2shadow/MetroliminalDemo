using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneStarter : MonoBehaviour
{
    [SerializeField] PlayableDirector playableDirector;

    void OnTriggerEnter(Collider other)
    {
        playableDirector.Play();    
    }
}
