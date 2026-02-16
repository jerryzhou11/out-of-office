using UnityEngine;

public class UIPanelRegistry : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPanels(pauseMenuPanel, gameOverPanel, winPanel);
        }
        else
        {
            Debug.LogError("UIPanelRegistry: No GameManager found in scene!");
        }
    }
}