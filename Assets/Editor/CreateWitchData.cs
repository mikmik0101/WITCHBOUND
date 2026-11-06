using UnityEngine;
using UnityEditor;

public class CreateWitchData
{
    [MenuItem("WITCHBOUND/Create Witch Data")]
    public static void CreateWitchDataAssets()
    {
        // Create Rona Tanyel - The Hollow Witch
        var mara = ScriptableObject.CreateInstance<WitchData>();
        mara.witchName = "Rona Tanyel";
        mara.description = "The Hollow Witch - Introductory hunter. Humanoid, ground-based, strong visual detection, moderate hearing, standard chase and attack.";
        mara.moveSpeed = 3f;
        mara.chaseSpeed = 7f;
        mara.detectionRange = 25f;
        mara.fieldOfView = 100f;
        mara.hearingRange = 18f;
        mara.attackDamage = 25f;
        mara.attackCooldown = 2.5f;
        mara.witchType = WitchType.Humanoid;
        mara.spawnThreshold = 0; // Spawns from the start
        
        AssetDatabase.CreateAsset(mara, "Assets/ScriptableObjects/Witch_MaraVeyne.asset");
        
        // Create Demonisa Belle - The Winged Witch
        var seraphine = ScriptableObject.CreateInstance<WitchData>();
        seraphine.witchName = "Demonisa Belle";
        seraphine.description = "The Winged Witch - Flying hunter. No legs, approaches from above, ignores ground obstacles, creates wind effects.";
        seraphine.moveSpeed = 5f; // Flying speed
        seraphine.chaseSpeed = 12f;
        seraphine.detectionRange = 35f;
        seraphine.fieldOfView = 120f;
        seraphine.hearingRange = 12f; // Less hearing, more visual
        seraphine.attackDamage = 30f;
        seraphine.attackCooldown = 3f;
        seraphine.witchType = WitchType.Winged;
        seraphine.spawnThreshold = 2; // Spawns at 2 generators
        
        AssetDatabase.CreateAsset(seraphine, "Assets/ScriptableObjects/Witch_Seraphine.asset");
        
        // Create Kyla Ni-Garcia - The Hound
        var morrow = ScriptableObject.CreateInstance<WitchData>();
        morrow.witchName = "Kyla Ni-Garcia";
        morrow.description = "The Hound - Supernatural beast. Very strong hearing, fast, tracks sound and injured survivors, howls to alert others.";
        morrow.moveSpeed = 6f;
        morrow.chaseSpeed = 14f;
        morrow.detectionRange = 15f; // Poor vision
        morrow.fieldOfView = 70f;
        morrow.hearingRange = 35f; // Exceptional hearing
        morrow.attackDamage = 35f;
        morrow.attackCooldown = 1.5f;
        morrow.witchType = WitchType.Hound;
        morrow.spawnThreshold = 4; // Spawns at 4 generators
        
        AssetDatabase.CreateAsset(morrow, "Assets/ScriptableObjects/Witch_Morrow.asset");
        
        // Create Jannak - The Crawling Witch
        var vespera = ScriptableObject.CreateInstance<WitchData>();
        vespera.witchName = "Jannak";
        vespera.description = "The Crawling Witch - Low-to-ground ambush predator. Fast bursts, crawls through tight spaces, excels at surprise attacks.";
        vespera.moveSpeed = 4f;
        vespera.chaseSpeed = 10f; // Burst speed
        vespera.detectionRange = 18f;
        vespera.fieldOfView = 80f;
        vespera.hearingRange = 22f;
        vespera.attackDamage = 28f;
        vespera.attackCooldown = 2f;
        vespera.witchType = WitchType.Crawler;
        vespera.spawnThreshold = 6; // Spawns at 6 generators
        
        AssetDatabase.CreateAsset(vespera, "Assets/ScriptableObjects/Witch_Vespera.asset");
        
        // Create Sandy Vill - The Tall Witch
        var motherGrin = ScriptableObject.CreateInstance<WitchData>();
        motherGrin.witchName = "Sandy Vill";
        motherGrin.description = "The Tall Witch - Towering presence. Slow but powerful, long detection range, blocks routes, creates psychological pressure.";
        motherGrin.moveSpeed = 2f;
        motherGrin.chaseSpeed = 5f;
        motherGrin.detectionRange = 40f; // Very long range
        motherGrin.fieldOfView = 110f;
        motherGrin.hearingRange = 15f;
        motherGrin.attackDamage = 50f; // High damage
        motherGrin.attackCooldown = 3.5f;
        motherGrin.witchType = WitchType.Tall;
        motherGrin.spawnThreshold = 8; // Spawns at 8 generators
        
        AssetDatabase.CreateAsset(motherGrin, "Assets/ScriptableObjects/Witch_MotherGrin.asset");
        
        // Create Beamoth - The Final Witch
        var evelyn = ScriptableObject.CreateInstance<WitchData>();
        evelyn.witchName = "Beamoth";
        evelyn.description = "The Final Witch - Ultimate hunter. Fast, aggressive, high detection, resistant to abilities, persistent pursuit, unique supernatural effects.";
        evelyn.moveSpeed = 5f;
        evelyn.chaseSpeed = 15f;
        evelyn.detectionRange = 50f;
        evelyn.fieldOfView = 130f;
        evelyn.hearingRange = 25f;
        evelyn.attackDamage = 45f;
        evelyn.attackCooldown = 1f; // Very fast attacks
        evelyn.witchType = WitchType.Final;
        evelyn.spawnThreshold = 10; // Spawns at 10 generators (final phase)
        
        AssetDatabase.CreateAsset(evelyn, "Assets/ScriptableObjects/Witch_Evelyn.asset");
        
        AssetDatabase.SaveAssets();
        Debug.Log("All 6 WitchData assets created!");
    }
}
