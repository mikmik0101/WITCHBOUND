using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TestPlayMode
{
    [MenuItem("WITCHBOUND/Test Play Mode")]
    public static void RunPlayModeTest()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game/MainGameScene.unity");
        
        EditorApplication.isPlaying = true;
        
        // Run a few frames
        for (int i = 0; i < 10; i++)
        {
            EditorApplication.Step();
        }
        
        EditorApplication.isPlaying = false;
        
        Debug.Log("Play mode test completed successfully!");
    }
}
