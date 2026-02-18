using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Day Tracking")]
    public int currentDay = 1;

    [Header("Floor Progression")]
    [Tooltip("Scene names in order. Must match Build Settings exactly.")]
    [SerializeField] private string[] floorScenes = new string[] { "Floor1", "Floor2" };
    public int currentFloor = 0; // index into floorScenes

    [Header("Clock Persistence")]
    public float savedClockMinutes = -1f; // -1 means "start fresh at 9 AM"

    public enum GameState { Playing, Paused, Won, Lost, InDialogue }
    public GameState State { get; private set; } = GameState.Playing;

    // Panels register themselves via RegisterPanels() on Start
    private GameObject pauseMenuPanel;
    private GameObject gameOverPanel;
    private GameObject winPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Clear stale panel references - panels will re-register themselves via RegisterPanels()
        pauseMenuPanel = null;
        gameOverPanel  = null;
        winPanel       = null;

        // Reset to playing - panels will register shortly after in their own Start()
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    // Called by each panel's UIPanel component on Start()
    public void RegisterPanels(GameObject pause, GameObject gameOver, GameObject win)
    {
        if (pause    != null) pauseMenuPanel = pause;
        if (gameOver != null) gameOverPanel  = gameOver;
        if (win      != null) winPanel       = win;
    }

    public void SetState(GameState newState)
    {
        State = newState;

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                SetPanelActive(pauseMenuPanel, false);
                SetPanelActive(gameOverPanel,  false);
                SetPanelActive(winPanel,       false);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                SetPanelActive(pauseMenuPanel, true);
                break;

            case GameState.Lost:
                Time.timeScale = 0f;
                SetPanelActive(gameOverPanel, true);
                break;

            case GameState.Won:
                Time.timeScale = 0f;
                SetPanelActive(winPanel, true);
                break;

            case GameState.InDialogue:
                // Time keeps running — dialogue wastes your precious time
                Time.timeScale = 1f;
                break;
        }
    }

    // ---- Button Callbacks ----

    public void Resume()
    {
        SetState(GameState.Playing);
    }

    /// <summary>
    /// Pause menu: give up, start fresh tomorrow from floor 1.
    /// </summary>
    public void ClockOutEarly()
    {
        currentDay++;
        currentFloor = 0;
        savedClockMinutes = -1f; // new day, fresh clock
        LoadFloor(currentFloor);
    }

    /// <summary>
    /// Game-over panel: time ran out, start fresh tomorrow from floor 1.
    /// </summary>
    public void NextDay()
    {
        currentDay++;
        currentFloor = 0;
        savedClockMinutes = -1f; // new day, fresh clock
        LoadFloor(currentFloor);
    }

    /// <summary>
    /// Staircase / win panel: advance to the next floor. Clock keeps running.
    /// </summary>
    public void NextFloor()
    {
        currentFloor++;

        if (currentFloor >= floorScenes.Length)
        {
            // Beaten every floor — for now, stay on the last floor
            // TODO: Replace with a proper ending / credits scene
            Debug.Log("You've cleared every floor! Game complete.");
            currentFloor = floorScenes.Length - 1;
        }

        // savedClockMinutes is already set by GameClock before scene transition
        LoadFloor(currentFloor);
    }

    private void LoadFloor(int floorIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(floorScenes[floorIndex]);
    }

    public bool IsLastFloor()
    {
        return currentFloor >= floorScenes.Length - 1;
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
