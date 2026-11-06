using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }
    
    [Header("Volume")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    
    [Header("Controls")]
    public Slider sensitivitySlider;
    
    [Header("Graphics")]
    public Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    
    [Header("Buttons")]
    public Button applyButton;
    public Button backButton;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        // Load saved settings
        LoadSettings();
        
        // Setup listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);
        if (backButton != null)
            backButton.onClick.AddListener(BackToMenu);
    }
    
    private void LoadSettings()
    {
        float masterVol = PlayerPrefs.GetFloat("WITCHBOUND_MasterVolume", 1f);
        float musicVol = PlayerPrefs.GetFloat("WITCHBOUND_MusicVolume", 1f);
        float sfxVol = PlayerPrefs.GetFloat("WITCHBOUND_SFXVolume", 1f);
        float sens = PlayerPrefs.GetFloat("WITCHBOUND_Sensitivity", 2f);
        int quality = PlayerPrefs.GetInt("WITCHBOUND_Quality", 2);
        bool fullscreen = PlayerPrefs.GetInt("WITCHBOUND_Fullscreen", 1) == 1;
        
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (sensitivitySlider != null) sensitivitySlider.value = sens;
        if (qualityDropdown != null) qualityDropdown.value = quality;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }
    
    private void OnSensitivityChanged(float value)
    {
        if (GameManager.Instance?.player?.GetComponent<CameraController>() != null)
        {
            GameManager.Instance.player.GetComponent<CameraController>().SetSensitivity(value);
        }
    }
    
    private void OnQualityChanged(int value)
    {
        QualitySettings.SetQualityLevel(value);
    }
    
    private void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;
    }
    
    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("WITCHBOUND_MasterVolume", masterVolumeSlider != null ? masterVolumeSlider.value : 1f);
        PlayerPrefs.SetFloat("WITCHBOUND_MusicVolume", musicVolumeSlider != null ? musicVolumeSlider.value : 1f);
        PlayerPrefs.SetFloat("WITCHBOUND_SFXVolume", sfxVolumeSlider != null ? sfxVolumeSlider.value : 1f);
        PlayerPrefs.SetFloat("WITCHBOUND_Sensitivity", sensitivitySlider != null ? sensitivitySlider.value : 2f);
        PlayerPrefs.SetInt("WITCHBOUND_Quality", qualityDropdown != null ? qualityDropdown.value : 2);
        PlayerPrefs.SetInt("WITCHBOUND_Fullscreen", fullscreenToggle != null && fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log("Settings applied and saved.");
    }
    
    public void BackToMenu()
    {
        gameObject.SetActive(false);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }
    }
}
