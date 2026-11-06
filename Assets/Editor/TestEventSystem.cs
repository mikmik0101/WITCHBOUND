using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TestEventSystem
{
    [MenuItem("WITCHBOUND/Test EventSystem")]
    public static void RunTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game/MainGameScene.unity");
        var eventSystems = Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"[TEST] EventSystem count: {eventSystems.Length}");
        foreach (var es in eventSystems)
        {
            Debug.Log($"[TEST] EventSystem: {es.name}, enabled: {es.enabled}");
            var modules = es.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
            Debug.Log($"[TEST] Input modules: {modules.Length}");
            foreach (var m in modules)
            {
                Debug.Log($"[TEST]   Module: {m.GetType().Name}, enabled: {m.enabled}");
            }
        }
    }
}
