using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Runtime presentation fixup + verification harness.
///
/// After the procedural town exists it grounds the player, places the survivors
/// in a visible arc, repairs every witch's layers/face, and (with -visualtest)
/// captures screenshots and logs an acceptance + witch-hunt table so behaviour
/// can be checked on screen rather than inferred from logs.
/// </summary>
[DefaultExecutionOrder(200)]
public class GameplayBootstrap : MonoBehaviour
{
    public static GameplayBootstrap Instance { get; private set; }

    public bool enableVisualTest = false;
    public bool enableDeathTest = false;
    public bool enableRepairTest = false;
    public bool enableProgressionTest = false;
    public bool enableChaseTest = false;
    public bool enableSprintTest = false;
    public bool enableSurvivorTest = false;
    public bool enableFacingTest = false;
    public bool enableNormalTest = false;
    public bool enableCombatTest = false;
    public bool enableFleetTest = false;
    public bool enableRestartTest = false;
    public bool enableEscapeTest = false;
    public bool enableSurvivalTest = false;
    public bool enableSpeedTest = false;
    public bool enableRun60Test = false;
    public bool enableCompanionTest = false;
    public bool enableLowHpTest = false;
    public string shotDir = "/tmp/opencode/shots";

    private PlayerController _player;
    private Camera _cam;
    private float _groundRef;
    private List<SurvivorAI> _survivors = new List<SurvivorAI>();
    private List<WitchAI> _witches = new List<WitchAI>();
    private List<GeneratorController> _generators = new List<GeneratorController>();

    private void Awake() { Instance = this; ParseArgs(); }

