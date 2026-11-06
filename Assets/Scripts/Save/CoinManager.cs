using UnityEngine;
using UnityEngine.Events;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }
    
    [Header("Settings")]
    public int startingCoins = 0;
    public int coinsPerGenerator = 50;
    public int coinsPerRescue = 100;
    public int coinsPerEscape = 500;
    public int coinsPerTeamEscape = 1000;
    
    [Header("State")]
    private int currentCoins;
    private int totalCoinsEarned;
    
    public delegate void OnCoinsChanged(int current, int delta);
    public event OnCoinsChanged OnCoinsChangedEvent;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadCoins();
    }
    
    private void Start()
    {
        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGeneratorRepairedEvent += OnGeneratorRepaired;
        }
    }
    
    private void OnGeneratorRepaired(int current, int total)
    {
        AddCoins(coinsPerGenerator);
    }
    
    public int GetCoins()
    {
        return currentCoins;
    }
    
    public int GetTotalEarned()
    {
        return totalCoinsEarned;
    }
    
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        
        currentCoins += amount;
        totalCoinsEarned += amount;
        OnCoinsChangedEvent?.Invoke(currentCoins, amount);
        SaveCoins();
    }
    
    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || currentCoins < amount)
            return false;
        
        currentCoins -= amount;
        OnCoinsChangedEvent?.Invoke(currentCoins, -amount);
        SaveCoins();
        return true;
    }
    
    public void AwardRescue()
    {
        AddCoins(coinsPerRescue);
    }
    
    public void AwardEscape(bool teamEscaped)
    {
        AddCoins(coinsPerEscape);
        if (teamEscaped)
            AddCoins(coinsPerTeamEscape);
    }
    
    private void SaveCoins()
    {
        PlayerPrefs.SetInt("WITCHBOUND_Coins", currentCoins);
        PlayerPrefs.SetInt("WITCHBOUND_TotalEarned", totalCoinsEarned);
        PlayerPrefs.Save();
    }
    
    private void LoadCoins()
    {
        currentCoins = PlayerPrefs.GetInt("WITCHBOUND_Coins", startingCoins);
        totalCoinsEarned = PlayerPrefs.GetInt("WITCHBOUND_TotalEarned", 0);
    }
    
    public void ResetCoins()
    {
        currentCoins = startingCoins;
        totalCoinsEarned = 0;
        SaveCoins();
        OnCoinsChangedEvent?.Invoke(currentCoins, 0);
    }
}
