using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueScript", menuName = "Dialogue/Script")]
public class DialogueScript : ScriptableObject
{
    public DialogueLine[] lines;
}
