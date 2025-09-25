using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public int dialogueIndex = 0;
    public DialogueScript dialogueScript;   // Сценарий диалога
    public DialogueRunner dialogueRunner;   // Ссылка на DialogueRunner

    private bool hasTriggered = false;      // Чтобы не запускать повторно

    PlayerController player;

    void Awake()
    {
        if (dialogueRunner == null) dialogueRunner = GameObject.FindObjectOfType<DialogueRunner>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
            hasTriggered = true;
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        if (dialogueRunner != null && dialogueScript != null)
        {
            dialogueRunner.StartDialogue(dialogueScript, player, dialogueIndex);
        }
        else
        {
            Debug.LogWarning("DialogueRunner или DialogueScript не назначены в инспекторе.");
        }
    }
}
