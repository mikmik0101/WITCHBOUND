using UnityEngine;
using UnityEditor;

public class CreateSurvivorData
{
    [MenuItem("WITCHBOUND/Create Survivor Data")]
    public static void CreateSurvivorDataAssets()
    {
        // Luis Padilla - The Analyst
        var ethan = ScriptableObject.CreateInstance<SurvivorData>();
        ethan.survivorName = "Luis Padilla";
        ethan.description = "Methodical researcher. High repair priority, moderate courage. Starts with Phase skill.";
        ethan.baseHealth = 100f;
        ethan.moveSpeed = 5f;
        ethan.courage = 0.6f;
        ethan.repairPriority = 0.8f;
        ethan.rescuePriority = 0.5f;
        
        AssetDatabase.CreateAsset(ethan, "Assets/ScriptableObjects/Survivor_Ethan.asset");
        
        // Mick Richards - The Protector
        var lucas = ScriptableObject.CreateInstance<SurvivorData>();
        lucas.survivorName = "Mick Richards";
        lucas.description = "Former security. High rescue priority, high courage. Starts with Emergency Weapon skill.";
        lucas.baseHealth = 120f;
        lucas.moveSpeed = 4.5f;
        lucas.courage = 0.8f;
        lucas.repairPriority = 0.4f;
        lucas.rescuePriority = 0.9f;
        
        AssetDatabase.CreateAsset(lucas, "Assets/ScriptableObjects/Survivor_Lucas.asset");
        
        // Josh Garcia - The Scout
        var adrian = ScriptableObject.CreateInstance<SurvivorData>();
        adrian.survivorName = "Josh Garcia";
        adrian.description = "Agile explorer. High exploration, moderate repair. Starts with Speed Burst skill.";
        adrian.baseHealth = 90f;
        adrian.moveSpeed = 6f;
        adrian.courage = 0.5f;
        adrian.repairPriority = 0.6f;
        adrian.rescuePriority = 0.6f;
        
        AssetDatabase.CreateAsset(adrian, "Assets/ScriptableObjects/Survivor_Adrian.asset");
        
        // Vincent Reid - The Ghost
        var noah = ScriptableObject.CreateInstance<SurvivorData>();
        noah.survivorName = "Vincent Reid";
        noah.description = "Stealth specialist. Low visibility to witches, high rescue. Starts with Invisibility skill.";
        noah.baseHealth = 85f;
        noah.moveSpeed = 5f;
        noah.courage = 0.7f;
        noah.repairPriority = 0.5f;
        noah.rescuePriority = 0.8f;
        
        AssetDatabase.CreateAsset(noah, "Assets/ScriptableObjects/Survivor_Noah.asset");
        
        AssetDatabase.SaveAssets();
        Debug.Log("All 4 SurvivorData assets created!");
    }
}
