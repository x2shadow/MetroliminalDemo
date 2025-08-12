using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTrigger : MonoBehaviour
{
    [SerializeField] string soundName;
    
    void OnTriggerEnter(Collider other)
    {
        AudioManager.Instance.PlaySound(soundName);
    }
}
