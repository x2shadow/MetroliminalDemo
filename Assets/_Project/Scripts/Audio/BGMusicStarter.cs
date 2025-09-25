using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMusicStarter : MonoBehaviour
{
    [SerializeField] string musicName;
    
    void Start()
    {
        AudioManager.Instance.PlayMusic(musicName);
    }
}
