using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keys : MonoBehaviour, IInteractable
{
    bool used = false;

    public void Interact(PlayerController player)
    {
        if (used) return;
        Debug.Log("Ключи подняты");
        player.hasKey = true;
        Destroy(gameObject);
        used = true;
    }

    public bool GetUsed() => used;
}
