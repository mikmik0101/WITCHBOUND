using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    // Sprint must stay clearly above the witch chase speed (max 7.0) or the chase
    // is unwinnable. See WitchAI.ApplyTypeCombatTuning for the other side.
    public float moveSpeed = 5f;
    public float sprintSpeed = 7.8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Forward-run acceleration")]
    // Holding W with real forward progress adds +speedStepAmount every
    // speedStepInterval seconds, linearly (5, 6, 7, ...). Never exponential.
    public float defaultMoveSpeed = 5f;
    public float speedStepInterval = 5f;
    public float speedStepAmount = 1f;
    public float maxMoveSpeed = 12f;
    // A single frame of contact with a wall must not wipe the run; only a real
    // stall longer than this grace resets the escalation.
    public float progressGrace = 0.4f;

    private float runTimer;
    private int speedLevel;
    private float stallTimer;

    [Header("Skill state")]
    // Set by PlayerSkillController only. Never touched by movement code.
    private float speedMultiplier = 1f;
    private bool phasing;
    private bool invisible;

    /// <summary>Multiplier applied on top of walk/sprint speed (Speed Burst).</summary>
    public float SpeedMultiplier => speedMultiplier;
    /// <summary>True while Phase Shift is running: no damage, no detection.</summary>
    public bool IsPhasing => phasing;
    /// <summary>True while Invisibility is running: witches lose track.</summary>
    public bool IsInvisible => invisible;
    
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    // Six monsters can reach the player at once. Without a short global guard
    // their impacts stack in the same second and delete a full health bar
    // instantly; 0.55 s keeps a swarm lethal but survivable if you break away.
    public float damageCooldown = 0.9f;
    private float _lastDamageTime = -99f;
    public PlayerHealthState healthState = PlayerHealthState.Healthy;
    public float deathSequenceDuration = 1.8f;
    
    [Header("Interaction")]
    public float interactionRange = 2f;
    public LayerMask interactableLayer;
    
    [Header("References")]
    public CameraController cameraController;
    public PlayerSkillController skillController;
    public Transform bodyRoot;
    public ProceduralAnimator bodyAnimator;
    
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isSprinting;
    private Vector2 moveInput;
    private bool interactPressed;
    private bool skillPressed;
    private bool isDead;
    private bool deathStarted;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool hasSpawnPoint;
    
    public bool IsSprinting => isSprinting;
    public bool IsDead => isDead;

    /// <summary>
    /// QA-only: drives the sprint branch of HandleInput without a key press.
    /// The automated X11 harness cannot inject modifier keys into Unity (the
    /// player ignores synthetic Shift events), so the sprint/escape window is
    /// measured by forcing the same code path the Shift key feeds.
    /// </summary>
    public static bool ForceSprintForQA = false;
    /// <summary>QA-only: feeds a constant W press for deterministic harness runs.</summary>
    public static bool ForceForwardForQA = false;

    /// <summary>Number of completed +1 speed steps in the current forward run.</summary>
    public int AccelerationLevel => speedLevel;
    /// <summary>Live movement speed (walk or sprint, acceleration included).</summary>
    public float CurrentSpeed => (isSprinting ? sprintSpeed + speedLevel * speedStepAmount
                                              : moveSpeed) * speedMultiplier;

    /// <summary>
    /// Read-only view of the forward-run speed, ramp included (5, 6, 7 ...).
    /// Companions mirror this so they can never drift out of step with the
    /// player they are escorting.
    /// </summary>
    public float RunSpeed => moveSpeed;
    public PlayerHealthState HealthState => healthState;
    public Vector3 SpawnPosition => spawnPosition;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;

        // The serialized moveSpeed is the designer-facing default; acceleration
        // always starts from it so a fresh run is predictable.
        defaultMoveSpeed = moveSpeed;
        ResetAcceleration();
        
        if (cameraController == null)
            cameraController = GetComponentInChildren<CameraController>();
        if (skillController == null)
            skillController = GetComponent<PlayerSkillController>();
        
        // Fix character controller settings if needed
        if (characterController != null)
        {
            characterController.height = 1.8f;
            characterController.radius = 0.4f;
            characterController.center = new Vector3(0, 0.9f, 0);
            characterController.skinWidth = 0.08f;
            characterController.stepOffset = 0.3f;
        }
    }
    
    private void Update()
    {
        if (InputStateManager.Instance != null && InputStateManager.Instance.IsMenuMode())
        {
            // Pausing is a documented reset point for the speed ramp.
            ResetAcceleration();
            return;
        }
        // A dead player is completely uncontrollable: no movement, look, sprint,
        // skills or generator interaction. CameraController also checks IsDead.
        if (isDead) return;
        // Try to keep player at proper height
        if (characterController != null && characterController.isGrounded)
        {
            RaycastHit hitGround;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hitGround, 2f))
            {
                if (hitGround.point.y < transform.position.y - 0.05f && transform.position.y - hitGround.point.y > 0.5f)
                {
                    // Player is sinking/floating too much, adjust
                    Vector3 pos = transform.position;
                    pos.y = hitGround.point.y + characterController.height * 0.5f - characterController.center.y;
                    transform.position = pos;
                }
            }
        }
        HandleInput();
        HandleMovement();
        HandleGravity();
        HandleInteraction();
        HandleSkill();
        UpdateHealthState();
        UpdateBodyAnimation();
    }

    private void UpdateBodyAnimation()
    {
        if (bodyAnimator == null) return;
        Vector3 v = characterController != null ? characterController.velocity : Vector3.zero;
        float horiz = new Vector2(v.x, v.z).magnitude;
        bodyAnimator.speed = horiz;
    }
    
    private void HandleInput()
    {
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (ForceForwardForQA) moveInput = new Vector2(0f, 1f);
        isSprinting = ForceSprintForQA || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        interactPressed = Input.GetKeyDown(KeyCode.E);
        skillPressed = Input.GetKeyDown(KeyCode.Q);
    }
    
    private void HandleMovement()
    {
        // FPS movement relative to the player's current facing. The camera owns
        // the pitch; the player body (rotated by Mouse X in CameraController)
        // owns yaw, so holding W while turning the mouse turns the run.
        Vector3 forward = transform.forward; forward.y = 0f;
        Vector3 right = transform.right; right.y = 0f;
        if (forward.sqrMagnitude > 0.001f) forward.Normalize();
        if (right.sqrMagnitude > 0.001f) right.Normalize();

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
        if (moveDirection.sqrMagnitude > 0.01f) moveDirection.Normalize();

        float currentSpeed = (isSprinting ? sprintSpeed + speedLevel * speedStepAmount : moveSpeed) * speedMultiplier;

        // Measure real displacement, not CharacterController.velocity: the last
        // Move() of the frame is the gravity step, so .velocity reads as
        // near-zero horizontally and the ramp would never advance.
        Vector3 before = transform.position;
        if (characterController != null && moveDirection.sqrMagnitude > 0.001f)
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        Vector3 delta = transform.position - before;
        delta.y = 0f;

        // ---- forward-run acceleration -------------------------------------
        // Only a held W with genuine forward progress feeds the timer. Backward
        // movement, strafing in place and pushing into a wall all fail the test.
        bool wantsForward = moveInput.y > 0.1f;
        float travelled = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
        bool progressing = wantsForward && Vector3.Dot(delta, forward) > 0.02f && travelled > 0.4f;
        UpdateAcceleration(wantsForward, progressing);
    }

    private void UpdateAcceleration(bool wantsForward, bool progressing)
    {
        if (!wantsForward)
        {
            ResetAcceleration();
            return;
        }

        if (progressing)
        {
            stallTimer = 0f;
            runTimer += Time.deltaTime;

            // Linear steps only: +speedStepAmount per completed interval.
            while (runTimer >= speedStepInterval)
            {
                runTimer -= speedStepInterval;
                speedLevel++;
                moveSpeed = Mathf.Min(defaultMoveSpeed + speedLevel * speedStepAmount, maxMoveSpeed);
                Debug.Log($"[SPEED] +{speedStepAmount:F0} -> {moveSpeed:F0} (level {speedLevel}) t={Time.time:F1}");
            }
        }
        else
        {
            stallTimer += Time.deltaTime;
            if (stallTimer >= progressGrace) ResetAcceleration();
        }
    }

    /// <summary>Drops the speed ramp back to the default and restarts the timer.</summary>
    public void ResetAcceleration()
    {
        if (speedLevel > 0)
            Debug.Log($"[SPEED] reset -> {defaultMoveSpeed:F0} (was level {speedLevel}) t={Time.time:F1}");
        runTimer = 0f;
        speedLevel = 0;
        stallTimer = 0f;
        moveSpeed = defaultMoveSpeed;
    }
    
    private void HandleGravity()
    {
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void HandleInteraction()
    {
        if (interactPressed)
        {
            if (cameraController != null)
            {
                Ray ray = new Ray(cameraController.transform.position, cameraController.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    interactable?.Interact(this);
                }
            }
        }
    }
    
    private void HandleSkill()
    {
        if (skillPressed && skillController != null)
        {
            skillController.ActivateSkill();
        }
    }
    
    private void UpdateHealthState()
    {
        if (isDead) return;

        float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        if (healthPercent <= 0)
            healthState = PlayerHealthState.Dead;
        else if (healthPercent <= 0.25f)
            healthState = PlayerHealthState.Downed;
        else if (healthPercent <= 0.5f)
            healthState = PlayerHealthState.Injured;
        else
            healthState = PlayerHealthState.Healthy;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            UIManager.Instance.SetLowHealthBlood(healthPercent);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f) return;
        // Phase Shift: the player is intangible, nothing can land.
        if (phasing)
        {
            Debug.Log($"[COMBAT] Player damage {damage:F0} ignored (Phase Shift active)");
            return;
        }
        if (Time.time - _lastDamageTime < damageCooldown) return;
        _lastDamageTime = Time.time;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        healthState = currentHealth <= 0f ? PlayerHealthState.Dead
                    : currentHealth <= maxHealth * 0.25f ? PlayerHealthState.Downed
                    : currentHealth <= maxHealth * 0.5f ? PlayerHealthState.Injured
                    : PlayerHealthState.Healthy;

        // Trigger damage effects
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            UIManager.Instance.ShowDamageFlash();
            UIManager.Instance.SetLowHealthBlood(maxHealth > 0f ? currentHealth / maxHealth : 0f);
        }
        if (cameraController == null) cameraController = GetComponentInChildren<CameraController>();
        if (cameraController != null) cameraController.Shake(0.18f, 0.055f);

        // Blood spray at the wound, biased away from the camera's forward axis so
        // it reads in first person.
        Vector3 spray = -transform.forward;
        spray.y = 0.25f;
        BloodVFX.SpawnImpact(transform.position + Vector3.up * 1.05f, spray, 1.2f);
        BloodVFX.SpawnGroundDecal(transform.position, 0.8f);

        Debug.Log($"[COMBAT] Player took {damage:F0} damage -> {currentHealth:F0}/{maxHealth:F0} ({healthState})");

        if (currentHealth <= 0f)
            Die();
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;

        float before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (currentHealth <= before) return;

        // The low-health wash is driven by damage events, so without this a
        // heal would leave the screen red until the next hit.
        healthState = currentHealth <= maxHealth * 0.25f ? PlayerHealthState.Downed
                    : currentHealth <= maxHealth * 0.5f ? PlayerHealthState.Injured
                    : PlayerHealthState.Healthy;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            UIManager.Instance.SetLowHealthBlood(maxHealth > 0f ? currentHealth / maxHealth : 0f);
        }
    }

    // ---- skill hooks (called only by PlayerSkillController) ----------------

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 1f, 2.2f);
        Debug.Log($"[SKILL] speed multiplier -> {speedMultiplier:F2} (live speed {CurrentSpeed:F2})");
    }

    public void SetPhasing(bool value)
    {
        phasing = value;
        Debug.Log($"[SKILL] phase {(value ? "ON (no damage, no detection)" : "OFF")}");
    }

    public void SetInvisible(bool value)
    {
        invisible = value;
        Debug.Log($"[SKILL] invisibility {(value ? "ON (witch detection reduced)" : "OFF")}");
    }

    private string _appearanceName;

    /// <summary>
    /// Rebuilds the first-person body with the chosen survivor's clothing/skin
    /// colours. Only cosmetic: the CharacterController, collider, speed and
    /// camera are untouched.
    /// </summary>
    public void ApplySurvivorAppearance(string survivorName)
    {
        if (string.IsNullOrEmpty(survivorName)) return;
        if (_appearanceName == survivorName && bodyRoot != null) return;

        var look = RuntimeCharacterFactory.GetSurvivorLook(survivorName);

        if (bodyRoot != null) Destroy(bodyRoot.gameObject);

        var body = RuntimeCharacterFactory.BuildPlayerBody(look.clothing, look.skin);
        body.transform.SetParent(transform, false);
        body.transform.localPosition = new Vector3(0f, 0f, 0.14f);
        bodyRoot = body.transform;
        bodyAnimator = body.GetComponent<ProceduralAnimator>();
        _appearanceName = survivorName;

        Debug.Log($"[PLAYER] appearance -> {survivorName}");
    }

    /// <summary>Remembers where a fresh session should put the player.</summary>
    public void RecordSpawnPoint()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        hasSpawnPoint = true;
    }

    /// <summary>
    /// Full player reset for a new session: alive, healthy, at the spawn point,
    /// camera/rotation/velocity/skill cleared, death sequence stopped. Without
    /// this the results screen's "Play Again" left a dead, frozen player.
    /// </summary>
    public void ResetForNewSession()
    {
        StopAllCoroutines();

        isDead = false;
        deathStarted = false;
        currentHealth = maxHealth;
        healthState = PlayerHealthState.Healthy;

        speedMultiplier = 1f;
        phasing = false;
        invisible = false;

        moveInput = Vector2.zero;
        isSprinting = false;
        interactPressed = false;
        skillPressed = false;
        velocity = Vector3.zero;
        _lastDamageTime = -99f;
        ResetAcceleration();

        if (characterController != null) characterController.enabled = false;
        if (hasSpawnPoint)
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
        if (characterController != null) characterController.enabled = true;

        if (bodyAnimator != null) bodyAnimator.speed = 0f;

        if (cameraController == null) cameraController = GetComponentInChildren<CameraController>();
        if (cameraController != null) cameraController.ResetLook();

        if (skillController == null) skillController = GetComponent<PlayerSkillController>();
        if (skillController != null) skillController.ResetForNewSession();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            // A new round starts at full health, so the low-health wash has to
            // go with it - it is driven by damage events and would otherwise
            // carry over from the round that just ended.
            UIManager.Instance.SetLowHealthBlood(1f);
            UIManager.Instance.SetDeathFade(0f);
            UIManager.Instance.ShowDeathText(false);
        }

        Debug.Log($"[PLAYER] reset: hp={currentHealth:F0}/{maxHealth:F0} pos={transform.position} " +
                  $"walk={moveSpeed:F1} sprint={sprintSpeed:F1} dead={isDead}");
    }

    /// <summary>Called exactly once when health reaches zero.</summary>
    private void Die()
    {
        if (deathStarted) return;
        deathStarted = true;
        isDead = true;
        currentHealth = 0f;
        healthState = PlayerHealthState.Dead;

        // Stop all control immediately.
        moveInput = Vector2.zero;
        isSprinting = false;
        ResetAcceleration();
        if (bodyAnimator != null) bodyAnimator.speed = 0f;
        if (characterController != null) velocity = Vector3.zero;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(0f, maxHealth);
            UIManager.Instance.ShowDamageFlash();
        }

        Debug.Log("[DEATH] Player died - movement, look, skills and repair disabled");
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        var cam = cameraController != null ? cameraController.transform : null;
        Vector3 basePos = cam != null ? cam.localPosition : Vector3.zero;
        Quaternion baseRot = cam != null ? cam.localRotation : Quaternion.identity;

        // 1) short camera shake (unscaled so it works even if timeScale changes)
        float shakeTime = 0.55f;
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.unscaledDeltaTime;
            if (cam != null)
            {
                cam.localPosition = basePos + new Vector3(
                    Random.Range(-0.045f, 0.045f),
                    Random.Range(-0.045f, 0.045f),
                    Random.Range(-0.02f, 0.02f));
                cam.localRotation = baseRot * Quaternion.Euler(
                    Random.Range(-1.8f, 1.8f),
                    Random.Range(-1.8f, 1.8f),
                    Random.Range(-4.5f, 4.5f));
            }
            yield return null;
        }
        if (cam != null) { cam.localPosition = basePos; cam.localRotation = baseRot; }

        // 2) fade to black + DEFEATED
        float fade = 0f;
        float fadeDuration = Mathf.Max(0.4f, deathSequenceDuration - shakeTime - 0.5f);
        while (fade < 1f)
        {
            fade += Time.unscaledDeltaTime / fadeDuration;
            if (UIManager.Instance != null) UIManager.Instance.SetDeathFade(Mathf.Clamp01(fade));
            yield return null;
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowDeathText(true);
        yield return new WaitForSecondsRealtime(0.5f);

        // 3) game over -> results screen (DEFEAT)
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
    }
}

public enum PlayerHealthState
{
    Healthy,
    Injured,
    Downed,
    Dead
}

public interface IInteractable
{
    void Interact(PlayerController player);
}
