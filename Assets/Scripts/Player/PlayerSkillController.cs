using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The four survivor skills, wired to real gameplay.
///
///   Luis Padilla   - PHASE SHIFT       intangible: witches cannot see or hit you
///   Mick Richards  - EMERGENCY WEAPON  stuns and knocks back nearby witches
///   Josh Garcia    - SPEED BURST       +45% movement speed
///   Vincent Reid   - INVISIBILITY      witches lose track of you
///
/// One activation input for all four: Q (handled by PlayerController so the
/// control never moves). The HUD shows the skill name, the key, and the
/// READY / ACTIVE / cooldown state.
///
/// Every activation is gated: no skill fires in the prologue, in a menu, while
/// paused, after death, after the round ends, or while on cooldown.
/// </summary>
public class PlayerSkillController : MonoBehaviour
{
    [Header("Skill")]
    public SkillData equippedSkill;
    public int skillLevel = 1;

    [Header("State")]
    public bool isSkillActive = false;
    public float currentCooldown = 0f;
    /// <summary>Seconds left in the current activation (0 when not active).</summary>
    public float activeRemaining = 0f;

    [Header("Tuning")]
    /// <summary>Multiplier applied to both walk and sprint during Speed Burst.</summary>
    public float speedBurstMultiplier = 1.45f;
    /// <summary>Witches inside this radius are hit by the emergency weapon.</summary>
    public float weaponRange = 10f;
    /// <summary>Seconds between emergency weapon pulses.</summary>
    public float weaponPulseInterval = 1.1f;
    /// <summary>How long a witch is stunned by one weapon pulse.</summary>
    public float weaponStunDuration = 2.2f;
    /// <summary>How far a weapon pulse throws a witch back.</summary>
    public float weaponKnockback = 4.5f;

    [Header("VFX")]
    public GameObject activeSkillVFX;

    private Coroutine skillCoroutine;
    private PlayerController _player;
    private Camera _camera;

    private float _weaponPulseTimer;
    private GameObject _weaponModel;
    private GameObject _overlayRoot;
    private CanvasGroup _overlayGroup;
    private Image _overlayImage;
    private Color _overlayColor;

    private readonly List<Renderer> _hiddenBody = new List<Renderer>();

    public delegate void OnSkillActivated(SkillData skill);
    public event OnSkillActivated OnSkillActivatedEvent;

    public delegate void OnSkillDeactivated(SkillData skill);
    public event OnSkillDeactivated OnSkillDeactivatedEvent;

    public delegate void OnCooldownChanged(float current, float max);
    public event OnCooldownChanged OnCooldownChangedEvent;

    // ---- effect queries used by the AI and by the player -------------------
    public bool IsPhasing { get { return isSkillActive && Type == SkillType.Phase; } }
    public bool IsInvisible { get { return isSkillActive && Type == SkillType.Invisibility; } }
    public bool IsSpeedBurst { get { return isSkillActive && Type == SkillType.SpeedBurst; } }
    public bool IsWeaponActive { get { return isSkillActive && Type == SkillType.EmergencyWeapon; } }

