using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TestClickDiag
{
    [MenuItem("WITCHBOUND/Test Click Diag")]
    public static void RunTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game/MainGameScene.unity");
        var canvas = GameObject.Find("UI_Canvas");
        if (canvas != null)
        {
            var buttons = canvas.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach (var btn in buttons)
            {
                var diag = btn.GetComponent<ClickDiagnostics>();
                if (diag != null)
                    Debug.Log($"ClickDiagnostics found on {btn.name}");
                else
                    Debug.LogError($"ClickDiagnostics MISSING on {btn.name}");
            }
        }
    }
}
