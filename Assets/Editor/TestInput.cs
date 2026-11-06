using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TestInput
{
    [MenuItem("WITCHBOUND/Test Input")]
    public static void RunTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game/MainGameScene.unity");
        var inputManager = GameObject.Find("InputStateManager");
        if (inputManager != null)
        {
            Debug.Log("InputStateManager found!");
            var comp = inputManager.GetComponent<InputStateManager>();
            if (comp != null)
                Debug.Log($"InputMode: {comp.currentMode}");
        }
        else
        {
            Debug.LogError("InputStateManager NOT found!");
        }
    }
}
