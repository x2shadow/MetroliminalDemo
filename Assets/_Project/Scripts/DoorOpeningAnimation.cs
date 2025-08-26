using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpeningAnimation : MonoBehaviour
{
    [SerializeField] Animation trainDoorOpeningAnimation;

    public void Play()
    {
        trainDoorOpeningAnimation.Play();
    }
}
