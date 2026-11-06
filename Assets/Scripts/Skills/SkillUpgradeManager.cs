using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class SkillUpgradeManager : MonoBehaviour
{
    public static SkillUpgradeManager Instance { get; private set; }
    
    [Header("Skill Definitions")]
    public List<SkillData> allSkills = new List<SkillData>();
    
    [Header("State")]
    private Dictionary<SkillType, int> skillLevels = new Dictionary<SkillType, int>();
    private Dictionary<SkillType, bool> skillUnlocked = new Dictionary<SkillType, bool>();
    
    public delegate void OnSkillUpgraded(SkillType skillType, int newLevel);
    public event OnSkillUpgraded OnSkillUpgradedEvent;
    
    public delegate void OnSkillUnlocked(SkillType skillType);
    public event OnSkillUnlocked OnSkillUnlockedEvent;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadSkillData();
    }
    
    private void LoadSkillData()
    {
        EnsureSkillList();

        foreach (var skill in allSkills)
        {
            if (!skillLevels.ContainsKey(skill.skillType))
            {
                skillLevels[skill.skillType] = 1;
            }
            if (!skillUnlocked.ContainsKey(skill.skillType))
            {
                skillUnlocked[skill.skillType] = (skill.skillType == SkillType.Phase); // Phase unlocked by default
            }
        }
        
        LoadFromPrefs();
    }

    /// <summary>
    /// The serialized list is only filled when the manager was authored in a
    /// scene. In this project it is created at runtime, so the four SkillData
    /// assets are loaded from Resources instead. Without this every survivor
    /// ended up with equippedSkill == null and the Q key did nothing.
    /// </summary>
    private void EnsureSkillList()
    {
        if (allSkills != null && allSkills.Count > 0) return;

        allSkills = new List<SkillData>();

        SkillData[] loaded = null;
        try { loaded = Resources.LoadAll<SkillData>(""); }
        catch (System.Exception e) { Debug.LogWarning("[SKILL] Resources load failed: " + e.Message); }

        if (loaded != null)
        {
            for (int i = 0; i < loaded.Length; i++)
            {
                var s = loaded[i];
                if (s == null) continue;
                if (allSkills.Exists(x => x != null && x.skillType == s.skillType)) continue;
                allSkills.Add(s);
            }
        }

        // Stable order = enum order, so index-based lookups stay predictable.
        allSkills.Sort((a, b) => a.skillType.CompareTo(b.skillType));

        Debug.Log($"[SKILL] loaded {allSkills.Count} skill definition(s) from Resources");
    }
    
    private void LoadFromPrefs()
    {
        foreach (var skill in allSkills)
        {
            string levelKey = $"WITCHBOUND_SkillLevel_{skill.skillType}";
            string unlockKey = $"WITCHBOUND_SkillUnlocked_{skill.skillType}";
            
            if (PlayerPrefs.HasKey(levelKey))
                skillLevels[skill.skillType] = PlayerPrefs.GetInt(levelKey);
            
            if (PlayerPrefs.HasKey(unlockKey))
                skillUnlocked[skill.skillType] = PlayerPrefs.GetInt(unlockKey) == 1;
        }
    }
    
    private void SaveToPrefs()
    {
        foreach (var skill in allSkills)
        {
            string levelKey = $"WITCHBOUND_SkillLevel_{skill.skillType}";
            string unlockKey = $"WITCHBOUND_SkillUnlocked_{skill.skillType}";
            
            PlayerPrefs.SetInt(levelKey, skillLevels[skill.skillType]);
            PlayerPrefs.SetInt(unlockKey, skillUnlocked[skill.skillType] ? 1 : 0);
        }
        PlayerPrefs.Save();
    }
    
    public int GetSkillLevel(SkillType skillType)
    {
        return skillLevels.TryGetValue(skillType, out int level) ? level : 1;
    }
    
    public bool IsSkillUnlocked(SkillType skillType)
    {
        return skillUnlocked.TryGetValue(skillType, out bool unlocked) && unlocked;
    }
    
    public bool CanUpgradeSkill(SkillType skillType)
    {
        var skill = allSkills.Find(s => s.skillType == skillType);
        if (skill == null) return false;
        
        int currentLevel = GetSkillLevel(skillType);
        return currentLevel < skill.maxLevel && IsSkillUnlocked(skillType);
    }
    
    public int GetUpgradeCost(SkillType skillType)
    {
        int level = GetSkillLevel(skillType);
        return level * 500; // Increasing cost per level
    }
    
    public bool TryUpgradeSkill(SkillType skillType)
    {
        if (!CanUpgradeSkill(skillType))
            return false;
        
        int cost = GetUpgradeCost(skillType);
        if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(cost))
            return false;
        
        skillLevels[skillType]++;
        SaveToPrefs();
        OnSkillUpgradedEvent?.Invoke(skillType, skillLevels[skillType]);
        return true;
    }
    
    public bool TryUnlockSkill(SkillType skillType)
    {
        if (IsSkillUnlocked(skillType))
            return false;
        
        var skill = allSkills.Find(s => s.skillType == skillType);
        if (skill == null) return false;
        
        int cost = 1000; // Base unlock cost
        if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(cost))
            return false;
        
        skillUnlocked[skillType] = true;
        SaveToPrefs();
        OnSkillUnlockedEvent?.Invoke(skillType);
        return true;
    }
    
    public SkillData GetSkillData(SkillType skillType)
    {
        EnsureSkillList();
        return allSkills.Find(s => s != null && s.skillType == skillType);
    }
    
    public List<SkillData> GetUnlockedSkills()
    {
        List<SkillData> unlocked = new List<SkillData>();
        foreach (var skill in allSkills)
        {
            if (IsSkillUnlocked(skill.skillType))
                unlocked.Add(skill);
        }
        return unlocked;
    }
    
    public void ApplySkillLevelToController(PlayerSkillController controller, SkillType skillType)
    {
        var skill = GetSkillData(skillType);
        if (skill != null)
        {
            controller.EquipSkill(skill);
            controller.skillLevel = GetSkillLevel(skillType);
        }
    }

    /// <summary>
    /// Gives the controller the skill that belongs to the chosen survivor.
    /// Falls back to the survivor's own definition when the manager is not in
    /// the scene, so a skill is never missing at the start of a round.
    /// </summary>
    public static void EquipSurvivorSkill(PlayerSkillController controller, string survivorName)
    {
        if (controller == null) return;

        SkillType type = CharacterLore.SkillFor(survivorName);
        int level = 1;

        var mgr = Instance;
        if (mgr != null)
        {
            mgr.ApplySkillLevelToController(controller, type);
            return;
        }

        var fallback = SkillData.Load(type);
        if (fallback != null) controller.EquipSkill(fallback);
        controller.skillLevel = level;
    }
}
