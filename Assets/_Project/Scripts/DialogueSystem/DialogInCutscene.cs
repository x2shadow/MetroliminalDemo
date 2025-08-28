using UnityEngine;

public class DialogInCutscene : MonoBehaviour
{
    public DialogueScript dialogueScript;   // Сценарий диалога
    public DialogueRunner dialogueRunner;   // Ссылка на DialogueRunner

    public PlayerController player;

    public void StartDialogue()
    {
        if (dialogueRunner != null && dialogueScript != null)
        {
            dialogueRunner.StartDialogue(dialogueScript, player, 0);
        }
        else
        {
            Debug.LogWarning("DialogueRunner или DialogueScript не назначены в инспекторе.");
        }
    }
}
