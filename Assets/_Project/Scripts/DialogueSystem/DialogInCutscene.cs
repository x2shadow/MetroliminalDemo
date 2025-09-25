using UnityEngine;

public class DialogInCutscene : MonoBehaviour
{
    public DialogueScript dialogueScript1;   // Сценарий диалога
    public DialogueScript dialogueScript2;
    public DialogueScript dialogueScript3;
    public DialogueScript dialogueScript4;
    public DialogueScript dialogueScript5;

    DialogueRunner dialogueRunner;   // Ссылка на DialogueRunner
    PlayerController player;

    void Awake()
    {
        if (dialogueRunner == null) dialogueRunner = GameObject.FindObjectOfType<DialogueRunner>();
        if (player == null) player = GameObject.Find("Player").GetComponent<PlayerController>();
    }

    public void StartDialogue1() => StartDialogue(dialogueScript1);
    public void StartDialogue2() => StartDialogue(dialogueScript2);
    public void StartDialogue3() => StartDialogue(dialogueScript3);
    public void StartDialogue4() => StartDialogue(dialogueScript4);
    public void StartDialogue5() => StartDialogue(dialogueScript5);
    
    void StartDialogue(DialogueScript dialogueScript)
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
