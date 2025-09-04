using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSignal : MonoBehaviour
{
    [SerializeField] Animation animationSignal;

    public void Play()
    {
        animationSignal.Play();
    }
}
