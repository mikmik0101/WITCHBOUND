using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-shot scene surgery for the 60-second chase redesign.
///
/// The saved scene still contains the old generator-based setup (10 generators,
/// the escape gate, one pre-placed witch and three AI teammates) and a UI canvas
/// built from the old layout. This removes the obsolete gameplay objects and
/// rebuilds the canvas from UIBuilder so the new menu/HUD/library exist as real
/// scene objects.
///
/// Run: WITCHBOUND/Apply Chase Mode Scene  (or -executeMethod ChaseModeSetup.Apply)
/// </summary>
public static class ChaseModeSetup
{
    private const string ScenePath = "Assets/Scenes/Game/MainGameScene.unity";

    [MenuItem("WITCHBOUND/Apply Chase Mode Scene")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ---- 1. remove obsolete gameplay objects -------------------------
        var all = Object.FindObjectsOfType<GameObject>(true);
        int removed = 0;
        foreach (var go in all)
        {
            if (go == null) continue;
            string n = go.name;

            bool obsolete =
                n.StartsWith("Generator_") ||      // 10 generator props
                n == "EscapeGate" ||               // generator-completion exit
                n.StartsWith("AI_Survivor_") ||    // chase mode is single player
                n.StartsWith("TestWitch_");        // spawner now creates all six

            if (!obsolete) continue;

            Object.DestroyImmediate(go);
            removed++;
        }
        Debug.Log("[CHASE SETUP] removed " + removed + " obsolete objects");

        // ---- 2. rebuild the UI canvas ------------------------------------
        var oldCanvas = GameObject.Find("UI_Canvas");
        if (oldCanvas != null)
        {
            Object.DestroyImmediate(oldCanvas);
            Debug.Log("[CHASE SETUP] old UI_Canvas removed");
        }

        var canvas = UIBuilder.BuildCompleteUI();
        var uiSetup = Object.FindObjectOfType<UISetup>();
        if (uiSetup != null) canvas.transform.SetParent(uiSetup.transform, false);

        // ---- 3. library controller ---------------------------------------
        if (Object.FindObjectOfType<CharacterLibraryUI>() == null)
        {
            var libObj = new GameObject("CharacterLibraryUI");
            libObj.AddComponent<CharacterLibraryUI>();
        }

        // ---- 4. character select controller -------------------------------
        if (Object.FindObjectOfType<CharacterSelectUI>() == null)
        {
            var csObj = new GameObject("CharacterSelectUI");
            csObj.AddComponent<CharacterSelectUI>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        int gens = Object.FindObjectsOfType<GeneratorController>(true).Length;
        int gates = Object.FindObjectsOfType<EscapeGate>(true).Length;
        int witches = Object.FindObjectsOfType<WitchAI>(true).Length;
        int survivors = Object.FindObjectsOfType<SurvivorAI>(true).Length;
        Debug.Log($"[CHASE SETUP] saved. generators={gens} gates={gates} preplacedWitches={witches} aiSurvivors={survivors} " +
                  $"uiCanvas={(GameObject.Find("UI_Canvas") != null)} libraryButton={(GameObject.Find("LibraryButton") != null)}");
    }
}
