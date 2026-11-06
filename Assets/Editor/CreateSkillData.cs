using UnityEngine;
using UnityEditor;

public class CreateSkillData
{
    [MenuItem("WITCHBOUND/Create Skill Data")]
    public static void CreateSkillDataAssets()
    {
        // Phase - Become untouchable
        var phase = ScriptableObject.CreateInstance<SkillData>();
        phase.skillName = "Phase";
        phase.description = "Become untouchable for a short duration. Witches cannot detect or damage you.";
        phase.skillType = SkillType.Phase;
        phase.cooldown = 45f;
        phase.duration = 3f;
        phase.maxLevel = 4;
        phase.levelDurations = new float[] { 3f, 4f, 5f, 6f };
        phase.levelCooldowns = new float[] { 45f, 40f, 35f, 30f };
        
        AssetDatabase.CreateAsset(phase, "Assets/ScriptableObjects/Skill_Phase.asset");
        
        // Invisibility - Become hard to detect
        var invis = ScriptableObject.CreateInstance<SkillData>();
        invis.skillName = "Invisibility";
        invis.description = "Become nearly invisible to witches. Detection range drastically reduced.";
        invis.skillType = SkillType.Invisibility;
        invis.cooldown = 50f;
        invis.duration = 5f;
        invis.maxLevel = 4;
        invis.levelDurations = new float[] { 5f, 6f, 7f, 8f };
        invis.levelCooldowns = new float[] { 50f, 45f, 40f, 35f };
        
        AssetDatabase.CreateAsset(invis, "Assets/ScriptableObjects/Skill_Invisibility.asset");
        
        // Speed Burst - Temporary speed increase
        var speed = ScriptableObject.CreateInstance<SkillData>();
        speed.skillName = "Speed Burst";
        speed.description = "Massively increase movement speed for a short time. Great for escaping chases.";
        speed.skillType = SkillType.SpeedBurst;
        speed.cooldown = 40f;
        speed.duration = 4f;
        speed.maxLevel = 4;
        speed.levelDurations = new float[] { 4f, 5f, 6f, 7f };
        speed.levelCooldowns = new float[] { 40f, 35f, 30f, 25f };
        
        AssetDatabase.CreateAsset(speed, "Assets/ScriptableObjects/Skill_SpeedBurst.asset");
        
        // Emergency Weapon - Limited defensive capability
        var weapon = ScriptableObject.CreateInstance<SkillData>();
        weapon.skillName = "Emergency Weapon";
        weapon.description = "Gain a temporary defensive weapon that can stun witches.";
        weapon.skillType = SkillType.EmergencyWeapon;
        weapon.cooldown = 60f;
        weapon.duration = 8f;
        weapon.maxLevel = 4;
        weapon.levelDurations = new float[] { 8f, 10f, 12f, 15f };
        weapon.levelCooldowns = new float[] { 60f, 55f, 50f, 45f };
        
        AssetDatabase.CreateAsset(weapon, "Assets/ScriptableObjects/Skill_EmergencyWeapon.asset");
        
        AssetDatabase.SaveAssets();
        Debug.Log("All 4 SkillData assets created!");
    }
}
