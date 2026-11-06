using UnityEngine;
using UnityEngine.Events;

public class GeneratorController : MonoBehaviour, IInteractable
{
    [Header("Config")]
    public GeneratorData generatorData;
    public string generatorId = "Gen_01";
    public float repairTime = 20f;
    public float interactionRange = 3.5f;

    [Header("State")]
    public float currentProgress = 0f;
    public bool isCompleted = false;
    public bool isBeingRepaired = false;
    public int repairersCount = 0;

    [Header("References")]
    public AudioSource audioSource;
    public ParticleSystem repairParticles;
    public ParticleSystem completionParticles;
    public Light generatorLight;
    public Renderer generatorRenderer;
    public Renderer indicatorRenderer;
    public GeneratorBeacon beacon;

    [Header("Events")]
    public UnityEvent<float> OnProgressChanged;
    public UnityEvent OnRepairStarted;
    public UnityEvent OnRepairInterrupted;
    public UnityEvent OnCompleted;

    /// <summary>QA only: logs repair start / progress milestones / duration.</summary>
    public static bool Verbose = false;

    private float repairSpeed = 1f;
    private bool playerRepairing = false;
    private float repairStartedAt = -1f;
    private int lastLoggedMilestone = -1;

    // Only the generator the player is actually standing at may drive the
    // interaction prompt. Without this, the other generators' "out of range"
    // branch hides the prompt again every frame.
    private static GeneratorController promptOwner;

    private void Awake()
    {
        if (generatorData != null) repairTime = generatorData.repairTime;
        if (repairTime <= 0f) repairTime = 20f;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (generatorLight != null) generatorLight.enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.RegisterGenerator(this);
    }

    private void Start()
    {
        UpdateVisuals();
        // Awake order is not guaranteed, so a generator that woke before the
        // GameManager missed the first registration. Register again here (the
        // list de-duplicates) so every generator is reset/used by the session.
        if (GameManager.Instance != null) GameManager.Instance.RegisterGenerator(this);
    }

    // Kept for the IInteractable contract (raycast path); the proximity
    // hold-to-repair path in Update() is the authoritative one.
    public void Interact(PlayerController player)
    {
        if (isCompleted) return;
        if (!isBeingRepaired) StartRepair(player);
        else StopRepair(player);
    }

