using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class TestButtonListeners
{
    [MenuItem("WITCHBOUND/Test Button Listeners")]
    public static void RunTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game/MainGameScene.unity");
        var canvas = GameObject.Find("UI_Canvas");
        if (canvas != null)
        {
            var buttons = canvas.GetComponentsInChildren<Button>();
            foreach (var btn in buttons)
            {
                Debug.Log($"[TEST] Button: {btn.name}, onClick listener count: {btn.onClick.GetPersistentEventCount()}");
                // Also check if there are any runtime listeners
                // We can't easily check non-persistent listeners, but we can check if the button has a ClickDiagnostics
                var diag = btn.GetComponent<ClickDiagnostics>();
                Debug.Log($"[TEST] ClickDiagnostics on {btn.name}: {(diag != null ? "YES" : "NO")}");
            }
        }
    }
}
