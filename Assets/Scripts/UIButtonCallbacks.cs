using UnityEngine;

/// <summary>
/// Routes UI button presses through GameManager.Instance (the singleton)
/// so they survive scene reloads. Wire panel buttons to this script
/// instead of directly to the GameManager GameObject.
/// </summary>
public class UIButtonCallbacks : MonoBehaviour
{
    // Win panel
    public void OnNextFloorPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.NextFloor();
    }

    // Game-over / day-end panel
    public void OnNextDayPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.NextDay();
    }

    // Pause menu
    public void OnResumePressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();
    }

    public void OnClockOutEarlyPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ClockOutEarly();
    }

    // Return to start menu
    public void OnReturnToMenuPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
    }
}
