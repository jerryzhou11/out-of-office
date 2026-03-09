using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu]
public class DialogueAsset : ScriptableObject
{
    [TextArea]
    public string[] dialogue;
}
