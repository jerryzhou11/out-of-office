using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private string firstFloorScene = "Floor1";

    public void OnPlayPressed()
    {
        // If GameManager exists (returning from a previous game), reset it
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            SceneManager.LoadScene(firstFloorScene);
        }
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }
}
