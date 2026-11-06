using UnityEngine;

public class UISetup : MonoBehaviour
{
    [Header("Auto-setup")]
    public bool setupOnStart = true;
    
    private void Awake()
    {
        if (setupOnStart)
        {
            SetupUI();
        }
    }
    
    [ContextMenu("Setup UI")]
    public void SetupUI()
    {
        var uiCanvas = UIBuilder.BuildCompleteUI();
        uiCanvas.transform.SetParent(transform);
        Debug.Log("UI setup complete!");
    }
}
