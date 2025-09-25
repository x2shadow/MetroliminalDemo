using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CH1_TriggerDelayLights : MonoBehaviour
{
    private bool hasTriggered = false;      // Чтобы не запускать повторно

    [SerializeField] GameObject secondTunnelLight;
    [SerializeField] float delay = 1f;    // Задержка между включением света

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            secondTunnelLight.SetActive(true);
            StartCoroutine(ActivateLightsSequentially());
        }
    }

    IEnumerator ActivateLightsSequentially()
    {
        // Получаем все SpotLight компоненты из дочерних объектов
        Light[] spotLights = secondTunnelLight.GetComponentsInChildren<Light>();
        
        // Сначала деактивируем все спотлайты
        //foreach (Light spotLight in spotLights)
        //{
        //    spotLight.enabled = false;
        //}
        
        // Включаем спотлайты от последнего к первому с задержкой
        for (int i = spotLights.Length - 1; i >= 0; i--)
        {
            spotLights[i].enabled = true;
            yield return new WaitForSeconds(delay);
        }
    }

}

