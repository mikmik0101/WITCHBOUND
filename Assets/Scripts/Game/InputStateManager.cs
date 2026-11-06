using UnityEngine;

public enum InputMode
{
    Menu,
    Gameplay,
    Pause
}

public class InputStateManager : MonoBehaviour
{
    public static InputStateManager Instance { get; private set; }
    
    public InputMode currentMode = InputMode.Menu;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        SetMode(InputMode.Menu);
    }
    
    public void SetMode(InputMode mode)
    {
        currentMode = mode;
        
        switch (mode)
        {
            case InputMode.Menu:
            case InputMode.Pause:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case InputMode.Gameplay:
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }
    
    public bool IsMenuMode()
    {
        return currentMode == InputMode.Menu || currentMode == InputMode.Pause;
    }
    
    public bool IsGameplayMode()
    {
        return currentMode == InputMode.Gameplay;
    }
}
