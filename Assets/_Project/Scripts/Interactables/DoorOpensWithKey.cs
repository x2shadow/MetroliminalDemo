using UnityEngine;

public class DoorOpensWithKey : MonoBehaviour, IInteractable
{
    [SerializeField] DialogueScript dialogueScript;

    public bool used = false;

    public void Interact(PlayerController player)
    {
        if (used) return;
        Debug.Log("Дверь использована!");

        if (player.hasKey)
        {
            GetComponent<Animation>().Play();
            used = true;    
        }
        else player.dialogueRunner.StartDialogue(dialogueScript, 0);
    }

    public bool GetUsed()
    {
        return used;
    }
}