    private void ParseArgs()
    {
        try
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
            {
                if (a == "-visualtest") enableVisualTest = true;
                else if (a == "-deathtest") enableDeathTest = true;
                else if (a == "-repairtest") enableRepairTest = true;
                else if (a == "-progressiontest") enableProgressionTest = true;
                else if (a == "-chasetest") enableChaseTest = true;
                else if (a == "-sprint") enableSprintTest = true;
                else if (a == "-survivortest") enableSurvivorTest = true;
                else if (a == "-facingtest") enableFacingTest = true;
                else if (a == "-normaltest") enableNormalTest = true;
                else if (a == "-combattest") enableCombatTest = true;
                else if (a == "-fleettest") enableFleetTest = true;
                else if (a == "-restarttest") enableRestartTest = true;
                else if (a == "-escapetest") enableEscapeTest = true;
                else if (a == "-survivaltest") enableSurvivalTest = true;
                else if (a == "-speedtest") enableSpeedTest = true;
                else if (a == "-run60") enableRun60Test = true;
                else if (a == "-companiontest") enableCompanionTest = true;
                else if (a == "-lowhptest") enableLowHpTest = true;
                else if (a == "-combatlog") WitchAI.VerboseCombat = true;
                else if (a == "-nohunt") WitchAI.HuntingDisabled = true;
                else if (a == "-packlog") WitchAI.VerbosePack = true;
                else if (a == "-repairlog") GeneratorController.Verbose = true;
                else if (a.StartsWith("-shotdir=")) shotDir = a.Substring("-shotdir=".Length);
            }
        }
        catch { }
    }

    private void Start() { StartCoroutine(BootstrapRoutine()); }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChangedEvent -= OnBootstrapStateChanged;
    }

    /// <summary>
    /// Companions belong to a round, not to the shell: they are built the first
    /// time gameplay starts and hidden while a menu, victory or defeat screen is
    /// up, so no survivor is left standing in the street behind the main menu.
    /// </summary>
    private void OnBootstrapStateChanged(GameState state)
    {
        bool inRound = state == GameState.Gameplay || state == GameState.EscapePhase;

        if (inRound && _survivors.Count == 0) RebuildCompanions();

        for (int i = 0; i < _survivors.Count; i++)
            if (_survivors[i] != null && _survivors[i].gameObject.activeSelf != inRound)
                _survivors[i].gameObject.SetActive(inRound);
    }

    private IEnumerator BootstrapRoutine()
    {
        yield return null;
        yield return null;

        // Subscribe once the managers exist: companion visibility follows the
        // game state, so nothing is left standing behind a menu.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChangedEvent -= OnBootstrapStateChanged;
            GameManager.Instance.OnGameStateChangedEvent += OnBootstrapStateChanged;
        }

        Collect();
        SnapPlayerToGround();
        PlaceSurvivors();
        FixGenerators();
        FixAllWitches();
        RecordSpawnPoints();

        Debug.Log("[BOOTSTRAP] Placement complete. " + SummaryLine() + " groundRef=" + _groundRef.ToString("F2"));

        yield return new WaitForSecondsRealtime(0.4f);
        LogVisualAcceptance(true);
        LogWitchStatus("at-spawn");
        LogButtonScreenPoints("boot");

        if (enableVisualTest) StartCoroutine(VisualTestRoutine());
        else if (enableDeathTest) StartCoroutine(DeathTestRoutine());
        else if (enableRepairTest) StartCoroutine(RepairTestRoutine());
        else if (enableProgressionTest) StartCoroutine(ProgressionTestRoutine());
        else if (enableChaseTest) StartCoroutine(ChaseTestRoutine());
        else if (enableSurvivorTest) StartCoroutine(SurvivorTestRoutine());
        else if (enableFacingTest) StartCoroutine(FacingTestRoutine());
        else if (enableNormalTest) StartCoroutine(NormalTestRoutine());
        else if (enableCombatTest) StartCoroutine(CombatTestRoutine());
        else if (enableFleetTest) StartCoroutine(FleetTestRoutine());
        else if (enableRestartTest) StartCoroutine(RestartTestRoutine());
        else if (enableEscapeTest) StartCoroutine(EscapeTestRoutine());
        else if (enableSurvivalTest) StartCoroutine(SurvivalTestRoutine());
        else if (enableSpeedTest) StartCoroutine(SpeedTestRoutine());
        else if (enableRun60Test) StartCoroutine(Run60Routine());
        else if (enableCompanionTest) StartCoroutine(CompanionTestRoutine());
        else if (enableLowHpTest) StartCoroutine(LowHpTestRoutine());
        else { yield return new WaitForSecondsRealtime(6f); LogWitchStatus("t+6s"); }
    }

    /// <summary>
    /// QA-only (-lowhptest): drives the low-health feedback through the three
    /// states that matter and holds each long enough to be photographed -
    /// 27 % health, healed back to full, then a brand new round. Phasing is
    /// switched on while the health is low so a witch cannot end the round in
    /// the middle of the check.
    /// </summary>
    private IEnumerator LowHpTestRoutine()
    {
        // Wait for the round the harness actually starts, instead of forcing
        // gameplay from the boot screen: the point of this check is to look at
        // the real HUD during a real round.
        float waited = 0f;
        while (waited < 90f &&
               (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Gameplay))
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.6f);

        _player = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (_player == null) _player = FindObjectOfType<PlayerController>();
        if (_player == null)
        {
            Debug.LogWarning("[LOWHP] no player found");
            yield break;
        }

        _player.SetPhasing(true);

        _player.currentHealth = _player.maxHealth * 0.27f;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(_player.currentHealth, _player.maxHealth);
            UIManager.Instance.SetLowHealthBlood(0.27f);
        }
        Debug.Log("[LOWHP] phase=LOW hp=" + _player.currentHealth.ToString("F0") + "/" +
                  _player.maxHealth.ToString("F0") + " (27%)");
        yield return new WaitForSecondsRealtime(7f);

        _player.Heal(_player.maxHealth);
        Debug.Log("[LOWHP] phase=HEALED hp=" + _player.currentHealth.ToString("F0") + "/" +
                  _player.maxHealth.ToString("F0"));
        yield return new WaitForSecondsRealtime(6f);

        _player.SetPhasing(false);
        if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(0.8f);

        // The reset clears phasing, and a hit taken during the capture window
        // would paint the damage flash over the frame we are checking.
        if (_player != null) _player.SetPhasing(true);

        Debug.Log("[LOWHP] phase=NEWROUND hp=" + _player.currentHealth.ToString("F0") + "/" +
                  _player.maxHealth.ToString("F0"));
        yield return new WaitForSecondsRealtime(5f);
        Debug.Log("[LOWHP] done");
    }

    /// <summary>
    /// Roster contract: for each of the four playable survivors, a fresh round
    /// must produce exactly three companions, none of them the player's own
    /// character, all grounded, with no duplicates left over from the last run.
    /// </summary>
    private IEnumerator CompanionTestRoutine()
    {
        var names = RuntimeCharacterFactory.SurvivorNames;
        for (int i = 0; i < names.Length; i++)
        {
            if (GameManager.Instance == null) yield break;
            GameManager.Instance.selectedSurvivor = names[i];
            GameManager.Instance.RequestNewGame();
            yield return new WaitForSecondsRealtime(1.4f);

            Collect();
            var roster = CompanionNames();
            int inScene = FindObjectsByType<SurvivorAI>(FindObjectsInactive.Include).Length;

            bool excluded = !roster.Contains(names[i]);
            bool grounded = true;
            float worst = 0f;
            for (int s = 0; s < _survivors.Count; s++)
            {
                if (_survivors[s] == null) continue;
                float dy = Mathf.Abs(_survivors[s].transform.position.y - _groundRef);
                if (dy > worst) worst = dy;
                if (dy > 0.6f) grounded = false;
            }

            // Minimum spacing between companions, so nobody spawns inside another.
            float closest = float.MaxValue;
            for (int a = 0; a < _survivors.Count; a++)
                for (int b = a + 1; b < _survivors.Count; b++)
                {
                    if (_survivors[a] == null || _survivors[b] == null) continue;
                    float d = Vector3.Distance(_survivors[a].transform.position, _survivors[b].transform.position);
                    if (d < closest) closest = d;
                }

            Debug.Log($"[COMPANION TEST] player={names[i]} count={_survivors.Count} ai=[{string.Join(", ", roster)}] " +
                      $"excluded={excluded} inScene={inScene} grounded={grounded}(worst dy {worst:F2}) " +
                      $"minSpacing={(closest == float.MaxValue ? -1f : closest):F1} " +
                      $"playerPos={(_player != null ? _player.transform.position.ToString("F1") : "null")}");
        }
        // Behaviour watch: one normal round, sampled every 1.5 s, so movement,
        // spread and the flee reaction are observed rather than assumed.
        GameManager.Instance.selectedSurvivor = names[0];
        GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.5f);

        for (int t = 0; t < 9; t++)
        {
            Collect();
            var parts = new List<string>();
            for (int s = 0; s < _survivors.Count; s++)
            {
                var sv = _survivors[s];
                if (sv == null) { parts.Add("null"); continue; }
                float nearest = float.MaxValue;
                for (int w = 0; w < _witches.Count; w++)
                {
                    if (_witches[w] == null) continue;
                    float d = Vector3.Distance(sv.transform.position, _witches[w].transform.position);
                    if (d < nearest) nearest = d;
                }
                parts.Add($"{sv.DisplayName.Split(' ')[0]}:{sv.currentState}@({sv.transform.position.x:F1},{sv.transform.position.z:F1})w{nearest:F0}");
            }
            Debug.Log($"[COMPANION WATCH] t={(t + 1) * 1.5f:F1}s | {string.Join(" | ", parts)}");
            yield return new WaitForSecondsRealtime(1.5f);
        }

        // Menu round trip through the exact callbacks the buttons invoke:
        // results -> Main Menu -> Play -> Enter the Town.
        GameManager.Instance.SetGameState(GameState.MainMenu);
        yield return new WaitForSecondsRealtime(0.8f);
        Debug.Log($"[COMPANION TEST] after Main Menu: state={GameManager.Instance.currentState} " +
                  $"active={FindObjectsByType<SurvivorAI>(FindObjectsInactive.Exclude).Length}");

        GameManager.Instance.SetGameState(GameState.CharacterSelect);
        yield return new WaitForSecondsRealtime(0.8f);
        GameManager.Instance.selectedSurvivor = "Vincent Reid";
        GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.4f);
        Collect();
        Debug.Log($"[COMPANION TEST] after menu round trip: state={GameManager.Instance.currentState} " +
                  $"count={_survivors.Count} active={FindObjectsByType<SurvivorAI>(FindObjectsInactive.Exclude).Length} " +
                  $"ai=[{string.Join(", ", CompanionNames())}]");

        // Renderer/grounding acceptance with the roster live (no framing rule:
        // companions wander freely, so "in front of the camera" is not required).
        LogVisualAcceptance(false);

        Debug.Log("[COMPANION TEST] done");
        yield return new WaitForSecondsRealtime(0.5f);
    }

    /// <summary>
    /// Survivable-chase check: a player who keeps sprinting and steers around
    /// walls must be able to reach the 60 s timer with all six witches live.
    /// The harness only supplies input (forward + sprint + a turn when a wall
    /// blocks the run); every AI, damage and victory rule is the real one.
    /// </summary>
    private IEnumerator Run60Routine()
    {
        if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.0f);
        Collect();
        if (_player == null) { Debug.Log("[RUN60] no player"); yield break; }

        PlayerController.ForceForwardForQA = true;
        PlayerController.ForceSprintForQA = true;
        Debug.Log("[RUN60] start: forward+sprint forced, six witches live");

        Vector3 last = _player.transform.position;
        float t = 0f, stuck = 0f, nextLog = 0f;
        while (t < 64f)
        {
            if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Gameplay)
                break;

            float moved = Vector3.Distance(_player.transform.position, last);
            last = _player.transform.position;
            if (moved < 0.05f) stuck += 0.25f; else stuck = 0f;
            if (stuck >= 0.5f)
            {
                _player.transform.rotation = Quaternion.Euler(0f, _player.transform.eulerAngles.y + 90f, 0f);
                stuck = 0f;
            }

            if (t >= nextLog)
            {
                Debug.Log($"[RUN60] t={t:F0} hp={_player.currentHealth:F0} timer={GameManager.Instance.timeRemaining:F1} " +
                          $"level={_player.AccelerationLevel} walk={_player.moveSpeed:F0} witches={CountWitches()}");
                nextLog += 5f;
            }

            yield return new WaitForSecondsRealtime(0.25f);
            t += 0.25f;
        }

        PlayerController.ForceForwardForQA = false;
        PlayerController.ForceSprintForQA = false;
        Debug.Log($"[RUN60] end t={t:F1} state={GameManager.Instance.currentState} hp={_player.currentHealth:F0} " +
                  $"timer={GameManager.Instance.timeRemaining:F1} survived={GameManager.Instance.SurvivedSeconds:F1}");
        yield return new WaitForSecondsRealtime(1f);
    }

    /// <summary>
    /// Forward-run acceleration contract: +1 speed unit every 5 s of genuine
    /// forward progress, no reset while turning, full reset on release.
    /// Runs on real player movement; witches are parked far away so nothing
    /// interrupts the measurement, and the harness steers the player when a
    /// building blocks the run (a human would steer too).
    /// </summary>
    private IEnumerator SpeedTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(0.6f);
        Collect();
        if (_player == null) { Debug.Log("[SPEEDTEST] no player"); yield break; }

        PlayerController.ForceForwardForQA = true;
        Debug.Log("[SPEEDTEST] forward forced, starting 17 s run");

        Vector3 last = _player.transform.position;
        float t = 0f, stuckFor = 0f, nextLog = 0f;
        while (t < 17f)
        {
            for (int i = 0; i < _witches.Count; i++)
                if (_witches[i] != null)
                    _witches[i].transform.position = _player.transform.position + new Vector3(220f, 0f, 220f);

            float moved = Vector3.Distance(_player.transform.position, last);
            last = _player.transform.position;
            if (moved < 0.05f) stuckFor += 0.25f; else stuckFor = 0f;
            if (stuckFor >= 0.5f)
            {
                _player.transform.rotation = Quaternion.Euler(0f, _player.transform.eulerAngles.y + 35f, 0f);
                stuckFor = 0f;
            }

            if (t >= nextLog)
            {
                Debug.Log($"[SPEEDTEST] t={t:F1} walk={_player.moveSpeed:F0} level={_player.AccelerationLevel} " +
                          $"speed={_player.CurrentSpeed:F1} companions=[{CompanionSpeedSnapshot()}] " +
                          $"pos={_player.transform.position.ToString("F1")}");
                nextLog += 2.5f;
            }

            yield return new WaitForSeconds(0.25f);
            t += 0.25f;
        }

        PlayerController.ForceForwardForQA = false;
        Debug.Log($"[SPEEDTEST] released at level={_player.AccelerationLevel} walk={_player.moveSpeed:F0} " +
                  $"companions=[{CompanionSpeedSnapshot()}]");
        yield return new WaitForSecondsRealtime(0.6f);
        Debug.Log($"[SPEEDTEST] after release level={_player.AccelerationLevel} walk={_player.moveSpeed:F0} " +
                  $"companions=[{CompanionSpeedSnapshot()}]");

        PlayerController.ForceForwardForQA = true;
        Debug.Log("[SPEEDTEST] second run starts");
        yield return new WaitForSecondsRealtime(6.2f);
        PlayerController.ForceForwardForQA = false;
        Debug.Log($"[SPEEDTEST] second run after 6.2 s level={_player.AccelerationLevel} walk={_player.moveSpeed:F0} " +
                  $"companions=[{CompanionSpeedSnapshot()}]");
        yield return new WaitForSecondsRealtime(0.6f);
        Debug.Log("[SPEEDTEST] done");
    }

    /// <summary>
    /// Forward-axis proof: the witch runs straight at the player-controlled
    /// camera, and each frame logs dot(forward, directionToPlayer). ~+1 means
    /// the side the face is authored on leads the movement instead of trailing it.
    /// </summary>
    private IEnumerator FacingTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(0.6f);

        // Clear the set so the witches only have the player to chase.
        foreach (var s in _survivors)
            if (s != null) s.transform.position = new Vector3(120f, 0f, 120f);

        if (_player == null) yield break;

        var live = new List<WitchAI>();
        foreach (var w in _witches)
            if (w != null && w.gameObject.activeInHierarchy) live.Add(w);
        if (live.Count == 0) yield break;

        // Test every witch that is actually in play, not just the first one -
        // each one must lead with its face, so every sample is logged.
        Vector3 parked = new Vector3(400f, 0f, 400f);
        foreach (var w in live) w.transform.position = parked;
        yield return new WaitForSecondsRealtime(0.2f);

        Vector3 fwd = Flatten(_player.transform.forward);
        _player.transform.rotation = Quaternion.LookRotation(fwd);

        int samples = live.Count * 2;
        int n = 0;
        for (int i = 0; i < live.Count; i++)
        {
            var w = live[i];
            if (w == null) continue;
            w.enabled = true;

            for (int pass = 0; pass < 2; pass++)
            {
                // First pass head-on, second pass from an angle: the face must
                // follow the real movement direction, not a fixed axis.
                Vector3 side = Vector3.Cross(Vector3.up, fwd) * (pass == 0 ? 0f : 4.5f);
                Vector3 spot = _player.transform.position + fwd * 10f + side;
                spot.y = _player.transform.position.y + 0.05f;
                w.transform.position = spot;
                w.transform.rotation = Quaternion.LookRotation(Flatten(_player.transform.position - spot));

                yield return new WaitForSecondsRealtime(0.5f);

                Vector3 toPlayer = Flatten(_player.transform.position - w.transform.position);
                float dot = Vector3.Dot(Flatten(w.transform.forward), toPlayer.normalized);
                float dist = Vector3.Distance(_player.transform.position, w.transform.position);
                n++;
                Debug.Log($"[FACING TEST] {n}/{samples} {w.Name}: dotForwardToPlayer={dot:F3} " +
                          $"dist={dist:F1} animSpeed={w.MeasuredSpeed:F2} state={w.currentState} " +
                          $"pass={(pass == 0 ? "head-on" : "angled")}");
                yield return Shot($"facingtest_{i + 1:00}_{pass}");
            }

            w.transform.position = parked;
            yield return new WaitForSecondsRealtime(0.15f);
        }

        Debug.Log($"[FACING TEST] complete ({live.Count} witches, {samples} samples; dot ~ +1 = face leads the run)");
        yield return new WaitForSecondsRealtime(20f);
    }

    // --------------------------------------------------- QA scenarios

    // ------------------------------------------------ session / combat QA

    private WitchAI LiveWitch()
    {
        for (int i = 0; i < _witches.Count; i++)
            if (_witches[i] != null && _witches[i].gameObject.activeInHierarchy) return _witches[i];
        return FindAnyObjectByType<WitchAI>();
    }

    private string SurvivorSnapshot()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.survivors == null) return "none";
        var parts = new List<string>();
        for (int i = 0; i < gm.survivors.Count; i++)
        {
            var s = gm.survivors[i];
            if (s == null) { parts.Add("null"); continue; }
            parts.Add($"{s.DisplayName}:{s.healthState}/{s.currentHealth:F0}");
        }
        return string.Join(" | ", parts.ToArray());
    }

    private void LogSessionSnapshot(string tag)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        Debug.Log($"[NORMAL TEST] {tag} state={gm.currentState} " +
                  $"playerHP={(_player != null ? _player.currentHealth : -1f):F0} " +
                  $"playerPos={(_player != null ? _player.transform.position.ToString("F2") : "null")} " +
                  $"playerDead={(_player != null && _player.IsDead)} " +
                  $"walk={(_player != null ? _player.moveSpeed : -1f):F1} sprint={(_player != null ? _player.sprintSpeed : -1f):F1} " +
                  $"witches={CountWitches()} names=[{string.Join(", ", WitchNames().ToArray())}] " +
                  $"survivors=[{SurvivorSnapshot()}] gens={gm.generatorsRepaired}/{gm.totalGenerators} " +
                  $"timeScale={Time.timeScale:F1}");
    }

    /// <summary>
    /// Normal (non-QA-flag) start: exactly one witch, fresh survivors, speeds and
    /// state readable from the log over a few seconds of real play.
    /// </summary>
    private IEnumerator NormalTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.2f);
        Collect();
        LogSessionSnapshot("t+0s");
        for (int i = 1; i <= 5; i++)
        {
            yield return new WaitForSecondsRealtime(2f);
            LogSessionSnapshot($"t+{i * 2}s");
        }
        yield return Shot("normaltest_01_play");
        yield return new WaitForSecondsRealtime(10f);
    }

    /// <summary>
    /// Attack lifecycle: a single witch is placed just outside reach of a frozen
    /// player. Logs prove wind-up -> impact (one HP drop) -> recovery -> cooldown,
    /// and that walking out of range during a wind-up misses.
    /// </summary>
    private IEnumerator CombatTestRoutine()
    {
        WitchAI.VerboseCombat = true;
        if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.2f);
        Collect();

        if (_player == null) { Debug.LogError("[COMBAT TEST] no player"); yield break; }
        _player.moveSpeed = 0f;
        _player.sprintSpeed = 0f;
        foreach (var s in _survivors)
            if (s != null) s.transform.position = new Vector3(320f, 0f, 320f);

        WitchAI w = LiveWitch();
        if (w == null) { Debug.LogError("[COMBAT TEST] no witch"); yield break; }
        w.enabled = true;

        Vector3 fwd = Flatten(_player.transform.forward);
        Vector3 spot = _player.transform.position + fwd * (w.AttackRange + 1.4f);
        spot.y = _player.transform.position.y;
        w.transform.position = spot;

        Debug.Log($"[COMBAT TEST] witch={w.Name} type={w.witchData?.witchType} range={w.AttackRange:F2} " +
                  $"cooldown={w.AttackCooldown:F2} windup={w.attackWindup:F2} recovery={w.attackRecovery:F2} " +
                  $"playerHP={_player.currentHealth:F0} startDist={Vector3.Distance(spot, _player.transform.position):F2}");

        float lastHp = _player.currentHealth;
        int drops = 0;
        float t = 0f;
        while (t < 6f && !_player.IsDead)
        {
            t += Time.deltaTime;
            if (_player.currentHealth < lastHp - 0.01f)
            {
                drops++;
                Debug.Log($"[COMBAT TEST] t={t:F2}s HP {lastHp:F0} -> {_player.currentHealth:F0} " +
                          $"drop#{drops} witchPhase='{w.AttackPhaseName}'");
                lastHp = _player.currentHealth;
            }
            if (drops >= 2) break;
            yield return null;
        }

        // Juke test: wait for the next wind-up, step out of reach, and confirm the
        // telegraphed attack misses instead of dealing delayed free damage.
        float waitWindup = 0f;
        while (waitWindup < 4f && w.AttackPhaseName != "Windup")
        {
            waitWindup += Time.deltaTime;
            yield return null;
        }
        float hpJuke = _player.currentHealth;
        Vector3 juke = _player.transform.position - Flatten(_player.transform.forward) * 8f;
        juke.y = _player.transform.position.y;
        _player.transform.position = juke;
        float jt = 0f;
        while (jt < 0.7f) { jt += Time.deltaTime; yield return null; }
        Debug.Log($"[COMBAT TEST] juke: windupWait={waitWindup:F2}s hpBefore={hpJuke:F0} hpAfter={_player.currentHealth:F0} " +
                  $"delta={hpJuke - _player.currentHealth:F0} (must be 0) phase='{w.AttackPhaseName}' " +
                  $"dist={Vector3.Distance(_player.transform.position, w.transform.position):F1}");
        yield return Shot("combattest_01_juke_miss");

        // Park the witch so the screenshot does not immediately kill the player.
        w.enabled = false;
        w.transform.position = new Vector3(400f, 0f, 400f);
        yield return new WaitForSecondsRealtime(10f);
    }

    /// <summary>
    /// Survivor panic: proves a survivor repairing a generator drops it the moment
    /// a witch enters detection range, runs away at flee speed, and returns to its
    /// objective once the threat is gone.
    /// </summary>
    private IEnumerator FleetTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.2f);
        Collect();

        if (_player != null) { _player.moveSpeed = 0f; _player.sprintSpeed = 0f; }
        if (_survivors.Count == 0 || _generators.Count == 0)
        {
            Debug.LogError("[FLEET TEST] missing survivor or generator");
            yield break;
        }

        SurvivorAI s = _survivors[0];
        GeneratorController gen = _generators[0];

        // Park the witch far away while the survivor settles onto a generator.
        WitchAI w = LiveWitch();
        if (w != null) { w.enabled = false; w.transform.position = new Vector3(400f, 0f, 400f); }

        Vector3 gp = gen.transform.position;
        Vector3 dir = Flatten(s.transform.position - gp);
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
        Vector3 pp = gp + dir * 1.8f;
        pp.y = gp.y;
        s.transform.position = pp;

        float waitRepair = 0f;
        while (waitRepair < 8f && !gen.isBeingRepaired)
        {
            waitRepair += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"[FLEET TEST] repair before threat: beingRepaired={gen.isBeingRepaired} " +
                  $"progress={gen.GetProgress() * 100f:F1}% repairers={gen.repairersCount} " +
                  $"survivor={s.currentState} detection={s.detectionRange:F1} dist={Vector3.Distance(s.transform.position, gp):F1}");

        if (w == null) { Debug.LogError("[FLEET TEST] no witch"); yield break; }

        // A disabled witch still counts as a threat (distance only), so the flee
        // response is tested without it actually dealing damage.
        Vector3 wp = s.transform.position + Flatten(Vector3.right) * 10f;
        wp.y = s.transform.position.y;
        w.transform.position = wp;
        yield return new WaitForSecondsRealtime(0.3f);

        Vector3 runFrom = s.transform.position;
        Debug.Log($"[FLEET TEST] after threat at {Vector3.Distance(w.transform.position, s.transform.position):F1}m: " +
                  $"state={s.currentState} repairCancelled={!gen.isBeingRepaired} repairers={gen.repairersCount} " +
                  $"fleeSpeed={s.sprintSpeed:F1}");

        yield return new WaitForSecondsRealtime(1.5f);
        float ran = Vector3.Distance(runFrom, s.transform.position);
        Debug.Log($"[FLEET TEST] flee displacement in 1.5s={ran:F2}m (expected >3m) state={s.currentState}");
        yield return Shot("fleettest_01_flee");

        // Threat gone -> survivor must resume its objective rather than keep fleeing.
        w.transform.position = new Vector3(400f, 0f, 400f);
        float waitSafe = 0f;
        while (waitSafe < 6f && s.currentState == SurvivorState.Flee)
        {
            waitSafe += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"[FLEET TEST] after safe: state={s.currentState} waited={waitSafe:F1}s (must not stay Flee)");
        yield return new WaitForSecondsRealtime(8f);
    }

    /// <summary>
    /// In-place restart: dirties a session (generator done, survivor dead, player
    /// dead), then calls the same entry point the Play Again button uses and
    /// verifies every fresh-start invariant.
    /// </summary>
    private IEnumerator RestartTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.2f);
        Collect();
        LogSessionSnapshot("before-dirty");

        if (_generators.Count > 0 && _generators[0] != null) _generators[0].DebugForceComplete();
        if (_survivors.Count > 0 && _survivors[0] != null) _survivors[0].TakeDamage(200f);
        yield return new WaitForSecondsRealtime(0.4f);
        LogSessionSnapshot("dirtied");

        if (_player != null) _player.TakeDamage(1000f);

        float t = 0f;
        while (t < 10f && GameManager.Instance.currentState != GameState.GameOver)
        {
            t += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"[RESTART TEST] death reached GameOver in {t:F1}s hp={(_player != null ? _player.currentHealth : -1f):F0} " +
                  $"dead={(_player != null && _player.IsDead)} gens={GameManager.Instance.generatorsRepaired} " +
                  $"survivor0={(_survivors.Count > 0 && _survivors[0] != null ? _survivors[0].healthState.ToString() : "?")}");
        yield return new WaitForSecondsRealtime(0.6f);

        GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.4f);
        Collect();

        int alive = 0;
        if (GameManager.Instance.survivors != null)
            foreach (var s in GameManager.Instance.survivors)
                if (s != null && !s.IsDead) alive++;

        Debug.Log($"[RESTART TEST] after reset: state={GameManager.Instance.currentState} timeScale={Time.timeScale:F1} " +
                  $"hp={(_player != null ? _player.currentHealth : -1f):F0}/{(_player != null ? _player.maxHealth : -1f):F0} " +
                  $"dead={(_player != null && _player.IsDead)} pos={(_player != null ? _player.transform.position.ToString() : "null")} " +
                  $"spawn={(_player != null ? _player.SpawnPosition.ToString() : "null")} " +
                  $"gens={GameManager.Instance.generatorsRepaired}/{GameManager.Instance.totalGenerators} " +
                  $"survivorsAlive={alive} survivor0={SurvivorSnapshot()} " +
                  $"witches={CountWitches()} names=[{string.Join(", ", WitchNames().ToArray())}] " +
                  $"deathOverlay={(UIManager.Instance != null && UIManager.Instance.deathOverlay != null && UIManager.Instance.deathOverlay.activeSelf)} " +
                  $"pauseOpen={(UIManager.Instance != null && UIManager.Instance.pauseMenuPanel != null && UIManager.Instance.pauseMenuPanel.activeSelf)}");

        yield return Shot("restarttest_01_fresh");
        yield return new WaitForSecondsRealtime(15f);
    }

    /// <summary>
    /// Chase-mode acceptance run: six monsters at t=0, live countdown, forward
    /// acceleration in +1 steps, timer expiry -> Victory, and a clean restart.
    /// (The older ChaseTestRoutine is the sprint-vs-witch distance test.)
    /// </summary>
    private IEnumerator SurvivalTestRoutine()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[CHASE TEST] no GameManager"); yield break; }

        gm.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.5f);
        Collect();

        Debug.Log($"[CHASE TEST] start: state={gm.currentState} monsters={CountWitches()} " +
                  $"names=[{string.Join(", ", WitchNames().ToArray())}] timer={gm.timeRemaining:F2} " +
                  $"running={gm.timerRunning} player={gm.selectedSurvivor} hp={(_player != null ? _player.currentHealth : -1f):F0}");

        // ---- acceleration: hold forward down a clear road -----------------
        // Monsters are parked for this phase so the run is not cut short by a
        // hit; the chase itself is measured separately below.
        ParkMonsters(true);

        if (_player != null)
        {
            var cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            // Outer ring road at z=48: no props, buildings or cars on it, so a
            // straight 80 m run is genuinely unobstructed.
            _player.transform.position = new Vector3(-40f, 0.2f, 48f);
            _player.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // face +X along the ring
            if (cc != null) cc.enabled = true;
        }

        PlayerController.ForceForwardForQA = true;
        float t = 0f;
        float lastLogged = -1f;
        while (t < 16.5f)
        {
            t += Time.deltaTime;
            if (_player != null && t - lastLogged >= 1f)
            {
                lastLogged = t;
                Debug.Log($"[CHASE TEST] t={t:F1}s speed={_player.moveSpeed:F1} level={_player.AccelerationLevel} " +
                          $"current={_player.CurrentSpeed:F1} pos={_player.transform.position.ToString("F1")}");
            }
            yield return null;
        }
        PlayerController.ForceForwardForQA = false;
        yield return new WaitForSecondsRealtime(1.2f);
        Debug.Log($"[CHASE TEST] after release: speed={(_player != null ? _player.moveSpeed : -1f):F1} " +
                  $"level={(_player != null ? _player.AccelerationLevel : -1)} (must be reset)");

        // ---- chase check: monsters close distance --------------------------
        PlaceMonstersAroundPlayer(26f);
        ParkMonsters(false);
        yield return new WaitForSecondsRealtime(0.4f);
        float d0 = NearestMonsterDistance();
        yield return new WaitForSecondsRealtime(3f);
        float d1 = NearestMonsterDistance();
        Debug.Log($"[CHASE TEST] pursuit: nearest {d0:F1}m -> {d1:F1}m over 3s " +
                  $"states=[{MonsterStates()}] (must decrease)");

        // ---- timer expiry -> Victory --------------------------------------
        ParkMonsters(true);
        float before = gm.timeRemaining;
        // Shorten the LIVE clock, not just the display duration: the tick compares
        // timeRemaining against zero.
        gm.survivalDuration = 60f;
        gm.timeRemaining = 2.5f;
        Debug.Log($"[CHASE TEST] shortening run for expiry check: remaining={before:F1}s -> victory in ~2.5s");

        float w = 0f;
        while (w < 8f && gm.currentState != GameState.Victory) { w += Time.deltaTime; yield return null; }

        Debug.Log($"[CHASE TEST] expiry: state={gm.currentState} (expected Victory) timer={gm.timeRemaining:F2} " +
                  $"running={gm.timerRunning} results={(UIManager.Instance != null && UIManager.Instance.resultsPanel != null && UIManager.Instance.resultsPanel.activeSelf)}");
        yield return Shot("chasetest_01_victory");

        // ---- death must beat the clock -------------------------------------
        gm.survivalDuration = 60f;
        gm.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.2f);
        Collect();
        if (_player != null) _player.TakeDamage(1000f);
        float d = 0f;
        while (d < 6f && gm.currentState != GameState.GameOver) { d += Time.deltaTime; yield return null; }
        Debug.Log($"[CHASE TEST] death: state={gm.currentState} (expected GameOver) running={gm.timerRunning} " +
                  $"timer={gm.timeRemaining:F2} survived={gm.SurvivedSeconds:F2}");

        // ---- try again -----------------------------------------------------
        gm.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.4f);
        Collect();
        Debug.Log($"[CHASE TEST] try again: state={gm.currentState} timer={gm.timeRemaining:F1} running={gm.timerRunning} " +
                  $"hp={(_player != null ? _player.currentHealth : -1f):F0} speed={(_player != null ? _player.moveSpeed : -1f):F1} " +
                  $"level={(_player != null ? _player.AccelerationLevel : -1)} monsters={CountWitches()} " +
                  $"names=[{string.Join(", ", WitchNames().ToArray())}]");
        yield return Shot("chasetest_02_retry");
        yield return new WaitForSecondsRealtime(4f);
    }

    /// <summary>
    /// End-of-run check: repairs all ten generators through the real completion
    /// path, waits for the gate to open on its own, walks the player to the exit
    /// and confirms VICTORY. Then presses Play Again and verifies the fresh
    /// session invariants.
    /// </summary>
    private IEnumerator EscapeTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.4f);
        Collect();

        if (_player == null) { Debug.LogError("[ESCAPE TEST] no player"); yield break; }
        if (_generators.Count != 10) Debug.LogError($"[ESCAPE TEST] expected 10 generators, found {_generators.Count}");

        // Clear the witches so the player survives the walk to the exit.
        ParkAllWitches();

        for (int i = 0; i < _generators.Count; i++)
        {
            if (_generators[i] == null) continue;
            _generators[i].DebugForceComplete();
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.6f);

        // The 10/10 threshold spawns the final witch; clear it too so the walk to
        // the exit is not a fight (combat is covered by -combattest).
        ParkAllWitches();

        var gm = GameManager.Instance;
        var gate = gm != null ? gm.escapeGate : null;
        Debug.Log($"[ESCAPE TEST] after 10/10: state={gm?.currentState} gens={gm?.generatorsRepaired}/{gm?.totalGenerators} " +
                  $"witches={CountWitches()} names=[{string.Join(", ", WitchNames().ToArray())}] " +
                  $"gateFound={(gate != null)} activated={gate?.isActivated} opening={gate?.isOpening} open={gate?.isOpen}");

        if (gate == null) { Debug.LogError("[ESCAPE TEST] no escape gate in the session"); yield break; }

        float waitOpen = 0f;
        while (waitOpen < 10f && !gate.isOpen) { waitOpen += Time.deltaTime; yield return null; }
        Debug.Log($"[ESCAPE TEST] gate open={gate.isOpen} after {waitOpen:F2}s (openTime={gate.openTime:F1})");
        yield return Shot("escapetest_01_gate_open");

        // Walk the player into the exit radius (the door panel is passable now).
        Vector3 exit = gate.transform.position;
        Vector3 approach = exit + Flatten(_player.transform.position - exit) * 2.5f;
        approach.y = _player.transform.position.y;
        TeleportPlayer(approach);
        Debug.Log($"[ESCAPE TEST] player moved to {approach.ToString("F2")} dist={Vector3.Distance(approach, exit):F2} radius={gate.escapeRadius:F1}");

        float waitWin = 0f;
        while (waitWin < 8f && gm.currentState != GameState.Victory) { waitWin += Time.deltaTime; yield return null; }
        Debug.Log($"[ESCAPE TEST] state={gm.currentState} after {waitWin:F2}s in the exit radius (expected Victory) " +
                  $"resultsPanel={(UIManager.Instance != null && UIManager.Instance.resultsPanel != null && UIManager.Instance.resultsPanel.activeSelf)} " +
                  $"timeScale={Time.timeScale:F1} cursorVisible={Cursor.visible} cursorLock={Cursor.lockState}");
        yield return Shot("escapetest_02_victory");

        // Play Again must produce a genuinely fresh run.
        gm.RequestNewGame();
        yield return new WaitForSecondsRealtime(1.4f);
        Collect();
        int alive = 0;
        if (gm.survivors != null)
            foreach (var s in gm.survivors) if (s != null && !s.IsDead) alive++;

        Debug.Log($"[ESCAPE TEST] after Play Again: state={gm.currentState} timeScale={Time.timeScale:F1} " +
                  $"hp={(_player != null ? _player.currentHealth : -1f):F0} dead={(_player != null && _player.IsDead)} " +
                  $"gens={gm.generatorsRepaired}/{gm.totalGenerators} gateActivated={gate.isActivated} gateOpen={gate.isOpen} " +
                  $"witches={CountWitches()} survivorsAlive={alive} " +
                  $"resultsPanel={(UIManager.Instance != null && UIManager.Instance.resultsPanel != null && UIManager.Instance.resultsPanel.activeSelf)} " +
                  $"hudPanel={(UIManager.Instance != null && UIManager.Instance.hudPanel != null && UIManager.Instance.hudPanel.activeSelf)}");
        yield return Shot("escapetest_03_play_again");
        yield return new WaitForSecondsRealtime(6f);
    }

    private void ParkMonsters(bool parked)
    {
        int i = 0;
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
        {
            if (w == null) continue;
            w.enabled = !parked;
            if (parked) w.transform.position = new Vector3(300f, 0f, 300f) + new Vector3(i++ * 3f, 0f, 0f);
        }
    }

    private void PlaceMonstersAroundPlayer(float radius)
    {
        int i = 0;
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
        {
            if (w == null || _player == null) continue;
            float a = i * 60f;
            Vector3 p = _player.transform.position + Quaternion.Euler(0f, a, 0f) * Vector3.forward * radius;
            p.y = _player.transform.position.y;
            w.transform.position = p;
            i++;
        }
    }

    private float NearestMonsterDistance()
    {
        if (_player == null) return -1f;
        float best = float.MaxValue;
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
        {
            if (w == null) continue;
            float d = Vector3.Distance(w.transform.position, _player.transform.position);
            if (d < best) best = d;
        }
        return best == float.MaxValue ? -1f : best;
    }

    private string MonsterStates()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
        {
            if (w == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(w.Name).Append(':').Append(w.currentState);
        }
        return sb.ToString();
    }

    private void ParkAllWitches()
    {
        int i = 0;
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
        {
            if (w == null) continue;
            w.enabled = false;
            w.transform.position = new Vector3(500f, 0f, 500f) + new Vector3(i++ * 3f, 0f, 0f);
        }
    }

    private void TeleportPlayer(Vector3 pos)
    {
        if (_player == null) return;
        var cc = _player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        _player.transform.position = pos;
        if (cc != null) cc.enabled = true;
    }

    /// <summary>
    /// Isolated death check: starts gameplay, drops one witch just outside
    /// attack range of a stationary player, and lets it kill them. Proves the
    /// real damage -> death lock -> DEFEATED -> results chain without edits.
    /// </summary>
    private IEnumerator DeathTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(0.5f);

        // Keep the teammates out of the test: they are valid witch targets, and a
        // fleeing survivor would otherwise pull the witch away from the player.
        if (GameManager.Instance != null && GameManager.Instance.survivors != null)
        {
            int idx = 0;
            foreach (var s in GameManager.Instance.survivors)
            {
                if (s == null) continue;
                Vector3 far = new Vector3(300f, 0f, 300f) + new Vector3(idx * 2f, 0f, 0f);
                if (s.agent != null && s.agent.enabled && s.agent.isOnNavMesh) s.agent.Warp(far);
                else s.transform.position = far;
                idx++;
            }
        }

        WitchAI w = _witches.Count > 0 ? _witches[0] : FindAnyObjectByType<WitchAI>();
        if (w != null && _player != null)
        {
            Vector3 fwd = Flatten(_player.transform.forward);
            Vector3 spot = _player.transform.position + fwd * 4.0f;
            spot.y = _player.transform.position.y;
            w.transform.position = spot;
            w.enabled = true;
            w.chaseSpeed = 6.5f;
            Debug.Log($"[DEATH TEST] witch '{w.Name}' placed {Vector3.Distance(spot, _player.transform.position):F1}m from player");
        }
        Debug.Log("[DEATH TEST] waiting for the witch to kill the player...");

        float t = 0f;
        bool loggedDead = false;
        Vector3 posAtDeath = Vector3.zero;
        bool skillTried = false;
        while (t < 40f)
        {
            t += Time.deltaTime;
            if (_player != null && _player.IsDead && !loggedDead)
            {
                loggedDead = true;
                posAtDeath = _player.transform.position;
                Debug.Log($"[DEATH TEST] player dead at t={t:F1}s hp=0 state={GameManager.Instance?.currentState} pos={posAtDeath}");
            }
            if (loggedDead && !skillTried)
            {
                skillTried = true;
                var sk = _player != null ? _player.skillController : null;
                if (sk != null)
                {
                    sk.ActivateSkill();
                    Debug.Log($"[DEATH TEST] skill press after death -> isSkillActive={sk.isSkillActive} " +
                              $"(must be False) cooldown={sk.currentCooldown:F1}");
                }
            }
            if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.GameOver)
            {
                Vector3 posAtOver = _player != null ? _player.transform.position : Vector3.zero;
                Debug.Log($"[DEATH TEST] GameOver reached at t={t:F1}s (results shown) " +
                          $"movedAfterDeath={Vector3.Distance(posAtDeath, posAtOver):F3}m (no-input lock check)");
                break;
            }
            yield return null;
        }
        Debug.Log($"[DEATH TEST] final: playerDead={_player?.IsDead} gameState={GameManager.Instance?.currentState} " +
                  $"fade={(UIManager.Instance != null && UIManager.Instance.deathFadeImage != null ? UIManager.Instance.deathFadeImage.color.a : -1f):F2}");
        yield return Shot("deathtest_01_results");

        // Keep reporting the live player while the results screen is up so the
        // harness can prove (a) the corpse does not move, (b) PLAY AGAIN really
        // respawns, and (c) WASD works again afterwards.
        if (_player == null) _player = FindAnyObjectByType<PlayerController>();
        LogButtonScreenPoints("results");
        for (int i = 0; i < 40; i++)
        {
            yield return new WaitForSecondsRealtime(1f);
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            Debug.Log($"[DEATH TEST] watch t={i + 1}s state={GameManager.Instance?.currentState} " +
                      $"dead={(_player != null && _player.IsDead)} hp={(_player != null ? _player.currentHealth : -1f):F0} " +
                      $"pos={(_player != null ? _player.transform.position.ToString("F2") : "null")} " +
                      $"mouse={Input.mousePosition.ToString("F0")} " +
                      $"witches={CountWitches()} gens={GameManager.Instance?.generatorsRepaired}");
            // In-engine captures: external X11 grabs of a redirected GL window
            // can return a stale/tiled framebuffer, which looks like duplicated UI.
            if (i == 1 || i == 3 || i == 6 || i == 10)
                yield return Shot($"deathtest_watch_{i + 1}");
        }
    }

    /// <summary>
    /// Isolated repair-time check: parks the player on a generator, pushes the
    /// witches far away, and leaves the player to hold E (driven by the harness).
    /// </summary>
    private IEnumerator RepairTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(0.5f);

        foreach (var w in _witches)
        {
            if (w == null) continue;
            w.transform.position = new Vector3(400f, 0f, 400f);
            w.enabled = false;
        }

        if (_player != null && _generators.Count > 0 && _generators[0] != null)
        {
            var g = _generators[0];
            Vector3 gp = g.transform.position;
            Vector3 dir = Flatten(_player.transform.position - gp);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            Vector3 pp = gp + dir * 2.0f;
            pp.y = gp.y;
            _player.transform.position = pp;
            _player.transform.rotation = Quaternion.LookRotation(Flatten(gp - pp));
            _player.moveSpeed = 0f;
            _player.sprintSpeed = 0f;
            GeneratorController.Verbose = true;
            Debug.Log($"[REPAIR TEST] player parked {Vector3.Distance(pp, gp):F2}m from {g.generatorId} (repairTime={g.repairTime:F1}s)");
        }
        else Debug.LogError("[REPAIR TEST] missing player or generator");

        // Log progress once per second until the harness stops the build.
        float t = 0f, nextLog = 0f;
        while (t < 90f)
        {
            t += Time.deltaTime;
            if (t >= nextLog)
            {
                nextLog += 1f;
                var g = _generators.Count > 0 ? _generators[0] : null;
                var ui = UIManager.Instance;
                if (g != null)
                    Debug.Log($"[REPAIR TEST] t={t:F1}s progress={g.GetProgress() * 100f:F0}% repairing={g.isBeingRepaired} " +
                              $"repairers={g.repairersCount} done={g.IsCompleted()} " +
                              $"promptActive={(ui != null && ui.interactionPrompt != null && ui.interactionPrompt.activeSelf)} " +
                              $"promptText='{(ui != null && ui.interactionText != null ? ui.interactionText.text : "<null>")}'");
            }
            yield return null;
        }
    }

    /// <summary>
    /// Spawn progression check: completes generators one at a time and logs the
    /// live witch roster so 1->2->3->4->5->6 can be read straight from the log.
    /// </summary>
    private IEnumerator ProgressionTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(0.5f);

        // Push the starting witch away so it cannot kill the stationary player
        // during the test.
        foreach (var w in _witches)
            if (w != null) w.transform.position = new Vector3(400f, 0f, 400f);

        var gens = new List<GeneratorController>(_generators);
        Debug.Log($"[PROGRESSION TEST] start live={CountWitches()} names=[{string.Join(", ", WitchNames().ToArray())}]");

        for (int i = 0; i < gens.Count && i < 10; i++)
        {
            if (gens[i] == null) continue;
            gens[i].DebugForceComplete();
            yield return new WaitForSecondsRealtime(1.2f);
            Debug.Log($"[PROGRESSION TEST] after generator {i + 1}: live={CountWitches()} names=[{string.Join(", ", WitchNames().ToArray())}]");
        }
    }

    /// <summary>
    /// Sprint-vs-chase check: starts the witch 10 m behind the player and logs
    /// player speed, witch speed and distance once a second while the harness
    /// holds Shift+W. Distance must grow if sprint (7.0) beats chase (~6.5).
    /// </summary>
    private IEnumerator ChaseTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(0.5f);

        if (enableSprintTest)
        {
            PlayerController.ForceSprintForQA = true;
            Debug.Log("[CHASE TEST] sprint forced on by QA override (Unity ignores XTEST modifier keys)");
        }

        // Park the survivors far away, otherwise the witch targets the nearest
        // survivor instead of the sprinting player and the distance measurement
        // is meaningless.
        foreach (var s in _survivors)
            if (s != null) s.transform.position = new Vector3(220f, 0f, 220f);

        WitchAI w = _witches.Count > 0 ? _witches[0] : FindAnyObjectByType<WitchAI>();
        if (w != null && _player != null)
        {
            Vector3 back = -Flatten(_player.transform.forward);
            Vector3 spot = _player.transform.position + back * 10f;
            spot.y = _player.transform.position.y;
            w.transform.position = spot;
            w.enabled = true;
            Debug.Log($"[CHASE TEST] player walk={_player.moveSpeed} sprint={_player.sprintSpeed} " +
                      $"witch chase={w.ChaseSpeed} startDist={Vector3.Distance(spot, _player.transform.position):F1}");
        }

        float t = 0f, next = 0f;
        Vector3 lastPlayerPos = _player != null ? _player.transform.position : Vector3.zero;
        float lastT = 0f;
        int chaseHits = 0;
        float lastChaseHp = _player != null ? _player.currentHealth : 100f;
        while (t < 12f)
        {
            t += Time.deltaTime;
            if (t >= next)
            {
                next += 1f;
                // CharacterController.velocity is overwritten by the final
                // gravity Move() each frame, so measure real displacement.
                Vector3 pp = _player != null ? _player.transform.position : Vector3.zero;
                float dt = Mathf.Max(0.01f, t - lastT);
                float pdx = pp.x - lastPlayerPos.x, pdz = pp.z - lastPlayerPos.z;
                float ps = Mathf.Sqrt(pdx * pdx + pdz * pdz) / dt;
                lastPlayerPos = pp;
                lastT = t;

                float dist = (w != null && _player != null)
                    ? Vector3.Distance(_player.transform.position, w.transform.position) : -1f;
                float ws = w != null ? w.MeasuredSpeed : 0f;
                float php = _player != null ? _player.currentHealth : -1f;
                if (php < lastChaseHp - 0.01f) { chaseHits++; lastChaseHp = php; }
                bool sprinting = _player != null && _player.IsSprinting;
                if (Input.inputString.Length > 0)
                    Debug.Log($"[INPUT] inputString='{Input.inputString}' (uppercase means Unity knows Shift)");
                Debug.Log($"[CHASE TEST] t={t:F1}s playerSpeed={ps:F2} hits={chaseHits} witchSpeed={ws:F2} " +
                          $"dist={dist:F1} sprinting={sprinting} playerHP={php:F0} " +
                          $"rawLS={Input.GetKey(KeyCode.LeftShift)} rawRS={Input.GetKey(KeyCode.RightShift)} " +
                          $"rawW={Input.GetKey(KeyCode.W)} anyKey={Input.anyKey} " +
                          $"held={DescribeHeldKeys()} " +
                          $"player={pp} witch={(w != null ? w.transform.position.ToString() : "null")}");
            }
            yield return null;
        }
        PlayerController.ForceSprintForQA = false;
    }

    /// <summary>
    /// Survivor state check: forces the three teammates into injured, downed
    /// and dead states, screenshots the live HUD, then confirms a witch will not
    /// target the corpse.
    /// </summary>
    private IEnumerator SurvivorTestRoutine()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(1f);

        var alive = new List<SurvivorAI>(_survivors);
        if (alive.Count >= 1) alive[0].TakeDamage(45f);   // 100 -> 55 = Injured
        if (alive.Count >= 2) alive[1].TakeDamage(80f);   // 100 -> 20 = Downed
        if (alive.Count >= 3) alive[2].TakeDamage(200f);  // dead

        yield return new WaitForSecondsRealtime(0.6f);
        for (int i = 0; i < alive.Count; i++)
            Debug.Log($"[SURVIVOR TEST] row{i} {alive[i].DisplayName} state={alive[i].healthState} " +
                      $"hp={alive[i].currentHealth:F0} active={alive[i].gameObject.activeInHierarchy}");
        yield return Shot("survivortest_01_hud_states");

        // Dead survivor must never be re-targeted.
        if (alive.Count >= 3 && _witches.Count > 0)
        {
            var w = _witches[0];
            Vector3 dp = alive[2].transform.position;
            w.transform.position = dp + new Vector3(0f, 0f, 2.5f);
            w.enabled = true;
            yield return new WaitForSecondsRealtime(1.5f);
            string tname = w.CurrentTarget == null ? "NONE" : (w.CurrentTarget.GetComponent<SurvivorAI>()?.DisplayName ?? w.CurrentTarget.name);
            Debug.Log($"[SURVIVOR TEST] witch target after standing on corpse='{tname}' (must not be {alive[2].DisplayName})");
        }

        // Live survivor must take repeated real hits from one witch and step down
        // through Injured -> Downed instead of dying from a single touch. The
        // victim is pinned (a cornered survivor) because a fleeing survivor runs
        // at 6.0 m/s, which is intentionally fast enough to break line of sight.
        if (alive.Count >= 1 && _witches.Count > 0 && _player != null && alive[0] != null)
        {
            var victim = alive[0];
            var w = _witches[0];
            Vector3 far = _player.transform.position + new Vector3(70f, 0f, 70f);
            far.y = _player.transform.position.y;
            if (victim.agent != null && victim.agent.enabled && victim.agent.isOnNavMesh) victim.agent.Warp(far);
            else victim.transform.position = far;
            victim.moveSpeed = 0f;
            victim.sprintSpeed = 0f;
            if (victim.hybridNav != null) victim.hybridNav.isStopped = true;
            w.enabled = true;
            w.transform.position = far + new Vector3(0f, 0f, 2.6f);

            Debug.Log($"[SURVIVOR TEST] hit-loop start victim={victim.DisplayName} hp={victim.currentHealth:F0} " +
                      $"state={victim.healthState} witch={w.Name} pinned=True");
            float lastVictimHp = victim.currentHealth;
            int victimHits = 0;
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSecondsRealtime(1.2f);
                if (victim.currentHealth < lastVictimHp - 0.01f)
                {
                    victimHits++;
                    Debug.Log($"[SURVIVOR TEST] hit-loop REAL HIT #{victimHits}: {victim.DisplayName} " +
                              $"{lastVictimHp:F0} -> {victim.currentHealth:F0} state={victim.healthState} " +
                              $"attackPhase='{w.AttackPhaseName}'");
                    lastVictimHp = victim.currentHealth;
                }
                string tgt = w.CurrentTarget == null ? "NONE" : (w.CurrentTarget.GetComponent<SurvivorAI>()?.DisplayName ?? w.CurrentTarget.name);
                Debug.Log($"[SURVIVOR TEST] hit-loop t={(i + 1) * 1.2f:F1}s victimHP={victim.currentHealth:F0}/{victim.maxHealth:F0} " +
                          $"state={victim.healthState} behaviour={victim.currentState} witchTarget={tgt} " +
                          $"attackPhase='{w.AttackPhaseName}' hits={victimHits}");
                if (victim.healthState == SurvivorHealthState.Downed || victim.IsDead) break;
            }

            // A downed survivor must be left alone (rescue state), not finished off.
            if (victim.healthState == SurvivorHealthState.Downed)
            {
                yield return new WaitForSecondsRealtime(2.4f);
                string tgt = w.CurrentTarget == null ? "NONE" : (w.CurrentTarget.GetComponent<SurvivorAI>()?.DisplayName ?? w.CurrentTarget.name);
                Debug.Log($"[SURVIVOR TEST] hit-loop downed victim={victim.DisplayName} hp={victim.currentHealth:F0} " +
                          $"witchTarget='{tgt}' (must not keep attacking a downed survivor)");
            }

            // Leave the scene safe for the screenshot.
            w.enabled = false;
            w.transform.position = new Vector3(400f, 0f, 400f);
        }
        yield return new WaitForSecondsRealtime(20f);
    }

    /// <summary>
    /// QA helper: logs where each visible UI button actually lives in screen
    /// pixels, so an external click harness can target it without guessing.
    /// Screen space is measured from the bottom-left, like Unity's Input.mousePosition.
    /// </summary>
    private void LogButtonScreenPoints(string tag)
    {
        var canvasObj = GameObject.Find("UI_Canvas");
        if (canvasObj == null) return;
        var buttons = canvasObj.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            var rt = b.transform as RectTransform;
            if (rt == null) continue;
            Vector2 p = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            var canvas = b.GetComponentInParent<Canvas>();
            var label = b.transform.Find("Text") as RectTransform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 labelScreen = label != null
                ? RectTransformUtility.WorldToScreenPoint(null, label.position)
                : Vector2.zero;
            Debug.Log($"[CLICKPOINT] {tag} button='{b.name}' rtPos=({rt.position.x:F0},{rt.position.y:F0}) " +
                      $"conv=({p.x:F0},{p.y:F0}) label=({labelScreen.x:F0},{labelScreen.y:F0}) " +
                      $"size=({rt.rect.width:F0}x{rt.rect.height:F0}) bl=({corners[0].x:F0},{corners[0].y:F0}) " +
                      $"tr=({corners[2].x:F0},{corners[2].y:F0}) " +
                      $"canvasSF={(canvas != null ? canvas.scaleFactor : -1f):F3} " +
                      $"pixelRect={(canvas != null ? canvas.pixelRect.ToString() : "none")} " +
                      $"screenSize=({Screen.width}x{Screen.height}) active={b.interactable}");
        }
    }

    /// <summary>QA helper: names every KeyCode Unity believes is held right now.</summary>
    private static readonly KeyCode[] _probeKeys =
    {
        KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.CapsLock, KeyCode.Tab, KeyCode.Space,
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.E, KeyCode.Q
    };

    private string DescribeHeldKeys()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _probeKeys.Length; i++)
            if (Input.GetKey(_probeKeys[i])) sb.Append(_probeKeys[i]).Append(' ');
        return sb.Length == 0 ? "none" : sb.ToString().Trim();
    }

    private int CountWitches()
    {
        int n = 0;
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
            if (w != null) n++;
        return n;
    }

    private List<string> WitchNames()
    {
        var names = new List<string>();
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
            if (w != null && w.witchData != null && !names.Contains(w.witchData.witchName))
                names.Add(w.witchData.witchName);
        return names;
    }

    private void Collect()
    {
        _player = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (_player == null) _player = FindAnyObjectByType<PlayerController>();
        _cam = Camera.main;
        _survivors.Clear(); _survivors.AddRange(FindObjectsByType<SurvivorAI>(FindObjectsInactive.Exclude));
        _witches.Clear(); _witches.AddRange(FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude));
        _generators.Clear(); _generators.AddRange(FindObjectsByType<GeneratorController>(FindObjectsInactive.Exclude));
    }

    /// <summary>
    /// The bootstrap placement is authoritative, so it is also what a new session
    /// restores: player and survivors each remember their placed position/rotation.
    /// </summary>
    private void RecordSpawnPoints()
    {
        if (_player != null) _player.RecordSpawnPoint();
        for (int i = 0; i < _survivors.Count; i++)
            if (_survivors[i] != null) _survivors[i].RecordSpawnPoint();
        Debug.Log($"[BOOTSTRAP] spawn points recorded: player={(_player != null)} survivors={_survivors.Count}");
    }

    private string SummaryLine()
    {
        return $"player={(_player != null)} survivors={_survivors.Count} witches={_witches.Count} generators={_generators.Count}";
    }

    // ------------------------------------------------------------- placement

    private float GroundY(Vector3 p, Transform ignore = null)
    {
        RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 30f);
        float best = float.NaN;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (ignore != null && (hits[i].transform == ignore || hits[i].transform.IsChildOf(ignore))) continue;
            if (float.IsNaN(best) || hits[i].point.y > best) best = hits[i].point.y;
        }
        return best;
    }

    private bool IsClear(Vector3 pos)
    {
        Vector3 a = pos + Vector3.up * 0.7f;
        Vector3 b = pos + Vector3.up * 1.5f;
        return !Physics.CheckCapsule(a, b, 0.4f, ~0, QueryTriggerInteraction.Ignore);
    }

    private void SnapPlayerToGround()
    {
        if (_player == null) { _groundRef = 0f; return; }
        Vector3 p = _player.transform.position;
        float gy = GroundY(p, _player.transform);
        if (!float.IsNaN(gy)) p.y = gy;
        _player.transform.position = p;
        _player.transform.rotation = Quaternion.identity;
        _groundRef = p.y;
    }

    /// <summary>
    /// Exactly three AI companions for the current round: every playable survivor
    /// except the one the player picked. The roster is rebuilt from scratch on
    /// each new session, which is what keeps it free of duplicates and stale
    /// references when the player changes character or presses Try Again.
    /// </summary>
    public void RebuildCompanions()
    {
        var gm = GameManager.Instance;
        // OfficialName() maps a previous name to the current one, so an older
        // saved pick still excludes the right survivor from the companion trio.
        string playerName = gm != null && !string.IsNullOrEmpty(gm.selectedSurvivor)
            ? CharacterLore.OfficialName(gm.selectedSurvivor)
            : RuntimeCharacterFactory.SurvivorNames[0];

        // Drop the previous roster first. Deactivate before Destroy so nothing
        // can act during the frame the destruction resolves.
        var old = FindObjectsByType<SurvivorAI>(FindObjectsInactive.Include);
        for (int i = 0; i < old.Length; i++)
        {
            if (old[i] == null) continue;
            old[i].gameObject.SetActive(false);
            Destroy(old[i].gameObject);
        }
        if (gm != null && gm.survivors != null) gm.survivors.Clear();
        _survivors.Clear();

        Vector3 origin = _player != null
            ? _player.transform.position
            : new Vector3(0f, _groundRef, 0f);
        float yaw = _player != null ? _player.transform.eulerAngles.y : 0f;

        var names = RuntimeCharacterFactory.SurvivorNames;
        int made = 0;
        for (int i = 0; i < names.Length && made < 3; i++)
        {
            if (names[i] == playerName) continue;

            // Same procedural body, face and animator the player uses.
            var go = RuntimeCharacterFactory.BuildSurvivor(names[i], origin, yaw);
            go.name = "Companion_" + names[i].Replace(" ", "_");

            var ai = go.AddComponent<SurvivorAI>();
            ai.displayName = names[i];
            ai.isPlayerTeammate = true;
            _survivors.Add(ai);
            made++;
        }

        // PlaceSurvivors picks a clear, spaced spot around the player and
        // re-applies each companion's face portrait.
        PlaceSurvivors();
        for (int i = 0; i < _survivors.Count; i++)
            if (_survivors[i] != null) _survivors[i].RecordSpawnPoint();

        float spawnSpacing = float.MaxValue;
        for (int a = 0; a < _survivors.Count; a++)
            for (int b = a + 1; b < _survivors.Count; b++)
            {
                if (_survivors[a] == null || _survivors[b] == null) continue;
                float d = Vector3.Distance(_survivors[a].transform.position, _survivors[b].transform.position);
                if (d < spawnSpacing) spawnSpacing = d;
            }

        // Old bodies are deactivated before Destroy, so an active-only count
        // proves the rebuild left no duplicates behind.
        int active = FindObjectsByType<SurvivorAI>(FindObjectsInactive.Exclude).Length;

        Debug.Log($"[COMPANION] roster player={playerName} count={made} active={active} " +
                  $"ai=[{string.Join(", ", CompanionNames())}] " +
                  $"spawnSpacing={(spawnSpacing == float.MaxValue ? -1f : spawnSpacing):F1}");
    }

    /// <summary>QA: the live moveSpeed of each companion, e.g. "5.0,5.0,5.0".</summary>
    private string CompanionSpeedSnapshot()
    {
        var parts = new List<string>();
        for (int i = 0; i < _survivors.Count; i++)
            if (_survivors[i] != null) parts.Add(_survivors[i].moveSpeed.ToString("F1"));
        return string.Join(",", parts);
    }

    private List<string> CompanionNames()
    {
        var list = new List<string>();
        for (int i = 0; i < _survivors.Count; i++)
            if (_survivors[i] != null) list.Add(_survivors[i].DisplayName);
        return list;
    }

    private void PlaceSurvivors()
    {
        if (_player == null) return;
        Vector3 fwd = Flatten(_player.transform.forward);
        Vector3 right = Flatten(_player.transform.right);
        Vector3 origin = _player.transform.position;

        Vector3[] candidates =
        {
            fwd * 4.2f + right * -2.6f,
            fwd * 4.2f + right *  2.6f,
            fwd * 5.5f,
            fwd * 3.5f + right * -3.5f,
            fwd * 3.5f + right *  3.5f,
            right * -4.5f, right * 4.5f, fwd * 7f,
        };

        // Freshly created colliders are not in the physics world until the end of
        // the frame, so spacing is enforced against the spots already handed out
        // rather than relying on IsClear alone.
        var used = new List<Vector3>();

        for (int i = 0; i < _survivors.Count; i++)
        {
            var s = _survivors[i];
            if (s == null) continue;
            Vector3 pos = PickSpot(origin, candidates, i, used);
            used.Add(pos);
            s.transform.position = pos;
            Vector3 look = origin - pos; look.y = 0f;
            if (look.sqrMagnitude > 0.01f) s.transform.rotation = Quaternion.LookRotation(look);
            var vis = s.GetComponentInChildren<CharacterVisual>();
            if (vis != null) RuntimeCharacterFactory.ApplyFace(vis);
            Debug.Log($"[BOOTSTRAP] survivor {s.name} -> {pos} (dist {Vector3.Distance(origin, pos):F1})");
        }
    }

    private Vector3 PickSpot(Vector3 origin, Vector3[] candidates, int offset, List<Vector3> used)
    {
        for (int k = 0; k < candidates.Length; k++)
        {
            Vector3 c = origin + candidates[(k + offset) % candidates.Length];
            c.y = _groundRef;
            if (IsClear(c) && !TooCloseToUsed(c, used)) return c;
        }
        // Nothing fully clear: take the first spot that at least does not overlap
        // another companion, so bodies never spawn inside each other.
        for (int k = 0; k < candidates.Length; k++)
        {
            Vector3 c = origin + candidates[(k + offset) % candidates.Length];
            c.y = _groundRef;
            if (!TooCloseToUsed(c, used)) return c;
        }
        Vector3 f = origin + candidates[offset % candidates.Length]; f.y = _groundRef; return f;
    }

    private static bool TooCloseToUsed(Vector3 p, List<Vector3> used)
    {
        for (int i = 0; i < used.Count; i++)
        {
            Vector3 d = p - used[i];
            d.y = 0f;
            if (d.sqrMagnitude < 1.6f * 1.6f) return true;
        }
        return false;
    }

    private void FixGenerators()
    {
        if (_generators.Count == 0) return;
        foreach (var g in _generators)
        {
            if (g == null) continue;
            Vector3 p = g.transform.position;
            float gy = GroundY(p, g.transform);
            p.y = (!float.IsNaN(gy) && Mathf.Abs(gy - _groundRef) <= 1.2f) ? gy : _groundRef;
            g.transform.position = p;
        }
        if (_player != null)
        {
            Vector3 fwd = Flatten(_player.transform.forward);
            Vector3 right = Flatten(_player.transform.right);
            Vector3 target = _player.transform.position + fwd * 11f + right * 5f;
            float gy = GroundY(target, null);
            target.y = (!float.IsNaN(gy) && Mathf.Abs(gy - _groundRef) <= 1.2f) ? gy : _groundRef;
            var g0 = _generators[0];
            if (g0 != null) g0.transform.position = target;
        }
    }

    private void FixAllWitches()
    {
        foreach (var w in _witches)
        {
            if (w == null) continue;
            w.survivorLayer = 1 << 0;
            w.obstacleLayer = 1 << 0;
            var vis = w.GetComponentInChildren<CharacterVisual>();
            if (vis != null)
            {
                if (vis.headTransform != null) w.headTransform = vis.headTransform;
                if (vis.faceRenderer != null) w.faceRenderer = vis.faceRenderer;
                RuntimeCharacterFactory.ApplyFace(vis);
            }
        }
    }

    private static Vector3 Flatten(Vector3 v) { v.y = 0f; return v.normalized; }

    // -------------------------------------------------------- verification

    private void LogWitchStatus(string tag)
    {
        Debug.Log($"[WITCH HUNT] ----- {tag} -----  count={_witches.Count}");
        if (_player == null) return;
        for (int i = 0; i < _witches.Count; i++)
        {
            var w = _witches[i];
            if (w == null) continue;
            float dist = Vector3.Distance(_player.transform.position, w.transform.position);
            string tgt = w.CurrentTarget == null ? "NONE" : w.CurrentTarget.name;
            var anim = w.GetComponentInChildren<ProceduralAnimator>();
            Debug.Log($"[WITCH HUNT] {w.witchData?.witchName ?? w.name}: state={w.currentState} target={tgt} distToPlayer={dist:F0} movedLastSec={w.DistanceMovedLastSecond:F1} animSpeed={(anim != null ? anim.speed : 0f):F1} pos={w.transform.position}");
        }
    }

    /// <param name="requireFraming">
    /// True only for the at-spawn pass, where the camera is deliberately aimed
    /// at the survivor arc. The end-of-run re-check happens at whatever angle
    /// the player ended on, so it validates existence/bounds/grounding instead
    /// of asserting that every actor happens to be in the frustum.
    /// </param>
    private void LogVisualAcceptance(bool requireFraming)
    {
        Debug.Log($"[VISUAL ACCEPT] ===== VISUAL ACCEPTANCE TEST (framing={requireFraming}) =====");
        if (_cam == null) _cam = Camera.main;
        int pass = 0, fail = 0;
        CheckPlayer(ref pass, ref fail);
        for (int i = 0; i < _survivors.Count; i++)
        {
            bool inFrontRequired = requireFraming && _survivors[i] != null && !_survivors[i].IsDead;
            CheckEntity("SURVIVOR " + (i + 1), _survivors[i]?.gameObject,
                _survivors[i] != null ? _survivors[i].transform.position : Vector3.zero,
                ref pass, ref fail, inFrontRequired, 45f);
        }
        for (int i = 0; i < _witches.Count; i++)
            CheckEntity("CHASER " + (i + 1) + " " + (_witches[i]?.witchData?.witchName ?? ""), _witches[i]?.gameObject, _witches[i] != null ? _witches[i].transform.position : Vector3.zero, ref pass, ref fail, false, 200f);
        for (int i = 0; i < _generators.Count; i++)
            CheckEntity("GENERATOR " + (i + 1), _generators[i]?.gameObject, _generators[i] != null ? _generators[i].transform.position : Vector3.zero, ref pass, ref fail, false, 45f);
        Debug.Log($"[VISUAL ACCEPT] ===== RESULT: {pass} pass / {fail} fail =====");
    }

    private void CheckPlayer(ref int pass, ref int fail)
    {
        if (_player == null || _cam == null) { Debug.Log("[VISUAL ACCEPT] PLAYER: FAIL"); fail++; return; }
        Vector3 p = _player.transform.position;
        bool grounded = Mathf.Abs(p.y - _groundRef) < 1f;
        Debug.Log($"[VISUAL ACCEPT] PLAYER: {(grounded ? "PASS" : "FAIL")} pos={p} grounded={grounded}");
        if (grounded) pass++; else fail++;
    }

    private void CheckEntity(string label, GameObject go, Vector3 pos, ref int pass, ref int fail, bool requireFront, float maxDist)
    {
        if (go == null) { Debug.Log($"[VISUAL ACCEPT] {label}: FAIL (missing)"); fail++; return; }
        var rends = go.GetComponentsInChildren<Renderer>();
        int enabledRend = 0; Bounds b = new Bounds(pos, Vector3.zero); bool first = true;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null || !rends[i].enabled) continue;
            enabledRend++;
            if (first) { b = rends[i].bounds; first = false; } else b.Encapsulate(rends[i].bounds);
        }
        bool hasBounds = !first && b.size.sqrMagnitude > 0.02f;
        float dist = _cam != null ? Vector3.Distance(_cam.transform.position, b.center) : -1f;
        bool inFront = false;
        if (_cam != null)
        {
            Vector3 dir = (b.center - _cam.transform.position).normalized;
            inFront = Vector3.Dot(_cam.transform.forward, dir) > 0.2f;
        }
        bool onGround = Mathf.Abs(pos.y - _groundRef) < 2.5f;
        bool near = dist > 0f && dist <= maxDist;
        bool ok = enabledRend > 0 && hasBounds && onGround && near && (!requireFront || inFront);
        // largest single child renderer (diagnostic for runaway bounds)
        float biggest = 0f; string biggestName = "";
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null || !rends[i].enabled) continue;
            float sz = rends[i].bounds.size.magnitude;
            if (sz > biggest) { biggest = sz; biggestName = rends[i].gameObject.name; }
        }
        Debug.Log($"[VISUAL ACCEPT] {label}: {(ok ? "PASS" : "FAIL")} rends={enabledRend} bounds={b.size} biggest={biggest:F1}('{biggestName}') dist={dist:F1} inFront={inFront} onGround={onGround}");
        if (ok) pass++; else fail++;
    }

    // ---------------------------------------------------------- visual test

    private IEnumerator KeepPlayerAliveForShots()
    {
        while (true)
        {
            if (_player != null && !_player.IsDead)
                _player.currentHealth = _player.maxHealth;
            yield return null;
        }
    }

    private IEnumerator VisualTestRoutine()
    {
        var cc = _player != null ? _player.GetComponentInChildren<CameraController>() : null;
        if (cc != null) cc.mouseSensitivity = 0f;
        // Keep the demo player alive so the witches keep hunting for the whole
        // test. Health is topped up every frame instead of inflated, so the HUD
        // still shows the real 100/100 instead of a test-only 1000000.
        if (_player != null) { _player.maxHealth = 100f; _player.currentHealth = 100f; }
        StartCoroutine(KeepPlayerAliveForShots());

        yield return new WaitForSecondsRealtime(2f);
        yield return Shot("00_main_menu");

        if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        yield return new WaitForSecondsRealtime(1.2f);

        if (_player != null) { _player.moveSpeed = 0f; _player.sprintSpeed = 0f; } // deterministic framing

        yield return Shot("01_gameplay");

        LookAt(_survivors.Count > 0 && _survivors[0] != null ? _survivors[0].transform.position : Vector3.zero);
        yield return Shot("02_survivors");

        // one close-up per witch type (proves they are visually distinct)
        yield return WitchCloseupByName("Rona Tanyel", "03a_mara");
        yield return WitchCloseupByName("Demonisa Belle", "03b_seraphine");
        yield return WitchCloseupByName("Kyla Ni-Garcia", "03c_morrow");
        yield return WitchCloseupByName("Jannak", "03d_vespera");
        yield return WitchCloseupByName("Sandy Vill", "03e_mothergrin");
        // second frame of the same witch 0.2s later -> limbs must differ (animating)
        yield return new WaitForSecondsRealtime(0.2f);
        yield return WitchCloseupByName("Sandy Vill", "03e2_mothergrin_anim");
        yield return WitchCloseupByName("Beamoth", "03f_evelyn");

        // Side profiles: proves Kyla is a four-legged hound and Demonisa is an
        // airborne winged torso with no walking legs.
        yield return WitchProfileShot("Kyla Ni-Garcia", "04c_morrow_profile");
        yield return WitchProfileShot("Demonisa Belle", "04d_seraphine_profile");

        // unambiguous face-texture proof (external image vs procedural default)
        yield return FaceCheckShot("Rona Tanyel", true, "08a_facecheck_mara");
        yield return FaceCheckShot("Luis Padilla", false, "08b_facecheck_ethan");
        yield return FaceCheckShot("Sandy Vill", true, "08c_facecheck_mothergrin");

        LookAt(_witches.Count > 0 && _witches[0] != null ? _witches[0].transform.position : Vector3.zero);
        yield return Shot("03_chaser");

        yield return AttackShotRoutine("03g_attack_windup", "03h_attack_impact");

        // player legs (look down)
        if (_cam != null)
        {
            _cam.transform.localRotation = Quaternion.Euler(74f, 0f, 0f);
            yield return Shot("04a_player_legs");
            _cam.transform.localRotation = Quaternion.identity;
        }

        LookAt(_generators.Count > 0 && _generators[0] != null ? _generators[0].transform.position : Vector3.zero);
        yield return Shot("04_generator");

        // overview of all witches spread around the player
        yield return OverviewShot("05_witches_overview");

        // Phase Shift VFX
        if (_player != null)
        {
            var sk = _player.GetComponent<PlayerSkillController>();
            if (sk != null) sk.ActivateSkill();
        }
        yield return new WaitForSecondsRealtime(0.35f);
        yield return Shot("06_phase_shift");

        if (PauseController.Instance != null) PauseController.Instance.Pause();
        yield return new WaitForSecondsRealtime(0.4f);
        yield return Shot("07_pause");
        if (PauseController.Instance != null) PauseController.Instance.Resume();

        LogWitchStatus("after-shots");
        yield return new WaitForSecondsRealtime(6f);
        LogWitchStatus("t+6s");
        LogVisualAcceptance(false);

        yield return new WaitForSecondsRealtime(0.8f);
        Debug.Log("[VISUAL TEST] complete, quitting");
        Application.Quit();
    }

    private IEnumerator WitchCloseupByName(string witchName, string shotName)
    {
        WitchAI w = _witches.Find(x => x != null && x.witchData != null && x.witchData.witchName == witchName);
        if (w == null || _player == null) yield break;

        // Move the witch to a clear spot in front of the player so framing is
        // never blocked by street clutter, hold the AI, then restore it.
        Vector3 originalPos = w.transform.position;
        Quaternion originalRot = w.transform.rotation;
        bool wasEnabled = w.enabled;
        w.enabled = false;

        Vector3 fwd = Flatten(_player.transform.forward);
        Vector3 spot = _player.transform.position + fwd * 4.6f;
        // Keep the flying witch visibly airborne even with her AI held.
        spot.y = GroundY(spot) + Mathf.Max(0f, w.hoverHeight);
        w.transform.position = spot;
        Vector3 look = _player.transform.position - spot; look.y = 0f;
        if (look.sqrMagnitude > 0.01f) w.transform.rotation = Quaternion.LookRotation(look);
        yield return null;

        // Frame the whole silhouette (wings / four legs / tall body), not a
        // guessed body height, and pitch the camera to its centre.
        Vector3 aim = spot + Vector3.up * 1.2f;
        Renderer[] rends = w.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            aim = b.center;
        }

        LookAt(new Vector3(aim.x, _player.transform.position.y, aim.z));
        if (_cam != null)
        {
            Vector3 camPos = _cam.transform.position;
            float dh = Vector2.Distance(new Vector2(camPos.x, camPos.z), new Vector2(aim.x, aim.z));
            float pitch = -Mathf.Atan2(aim.y - camPos.y, Mathf.Max(0.1f, dh)) * Mathf.Rad2Deg;
            _cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
        yield return new WaitForEndOfFrame();
        yield return Shot(shotName);

        w.transform.position = originalPos;
        w.transform.rotation = originalRot;
        w.enabled = wasEnabled;
    }

    /// <summary>
    /// Side-on framing: the head-on closeup cannot prove a quadruped has four
    /// legs (the far pair is hidden) or that the flying witch has no walking
    /// legs. Rotating the rig 90 degrees shows the real silhouette.
    /// </summary>
    private IEnumerator WitchProfileShot(string witchName, string shotName)
    {
        WitchAI w = _witches.Find(x => x != null && x.witchData != null && x.witchData.witchName == witchName);
        if (w == null || _player == null) yield break;

        Vector3 originalPos = w.transform.position;
        Quaternion originalRot = w.transform.rotation;
        bool wasEnabled = w.enabled;
        w.enabled = false;

        Vector3 fwd = Flatten(_player.transform.forward);
        Vector3 spot = _player.transform.position + fwd * 5.2f;
        spot.y = GroundY(spot) + Mathf.Max(0f, w.hoverHeight);
        w.transform.position = spot;
        // Face +X so the camera sees a clean side profile.
        w.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, fwd));
        yield return null;

        Vector3 aim = spot + Vector3.up * 1.2f;
        Renderer[] rends = w.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            aim = b.center;
        }
        LookAt(new Vector3(aim.x, _player.transform.position.y, aim.z));
        if (_cam != null)
        {
            Vector3 camPos = _cam.transform.position;
            float dh = Vector2.Distance(new Vector2(camPos.x, camPos.z), new Vector2(aim.x, aim.z));
            float pitch = -Mathf.Atan2(aim.y - camPos.y, Mathf.Max(0.1f, dh)) * Mathf.Rad2Deg;
            _cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
        yield return new WaitForEndOfFrame();
        yield return Shot(shotName);

        w.transform.position = originalPos;
        w.transform.rotation = originalRot;
        w.enabled = wasEnabled;
    }

    /// <summary>
    /// Captures the attack tell and the impact frame so the wind-up/recovery
    /// sequence can be checked visually, not only from combat timings.
    /// </summary>
    private IEnumerator AttackShotRoutine(string windupShot, string impactShot)
    {
        WitchAI w = null;
        foreach (var cand in _witches)
            if (cand != null && cand.witchData != null && CharacterLore.OfficialName(cand.witchData.witchName) == "Rona Tanyel") { w = cand; break; }
        if (w == null || _player == null) w = _witches.Count > 0 ? _witches[0] : null;
        if (w == null || _player == null || _cam == null) yield break;

        Vector3 oldPos = w.transform.position;
        bool wasEnabled = w.enabled;
        w.enabled = true;

        Vector3 fwd = Flatten(_player.transform.forward);
        Vector3 spot = _player.transform.position + fwd * (w.AttackRange + 0.05f);
        spot.y = GroundY(spot);
        w.transform.position = spot;
        LookAt(spot + Vector3.up * 1.3f);
        _cam.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);

        float hpBefore = _player.currentHealth;

        float wait = 0f;
        while (wait < 4f && w.AttackPhaseName != "Windup")
        {
            wait += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.12f);
        yield return Shot(windupShot);

        float waitImpact = 0f;
        while (waitImpact < 2f && w.AttackPhaseName != "Recovery")
        {
            waitImpact += Time.deltaTime;
            yield return null;
        }
        yield return Shot(impactShot);
        Debug.Log($"[VISUAL TEST] attack shots: windupWait={wait:F2}s phase='{w.AttackPhaseName}' " +
                  $"hpBefore={hpBefore:F0} hpAfter={_player.currentHealth:F0}");

        w.transform.position = oldPos;
        w.enabled = wasEnabled;
    }

    /// <summary>Shows the actual face texture used for a character, filling the view.</summary>
    private IEnumerator FaceCheckShot(string displayName, bool isWitch, string shotName)
    {
        Texture2D tex = RuntimeCharacterFactory.ResolveFace(isWitch ? null : displayName, isWitch ? displayName : null,
            isWitch ? RuntimeCharacterFactory.WitchTypeFromName(displayName) : WitchType.Humanoid);
        if (tex == null || _cam == null) yield break;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "FaceCheck";
        var col = quad.GetComponent<Collider>(); if (col != null) Destroy(col);
        var sh = Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
        var mat = new Material(sh);
        if (mat.HasProperty("_MainTex")) mat.mainTexture = tex;
        quad.GetComponent<Renderer>().sharedMaterial = mat;
        quad.transform.SetParent(_cam.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0f, 1.2f);
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = Vector3.one * 1.1f;
        yield return Shot(shotName);
        Destroy(quad);
    }

    private IEnumerator OverviewShot(string name)
    {
        if (_player == null) yield break;
        var camGO = new GameObject("OverviewCam");
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.cullingMask = ~(1 << 31);
        cam.clearFlags = CameraClearFlags.Skybox;
        Vector3 o = _player.transform.position;
        camGO.transform.position = o + new Vector3(0f, 20f, -12f);
        camGO.transform.LookAt(o + Vector3.up * 2.5f);

        var main = Camera.main;
        if (main != null) main.enabled = false;
        yield return new WaitForEndOfFrame();
        yield return Shot(name);
        if (main != null) main.enabled = true;
        Object.Destroy(camGO);
    }

    private void LookAt(Vector3 target)
    {
        if (_player == null || _cam == null) return;
        Vector3 dir = target - _cam.transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) _player.transform.rotation = Quaternion.LookRotation(dir);
        _cam.transform.localRotation = Quaternion.identity;
    }

    private IEnumerator Shot(string name)
    {
        try
        {
            Directory.CreateDirectory(shotDir);
            string path = Path.Combine(shotDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[VISUAL TEST] screenshot -> " + path);
        }
        catch (System.Exception e) { Debug.LogError("[VISUAL TEST] screenshot failed: " + e.Message); }
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSecondsRealtime(0.15f);
    }
}
