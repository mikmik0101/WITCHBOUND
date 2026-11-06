using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }
    
    [Header("References")]
    public GameManager gameManager;
    
    [Header("Current Objective")]
    public Objective currentObjective = Objective.RepairGenerators;
    public int generatorsRepaired = 0;
    public int totalGenerators = 10;
    
    public delegate void OnObjectiveChangedDelegate(Objective objective);
    public event OnObjectiveChangedDelegate OnObjectiveChangedEvent;
    
    public UnityEvent<int, int> OnGeneratorProgress;
    public UnityEvent OnAllGeneratorsComplete;
    public UnityEvent OnEscapeActivated;
    
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
    
    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;
        
        if (gameManager != null)
        {
            gameManager.OnGeneratorRepairedEvent += OnGeneratorRepaired;
            gameManager.OnGameStateChangedEvent += OnGameStateChanged;
        }
        
        UpdateObjective(Objective.RepairGenerators);
    }
    
    private void OnGeneratorRepaired(int current, int total)
    {
        generatorsRepaired = current;
        totalGenerators = total;
        OnGeneratorProgress?.Invoke(current, total);
        
        if (current >= total)
        {
            OnAllGeneratorsComplete?.Invoke();
            UpdateObjective(Objective.Escpape);
        }
    }
    
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.EscapePhase:
                UpdateObjective(Objective.Escpape);
                OnEscapeActivated?.Invoke();
                break;
            case GameState.Victory:
                UpdateObjective(Objective.Escaped);
                break;
            case GameState.GameOver:
                UpdateObjective(Objective.Failed);
                break;
        }
    }
    
    public void UpdateObjective(Objective newObjective)
    {
        currentObjective = newObjective;
        OnObjectiveChangedEvent?.Invoke(newObjective);
    }

    /// <summary>Clears generator progress and restores the starting objective.</summary>
    public void ResetForNewSession()
    {
        generatorsRepaired = 0;
        totalGenerators = gameManager != null ? gameManager.totalGenerators : 10;
        OnGeneratorProgress?.Invoke(0, totalGenerators);
        UpdateObjective(Objective.RepairGenerators);
        Debug.Log($"[OBJECTIVE] reset -> RepairGenerators 0/{totalGenerators}");
    }
    
    public string GetObjectiveText()
    {
        switch (currentObjective)
        {
            case Objective.RepairGenerators:
                return $"Repair Generators: {generatorsRepaired}/{totalGenerators}";
            case Objective.Escpape:
                return "Reach the Escape Gate!";
            case Objective.Escaped:
                return "ESCAPED!";
            case Objective.Failed:
                return "GAME OVER";
            default:
                return "";
        }
    }
}

public enum Objective
{
    RepairGenerators,
    Escpape,
    Escaped,
    Failed
}
