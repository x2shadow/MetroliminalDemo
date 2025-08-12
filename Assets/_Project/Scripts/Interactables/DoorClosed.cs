using UnityEngine;

public class DoorClosed : MonoBehaviour, IInteractable
{
    [SerializeField] DialogueScript dialogueScript;

    public void Interact(PlayerController player)
    {
        Debug.Log("Дверь использована!");
        player.dialogueRunner.StartDialogue(dialogueScript, 0);
    }
}
