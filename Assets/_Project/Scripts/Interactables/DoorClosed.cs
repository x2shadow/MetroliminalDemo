using UnityEngine;

public class DoorClosed : MonoBehaviour, IInteractable
{
    [SerializeField] DialogueScript dialogueScript;

    public bool used = false;

    public void Interact(PlayerController player)
    {
        if (used) return;
        Debug.Log("Дверь использована!");
        player.dialogueRunner.StartDialogue(dialogueScript, 0);
        used = true;
    }

    public bool GetUsed()
    {
        return used;
    }
}
