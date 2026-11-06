using UnityEngine;

/// <summary>
/// The single authoritative ESC/pause handler.
///
/// Runs from an always-active GameObject and reads legacy input directly, so it
/// works even when Time.timeScale == 0 and does not depend on the EventSystem,
/// button clicks, cursor state, or any UI hover. Update() keeps running while
/// the game is paused, so ESC always toggles pause/resume.
/// </summary>
public class PauseController : MonoBehaviour
{
    public static PauseController Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (IsPaused)
        {
            Resume();
        }
        else if (gm.currentState == GameState.Gameplay || gm.currentState == GameState.EscapePhase)
        {
            Pause();
        }
        // In menus/results ESC is intentionally ignored.
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (InputStateManager.Instance != null) InputStateManager.Instance.SetMode(InputMode.Pause);
        if (UIManager.Instance != null && UIManager.Instance.pauseMenuPanel != null)
            UIManager.Instance.pauseMenuPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // The forward-run ramp belongs to one continuous run: pausing ends it,
        // so resuming never continues a half-finished +1 step.
        var gm2 = GameManager.Instance;
        if (gm2 != null && gm2.player != null) gm2.player.ResetAcceleration();

        Debug.Log("[PAUSE] Paused (timeScale=0, cursor unlocked)");
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (UIManager.Instance != null && UIManager.Instance.pauseMenuPanel != null)
            UIManager.Instance.pauseMenuPanel.SetActive(false);
        if (InputStateManager.Instance != null) InputStateManager.Instance.SetMode(InputMode.Gameplay);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Debug.Log("[PAUSE] Resumed (timeScale=1, cursor locked)");
    }

    /// <summary>
    /// Clears the pause flag without relying on the ESC toggle. Used when a new
    /// session starts from the pause menu's Restart button, so the next ESC press
    /// pauses instead of "resuming" a game that is already running.
    /// </summary>
    public void ForceResume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (UIManager.Instance != null && UIManager.Instance.pauseMenuPanel != null)
            UIManager.Instance.pauseMenuPanel.SetActive(false);
        if (InputStateManager.Instance != null) InputStateManager.Instance.SetMode(InputMode.Gameplay);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
