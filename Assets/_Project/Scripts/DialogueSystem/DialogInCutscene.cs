using UnityEngine;

public class DialogInCutscene : MonoBehaviour
{
    public DialogueScript dialogueScript;   // Сценарий диалога

    DialogueRunner dialogueRunner;   // Ссылка на DialogueRunner
    PlayerController player;

    void Awake()
    {
        if (dialogueRunner == null) dialogueRunner = GameObject.FindObjectOfType<DialogueRunner>();
        if (player == null) player = GameObject.Find("Player").GetComponent<PlayerController>();
    }

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
