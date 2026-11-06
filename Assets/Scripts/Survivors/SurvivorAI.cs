using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class SurvivorAI : MonoBehaviour
{
    [Header("Data")]
    public SurvivorData survivorData;
    
    [Header("Identity")]
    public string displayName = "";

    [Header("State")]
    public SurvivorState currentState = SurvivorState.Idle;
    public SurvivorHealthState healthState = SurvivorHealthState.Healthy;
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    /// <summary>True once the survivor has died (body stays in the world).</summary>
    public bool IsDead => healthState == SurvivorHealthState.Dead;
    public string DisplayName => string.IsNullOrEmpty(displayName)
        ? (survivorData != null && !string.IsNullOrEmpty(survivorData.survivorName) ? survivorData.survivorName : name)
        : displayName;
    
    [Header("Behavior")]
    public float moveSpeed = 3.2f;        // objective pace (contract: 3.0-3.5)
    public float sprintSpeed = 5.4f;      // flee pace (contract: 5.0-6.0)
    public float repairSpeed = 1f;
    public float detectionRange = 20f;    // panic radius (contract: 18-25)
    public float fleeRange = 10f;
    public float rescueRange = 5f;
    
    [Header("Components")]
    public NavMeshAgent agent;
    public HybridNavigation hybridNav;
    public Animator animator;
    public AudioSource audioSource;
    public SkinnedMeshRenderer bodyRenderer;
    private ProceduralAnimator proceduralAnimator;
    
    // Internal
    private GeneratorController currentGenerator;
    private GeneratorController repairingGenerator;
    private Transform fleeTarget;
    private SurvivorAI rescueTargetSurvivor;
    private float stateTimer;
    public bool isPlayerTeammate = true;
    private Coroutine behaviorCoroutine;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool hasSpawnPoint;
    private Quaternion modelInitialRotation;
    private Transform modelRoot;
    
    public delegate void OnStateChanged(SurvivorState newState);
    public event OnStateChanged OnStateChangedEvent;
    
    public delegate void OnRescueComplete();
    public event OnRescueComplete OnRescueCompleteEvent;
    
    public delegate void OnEscapeDelegate();
    public event OnEscapeDelegate OnEscapeEvent;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hybridNav = GetComponent<HybridNavigation>();
        if (hybridNav == null) hybridNav = gameObject.AddComponent<HybridNavigation>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        proceduralAnimator = GetComponentInChildren<ProceduralAnimator>();
        
        if (survivorData != null)
        {
            ApplySurvivorData();
        }
        else
        {
            maxHealth = 100f;
            currentHealth = maxHealth;
        }

        if (string.IsNullOrEmpty(displayName))
            displayName = string.IsNullOrEmpty(name) ? "Survivor" : name;

        // Companion pace is not a separate balance value: it is copied from the
        // player every frame (see SyncSpeedWithPlayer), so the old fixed
        // 3.0-3.5 / 5.0-6.0 clamps are gone - they would fight the ramp.
        detectionRange = Mathf.Clamp(detectionRange < 18f ? 20f : detectionRange, 18f, 25f);

        modelRoot = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (modelRoot != null) modelInitialRotation = modelRoot.localRotation;
    }
    
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSurvivor(this);
        }

        if (!hasSpawnPoint) RecordSpawnPoint();
        
        SetState(SurvivorState.Explore);
        behaviorCoroutine = StartCoroutine(BehaviorRoutine());
        UpdateHudStatus();
    }
    
    private void ApplySurvivorData()
    {
        moveSpeed = survivorData.moveSpeed;
        maxHealth = survivorData.baseHealth > 0f ? survivorData.baseHealth : 100f;
        currentHealth = maxHealth;
    }
    
    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentState != GameState.Gameplay && 
            GameManager.Instance.currentState != GameState.EscapePhase)
            return;

        // A dead survivor never moves, never animates, never repairs.
        if (IsDead) return;

        SyncSpeedWithPlayer();

        stateTimer += Time.deltaTime;
        
        UpdateThreatDetection();
        CheckForThreats();
        CheckForDownedTeammates();
        
        // Run the current state's behaviour every frame. Previously this only ran
        // on state *change*, so survivors froze after reaching their destination
        // and never actually repaired anything.
        ExecuteStateBehavior();
    }
    
    /// <summary>
    /// Companions run at exactly the player's current forward-run speed,
    /// including the +1 step every five seconds of sustained running, so they
    /// can neither fall permanently behind nor outrun the person they escort.
    /// One-way read of PlayerController.RunSpeed; no callbacks, no cycles.
    /// </summary>
    private void SyncSpeedWithPlayer()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;

        float run = gm.player.RunSpeed;
        if (run <= 0.01f) return;

        moveSpeed = run;
        sprintSpeed = run;   // fleeing is not a faster gear than the player's run
    }

    private IEnumerator BehaviorRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (healthState == SurvivorHealthState.Dead)
                yield break;
            
            EvaluatePriorities();
        }
    }
    
    private void EvaluatePriorities()
    {
        // A downed or dead survivor must not re-enter the flee/repair priorities:
        // previously the 1 s priority tick flipped a downed survivor back to Flee
        // and its body started running away while the HUD said "Downed".
        if (healthState == SurvivorHealthState.Downed || healthState == SurvivorHealthState.Dead)
            return;

        if (IsInImmediateDanger())
        {
            SetState(SurvivorState.Flee);
            return;
        }
        
        if (rescueTargetSurvivor != null && CanRescue(rescueTargetSurvivor))
        {
            SetState(SurvivorState.Rescue);
            return;
        }
        
        if (GameManager.Instance.currentState == GameState.EscapePhase)
        {
            SetState(SurvivorState.SeekEscape);
            return;
        }

        // Stay on the generator we already committed to instead of re-rolling the
        // nearest generator every second and starting/stopping repairs.
        if (currentState == SurvivorState.RepairGenerator && currentGenerator != null && !currentGenerator.IsCompleted())
            return;

        var generator = FindSafeGenerator();
        if (generator != null)
        {
            SetState(SurvivorState.RepairGenerator);
            currentGenerator = generator;
            return;
        }
        
        if (isPlayerTeammate && GameManager.Instance.player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, GameManager.Instance.player.transform.position);
            if (distToPlayer > 15f)
            {
                SetState(SurvivorState.Follow);
                return;
            }
        }
        
        SetState(SurvivorState.Explore);
    }
    
    /// <summary>
    /// Per-frame threat check: any live witch inside the panic radius makes the
    /// survivor drop what they are doing and run immediately. Uses hysteresis so
    /// they do not flicker between flee and repair at the edge of the radius.
    /// </summary>
    private void UpdateThreatDetection()
    {
        if (GameManager.Instance == null || IsDead) return;

        float nearest = float.MaxValue;
        Transform threat = null;
        var witches = GameManager.Instance.activeWitches;
        if (witches != null)
        {
            for (int i = 0; i < witches.Count; i++)
            {
                var w = witches[i];
                if (w == null || !w.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(transform.position, w.transform.position);
                if (d < nearest) { nearest = d; threat = w.transform; }
            }
        }

        if (nearest <= detectionRange && threat != null)
        {
            fleeTarget = threat;
            if (currentState != SurvivorState.Flee && currentState != SurvivorState.Downed && currentState != SurvivorState.Dead)
            {
                SetState(SurvivorState.Flee);   // SetState also releases any generator
                Debug.Log($"[SURVIVOR] {DisplayName} detected witch at {nearest:F1}m -> FLEE (repair/interaction cancelled)");
            }
        }
        else if (currentState == SurvivorState.Flee && nearest > detectionRange * 1.25f)
        {
            fleeTarget = null;
            SetState(SurvivorState.Explore);
            Debug.Log($"[SURVIVOR] {DisplayName} safe again (nearest witch {nearest:F1}m) -> resume objective");
        }
    }

    private bool IsInImmediateDanger()
    {
        if (GameManager.Instance == null) return false;
        if (healthState == SurvivorHealthState.Downed || healthState == SurvivorHealthState.Dead) return false;
        var witches = GameManager.Instance.activeWitches;
        if (witches == null) return false;
        for (int i = 0; i < witches.Count; i++)
        {
            var witch = witches[i];
            if (witch == null || !witch.gameObject.activeInHierarchy) continue;
            float dist = Vector3.Distance(transform.position, witch.transform.position);
            if (dist <= detectionRange)
            {
                fleeTarget = witch.transform;
                return true;
            }
        }
        return false;
    }
    
    private void CheckForThreats()
    {
        float navSpeed = hybridNav != null ? hybridNav.velocity.magnitude : 0f;
        if (animator != null)
        {
            animator.SetFloat("Speed", navSpeed);
            animator.SetBool("IsFleeing", currentState == SurvivorState.Flee);
        }
        if (proceduralAnimator != null)
            proceduralAnimator.speed = navSpeed;
    }
    
    private void CheckForDownedTeammates()
    {
        if (GameManager.Instance == null) return;
        
        float closestDist = float.MaxValue;
        SurvivorAI closestDowned = null;
        
        foreach (var survivor in GameManager.Instance.survivors)
        {
            if (survivor != this && survivor.healthState == SurvivorHealthState.Downed)
            {
                float dist = Vector3.Distance(transform.position, survivor.transform.position);
                if (dist < closestDist && dist < rescueRange * 3f)
                {
                    closestDist = dist;
                    closestDowned = survivor;
                }
            }
        }
        
        rescueTargetSurvivor = closestDowned;
    }
    
    private bool CanRescue(SurvivorAI target)
    {
        if (target == null || target.healthState != SurvivorHealthState.Downed)
            return false;
        
        foreach (var witch in GameManager.Instance.activeWitches)
        {
            if (witch != null && (witch.currentState == WitchState.Chase || witch.currentState == WitchState.Attack))
            {
                float dist = Vector3.Distance(target.transform.position, witch.transform.position);
                if (dist < 15f)
                    return false;
            }
        }
        
        return true;
    }
    
    private GeneratorController FindSafeGenerator()
    {
        if (GameManager.Instance == null) return null;
        
        GeneratorController bestGenerator = null;
        float bestScore = float.MaxValue;
        
        foreach (var gen in GameManager.Instance.generators)
        {
            if (gen != null && !gen.IsCompleted() && !gen.isBeingRepaired)
            {
                float dist = Vector3.Distance(transform.position, gen.transform.position);
                float safetyScore = GetSafetyScore(gen.transform.position);
                
                float score = dist + safetyScore * 10f;
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestGenerator = gen;
                }
            }
        }
        
        return bestGenerator;
    }
    
    private float GetSafetyScore(Vector3 position)
    {
        if (GameManager.Instance == null) return 0f;
        
        float score = 0f;
        foreach (var witch in GameManager.Instance.activeWitches)
        {
            if (witch != null)
            {
                float dist = Vector3.Distance(position, witch.transform.position);
                if (dist < 20f)
                    score += (20f - dist) / 20f;
            }
        }
        return score;
    }
    
    private void ExecuteStateBehavior()
    {
        switch (currentState)
        {
            case SurvivorState.Idle:
                hybridNav.isStopped = true;
                break;
            case SurvivorState.Explore:
                Explore();
                break;
            case SurvivorState.Follow:
                FollowPlayer();
                break;
            case SurvivorState.FindGenerator:
                FindAndMoveToGenerator();
                break;
            case SurvivorState.RepairGenerator:
                RepairGenerator();
                break;
            case SurvivorState.Investigate:
                Investigate();
                break;
            case SurvivorState.AvoidWitch:
                AvoidWitch();
                break;
            case SurvivorState.Flee:
                Flee();
                break;
            case SurvivorState.UseSkill:
                UseSkill();
                break;
            case SurvivorState.HelpTeammate:
                HelpTeammate();
                break;
            case SurvivorState.Rescue:
                Rescue();
                break;
            case SurvivorState.Revive:
                Revive();
                break;
            case SurvivorState.SeekEscape:
                SeekEscape();
                break;
            case SurvivorState.Escape:
                Escape();
                break;
            case SurvivorState.Downed:
                if (hybridNav != null) hybridNav.isStopped = true;
                break;
            case SurvivorState.Dead:
                // Keep the body in the world for readability; just stop all AI.
                if (hybridNav != null) hybridNav.isStopped = true;
                if (proceduralAnimator != null) proceduralAnimator.speed = 0f;
                break;
        }
    }
    
    private void Explore()
    {
        hybridNav.speed = moveSpeed;

        if (hybridNav.pathPending || hybridNav.remainingDistance >= 2f) return;

        // The town is generated at runtime and has no baked NavMesh, so the old
        // "only move when NavMesh.SamplePosition succeeds" test never picked a
        // destination and every companion stood still. Sample first, then fall
        // back to the raw point (SimplePathfinding moves without a NavMesh).
        Vector3 anchor = transform.position;
        var gm = GameManager.Instance;
        if (isPlayerTeammate && gm != null && gm.player != null)
            anchor = gm.player.transform.position;   // companions stay with the group

        // Fixed bearing per survivor (120 deg apart) so three bodies never pick
        // the same wander point and walk into each other.
        int slot = gm != null && gm.survivors != null ? gm.survivors.IndexOf(this) : 0;
        float angle = slot * 120f + Random.Range(-30f, 30f);
        float radius = Random.Range(5f, 12f);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;

        Vector3 target = anchor + dir;
        target.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 12f, NavMesh.AllAreas))
            hybridNav.SetDestination(hit.position);
        else
            hybridNav.SetDestination(target);
    }
    
    private void FollowPlayer()
    {
        if (GameManager.Instance?.player == null) return;
        
        hybridNav.speed = moveSpeed;
        hybridNav.SetDestination(GameManager.Instance.player.transform.position);
    }
    
    private void FindAndMoveToGenerator()
    {
        var gen = FindSafeGenerator();
        if (gen != null)
        {
            currentGenerator = gen;
            hybridNav.speed = moveSpeed;
            hybridNav.SetDestination(gen.transform.position);
        }
    }
    
    private void RepairGenerator()
    {
        if (currentGenerator == null || currentGenerator.IsCompleted())
        {
            currentGenerator = null;
            SetState(SurvivorState.FindGenerator);
            return;
        }
        
        float dist = Vector3.Distance(transform.position, currentGenerator.transform.position);

        // Walk to the generator first - only stop and repair once we are close.
        if (dist > 2.5f)
        {
            if (repairingGenerator != null) { repairingGenerator.StopRepair(null); repairingGenerator = null; }
            hybridNav.isStopped = false;
            hybridNav.speed = moveSpeed;
            hybridNav.SetDestination(currentGenerator.transform.position);
            return;
        }

        hybridNav.speed = 0f;
        hybridNav.isStopped = true;
        
        Vector3 lookDir = (currentGenerator.transform.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 5f * Time.deltaTime);

        // Register as a repairer exactly once (idempotent per generator).
        if (repairingGenerator != currentGenerator)
        {
            currentGenerator.StartRepair(null);
            repairingGenerator = currentGenerator;
        }
    }
    
    private void Investigate()
    {
    }
    
    private void AvoidWitch()
    {
    }
    
    private void Flee()
    {
        if (fleeTarget == null)
        {
            SetState(SurvivorState.Explore);
            return;
        }

        if (hybridNav == null) return;
        hybridNav.isStopped = false;
        hybridNav.speed = sprintSpeed;

        Vector3 away = transform.position - fleeTarget.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = transform.forward;
        away.Normalize();

        // Each survivor breaks away on its own bearing (-35/0/+35 deg) instead of
        // all three running down the same line and clumping together.
        var gm = GameManager.Instance;
        int slot = gm != null && gm.survivors != null ? gm.survivors.IndexOf(this) : 0;
        float spread = Mathf.Clamp(slot, 0, 2) * 35f - 35f;
        Vector3 escape = Quaternion.Euler(0f, spread, 0f) * away;
        // Keep the break-away point inside the playable area: aiming 14 m past
        // the boundary made survivors run into the wall and die in the corner.
        Vector3 desired = TownLayout.ClampToPlayable(transform.position + escape * 14f, 4f);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, 14f, NavMesh.AllAreas))
            hybridNav.SetDestination(hit.position);
        else
            hybridNav.SetDestination(desired);
    }
    
    private void UseSkill()
    {
    }
    
    private void HelpTeammate()
    {
    }
    
    private void Rescue()
    {
        if (rescueTargetSurvivor == null || rescueTargetSurvivor.healthState != SurvivorHealthState.Downed)
        {
            rescueTargetSurvivor = null;
            SetState(SurvivorState.Explore);
            return;
        }
        
        hybridNav.speed = moveSpeed;
        hybridNav.SetDestination(rescueTargetSurvivor.transform.position);
        
        if (Vector3.Distance(transform.position, rescueTargetSurvivor.transform.position) < 2f)
        {
            hybridNav.isStopped = true;
            StartCoroutine(PerformRescue());
        }
    }
    
    private IEnumerator PerformRescue()
    {
        float rescueTime = 3f;
        float timer = 0f;
        
        while (timer < rescueTime)
        {
            if (rescueTargetSurvivor == null || rescueTargetSurvivor.healthState != SurvivorHealthState.Downed)
                yield break;
            
            if (IsInImmediateDanger())
            {
                yield break;
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        rescueTargetSurvivor.Revive();
        rescueTargetSurvivor = null;
        OnRescueCompleteEvent?.Invoke();
        SetState(SurvivorState.Explore);
    }
    
    private void Revive()
    {
        currentHealth = Mathf.Max(50f, maxHealth * 0.5f);
        healthState = SurvivorHealthState.Injured;
        UpdateHudStatus();
        SetState(SurvivorState.Explore);
    }
    
    private void SeekEscape()
    {
        if (GameManager.Instance?.escapeGate == null) return;
        
        hybridNav.speed = moveSpeed;
        hybridNav.SetDestination(GameManager.Instance.escapeGate.transform.position);
    }
    
    public void Escape()
    {
        OnEscapeEvent?.Invoke();
        gameObject.SetActive(false);
    }
    
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    /// <summary>
    /// Applies one hit. Survivors need several hits to die, run from whoever hit
    /// them, and drop straight to the correct health band:
    /// Healthy >=60, Injured &lt;60, Downed &lt;=20, Dead 0.
    /// </summary>
    public void TakeDamage(float damage, Transform source)
    {
        if (IsDead || damage <= 0f) return;

        float before = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // Blood feedback for a wounded companion: spray away from the attacker
        // and a small stain under them. Same pooled system as the player, so a
        // six-witch brawl cannot flood the scene.
        Vector3 spray = source != null
            ? transform.position - source.position
            : -transform.forward;
        spray.y = 0.25f;
        BloodVFX.SpawnImpact(transform.position + Vector3.up * 1.05f, spray, 0.85f);
        if (currentHealth <= maxHealth * 0.35f)
            BloodVFX.SpawnGroundDecal(transform.position, 0.6f);

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        healthState = currentHealth <= maxHealth * 0.20f ? SurvivorHealthState.Downed
                    : currentHealth <= maxHealth * 0.60f ? SurvivorHealthState.Injured
                    : SurvivorHealthState.Healthy;

        if (healthState == SurvivorHealthState.Downed)
        {
            if (currentState != SurvivorState.Downed) SetState(SurvivorState.Downed);
        }
        else if (source != null)
        {
            // Hit from behind / no warning: panic immediately instead of standing
            // in the open until the next 1-second priority tick.
            fleeTarget = source;
            SetState(SurvivorState.Flee);
        }

        Debug.Log($"[SURVIVOR] {DisplayName} took {damage:F0} ({before:F0}->{currentHealth:F0}/{maxHealth:F0}) " +
                  $"state={healthState} behaviour={currentState}");
        UpdateHudStatus();
    }
    
    private void Die()
    {
        if (IsDead) return;

        currentHealth = 0f;
        healthState = SurvivorHealthState.Dead;

        // Give up any generator so repair progress is not held by a corpse.
        if (repairingGenerator != null) { repairingGenerator.StopRepair(null); repairingGenerator = null; }
        currentGenerator = null;

        if (behaviorCoroutine != null) { StopCoroutine(behaviorCoroutine); behaviorCoroutine = null; }
        if (hybridNav != null) hybridNav.isStopped = true;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        if (proceduralAnimator != null) proceduralAnimator.speed = 0f;

        // Leave the body visible but clearly dead (fallen forward).
        var model = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (model != null)
            model.localRotation = Quaternion.Euler(78f, model.localEulerAngles.y, 0f);

        SetState(SurvivorState.Dead);
        Debug.Log($"[SURVIVOR] {DisplayName} DIED (body kept, excluded from witch targeting)");
        UpdateHudStatus();
    }

    /// <summary>Captures where this survivor should stand when a session starts.</summary>
    public void RecordSpawnPoint()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        hasSpawnPoint = true;
    }

    /// <summary>
    /// Revives and repositions this survivor for a brand-new session. Everything
    /// that can leak across runs (health, state, repair, flee target, AI routine,
    /// fallen body pose) is reset here.
    /// </summary>
    public void ResetForNewSession()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        StopAllCoroutines();
        behaviorCoroutine = null;

        if (repairingGenerator != null) { repairingGenerator.StopRepair(null); repairingGenerator = null; }
        currentGenerator = null;
        rescueTargetSurvivor = null;
        fleeTarget = null;

        currentHealth = maxHealth;
        healthState = SurvivorHealthState.Healthy;
        currentState = SurvivorState.Idle;
        stateTimer = 0f;

        if (hasSpawnPoint)
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(spawnPosition);
        }

        if (modelRoot != null) modelRoot.localRotation = modelInitialRotation;
        if (proceduralAnimator != null) proceduralAnimator.speed = 0f;
        if (animator != null) { animator.SetFloat("Speed", 0f); animator.SetBool("IsFleeing", false); }

        if (hybridNav != null) { hybridNav.ResetPath(); hybridNav.isStopped = false; }

        SetState(SurvivorState.Explore);
        behaviorCoroutine = StartCoroutine(BehaviorRoutine());
        UpdateHudStatus();

        Debug.Log($"[SURVIVOR] {DisplayName} reset: hp={currentHealth:F0}/{maxHealth:F0} state={healthState} " +
                  $"pos={transform.position} behaviour={currentState}");
    }

    /// <summary>Pushes the live state to the teammate HUD row for this survivor.</summary>
    private void UpdateHudStatus()
    {
        if (UIManager.Instance == null || GameManager.Instance == null) return;
        int index = GameManager.Instance.survivors.IndexOf(this);
        if (index < 0) return;
        float percent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        UIManager.Instance.UpdateTeammateStatus(index, DisplayName, healthState, percent);
    }
    
    public void SetState(SurvivorState newState)
    {
        if (currentState == newState) return;

        // Release any generator we were repairing before switching behaviour.
        if (repairingGenerator != null && (newState != SurvivorState.RepairGenerator || repairingGenerator != currentGenerator))
        {
            repairingGenerator.StopRepair(null);
            repairingGenerator = null;
        }

        currentState = newState;
        stateTimer = 0f;
        
        OnStateChangedEvent?.Invoke(newState);
        ExecuteStateBehavior();
    }
}

public enum SurvivorState
{
    Idle,
    Explore,
    Follow,
    FindGenerator,
    RepairGenerator,
    Investigate,
    AvoidWitch,
    Flee,
    UseSkill,
    HelpTeammate,
    Rescue,
    Revive,
    SeekEscape,
    Escape,
    Downed,
    Dead
}

public enum SurvivorHealthState
{
    Healthy,
    Injured,
    Downed,
    Dead
}
