using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "WITCHBOUND/Data/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName = "Skill";
    [TextArea] public string description = "";
    public SkillType skillType;
    public float cooldown = 30f;
    public float duration = 5f;
    public int maxLevel = 4;
    public float[] levelDurations;
    public float[] levelCooldowns;
    public GameObject vfxPrefab;
    public AudioClip activationSound;
    public AudioClip loopSound;

    private static SkillData[] _all;

    /// <summary>
    /// Loads one skill definition from Resources, so a controller can always be
    /// given its ability even when SkillUpgradeManager is not in the scene.
    /// </summary>
    public static SkillData Load(SkillType type)
    {
        if (_all == null)
        {
            _all = new SkillData[0];
            try { _all = Resources.LoadAll<SkillData>(""); }
            catch { /* Resources unavailable - caller falls back to no skill */ }
        }

        for (int i = 0; i < _all.Length; i++)
            if (_all[i] != null && _all[i].skillType == type) return _all[i];

        return null;
    }

    /// <summary>Clears the static cache (editor play-mode restarts).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _all = null; }
}

public enum SkillType
{
    Phase,
    Invisibility,
    SpeedBurst,
    EmergencyWeapon
}
