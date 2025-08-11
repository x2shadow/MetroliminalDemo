using UnityEngine;
using TMPro;

public class DialogueScriptUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text text;

    public void Show(string message)
    {
        text.text = message;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
