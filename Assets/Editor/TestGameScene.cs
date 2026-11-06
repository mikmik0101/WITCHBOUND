using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TestGameScene
{
    [MenuItem("WITCHBOUND/Test Game Scene")]
    public static void RunTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game/MainGameScene.unity");
        
        var gm = GameObject.Find("GameManager");
        var player = GameObject.Find("Player");
        var generator = GameObject.Find("Generator_01");
        var gate = GameObject.Find("EscapeGate");
        var witch = GameObject.Find("TestWitch_MaraVeyne");
        
        bool success = true;
        
        if (gm == null) { Debug.LogError("GameManager not found"); success = false; }
        else { Debug.Log("GameManager found"); }
        
        if (player == null) { Debug.LogError("Player not found"); success = false; }
        else { 
            Debug.Log("Player found"); 
            var pc = player.GetComponent<PlayerController>();
            if (pc == null) { Debug.LogError("PlayerController missing"); success = false; }
            else Debug.Log("PlayerController found");
        }
        
        if (generator == null) { Debug.LogError("Generator not found"); success = false; }
        else { 
            Debug.Log("Generator found"); 
            var gc = generator.GetComponent<GeneratorController>();
            if (gc == null) { Debug.LogError("GeneratorController missing"); success = false; }
            else Debug.Log("GeneratorController found");
        }
        
        if (gate == null) { Debug.LogError("EscapeGate not found"); success = false; }
        else { 
            Debug.Log("EscapeGate found"); 
            var eg = gate.GetComponent<EscapeGate>();
            if (eg == null) { Debug.LogError("EscapeGate component missing"); success = false; }
            else Debug.Log("EscapeGate component found");
        }
        
        if (witch == null) { Debug.LogError("TestWitch_MaraVeyne not found"); success = false; }
        else { 
            Debug.Log("TestWitch_MaraVeyne found"); 
            var wc = witch.GetComponent<WitchAI>();
            if (wc == null) { Debug.LogError("WitchAI missing"); success = false; }
            else { 
                Debug.Log("WitchAI found"); 
                if (wc.witchData != null)
                    Debug.Log($"WitchData assigned: {wc.witchData.witchName}");
                else
                    Debug.LogWarning("WitchData NOT assigned!");
            }
        }
        
        if (success)
            Debug.Log("=== ALL TESTS PASSED ===");
        else
            Debug.LogError("=== SOME TESTS FAILED ===");
    }
}
