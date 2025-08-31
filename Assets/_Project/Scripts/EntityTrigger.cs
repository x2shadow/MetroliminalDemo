using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityTrigger : MonoBehaviour
{
    public GameObject firstEntity;
    public GameObject secondEntity;
    public float pause = 1f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            firstEntity.SetActive(false);
            StartCoroutine(waitPause());
        }
    }

    IEnumerator waitPause()
    {
        yield return new WaitForSeconds(pause);
        secondEntity.SetActive(true);
        gameObject.SetActive(false);
    }
}
