using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public GameObject playerPanel;
    public TMP_Text playerText;

    public GameObject brotherPanel;
    public TMP_Text brotherText;

    public float dialogueDuration = 3f;

    public void ShowPlayerDialogue(string text)
    {
        StartCoroutine(ShowDialogue(playerPanel, playerText, text));
    }

    public void ShowBrotherDialogue(string text)
    {
        StartCoroutine(ShowDialogue(brotherPanel, brotherText, text));
    }

    private IEnumerator ShowDialogue(GameObject panel, TMP_Text textComponent, string text)
    {
        panel.SetActive(true);
        textComponent.text = text;
        yield return new WaitForSeconds(dialogueDuration);
        panel.SetActive(false);
    }
}
