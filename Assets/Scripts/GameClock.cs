using UnityEngine;
using TMPro;

public class GameClock : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("How many real seconds pass per 10 in-game minutes")]
    [SerializeField] private float secondsPer10GameMinutes = 2f; // Change to 1f for faster days

    [Header("Clock Range")]
    private const int START_MINUTES = 9 * 60;   // 9:00 AM  = 540
    private const int END_MINUTES   = 17 * 60;  // 5:00 PM  = 1020

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI dayText;

    private float currentMinutes;   // current game time in minutes from midnight
    private float minutesPerSecond; // derived from secondsPer10GameMinutes
    private bool dayEnded = false;

    void Start()
    {
        minutesPerSecond = 10f / secondsPer10GameMinutes;

        // Restore clock from GameManager if mid-day (floor transition), otherwise fresh 9 AM
        if (GameManager.Instance != null && GameManager.Instance.savedClockMinutes >= 0f)
        {
            currentMinutes = GameManager.Instance.savedClockMinutes;
        }
        else
        {
            currentMinutes = START_MINUTES;
        }

        UpdateClockUI();
        UpdateDayUI();
    }

    void Update()
    {
        // Don't tick if game isn't active (but DO tick during dialogue — that's the punishment)
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.State;
        if (state != GameManager.GameState.Playing && state != GameManager.GameState.InDialogue) return;
        if (dayEnded) return;

        currentMinutes += minutesPerSecond * Time.deltaTime;

        // Keep GameManager in sync so floor transitions preserve the time
        GameManager.Instance.savedClockMinutes = currentMinutes;

        if (currentMinutes >= END_MINUTES)
        {
            currentMinutes = END_MINUTES;
            dayEnded = true;
            UpdateClockUI();
            GameManager.Instance.SetState(GameManager.GameState.Lost);
            return;
        }

        UpdateClockUI();
    }

    private void UpdateClockUI()
    {
        if (clockText == null) return;

        int totalMinutes = Mathf.FloorToInt(currentMinutes);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        string period = hours >= 12 ? "PM" : "AM";
        int displayHour = hours > 12 ? hours - 12 : hours;
        if (displayHour == 0) displayHour = 12;

        clockText.text = $"{displayHour}:{minutes:00} {period}";
    }

    private void UpdateDayUI()
    {
        if (dayText == null) return;

        int day = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;
        dayText.text = $"Day {day}";
    }

    // Read-only access for other systems if needed
    public float GetCurrentMinutes() => currentMinutes;
    public float GetProgress() => (currentMinutes - START_MINUTES) / (END_MINUTES - START_MINUTES);
}
