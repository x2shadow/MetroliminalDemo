using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CH1_TrainDoorsTrigger : MonoBehaviour
{
    private bool hasTriggered = false;      // Чтобы не запускать повторно

    [SerializeField] Animation trainDoorClosingAnimation;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            trainDoorClosingAnimation.Play("DoorClosing");
        }
    }
}
