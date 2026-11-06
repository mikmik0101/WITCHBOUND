using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("HUD")]
    public GameObject hudPanel;
    public Image healthBar;
    public Text healthText;
    public Image skillIcon;
    public Image skillCooldownOverlay;
    public Text skillCooldownText;
    public Text generatorProgressText;
    public Text generatorsRemainingText;
    public Text objectiveText;
    public Text skillText;
    public GameObject[] teammateIndicators;
    public Image[] teammateHealthBars;
    public Text[] teammateTexts;
    public Image[] teammateDots;
    
    [Header("Chase HUD")]
    public Text survivalText;
    public Text speedText;
    public GameObject threatWarning;

    [Header("Interaction")]
    public GameObject interactionPrompt;
    public Text interactionText;
    
    [Header("Skill")]
    public GameObject skillActivatedEffect;
    public float skillFlashDuration = 0.5f;
    
    [Header("Damage")]
    public GameObject damageVignette;
    public float damageFlashDuration = 0.3f;

    [Header("Blood screen")]
    /// <summary>Edge-weighted red wash that stays on while health is critical.</summary>
    public Image bloodVignette;
    public float bloodFlashPeak = 0.55f;

    private float _bloodTarget;
    private float _bloodCurrent;
    private static Sprite _vignetteSprite;
    
    [Header("Menus")]
    public GameObject mainMenuPanel;
    public GameObject characterSelectPanel;
    public GameObject pauseMenuPanel;
    public GameObject resultsPanel;
    public GameObject settingsPanel;
    public GameObject skillsPanel;
    public GameObject upgradePanel;
    public GameObject libraryPanel;
    
    [Header("Results")]
    public Text resultTitleText;
    public Text resultTimeText;
    public Text resultMonstersText;
    public Text resultHintText;
    // Legacy fields kept so older bindings do not break.
    public Text generatorsRepairedText;
    public Text survivorsRescuedText;
    public Text survivorsEscapedText;
    public Text coinsEarnedText;
    public Text bonusText;
    
    [Header("Death")]
    public GameObject deathOverlay;
    public Image deathFadeImage;
    public Text deathText;
    
    private Coroutine interactionPromptCoroutine;
    private Coroutine damageFlashCoroutine;
    
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
        Debug.Log("[UI] UIManager.Start - GameManager.Instance=" + (GameManager.Instance != null));
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGeneratorRepairedEvent += OnGeneratorRepaired;
            GameManager.Instance.OnGameStateChangedEvent += OnGameStateChanged;
        }
        
        // ObjectiveManager is a generator-era system and is intentionally not
        // subscribed: the chase HUD has a fixed objective line.
        
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChangedEvent += OnCoinsChanged;
        }
        
        ShowMainMenu();
        
        // Attach menu button callbacks at runtime (closures do not serialize
        // into the saved scene, so the built player has none otherwise).
        MenuButtonWiring.WireAll();

        // Bind runtime-generated HUD widgets to UIManager fields.
        HudWiring.Wire(this);

        // Floating names over companions and witches (gameplay only).
        CharacterNameTags.EnsureExists();

        // The launch prologue runs once per process and covers the menu until
        // it is skipped or finishes; PrologueController re-shows the menu.
        bool prologue = PrologueController.ShowOnLaunch();
        if (prologue && mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        Debug.Log($"[UI] prologue={prologue} menuPanel={(mainMenuPanel != null)} menuActive={(mainMenuPanel != null && mainMenuPanel.activeSelf)}");

        // Survivors register in Start() too, and Start order is arbitrary, so a
        // cheap periodic refresh guarantees the teammate rows always reflect the
        // real HEALTHY/INJURED/DOWNED/DEAD state (never a stale static label).
        StartCoroutine(TeammateHudRoutine());
        StartCoroutine(ChaseHudRoutine());
    }

    /// <summary>
    /// Cheap 5 Hz refresh for the two values that change without an event:
    /// live movement speed and "a monster is close" proximity warning.
    /// </summary>
    private IEnumerator ChaseHudRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.2f);

            var gm = GameManager.Instance;
            if (gm == null) continue;

            if (gm.currentState != GameState.Gameplay)
            {
                SetThreatWarning(false);
                continue;
            }

            var p = gm.player;
            if (p == null) continue;

            UpdateSpeed(p.CurrentSpeed, p.AccelerationLevel);

            float nearest = float.MaxValue;
            if (gm.activeWitches != null)
            {
                for (int i = 0; i < gm.activeWitches.Count; i++)
                {
                    var w = gm.activeWitches[i];
                    if (w == null || !w.gameObject.activeInHierarchy) continue;
                    float d = Vector3.Distance(p.transform.position, w.transform.position);
                    if (d < nearest) nearest = d;
                }
            }

            SetThreatWarning(!p.IsDead && nearest < 14f);
        }
    }

    private IEnumerator TeammateHudRoutine()
    {
        while (true)
        {
            RefreshTeammateHud();
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    public void RefreshTeammateHud()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.survivors == null) return;

        for (int i = 0; i < gm.survivors.Count && i < 8; i++)
        {
            var s = gm.survivors[i];
            if (s == null) continue;
            float pct = s.maxHealth > 0f ? Mathf.Clamp01(s.currentHealth / s.maxHealth) : 0f;
            UpdateTeammateStatus(i, s.DisplayName, s.healthState, pct);
        }
    }
    // ESC/pause input is owned exclusively by PauseController so it can be read
    // independently of Time.timeScale. No ESC handling belongs here.
    
    public void PauseGame()
    {
        // PauseController owns the pause flag; pausing behind its back left ESC
        // toggling the wrong way on the next press.
        if (PauseController.Instance != null)
        {
            PauseController.Instance.Pause();
            return;
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f;
            if (InputStateManager.Instance != null)
            {
                InputStateManager.Instance.SetMode(InputMode.Pause);
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentState = GameState.Gameplay; // Keep track
            }
            Debug.Log("[PAUSE] Game paused");
        }
    }
    
    private void OnGeneratorRepaired(int current, int total)
    {
        UpdateGeneratorUI(current, total);
    }
    
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameState.CharacterSelect:
                ShowCharacterSelect();
                break;
            case GameState.CharacterLibrary:
                ShowLibrary();
                break;
            case GameState.Gameplay:
                ShowHUD();
                break;
            case GameState.EscapePhase:
                ShowHUD();
                break;
            case GameState.Victory:
                ShowResults(true);
                break;
            case GameState.GameOver:
                ShowResults(false);
                break;
        }
    }
    
    private void OnObjectiveChanged(Objective objective)
    {
        UpdateObjectiveText(objective);
    }
    
    private void OnCoinsChanged(int current, int delta)
    {
    }
    
    public void UpdateHealth(float current, float max)
    {
        if (healthBar != null)
            healthBar.fillAmount = current / max;
        
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
    
    private bool _skillFlashPlayed;

    public void UpdateSkill(SkillData skill, float cooldown, float maxCooldown, bool isActive, float activeRemaining = 0f)
    {
        if (skillText != null)
        {
            string nm = skill != null ? skill.skillName.ToUpperInvariant() : "SKILL";

            // READY / ACTIVE (with time left) / COOLDOWN (with time left).
            string state;
            if (isActive) state = activeRemaining > 0f ? $"ACTIVE  {activeRemaining:F1}s" : "ACTIVE";
            else if (cooldown > 0f) state = $"COOLDOWN  {cooldown:F1}s";
            else state = "READY";

            skillText.text = $"{nm}  [Q]   {state}";
        }

        if (skillCooldownOverlay != null)
        {
            skillCooldownOverlay.fillAmount = maxCooldown > 0f ? Mathf.Clamp01(cooldown / maxCooldown) : 0f;
            skillCooldownOverlay.gameObject.SetActive(cooldown > 0);
        }
        
        if (skillCooldownText != null)
        {
            skillCooldownText.text = cooldown > 0 ? $"{Mathf.CeilToInt(cooldown)}s" : "";
            skillCooldownText.gameObject.SetActive(cooldown > 0);
        }

        // Flash once per activation, not once per frame.
        if (isActive)
        {
            if (!_skillFlashPlayed && skillActivatedEffect != null)
            {
                _skillFlashPlayed = true;
                StartCoroutine(FlashSkillEffect());
            }
        }
        else
        {
            _skillFlashPlayed = false;
        }
    }
    
    private IEnumerator FlashSkillEffect()
    {
        skillActivatedEffect.SetActive(true);
        yield return new WaitForSeconds(skillFlashDuration);
        skillActivatedEffect.SetActive(false);
    }
    
    public void UpdateGeneratorUI(int current, int total)
    {
        if (generatorProgressText != null)
            generatorProgressText.text = $"POWER GRID  {current}/{total}";
        
        if (generatorsRemainingText != null)
            generatorsRemainingText.text = $"{current}/{total}";
    }
    
    /// <summary>Directly set the interaction prompt without coroutines (safe to call every frame).</summary>
    public void SetInteractionPrompt(string text)
    {
        if (interactionPrompt == null) return;
        if (!interactionPrompt.activeSelf) interactionPrompt.SetActive(true);
        if (interactionText != null && interactionText.text != text) interactionText.text = text;
    }
    
    public void UpdateObjectiveText(Objective objective)
    {
        if (objectiveText != null)
        {
            string text = objective switch
            {
                Objective.RepairGenerators => "Repair all generators",
                Objective.Escpape => "Reach the escape gate!",
                Objective.Escaped => "YOU ESCAPED!",
                Objective.Failed => "GAME OVER",
                _ => ""
            };
            objectiveText.text = text;
        }
    }
    
    public void ShowInteractionPrompt(string text, float duration = 0f)
    {
        if (interactionPromptCoroutine != null)
            StopCoroutine(interactionPromptCoroutine);
        
        interactionPromptCoroutine = StartCoroutine(ShowInteractionPromptRoutine(text, duration));
    }
    
    private IEnumerator ShowInteractionPromptRoutine(string text, float duration)
    {
        if (interactionPrompt == null) yield break;

        interactionPrompt.SetActive(true);
        if (interactionText != null) interactionText.text = text;
        
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            interactionPrompt.SetActive(false);
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (interactionPromptCoroutine != null)
            StopCoroutine(interactionPromptCoroutine);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }
    
    public void ShowDamageFlash()
    {
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
    }
    
    private IEnumerator DamageFlashRoutine()
    {
        if (damageVignette == null) yield break;

        // Edge-weighted, so a hit darkens the corners of the screen instead of
        // dropping a flat red sheet over the gameplay.
        var img = damageVignette.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = VignetteSprite();
            img.type = Image.Type.Simple;
            var baseColor = img.color;
            baseColor.a = bloodFlashPeak;
            img.color = baseColor;
        }

        damageVignette.SetActive(true);

        float duration = Mathf.Max(0.18f, damageFlashDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            if (img != null)
            {
                var c = img.color;
                c.a = bloodFlashPeak * k;
                img.color = c;
            }
            yield return null;
        }

        if (img != null)
        {
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
        damageVignette.SetActive(false);
    }

    /// <summary>
    /// Persistent-but-subtle blood at the screen edges while the player is in
    /// bad shape. Fades to nothing as soon as health recovers, and never gets
    /// strong enough to hide the HUD.
    /// </summary>
    public void SetLowHealthBlood(float healthPercent)
    {
        if (healthPercent >= 0.30f)
        {
            _bloodTarget = 0f;
        }
        else
        {
            // Crossing 30 % must be immediately readable, so the wash starts at
            // a visible value instead of fading up from nothing. It then climbs
            // quickly but tops out well below full opacity, and the vignette
            // shape keeps the centre of the screen clear.
            float k = Mathf.InverseLerp(0.30f, 0f, Mathf.Clamp01(healthPercent));
            float curve = Mathf.Pow(k, 0.65f);
            _bloodTarget = Mathf.Lerp(0.22f, 0.46f, curve);
        }

        if (_bloodTarget <= 0.0001f)
        {
            _bloodCurrent = 0f;
            if (bloodVignette != null) bloodVignette.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (_bloodTarget <= 0.0001f && _bloodCurrent <= 0.0001f) return;

        EnsureBloodVignette();
        if (bloodVignette == null) return;

        if (!bloodVignette.gameObject.activeSelf) bloodVignette.gameObject.SetActive(true);

        // Slow pulse so it reads as a heartbeat rather than a static overlay.
        float pulse = 0.86f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.14f;
        _bloodCurrent = Mathf.Lerp(_bloodCurrent, _bloodTarget * pulse, Time.unscaledDeltaTime * 4f);

        var c = bloodVignette.color;
        c.a = _bloodCurrent;
        bloodVignette.color = c;

        if (_bloodTarget <= 0.0001f && _bloodCurrent < 0.004f)
        {
            bloodVignette.gameObject.SetActive(false);
            _bloodCurrent = 0f;
        }
    }

    private void EnsureBloodVignette()
    {
        if (bloodVignette != null) return;

        Transform parent = damageVignette != null ? damageVignette.transform.parent : null;
        if (parent == null) return;

        var go = new GameObject("BloodVignette");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();

        bloodVignette = go.AddComponent<Image>();
        bloodVignette.sprite = VignetteSprite();
        bloodVignette.type = Image.Type.Simple;
        bloodVignette.color = new Color(0.46f, 0.020f, 0.030f, 0f);
        bloodVignette.raycastTarget = false;
        go.SetActive(false);
    }

    /// <summary>Generated once: transparent centre, blood-dark corners.</summary>
    private static Sprite VignetteSprite()
    {
        if (_vignetteSprite != null) return _vignetteSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        tex.name = "BloodVignetteTex";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = (y / (size - 1f)) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (size - 1f)) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny) * 0.7071f;   // 0 centre, 1 corner
                // 2.3 keeps the very centre clean but lets the wash reach far
                // enough in to be noticed at a glance on a dark screen.
                float a = Mathf.Clamp01(Mathf.Pow(d, 2.3f));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);

        _vignetteSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _vignetteSprite;
    }
    
    public void UpdateTeammateStatus(int index, SurvivorHealthState state, float healthPercent)
    {
        UpdateTeammateStatus(index, null, state, healthPercent);
    }

    /// <summary>
    /// Updates one teammate HUD row (name + live health state + bar). Dead
    /// survivors must never read HEALTHY, so the state is the source of truth.
    /// </summary>
    public void UpdateTeammateStatus(int index, string displayName, SurvivorHealthState state, float healthPercent)
    {
        if (index < 0) return;

        if (teammateIndicators != null && index < teammateIndicators.Length &&
            teammateIndicators[index] != null)
            teammateIndicators[index].SetActive(true);

        Color color = state switch
        {
            SurvivorHealthState.Healthy => new Color(0.55f, 0.85f, 0.55f),
            SurvivorHealthState.Injured => new Color(0.95f, 0.80f, 0.30f),
            SurvivorHealthState.Downed => new Color(0.85f, 0.20f, 0.18f),
            SurvivorHealthState.Dead => new Color(0.45f, 0.45f, 0.45f),
            _ => Color.white
        };

        if (teammateHealthBars != null && index < teammateHealthBars.Length && teammateHealthBars[index] != null)
        {
            teammateHealthBars[index].fillAmount = Mathf.Clamp01(healthPercent);
            teammateHealthBars[index].color = color;
        }

        // The status dot must match the state too - a green dot next to DEAD
        // reads as a bug even when the label and bar are correct.
        if (teammateDots != null && index < teammateDots.Length && teammateDots[index] != null)
            teammateDots[index].color = color;

        if (teammateTexts != null && index < teammateTexts.Length && teammateTexts[index] != null)
        {
            string label = string.IsNullOrEmpty(displayName)
                ? $"SURVIVOR {index + 1:00}"
                : displayName.ToUpperInvariant();
            teammateTexts[index].text = $"{label}  //  {state.ToString().ToUpperInvariant()}";
            teammateTexts[index].color = color;
        }
    }

    // --------------------------------------------------------------- death

    /// <summary>0 = clear, 1 = fully black. Used by PlayerController.DeathSequence.</summary>
    public void SetDeathFade(float alpha)
    {
        if (deathOverlay != null && !deathOverlay.activeSelf)
            deathOverlay.SetActive(true);

        if (deathFadeImage != null)
        {
            var c = deathFadeImage.color;
            c.a = Mathf.Clamp01(alpha);
            deathFadeImage.color = c;
        }
    }

    public void ShowDeathText(bool show)
    {
        if (deathText != null)
            deathText.gameObject.SetActive(show);
    }

    public void ResetDeathOverlay()
    {
        if (deathOverlay != null) deathOverlay.SetActive(false);
        if (deathText != null) deathText.gameObject.SetActive(false);
        if (deathFadeImage != null)
        {
            var c = deathFadeImage.color;
            c.a = 0f;
            deathFadeImage.color = c;
        }
    }
    
    public void ShowMainMenu()
    {
        SetActivePanel(mainMenuPanel);
    }
    
    public void ShowCharacterSelect()
    {
        SetActivePanel(characterSelectPanel);
    }
    
    public void ShowHUD()
    {
        ResetDeathOverlay();
        SetActivePanel(hudPanel);
        SetThreatWarning(false);
        if (objectiveText != null) objectiveText.text = "Survive 2 minutes";
        if (GameManager.Instance != null)
            UpdateSurvivalTimer(GameManager.Instance.timeRemaining, GameManager.Instance.survivalDuration);
    }
    
    public void ShowPauseMenu()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    public void HidePauseMenu()
    {
        // Resume through PauseController so its IsPaused flag clears too. The
        // RESUME button used to leave it set, and the next ESC press was
        // swallowed "resuming" a game that was already running.
        if (PauseController.Instance != null)
        {
            PauseController.Instance.Resume();
            return;
        }

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.SetMode(InputMode.Gameplay);
        }
        Debug.Log("[PAUSE] Game resumed");
    }
    
    public void ShowResults(bool victory)
    {
        ResetDeathOverlay();
        SetActivePanel(resultsPanel);
        SetThreatWarning(false);

        var gm = GameManager.Instance;
        float survived = gm != null ? gm.SurvivedSeconds : 0f;
        float total = gm != null ? gm.survivalDuration : 120f;

        if (resultTitleText != null)
        {
            resultTitleText.text = victory ? "YOU SURVIVED" : "DEFEAT";
            resultTitleText.color = victory
                ? new Color(0.55f, 0.85f, 0.55f)
                : new Color(0.66f, 0.10f, 0.12f);
        }

        if (resultTimeText != null)
        {
            resultTimeText.text = victory
                ? $"Survived: {total:F0} seconds"
                : $"Survived: {survived:F1} of {total:F0} seconds";
        }

        if (resultMonstersText != null)
        {
            int count = 0;
            if (gm != null && gm.activeWitches != null)
                for (int i = 0; i < gm.activeWitches.Count; i++)
                    if (gm.activeWitches[i] != null) count++;
            resultMonstersText.text = $"Monsters hunting: {count}";
        }

        if (resultHintText != null)
        {
            resultHintText.text = victory
                ? "You outlasted Ashwood. For now."
                : "The town keeps what it catches.";
        }
    }
    
    public void ShowLibrary()
    {
        SetActivePanel(libraryPanel);
        if (CharacterLibraryUI.Instance != null) CharacterLibraryUI.Instance.Show();
    }

    /// <summary>Countdown readout. Turns red in the last ten seconds.</summary>
    public void UpdateSurvivalTimer(float remaining, float total)
    {
        if (survivalText == null) return;
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        survivalText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        survivalText.color = seconds <= 10
            ? new Color(0.95f, 0.28f, 0.24f)
            : new Color(0.80f, 0.79f, 0.73f);
    }

    /// <summary>Live movement speed and how many +1 steps the run has earned.</summary>
    public void UpdateSpeed(float speed, int level)
    {
        if (speedText == null) return;
        speedText.text = level > 0 ? $"{speed:F1}  x{level + 1}" : $"{speed:F1}";
    }

    public void SetThreatWarning(bool visible)
    {
        if (threatWarning == null) return;
        if (threatWarning.activeSelf != visible) threatWarning.SetActive(visible);
    }

    public void ShowSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }
    
    public void HideSettings()
    {
        settingsPanel.SetActive(false);
    }
    
    public void ShowSkills()
    {
        SetActivePanel(skillsPanel);
    }
    
    public void HideSkills()
    {
        SetActivePanel(mainMenuPanel);
    }
    
    public void ShowUpgrades()
    {
        upgradePanel.SetActive(true);
    }
    
    public void HideUpgrades()
    {
        upgradePanel.SetActive(false);
    }
    
    private void SetActivePanel(GameObject panel)
    {
        mainMenuPanel?.SetActive(false);
        characterSelectPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        resultsPanel?.SetActive(false);
        skillsPanel?.SetActive(false);
        libraryPanel?.SetActive(false);
        
        if (panel != null)
            panel.SetActive(true);
    }
}
