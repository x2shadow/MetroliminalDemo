using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class KeysCutscene : MonoBehaviour, IInteractable
{
    [SerializeField] PlayableDirector playableDirector;

    bool used = false;

    public void Interact(PlayerController player)
    {
        if (used) return;
        Debug.Log("Ключи подняты");
        player.hasKey = true;
        playableDirector.Play();
        Destroy(gameObject);
        used = true;
    }

    public bool GetUsed() => used;
}
