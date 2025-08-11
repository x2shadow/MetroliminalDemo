using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueRunner : MonoBehaviour
{
    public DialogueScriptUI playerUI;

    public PlayerController player;

    [Header("Другой персонаж")]
    public DialogueScriptUI otherOneUI;
    public AudioSource audioSource;
    public AudioClip otherVoiceClip;
    
    bool skipPressed = false;

    public void StartDialogue(DialogueScript script, PlayerController player, int index)
    {
        player.isDialogueActive = true;
        // Подписываемся на событие Skip
        player.inputActions.Player.Click.performed += OnClick;
        StartCoroutine(RunDialogue(script, player, index));
    }

    public void StartDialogue(DialogueScript script, int index)
    {
        player.isDialogueActive = true;
        // Подписываемся на событие Skip
        player.inputActions.Player.Click.performed += OnClick;
        StartCoroutine(RunDialogue(script, player, index));
    }

    public IEnumerator StartDialogueCoroutine(DialogueScript script, int index)
    {
        player.isDialogueActive = true;
        player.inputActions.Player.Click.performed += OnClick;
        yield return RunDialogue(script, player, index);
    }

    private IEnumerator RunDialogue(DialogueScript script, PlayerController player, int index)
    {
        foreach (var line in script.lines)
        {
            if (line.speaker == DialogueLine.Speaker.Player)
                playerUI.Show(line.text);
            else
                otherOneUI.Show(line.text);

            yield return new WaitForSeconds(line.duration);

            playerUI.Hide();
            //otherOneUI.Hide();
        }

        //player.EndDialogue(index);
    }

    private IEnumerator RunDialogue2(DialogueScript script, PlayerController player, int index)
    {
        foreach (var line in script.lines)
        {
            // Сброс флага перед каждой репликой
            skipPressed = false;

            if (line.speaker == DialogueLine.Speaker.Player)
                playerUI.Show(line.text);
            else
            {
                otherOneUI.Show(line.text);
                audioSource.Play();
            }

            // Вместо ожидания по времени ждём нажатия ЛКМ (Skip)
            yield return new WaitUntil(() => skipPressed);
            
            playerUI.Hide();
            //otherOneUI.Hide();
        }

        // Отписываемся от события Click после завершения диалога
        player.inputActions.Player.Click.performed -= OnClick;

        //player.EndDialogue(index);
    }


    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed)
            skipPressed = true;
    }

}
