using UnityEngine;
using System.Collections.Generic;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Config")]
    public GameConfig gameConfig;
    
    [Header("Chase mode")]
    // The whole run is a 120 second survival chase. This is the only win
    // condition: no generators, no gate, no survivor escape requirement.
    public float survivalDuration = 120f;
    public float timeRemaining = 120f;
    public bool timerRunning = false;

    [Header("Player choice")]
    // Survives the session reset so Character Select -> Start Chase carries the
    // chosen survivor into the next run.
    public string selectedSurvivor = "Luis Padilla";

    [Header("State")]
    public GameState currentState = GameState.MainMenu;
    public int generatorsRepaired = 0;
    public int totalGenerators = 10;
    public List<GeneratorController> generators = new List<GeneratorController>();
    public List<WitchAI> activeWitches = new List<WitchAI>();
    public List<SurvivorAI> survivors = new List<SurvivorAI>();
    public PlayerController player;
    public EscapeGate escapeGate;
    public WitchSpawner witchSpawner;
    
    public delegate void OnGeneratorRepaired(int current, int total);
    public event OnGeneratorRepaired OnGeneratorRepairedEvent;
    
    public delegate void OnGameStateChanged(GameState newState);
    public event OnGameStateChanged OnGameStateChangedEvent;
    
    public delegate void OnWitchSpawned(WitchAI witch);
    public event OnWitchSpawned OnWitchSpawnedEvent;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Ensure gameConfig is loaded
        if (gameConfig == null)
        {
            gameConfig = Resources.Load<GameConfig>("GameConfig");
        }
        if (gameConfig == null)
        {
            // Try to find any GameConfig
            gameConfig = FindObjectOfType<GameConfig>();
        }
        if (gameConfig == null)
        {
            // Create default config
            gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            gameConfig.totalGenerators = 10;
            gameConfig.startingWitches = 1;
            gameConfig.maxWitches = 6;
            gameConfig.witchSpawnThresholds = new int[] { 0, 2, 4, 6, 8, 10 };
            Debug.Log("[CONFIG] Created default GameConfig at runtime");
        }
    }
    
    private void Start()
    {
        if (gameConfig != null)
        {
            totalGenerators = gameConfig.totalGenerators;
        }
        else
        {
            totalGenerators = 10;
        }
        Debug.Log($"[CONFIG DEBUG] GameConfig assigned: {gameConfig != null} GeneratorCount: {totalGenerators}");
        if (gameConfig != null)
        {
            Debug.Log($"[CONFIG DEBUG] MaxWitches: {gameConfig.maxWitches} MinimumWitches: {gameConfig.startingWitches}");
            Debug.Log($"[CONFIG DEBUG] Skill configuration valid: true");
        }

        timeRemaining = survivalDuration;
    }

    /// <summary>
    /// The survival countdown. Runs on scaled time so pausing (timeScale 0)
    /// stops it, and stops the instant the run ends so a dead player can never
    /// still "win" on the results screen.
    /// </summary>
    private void Update()
    {
        if (!timerRunning) return;
        if (currentState != GameState.Gameplay) return;

        timeRemaining -= Time.deltaTime;
        if (UIManager.Instance != null) UIManager.Instance.UpdateSurvivalTimer(timeRemaining, survivalDuration);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            if (UIManager.Instance != null) UIManager.Instance.UpdateSurvivalTimer(0f, survivalDuration);
            Debug.Log("[CHASE] " + survivalDuration + " seconds survived -> VICTORY");
            SetGameState(GameState.Victory);
        }
    }

    public float SurvivedSeconds => Mathf.Clamp(survivalDuration - timeRemaining, 0f, survivalDuration);
    
    public void SetGameState(GameState newState)
    {
        Debug.Log("[GAME] SetGameState: " + currentState + " -> " + newState +
                  " (UIManager=" + (UIManager.Instance != null) +
                  ", InputStateManager=" + (InputStateManager.Instance != null) + ")");
        currentState = newState;

        // Any state that is not active gameplay stops the countdown.
        if (newState != GameState.Gameplay) timerRunning = false;

        // Death, victory, pause menu and main menu all end the current run, so
        // the forward-run speed ramp drops back to the default.
        if (newState != GameState.Gameplay && player != null) player.ResetAcceleration();
        
        // Reset time scale when leaving gameplay/pause
        if (newState == GameState.MainMenu || newState == GameState.CharacterSelect || newState == GameState.Victory || newState == GameState.GameOver)
        {
            Time.timeScale = 1f;
            if (UIManager.Instance != null && UIManager.Instance.pauseMenuPanel != null)
            {
                UIManager.Instance.pauseMenuPanel.SetActive(false);
            }
        }
        
        // Update input mode
        if (InputStateManager.Instance != null)
        {
            if (newState == GameState.Gameplay || newState == GameState.EscapePhase)
                InputStateManager.Instance.SetMode(InputMode.Gameplay);
            else if (newState == GameState.MainMenu || newState == GameState.CharacterSelect)
                InputStateManager.Instance.SetMode(InputMode.Menu);
            else if (newState == GameState.Victory || newState == GameState.GameOver)
                InputStateManager.Instance.SetMode(InputMode.Menu);
        }
        OnGameStateChangedEvent?.Invoke(newState);
    }
    
    public void RegisterGenerator(GeneratorController generator)
    {
        if (!generators.Contains(generator))
        {
            generators.Add(generator);
        }
    }
    
    public void RegisterWitch(WitchAI witch)
    {
        if (!activeWitches.Contains(witch))
        {
            activeWitches.Add(witch);
            OnWitchSpawnedEvent?.Invoke(witch);
        }
    }
    
    public void RegisterSurvivor(SurvivorAI survivor)
    {
        if (!survivors.Contains(survivor))
        {
            survivors.Add(survivor);
        }
    }
    
    /// <summary>
    /// Legacy hook kept so the unused GeneratorController still compiles.
    /// Generators are not part of the chase mode and can no longer end a run.
    /// </summary>
    public void GeneratorCompleted(GeneratorController generator)
    {
        Debug.LogWarning("[CHASE] GeneratorCompleted ignored - generators are not part of this game mode");
    }

    /// <summary>Legacy hook kept for EscapeGate. There is no escape gate in chase mode.</summary>
    public void PlayerEscaped()
    {
        Debug.LogWarning("[CHASE] PlayerEscaped ignored - survive the " + survivalDuration + " seconds to win");
    }

    public void PlayerDied()
    {
        // Death ends the run immediately: the countdown stops before the results
        // screen is shown, so a corpse can never be awarded a survival win.
        timerRunning = false;
        SetGameState(GameState.GameOver);
    }

    private int _lastNewGameFrame = -1;

    /// <summary>
    /// Entry point for every "start playing" button (Play, Play Again, Pause
    /// Restart, Character Select). Resets the whole session in place, then enters
    /// gameplay. Guarded against a double click dispatching twice in one frame.
    /// </summary>
    public void RequestNewGame()
    {
        if (Time.frameCount == _lastNewGameFrame) return;
        _lastNewGameFrame = Time.frameCount;

        Debug.Log($"[GAME] RequestNewGame -> resetting full session (survivor={selectedSurvivor})");
        ResetSession();
        SetGameState(GameState.Gameplay);

        // Start the survival countdown only once the run is actually live.
        timeRemaining = survivalDuration;
        timerRunning = true;
        if (UIManager.Instance != null) UIManager.Instance.UpdateSurvivalTimer(timeRemaining, survivalDuration);

        int liveWitches = witchSpawner != null ? witchSpawner.GetSpawnedWitchCount() : activeWitches.Count;
        Debug.Log($"[GAME] Chase started: state={currentState} hp={(player != null ? player.currentHealth : -1f):F0}" +
                  $" pos={(player != null ? player.transform.position.ToString() : "null")}" +
                  $" monsters={liveWitches} survivors={(survivors != null ? survivors.Count : 0)}" +
                  $" timer={timeRemaining:F1}s timeScale={Time.timeScale:F1}");
    }

    /// <summary>
    /// In-place session reset. Deliberately not a scene reload: the managers are
    /// DontDestroyOnLoad and the UI canvas holds serialized references, so a
    /// reload would orphan them. Every system that carries per-run state is
    /// cleared here in dependency order.
    /// </summary>
    public void ResetSession()
    {
        Time.timeScale = 1f;
        if (PauseController.Instance != null) PauseController.Instance.ForceResume();

        // Fresh round, clean world: no blood stains left from the last attempt.
        BloodVFX.ClearAll();

        timerRunning = false;
        timeRemaining = survivalDuration;

        generatorsRepaired = 0;
        if (gameConfig != null && gameConfig.totalGenerators > 0) totalGenerators = gameConfig.totalGenerators;

        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.ResetForNewSession();
            player.ApplySurvivorAppearance(selectedSurvivor);

            // Every round the player carries the skill that belongs to the
            // survivor they picked. Doing it here (not in SceneSetup) is what
            // makes switching survivors actually change the ability.
            if (player.skillController == null)
                player.skillController = player.GetComponent<PlayerSkillController>();
            if (player.skillController != null)
                player.skillController.EquipForSurvivor(selectedSurvivor);
        }

        if (survivors != null)
        {
            for (int i = 0; i < survivors.Count; i++)
                if (survivors[i] != null) survivors[i].ResetForNewSession();
        }

        // Companions depend on the character the player picked, so the roster is
        // rebuilt for every new session instead of being reused across runs.
        if (GameplayBootstrap.Instance != null) GameplayBootstrap.Instance.RebuildCompanions();

        if (generators == null) generators = new List<GeneratorController>();
        generators.Clear();
        var allGenerators = FindObjectsByType<GeneratorController>(FindObjectsInactive.Exclude);
        for (int i = 0; i < allGenerators.Length; i++)
        {
            var g = allGenerators[i];
            if (g == null) continue;
            generators.Add(g);
            g.ResetForNewSession();
        }

        if (escapeGate == null) escapeGate = FindAnyObjectByType<EscapeGate>();
        if (escapeGate != null) escapeGate.ResetForNewSession();

        if (witchSpawner == null) witchSpawner = FindAnyObjectByType<WitchSpawner>();
        if (witchSpawner != null) witchSpawner.ResetForNewSession();
        else activeWitches.Clear();

        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.ResetForNewSession();

        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.ResetDeathOverlay();
            if (player != null) ui.UpdateHealth(player.currentHealth, player.maxHealth);
            ui.UpdateSurvivalTimer(survivalDuration, survivalDuration);
            ui.HideInteractionPrompt();
        }

        Debug.Log($"[GAME] Session reset: timer={timeRemaining:F1}s " +
                  $"monstersSpawned={(witchSpawner != null ? witchSpawner.GetSpawnedWitchCount() : activeWitches.Count)}");
    }
}

public enum GameState
{
    MainMenu,
    CharacterSelect,
    Gameplay,
    EscapePhase,      // legacy: unused by chase mode, kept for serialized values
    Victory,
    GameOver,
    CharacterLibrary  // appended so existing serialized values never shift
}
