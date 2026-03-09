using UnityEngine;

public class tutorial : MonoBehaviour
{
    [SerializeField] bool firstInteraction = true;
    [SerializeField] int progress;

    public DialogueAsset dialogueAsset;

    [HideInInspector]
    public int StartTutorial
    {
        get
        {
            if (firstInteraction)
            {
                firstInteraction = false;
                return 0;
            }
            else
            {
                return progress;
            }
        }
    }
}
