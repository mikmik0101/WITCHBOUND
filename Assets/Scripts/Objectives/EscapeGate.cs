using UnityEngine;
using UnityEngine.Events;

public class EscapeGate : MonoBehaviour, IInteractable
{
    [Header("Config")]
    public float openTime = 5f;
    public float interactionRange = 3f;
    // How close the player must get to the open gate to finish the run. Matches
    // the old "all survivors at the gate" reach (interactionRange * 2) but only
    // ever applies to the player, so a stuck or downed teammate can never lock
    // the exit.
    public float escapeRadius = 6f;
    
    [Header("State")]
    public bool isActivated = false;
    public bool isOpen = false;
    public bool isOpening = false;
    
    [Header("References")]
    public Animator gateAnimator;
    public AudioSource audioSource;
    public AudioClip activationSound;
    public AudioClip openingSound;
    public AudioClip openSound;
    public ParticleSystem activationParticles;
    public Light[] gateLights;
    
    [Header("Events")]
    public UnityEvent OnActivated;
    public UnityEvent OnOpened;
    public UnityEvent OnEscape;
    
    private float openTimer = 0f;

    // The solid panel that blocks the doorway. It is dropped (collider off) when
    // the gate opens so the exit is physically passable.
    private Collider gatePanelCollider;
    private Renderer gatePanelRenderer;

    // Victory must fire once per run: without this every frame spent inside the
    // exit radius re-sent PlayerEscaped()/Victory.
    private bool escapeTriggered;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        var panel = transform.Find("GateModel");
        if (panel != null)
        {
            gatePanelCollider = panel.GetComponent<Collider>();
            gatePanelRenderer = panel.GetComponent<Renderer>();
        }

        // Initially disable gate interaction
        if (gateLights != null)
        {
            foreach (var light in gateLights)
            {
                if (light != null)
                    light.enabled = false;
            }
        }
    }

    /// <summary>
    /// Power restored: light the gate and start opening it. Opening used to be
    /// reachable only through Interact(), which needed a raycast layer the scene
    /// never set, so the run could not be finished. Power restoration now opens
    /// the exit by itself, exactly as the objective text promises.
    /// </summary>
    public void Activate()
    {
        if (isActivated) return;
        
        isActivated = true;
        OnActivated?.Invoke();
        
        if (activationSound != null && audioSource != null)
            audioSource.PlayOneShot(activationSound);
        
        if (activationParticles != null)
            activationParticles.Play();
        
        if (gateLights != null)
        {
            foreach (var light in gateLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                    light.color = Color.green;
                }
            }
        }
        
        Debug.Log("Escape gate activated! Power restored!");

        StartOpening();
    }
    
    /// <summary>
    /// Manual trigger (E on the gate). Only starts the same opening sequence the
    /// power restoration starts; teammates are no longer a precondition because
    /// a downed or blocked survivor could permanently lock the exit.
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (!isActivated || isOpen || isOpening) return;
        StartOpening();
    }
    
    private void StartOpening()
    {
        if (isOpen || isOpening) return;

        isOpening = true;
        openTimer = 0f;
        
        if (openingSound != null && audioSource != null)
            audioSource.PlayOneShot(openingSound);
        
        if (gateAnimator != null)
            gateAnimator.SetTrigger("Open");

        Debug.Log($"[GATE] opening started (openTime={openTime:F1}s)");
    }
    
    private void Update()
    {
        if (isOpening && !isOpen)
        {
            openTimer += Time.deltaTime;
            
            if (openTimer >= openTime)
            {
                OpenGate();
            }
        }

        // The exit is checked continuously while open. Previously it was checked
        // once, on the exact frame the door finished opening, so a player who was
        // not already standing on the gate could never win.
        if (isOpen) CheckEscape();
    }
    
    private void OpenGate()
    {
        isOpening = false;
        isOpen = true;
        OnOpened?.Invoke();
        
        if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound);
        
        if (gateLights != null)
        {
            foreach (var light in gateLights)
            {
                if (light != null)
                    light.color = Color.green;
            }
        }

        // Make the doorway passable and visibly "open".
        if (gatePanelCollider != null) gatePanelCollider.enabled = false;
        if (gatePanelRenderer != null && gatePanelRenderer.sharedMaterial != null &&
            gatePanelRenderer.sharedMaterial.HasProperty("_Color"))
            gatePanelRenderer.sharedMaterial.color = new Color(0.25f, 1f, 0.45f);

        Debug.Log("[GATE] open - the exit is live");
        
        // Check for escape
        CheckEscape();
    }
    
    private void CheckEscape()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        float radius = Mathf.Max(1f, escapeRadius);

        // Teammates that reach the exit leave the map (deactivated once).
        if (gm.survivors != null)
        {
            for (int i = 0; i < gm.survivors.Count; i++)
            {
                var survivor = gm.survivors[i];
                if (survivor == null || survivor.healthState == SurvivorHealthState.Dead) continue;
                if (!survivor.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(survivor.transform.position, transform.position) < radius)
                {
                    Debug.Log($"[GATE] {survivor.DisplayName} escaped");
                    survivor.Escape();
                }
            }
        }

        if (escapeTriggered) return;
        if (gm.player == null || gm.player.IsDead) return;

        if (Vector3.Distance(gm.player.transform.position, transform.position) < radius)
        {
            escapeTriggered = true;
            Debug.Log("[GATE] player reached the exit -> VICTORY");
            OnEscape?.Invoke();
            gm.PlayerEscaped();
        }
    }

    /// <summary>
    /// Closes and powers down the gate for a new session so a previous victory
    /// cannot leak an open, lit gate into the next run.
    /// </summary>
    public void ResetForNewSession()
    {
        isActivated = false;
        isOpen = false;
        isOpening = false;
        openTimer = 0f;
        escapeTriggered = false;

        // Close the doorway again: a previous run must not leave a passable,
        // green-lit exit in the next one.
        if (gatePanelCollider != null) gatePanelCollider.enabled = true;
        if (gatePanelRenderer != null && gatePanelRenderer.sharedMaterial != null &&
            gatePanelRenderer.sharedMaterial.HasProperty("_Color"))
            gatePanelRenderer.sharedMaterial.color = new Color(0.32f, 0.33f, 0.35f);

        if (gateAnimator != null)
        {
            gateAnimator.Rebind();
            gateAnimator.Update(0f);
        }
        if (activationParticles != null) activationParticles.Stop();
        if (audioSource != null) audioSource.Stop();

        if (gateLights != null)
        {
            foreach (var light in gateLights)
            {
                if (light == null) continue;
                light.enabled = false;
                light.color = Color.white;
            }
        }

        Debug.Log("[GATE] reset: closed, unpowered");
    }
}
