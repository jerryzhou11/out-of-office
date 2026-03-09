using UnityEngine; 
//https://www.youtube.com/watch?v=a1RFxtuTVsk
public class TutorialManager : MonoBehaviour
{
    public GameObject[] popUps;
    private int popUpIndex;
    public GameObject Spawner;

    void Update()
    {
        for (int i = 0; i < popUps.Length; i++)
        {
            if (i == popUpIndex)
            {
                popUps[popUpIndex].SetActive(true);
            }
            else
            {
                popUps[popUpIndex].SetActive(false);
            }

            if (popUpIndex == 0)
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
                {
                    popUpIndex++;
                }
            } else if (popUpIndex == 1)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    popUpIndex++;
                }
            }
        }
    }
}
