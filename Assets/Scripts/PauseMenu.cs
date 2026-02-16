using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    void Update()
    {
        // Only listen for escape when playing or paused
        if (GameManager.Instance == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (GameManager.Instance.State == GameManager.GameState.Playing)
            {
                GameManager.Instance.SetState(GameManager.GameState.Paused);
            }
            else if (GameManager.Instance.State == GameManager.GameState.Paused)
            {
                GameManager.Instance.Resume();
            }
        }
    }

    // Button callbacks - wire these up in the Inspector
    public void OnResumePressed()
    {
        GameManager.Instance.Resume();
    }

    public void OnClockOutEarlyPressed()
    {
        GameManager.Instance.ClockOutEarly();
    }
}