    public void StartRepair(PlayerController player)
    {
        if (isCompleted) return;
        if (!isBeingRepaired)
        {
            isBeingRepaired = true;
            repairStartedAt = Time.time;
            lastLoggedMilestone = -1;
            if (Verbose)
                Debug.Log($"[REPAIR] {generatorId} started at {currentProgress * 100f:F0}%");
        }
        repairersCount++;
        OnRepairStarted?.Invoke();

        if (generatorData != null && generatorData.repairingSound != null && audioSource != null)
        {
            audioSource.clip = generatorData.repairingSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        if (repairParticles != null) repairParticles.Play();
        if (generatorLight != null) generatorLight.enabled = true;
    }

    public void StopRepair(PlayerController player)
    {
        if (!isBeingRepaired) return;
        repairersCount = Mathf.Max(0, repairersCount - 1);
        if (repairersCount == 0)
        {
            isBeingRepaired = false;
            OnRepairInterrupted?.Invoke();
            if (generatorData != null && generatorData.interruptedSound != null && audioSource != null)
                audioSource.PlayOneShot(generatorData.interruptedSound);
            if (repairParticles != null) repairParticles.Stop();
            if (generatorLight != null) generatorLight.enabled = false;
            if (Verbose)
                Debug.Log($"[REPAIR] {generatorId} interrupted at {currentProgress * 100f:F0}% (progress kept)");
        }
    }

    private void Update()
    {
        UpdatePlayerInteraction();

        if (isBeingRepaired && !isCompleted)
        {
            // Progress is always Time.deltaTime based, so the UI moves every frame.
            // A second helper adds a small cooperation bonus but can never make
            // repair instant.
            float cooperation = 1f + 0.35f * Mathf.Max(0, repairersCount - 1);
            float speed = repairSpeed * cooperation;
            currentProgress += (speed * Time.deltaTime) / repairTime;
            currentProgress = Mathf.Clamp01(currentProgress);
            OnProgressChanged?.Invoke(currentProgress);
            UpdateVisuals();

            if (Verbose)
            {
                int milestone = Mathf.FloorToInt(currentProgress * 10f);
                if (milestone != lastLoggedMilestone)
                {
                    lastLoggedMilestone = milestone;
                    Debug.Log($"[REPAIR] {generatorId} progress {currentProgress * 100f:F0}% (repairers={repairersCount})");
                }
            }

            if (currentProgress >= 1f) CompleteGenerator();
        }
    }

    private void UpdatePlayerInteraction()
    {
        var player = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (player == null || isCompleted || player.IsDead)
        {
            if (playerRepairing) { StopRepair(player); playerRepairing = false; }
            if (promptOwner == this)
            {
                promptOwner = null;
                if (UIManager.Instance != null) UIManager.Instance.HideInteractionPrompt();
            }
            return;
        }

        float dist = Vector3.Distance(player.transform.position, transform.position);
        bool inRange = dist <= interactionRange;
        bool holding = inRange && Input.GetKey(KeyCode.E);

        if (holding && !playerRepairing) { StartRepair(player); playerRepairing = true; }
        else if (!holding && playerRepairing) { StopRepair(player); playerRepairing = false; }

        if (inRange && UIManager.Instance != null)
        {
            promptOwner = this;
            string txt = isBeingRepaired
                ? $"REPAIRING...  {Mathf.RoundToInt(currentProgress * 100f)}%"
                : "[ E ]  HOLD TO REPAIR";
            UIManager.Instance.SetInteractionPrompt(txt);
        }
        else if (promptOwner == this)
        {
            promptOwner = null;
            if (UIManager.Instance != null) UIManager.Instance.HideInteractionPrompt();
        }
    }

    private void CompleteGenerator()
    {
        isCompleted = true;
        isBeingRepaired = false;
        playerRepairing = false;
        repairersCount = 0;
        currentProgress = 1f;

        if (Verbose && repairStartedAt >= 0f)
            Debug.Log($"[REPAIR] {generatorId} COMPLETED in {Time.time - repairStartedAt:F1}s of repair");
        repairStartedAt = -1f;

        OnCompleted?.Invoke();

        if (generatorData != null && generatorData.completedSound != null && audioSource != null)
            audioSource.PlayOneShot(generatorData.completedSound);
        if (repairParticles != null) repairParticles.Stop();
        if (completionParticles != null) completionParticles.Play();

        // indicator turns green and the distance beacon switches off
        if (indicatorRenderer != null && indicatorRenderer.sharedMaterial != null && indicatorRenderer.sharedMaterial.HasProperty("_Color"))
            indicatorRenderer.sharedMaterial.color = new Color(0.25f, 1f, 0.35f);
        if (generatorLight != null) { generatorLight.enabled = true; generatorLight.color = new Color(0.3f, 1f, 0.4f); }
        if (beacon != null) beacon.Stop();

        // brief completion flourish
        SkillVFX.Spawn(transform.position + Vector3.up * 1f, new Color(0.3f, 1f, 0.45f));

        if (UIManager.Instance != null)
            UIManager.Instance.SetInteractionPrompt("GENERATOR REPAIRED");

        if (GameManager.Instance != null) GameManager.Instance.GeneratorCompleted(this);
        Debug.Log($"[GENERATOR] {generatorId} completed -> {GameManager.Instance?.generatorsRepaired}/{GameManager.Instance?.totalGenerators}");
    }

    private void UpdateVisuals()
    {
        if (indicatorRenderer != null && indicatorRenderer.sharedMaterial != null && indicatorRenderer.sharedMaterial.HasProperty("_Color"))
        {
            if (!isCompleted)
            {
                Color c = Color.Lerp(new Color(1f, 0.20f, 0.15f), new Color(1f, 0.85f, 0.15f), currentProgress);
                indicatorRenderer.sharedMaterial.color = c;
            }
        }
    }

    public float GetProgress() { return currentProgress; }
    public bool IsCompleted() { return isCompleted; }

    /// <summary>QA only (-progressiontest): completes this generator instantly.</summary>
    public void DebugForceComplete()
    {
        if (isCompleted) return;
        isBeingRepaired = true;
        if (repairStartedAt < 0f) repairStartedAt = Time.time;
        currentProgress = 1f;
        CompleteGenerator();
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        isCompleted = currentProgress >= 1f;
        UpdateVisuals();
        OnProgressChanged?.Invoke(currentProgress);
    }

    /// <summary>
    /// Puts this generator back to "unrepaired" for a new session: progress,
    /// repairers, prompt ownership, beacon, lights, particles and audio cleared.
    /// </summary>
    public void ResetForNewSession()
    {
        if (isBeingRepaired && repairersCount > 0) repairersCount = 0;

        isCompleted = false;
        isBeingRepaired = false;
        playerRepairing = false;
        repairersCount = 0;
        currentProgress = 0f;
        repairStartedAt = -1f;
        lastLoggedMilestone = -1;

        if (promptOwner == this)
        {
            promptOwner = null;
            if (UIManager.Instance != null) UIManager.Instance.HideInteractionPrompt();
        }

        if (audioSource != null) audioSource.Stop();
        if (repairParticles != null) repairParticles.Stop();
        if (completionParticles != null) completionParticles.Stop();

        if (generatorLight != null)
        {
            generatorLight.enabled = false;
            generatorLight.color = Color.white;
        }

        if (beacon != null)
        {
            beacon.active = true;
            if (beacon.beaconLight != null) beacon.beaconLight.enabled = true;
            if (beacon.orb != null) beacon.orb.enabled = true;
        }

        UpdateVisuals();
        OnProgressChanged?.Invoke(0f);

        Debug.Log($"[GENERATOR] {generatorId} reset: progress=0% completed=False repairing=False");
    }
}
