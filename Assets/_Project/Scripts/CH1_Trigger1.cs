using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CH1_Trigger1 : MonoBehaviour
{
    private bool hasTriggered = false;      // Чтобы не запускать повторно

    [SerializeField] GameObject secondTunnelLight;
    [SerializeField] Animation trainDoorClosingAnimation;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            secondTunnelLight.SetActive(true);
            trainDoorClosingAnimation.Play("DoorClosing");
        }
    }
}