    private SkillType Type { get { return equippedSkill != null ? equippedSkill.skillType : SkillType.Phase; } }

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown < 0f) currentCooldown = 0f;
            OnCooldownChangedEvent?.Invoke(currentCooldown, GetCurrentCooldown());
        }

        if (isSkillActive)
        {
            activeRemaining = Mathf.Max(0f, activeRemaining - Time.deltaTime);

            if (IsWeaponActive) TickWeapon();
            if (_overlayGroup != null)
                _overlayGroup.alpha = 0.55f + Mathf.Sin(Time.unscaledTime * 3.4f) * 0.12f;
        }

        // Keep the HUD skill readout live (READY / ACTIVE / cooldown seconds).
        if (UIManager.Instance != null && equippedSkill != null)
            UIManager.Instance.UpdateSkill(equippedSkill, currentCooldown, GetCurrentCooldown(),
                isSkillActive, activeRemaining);
    }

    // =====================================================================
    // activation
    // =====================================================================

    /// <summary>True when the skill may fire right now. <paramref name="reason"/> explains a refusal.</summary>
    public bool CanActivate(out string reason)
    {
        reason = null;

        if (_player == null) _player = GetComponent<PlayerController>();

        if (equippedSkill == null) { reason = "no skill equipped"; return false; }
        if (_player != null && _player.IsDead) { reason = "player is dead"; return false; }
        if (isSkillActive) { reason = "already active"; return false; }
        if (currentCooldown > 0f) { reason = string.Format("cooldown {0:F1}s", currentCooldown); return false; }
        if (PrologueController.IsShowing) { reason = "prologue is showing"; return false; }
        if (Time.timeScale <= 0.001f) { reason = "game is paused"; return false; }

        var gm = GameManager.Instance;
        if (gm == null) { reason = "no game manager"; return false; }
        if (gm.currentState != GameState.Gameplay)
        {
            reason = "state is " + gm.currentState;
            return false;
        }

        if (InputStateManager.Instance != null && InputStateManager.Instance.IsMenuMode())
        {
            reason = "menu mode";
            return false;
        }

        return true;
    }

    public void ActivateSkill()
    {
        string reason;
        if (!CanActivate(out reason))
        {
            Debug.Log($"[SKILL] refused ({reason}) skill={(equippedSkill != null ? equippedSkill.skillName : "null")} " +
                      $"cooldown={currentCooldown:F1} active={isSkillActive} state={(GameManager.Instance != null ? GameManager.Instance.currentState.ToString() : "null")}");
            return;
        }

        isSkillActive = true;
        activeRemaining = GetCurrentDuration();
        currentCooldown = GetCurrentCooldown();
        _weaponPulseTimer = 0f;

        Debug.Log($"[SKILL] ACTIVATED {equippedSkill.skillName} ({equippedSkill.skillType}) " +
                  $"duration={activeRemaining:F1}s cooldown={currentCooldown:F1}s");

        OnSkillActivatedEvent?.Invoke(equippedSkill);
        OnCooldownChangedEvent?.Invoke(currentCooldown, GetCurrentCooldown());

        skillCoroutine = StartCoroutine(SkillRoutine());
    }

    private IEnumerator SkillRoutine()
    {
        float duration = GetCurrentDuration();

        if (equippedSkill.activationSound != null)
            AudioSource.PlayClipAtPoint(equippedSkill.activationSound, transform.position);

        SpawnActivationVFX();

        // Apply skill effect based on type
        ApplySkillEffect(true);

        float t = 0f;
        while (t < duration)
        {
            // The round can end or the player can die mid-skill: stop cleanly.
            if (_player != null && _player.IsDead) break;
            var gm = GameManager.Instance;
            if (gm != null && gm.currentState != GameState.Gameplay) break;

            t += Time.deltaTime;
            yield return null;
        }

        ApplySkillEffect(false);
        EndVisuals();

        isSkillActive = false;
        activeRemaining = 0f;
        skillCoroutine = null;

        Debug.Log($"[SKILL] ended {equippedSkill.skillName}, cooldown {currentCooldown:F1}s remaining");
        OnSkillDeactivatedEvent?.Invoke(equippedSkill);
    }

    private void SpawnActivationVFX()
    {
        Color c = SkillColor(Type);

        if (activeSkillVFX != null) Destroy(activeSkillVFX);

        if (equippedSkill != null && equippedSkill.vfxPrefab != null)
        {
            activeSkillVFX = Instantiate(equippedSkill.vfxPrefab, transform.position, Quaternion.identity, transform);
            return;
        }

        activeSkillVFX = SkillVFX.Spawn(transform.position, c);
        ShowOverlay(c);
    }

    private static Color SkillColor(SkillType type)
    {
        switch (type)
        {
            case SkillType.Phase: return new Color(0.35f, 0.85f, 1f);
            case SkillType.Invisibility: return new Color(0.62f, 0.55f, 0.95f);
            case SkillType.SpeedBurst: return new Color(1f, 0.72f, 0.30f);
            case SkillType.EmergencyWeapon: return new Color(1f, 0.42f, 0.28f);
            default: return Color.white;
        }
    }

    // =====================================================================
    // the four effects
    // =====================================================================

    private void ApplySkillEffect(bool activate)
    {
        if (equippedSkill == null) return;
        if (_player == null) _player = GetComponent<PlayerController>();

        switch (equippedSkill.skillType)
        {
            case SkillType.Phase:
                // Intangible: no damage, and witches stop perceiving the player.
                if (_player != null) _player.SetPhasing(activate);
                SetBodyVisible(!activate);
                break;

            case SkillType.Invisibility:
                // Witches lose track unless they are almost on top of you.
                if (_player != null) _player.SetInvisible(activate);
                SetBodyVisible(!activate);
                break;

            case SkillType.SpeedBurst:
                if (_player != null) _player.SetSpeedMultiplier(activate ? speedBurstMultiplier : 1f);
                break;

            case SkillType.EmergencyWeapon:
                if (activate) BuildWeaponModel();
                else DestroyWeaponModel();
                break;
        }
    }

    // ---- emergency weapon ---------------------------------------------------

    private void TickWeapon()
    {
        _weaponPulseTimer -= Time.deltaTime;
        if (_weaponPulseTimer > 0f) return;
        _weaponPulseTimer = weaponPulseInterval;

        Vector3 origin = transform.position + Vector3.up * 1.1f;
        int hits = 0;

        var witches = GameManager.Instance != null ? GameManager.Instance.activeWitches : null;
        if (witches != null)
        {
            for (int i = 0; i < witches.Count; i++)
            {
                var w = witches[i];
                if (w == null) continue;

                Vector3 to = w.transform.position - origin;
                to.y = 0f;
                if (to.magnitude > weaponRange) continue;

                Vector3 dir = to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;
                w.Stun(weaponStunDuration, dir, weaponKnockback);
                ImpactVFX.Spawn(w.transform.position + Vector3.up * 1.1f, new Color(1f, 0.45f, 0.2f));
                hits++;
            }
        }

        SpawnTracer(origin, hits);
        if (hits > 0)
            Debug.Log($"[SKILL] Emergency Weapon pulse stunned {hits} witch(es)");
    }

    private void SpawnTracer(Vector3 origin, int hits)
    {
        if (_camera == null) _camera = GetComponentInChildren<Camera>();
        Vector3 dir = _camera != null ? _camera.transform.forward : transform.forward;

        var go = new GameObject("WeaponTracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.015f;
        lr.material = ProcMaterials.Unlit("tracer_" + (hits > 0 ? "hit" : "miss"),
            hits > 0 ? new Color(1f, 0.55f, 0.25f) : new Color(0.6f, 0.6f, 0.7f));
        lr.startColor = Color.white;
        lr.endColor = new Color(1f, 0.4f, 0.2f, 0f);
        lr.SetPosition(0, origin + dir * 0.4f);
        lr.SetPosition(1, origin + dir * (hits > 0 ? weaponRange : 6f));
        Object.Destroy(go, 0.08f);
    }

    private void BuildWeaponModel()
    {
        if (_weaponModel != null) return;
        if (_camera == null) _camera = GetComponentInChildren<Camera>();
        if (_camera == null) return;

        var metal = ProcMaterials.Lit("weapon_metal", new Color(0.16f, 0.16f, 0.18f));
        var grip = ProcMaterials.Lit("weapon_grip", new Color(0.09f, 0.07f, 0.06f));
        var glow = ProcMaterials.Emissive("weapon_glow", new Color(0.35f, 0.12f, 0.05f),
            new Color(1f, 0.45f, 0.2f), 1.4f);

        _weaponModel = new GameObject("EmergencyWeapon");
        _weaponModel.transform.SetParent(_camera.transform, false);
        _weaponModel.transform.localPosition = new Vector3(0.30f, -0.28f, 0.62f);
        _weaponModel.transform.localRotation = Quaternion.Euler(0f, -4f, 0f);

        ProcBuild.Box(_weaponModel.transform, "Body", new Vector3(0f, 0f, 0f),
            new Vector3(0.10f, 0.11f, 0.34f), metal, false, false);
        ProcBuild.Box(_weaponModel.transform, "Barrel", new Vector3(0f, 0.015f, 0.24f),
            new Vector3(0.055f, 0.055f, 0.22f), metal, false, false);
        ProcBuild.Box(_weaponModel.transform, "Grip", new Vector3(0f, -0.11f, -0.07f),
            new Vector3(0.075f, 0.16f, 0.09f), grip, false, false);
        ProcBuild.Box(_weaponModel.transform, "Cell", new Vector3(0f, 0.055f, -0.04f),
            new Vector3(0.06f, 0.03f, 0.10f), glow, false, false);

        Debug.Log("[SKILL] Emergency Weapon model attached to the camera");
    }

    private void DestroyWeaponModel()
    {
        if (_weaponModel != null)
        {
            Destroy(_weaponModel);
            _weaponModel = null;
        }
    }

    // ---- screen tint --------------------------------------------------------

    private void ShowOverlay(Color color)
    {
        if (_overlayRoot != null) return;

        _overlayRoot = new GameObject("SkillOverlayCanvas");
        _overlayRoot.transform.SetParent(transform, false);

        var canvas = _overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var img = new GameObject("Tint");
        img.transform.SetParent(_overlayRoot.transform, false);
        var rect = img.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _overlayImage = img.AddComponent<Image>();
        _overlayColor = new Color(color.r, color.g, color.b, 0.10f);
        _overlayImage.color = _overlayColor;
        _overlayImage.raycastTarget = false;

        _overlayGroup = _overlayRoot.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0.55f;
        _overlayGroup.blocksRaycasts = false;
        _overlayGroup.interactable = false;
    }

    private void HideOverlay()
    {
        if (_overlayRoot != null)
        {
            Destroy(_overlayRoot);
            _overlayRoot = null;
            _overlayGroup = null;
            _overlayImage = null;
        }
    }

    // ---- body visibility ----------------------------------------------------

    private void SetBodyVisible(bool visible)
    {
        if (_player == null) return;
        var root = _player.bodyRoot;
        if (root == null) return;

        if (!visible)
        {
            _hiddenBody.Clear();
            root.GetComponentsInChildren(true, _hiddenBody);
            for (int i = 0; i < _hiddenBody.Count; i++)
                if (_hiddenBody[i] != null) _hiddenBody[i].enabled = false;
        }
        else
        {
            for (int i = 0; i < _hiddenBody.Count; i++)
                if (_hiddenBody[i] != null) _hiddenBody[i].enabled = true;
            _hiddenBody.Clear();
        }
    }

    private void EndVisuals()
    {
        if (activeSkillVFX != null)
        {
            Destroy(activeSkillVFX);
            activeSkillVFX = null;
        }
        HideOverlay();
        DestroyWeaponModel();
    }

    // =====================================================================
    // configuration
    // =====================================================================

    public float GetCurrentCooldown()
    {
        if (equippedSkill == null) return 0f;
        if (equippedSkill.levelCooldowns != null && equippedSkill.levelCooldowns.Length >= skillLevel)
            return equippedSkill.levelCooldowns[skillLevel - 1];
        return equippedSkill.cooldown;
    }

    public float GetCurrentDuration()
    {
        if (equippedSkill == null) return 0f;
        if (equippedSkill.levelDurations != null && equippedSkill.levelDurations.Length >= skillLevel)
            return equippedSkill.levelDurations[skillLevel - 1];
        return equippedSkill.duration;
    }

    public void EquipSkill(SkillData skill)
    {
        // Never leave an effect running from the previous survivor's skill.
        if (isSkillActive) ForceStop();

        equippedSkill = skill;
        skillLevel = 1;
        currentCooldown = 0f;
        activeRemaining = 0f;

        Debug.Log($"[SKILL] equipped {(skill != null ? skill.skillName + " (" + skill.skillType + ")" : "none")}");
    }

    /// <summary>Gives the player the skill that belongs to the chosen survivor.</summary>
    public void EquipForSurvivor(string survivorName)
    {
        SkillType type = CharacterLore.SkillFor(survivorName);
        SkillData data = SkillData.Load(type);
        EquipSkill(data);

        var mgr = SkillUpgradeManager.Instance;
        if (mgr != null) skillLevel = mgr.GetSkillLevel(type);

        Debug.Log($"[SKILL] {survivorName} -> {type} (level {skillLevel})");
    }

    /// <summary>Stops an active skill immediately, without waiting for the timer.</summary>
    public void ForceStop()
    {
        if (skillCoroutine != null) StopCoroutine(skillCoroutine);
        skillCoroutine = null;

        if (isSkillActive) ApplySkillEffect(false);
        EndVisuals();
        SetBodyVisible(true);

        isSkillActive = false;
        activeRemaining = 0f;
    }

    /// <summary>Clears active skill state, VFX and cooldown for a fresh session.</summary>
    public void ResetForNewSession()
    {
        StopAllCoroutines();
        skillCoroutine = null;

        if (isSkillActive) ApplySkillEffect(false);
        EndVisuals();
        SetBodyVisible(true);

        isSkillActive = false;
        currentCooldown = 0f;
        activeRemaining = 0f;
        _weaponPulseTimer = 0f;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateSkill(equippedSkill, 0f, GetCurrentCooldown(), false, 0f);

        Debug.Log($"[SKILL] reset: active=False cooldown=0 skill={(equippedSkill != null ? equippedSkill.skillName : "none")}");
    }

    public void UpgradeSkill()
    {
        if (equippedSkill != null && skillLevel < equippedSkill.maxLevel)
        {
            skillLevel++;
        }
    }
}
