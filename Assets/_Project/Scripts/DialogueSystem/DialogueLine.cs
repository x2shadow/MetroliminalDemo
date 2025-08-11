using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public enum Speaker { Player, OldOne, Note }
    public Speaker speaker;
    [TextArea] public string text;
    public float duration = 3f;
}
