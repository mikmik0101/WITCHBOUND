using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AI;

public class BakeNavMesh
{
    [MenuItem("WITCHBOUND/Bake NavMesh")]
    public static void BakeNavigationMesh()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game/MainGameScene.unity");
        
        NavMeshBuilder.BuildNavMesh();
        
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("NavMesh baked successfully!");
    }
}
