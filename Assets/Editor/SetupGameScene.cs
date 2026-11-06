using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupGameScene
{
    [MenuItem("WITCHBOUND/Setup Game Scene")]
    public static void CreateGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Load data assets
        var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
        var testWitchData = AssetDatabase.LoadAssetAtPath<WitchData>("Assets/ScriptableObjects/Witch_MaraVeyne.asset");
        var testSkillData = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/ScriptableObjects/Skill_Phase.asset");
        var generatorData = AssetDatabase.LoadAssetAtPath<GeneratorData>("Assets/ScriptableObjects/Generator_Standard.asset");
        
        // Add SceneSetup component to a temporary object
        var setupObj = new GameObject("SceneSetup");
        var setup = setupObj.AddComponent<SceneSetup>();
        setup.gameConfig = gameConfig;
        setup.testWitchData = testWitchData;
        setup.testSkillData = testSkillData;
        setup.generatorData = generatorData;
        
        // Run setup
        setup.SetupScene();
        
        // Remove setup object
        Object.DestroyImmediate(setupObj);
        
        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game/MainGameScene.unity");
        
        Debug.Log("Game scene created successfully!");
    }
}
