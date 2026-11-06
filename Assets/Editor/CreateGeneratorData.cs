using UnityEngine;
using UnityEditor;

public class CreateGeneratorData
{
    [MenuItem("WITCHBOUND/Create Generator Data")]
    public static void CreateGeneratorDataAsset()
    {
        var gen = ScriptableObject.CreateInstance<GeneratorData>();
        gen.generatorId = "Gen_Standard";
        gen.repairTime = 60f;
        
        AssetDatabase.CreateAsset(gen, "Assets/ScriptableObjects/Generator_Standard.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("GeneratorData created!");
    }
}
