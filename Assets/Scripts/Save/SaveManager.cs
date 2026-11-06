using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class GameSaveData
{
    public int coins;
    public int totalCoinsEarned;
    public Dictionary<string, int> skillLevels = new Dictionary<string, int>();
    public Dictionary<string, bool> skillUnlocked = new Dictionary<string, bool>();
    public Dictionary<string, int> survivorProgress = new Dictionary<string, int>();
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public float mouseSensitivity = 2f;
    public int graphicsQuality = 2;
    public bool fullscreen = true;
    public int resolutionIndex = 0;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private const string SAVE_KEY = "WITCHBOUND_SaveData";
    private GameSaveData currentSaveData;
    
    public delegate void OnSaveLoaded();
    public event OnSaveLoaded OnSaveLoadedEvent;
    
    public delegate void OnSaveSaved();
    public event OnSaveSaved OnSaveSavedEvent;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SaveGame()
    {
        if (currentSaveData == null)
            currentSaveData = new GameSaveData();
        
        // Save coins
        if (CoinManager.Instance != null)
        {
            currentSaveData.coins = CoinManager.Instance.GetCoins();
            currentSaveData.totalCoinsEarned = CoinManager.Instance.GetTotalEarned();
        }
        
        // Save skills
        if (SkillUpgradeManager.Instance != null)
        {
            currentSaveData.skillLevels.Clear();
            currentSaveData.skillUnlocked.Clear();
            
            foreach (var skill in SkillUpgradeManager.Instance.allSkills)
            {
                currentSaveData.skillLevels[skill.skillType.ToString()] = SkillUpgradeManager.Instance.GetSkillLevel(skill.skillType);
                currentSaveData.skillUnlocked[skill.skillType.ToString()] = SkillUpgradeManager.Instance.IsSkillUnlocked(skill.skillType);
            }
        }
        
        // Save settings
        currentSaveData.masterVolume = AudioManager.Instance != null ? 1f : 1f; // Would get from mixer
        currentSaveData.musicVolume = 1f;
        currentSaveData.sfxVolume = 1f;
        
        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        OnSaveSavedEvent?.Invoke();
        Debug.Log("Game saved successfully");
    }
    
    public void LoadGame()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        
        if (string.IsNullOrEmpty(json))
        {
            currentSaveData = new GameSaveData();
            Debug.Log("No save data found, starting fresh");
        }
        else
        {
            currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log("Game loaded successfully");
        }
        
        ApplySaveData();
        OnSaveLoadedEvent?.Invoke();
    }
    
    private void ApplySaveData()
    {
        // Apply coins
        if (CoinManager.Instance != null && currentSaveData != null)
        {
            // CoinManager loads from its own prefs, but we could sync here
        }
        
        // Apply skills
        if (SkillUpgradeManager.Instance != null && currentSaveData != null)
        {
            foreach (var skill in SkillUpgradeManager.Instance.allSkills)
            {
                string key = skill.skillType.ToString();
                
                if (currentSaveData.skillLevels.TryGetValue(key, out int level))
                {
                    // Directly set level (bypassing cost)
                    // Would need a method to set level directly
                }
                
                if (currentSaveData.skillUnlocked.TryGetValue(key, out bool unlocked))
                {
                    // Set unlocked state
                }
            }
        }
        
        // Apply settings
        if (AudioManager.Instance != null && currentSaveData != null)
        {
            AudioManager.Instance.SetMasterVolume(currentSaveData.masterVolume);
            AudioManager.Instance.SetMusicVolume(currentSaveData.musicVolume);
            AudioManager.Instance.SetSFXVolume(currentSaveData.sfxVolume);
        }
        
        if (GameManager.Instance?.player?.GetComponent<CameraController>() != null && currentSaveData != null)
        {
            GameManager.Instance.player.GetComponent<CameraController>().SetSensitivity(currentSaveData.mouseSensitivity);
        }
        
        QualitySettings.SetQualityLevel(currentSaveData.graphicsQuality);
        Screen.fullScreen = currentSaveData.fullscreen;
    }
    
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        currentSaveData = new GameSaveData();
        Debug.Log("Save data deleted");
    }
    
    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }
}
