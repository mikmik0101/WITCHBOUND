using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Simple Nextbot-style chaser.
///
/// Every frame it: finds the nearest valid target (player or survivor), moves
/// directly toward it, and attacks when close. It can never target itself or
/// another witch, can never freeze on a failed path, and immediately picks a new
/// target when the current one dies or disappears.
/// </summary>
public class WitchAI : MonoBehaviour
{
    [Header("Data")]
    public WitchData witchData;

    [Header("State")]
    public WitchState currentState = WitchState.Idle;
    public WitchState previousState = WitchState.Idle;

    [Header("Tuning (overridden by WitchData, then clamped to the balance contract)")]
    public float moveSpeed = 4f;
    public float chaseSpeed = 6.5f;             // always below player sprint (7.8)
    public float attackDamage = 18f;
    public float attackRange = 1.9f;            // stop distance: the witch never walks into the player
    public float attackCooldown = 1.0f;         // time from attack end to the next attack start
    public float retargetInterval = 0.4f;
    public float groundSnapHeight = 6f;
    public float turnSpeedDegrees = 720f;

    [Header("Attack lifecycle (per-type values set in ApplyWitchData)")]
    public float attackWindup = 0.35f;          // telegraphed tell before the hit lands
    public float attackRecovery = 0.45f;        // follow-through, cannot damage during this phase
    public float impactRangeSlack = 0.35f;      // a victim who stepped out of range avoids the hit
    public float hoverHeight = 0f;              // Winged witches fly this high above the ground

    /// <summary>QA readout: "" / "Windup" / "Recovery".</summary>
    public string AttackPhaseName
    {
        get
        {
            switch (_attackPhase)
            {
                case AttackPhase.Windup: return "Windup";
                case AttackPhase.Recovery: return "Recovery";
                default: return "";
            }
        }
    }
    public float AttackDamage { get { return attackDamage; } }
    public float AttackRange { get { return attackRange; } }
    public float AttackCooldown { get { return attackCooldown; } }

    [Header("Detection")]
    public LayerMask survivorLayer = 1;   // Default layer (player + survivors)
    public LayerMask obstacleLayer = 1;

    [Header("Perception (no omniscient tracking)")]
    public float awarenessRange = 34f;     // must also have line of sight
    public float hearingRange = 20f;       // through walls, sprinting is louder
    public float sprintHearingMultiplier = 1.6f;
    public float perceptionInterval = 0.15f;
    public float searchDuration = 10f;     // how long it hunts the last known spot
    public float searchRadius = 14f;
    public float wanderRadius = 42f;
    public int packSlot = 0;               // per-monster approach/search variation

    [Header("Pack spacing (chase mode)")]
    // Six chasers used to converge on the player's exact position and stack
    // 1.5 m in front of the first-person camera. These values keep the swarm
    // dangerous without turning the screen into a wall of faces:
    //  - only the two closest witches may close to striking distance,
    //  - everyone else holds a fanned ring,
    //  - bodies repel each other so no two occupy the same spot.
    public int maxAttackers = 2;           // witches allowed inside attack range
    public float holdRingBase = 4.6f;      // stand-off ring for the rest
    public float holdRingStep = 0.8f;      // per-slot ring variation
    public float separationRadius = 2.4f;  // minimum spacing between bodies
    public float cameraClearance = 1.35f;  // hard floor on horizontal distance

    // --- perception state ---
    private bool _hasTarget;
    private Vector3 _lastKnownPos;
    private float _perceptionTimer;
    private int _searchPhase;              // 0 = go to last known, 1 = search, 2 = wander
    private float _searchPhaseTimer;       // NOT the attack _phaseTimer
    private float _searchAroundTimer;
    private Vector3 _searchDestination;
    private Vector3 _stuckCheckPos;
    private Vector3 _detourDir;
    private float _detourTimer;

    // --- stun (Emergency Weapon) ---
    private float _stunnedUntil = -1f;

    /// <summary>True while an emergency-weapon pulse has this witch planted.</summary>
    public bool IsStunned { get { return Time.time < _stunnedUntil; } }

    /// <summary>
    /// Stops the witch for <paramref name="seconds"/> and pushes it back.
    /// Used by the Emergency Weapon skill; no other system stuns.
    /// </summary>
    public void Stun(float seconds, Vector3 awayDirection, float knockback)
    {
        _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + Mathf.Max(0.1f, seconds));
        CancelAttack();

        Vector3 dir = awayDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        Vector3 target = TownLayout.ClampToPlayable(transform.position + dir * Mathf.Max(0f, knockback), 4f);
        target.y = transform.position.y;
        transform.position = target;

        if (_animator != null) _animator.speed = 0f;

        Debug.Log($"[SKILL] {Name} stunned for {seconds:F1}s and knocked back {knockback:F1}m");
    }

    [Header("Components")]
    public NavMeshAgent agent;
    public HybridNavigation hybridNav;
    public Transform headTransform;
    public Renderer faceRenderer;
    public Material faceMaterial;

    public delegate void OnStateChanged(WitchState newState, WitchState oldState);
    public event OnStateChanged OnStateChangedEvent;
    public delegate void OnSurvivorDetected(Transform survivor);
    public event OnSurvivorDetected OnSurvivorDetectedEvent;
    public delegate void OnAttack(PlayerController player);
    public event OnAttack OnAttackEvent;

    private Transform _target;
    private SurvivorAI _targetSurvivor;
    private PlayerController _targetPlayer;
    private float _lastAttackTime;
    private float _retargetTimer;
    private ProceduralAnimator _animator;
    private float _stuckTimer;
    private Vector3 _lastPos;
    private Vector3 _prevPos;
    private float _measuredSpeed;

    private enum AttackPhase { None, Windup, Recovery }
    private AttackPhase _attackPhase = AttackPhase.None;
    private float _phaseTimer;

    public Transform CurrentTarget { get { return _target; } }
    public float MeasuredSpeed { get { return _measuredSpeed; } }
    public float ChaseSpeed { get { return chaseSpeed; } }

    /// <summary>QA only: logs each damaging attack.</summary>
    public static bool VerboseCombat = false;

    /// <summary>QA-only (-nohunt): witches stop acquiring targets.</summary>
    public static bool HuntingDisabled = false;

    /// <summary>QA only: once a second, logs this witch's distance and pack slot.</summary>
    public static bool VerbosePack = false;
    private float _packLogTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hybridNav = GetComponent<HybridNavigation>();
        if (hybridNav == null) hybridNav = gameObject.AddComponent<HybridNavigation>();
        // Nextbot movement is direct - make sure the nav stack never fights us.
        if (agent != null) agent.enabled = false;
        var sp = GetComponent<SimplePathfinding>();
        if (sp != null) sp.enabled = false;
        var hn = GetComponent<HybridNavigation>();
        if (hn != null) hn.enabled = false;

        _animator = GetComponentInChildren<ProceduralAnimator>();
        _lastPos = transform.position;
        _prevPos = transform.position;
    }

    private void Start()
    {
        if (witchData != null) ApplyWitchData();
        if (GameManager.Instance != null) GameManager.Instance.RegisterWitch(this);

        _stuckCheckPos = transform.position;
        _searchPhase = 2;
        _searchDestination = transform.position;
        PickWanderPoint();

        // A monster that has not perceived the player yet wanders the town; it
        // does not start the run already knowing where the player is.
        SetState(WitchState.Wander);
    }

    private void ApplyWitchData()
    {
        moveSpeed = witchData.moveSpeed;
        // Balance contract: dangerous, but slower than the player's 7.8 sprint so
        // a hit never becomes an unavoidable death sentence.
        chaseSpeed = Mathf.Clamp(Mathf.Max(witchData.chaseSpeed, witchData.moveSpeed), 5.8f, 7.0f);
        attackDamage = Mathf.Clamp(witchData.attackDamage, 15f, 25f);
        ApplyTypeCombatTuning();
    }

    /// <summary>
    /// Per-silhouette reach, cooldown and telegraph. Values come from the design
    /// contract so a hound snaps at ~1.8 m while the flying witch can strike from
    /// ~2.5 m, and no collider can ever deal damage from several metres away.
    /// </summary>
    private void ApplyTypeCombatTuning()
    {
        if (witchData == null) return;
        switch (witchData.witchType)
        {
            case WitchType.Winged:
                attackRange = 2.5f;
                attackCooldown = Mathf.Clamp(witchData.attackCooldown, 1.0f, 1.5f);
                attackWindup = 0.40f;
                attackRecovery = 0.55f;
                hoverHeight = 0.75f;              // visibly airborne at medium range
                break;

            case WitchType.Hound:
                attackRange = 1.8f;
                attackCooldown = Mathf.Clamp(witchData.attackCooldown, 0.8f, 1.2f);
                attackWindup = 0.28f;
                attackRecovery = 0.38f;
                hoverHeight = 0f;
                break;

            case WitchType.Crawler:
                attackRange = 1.5f;
                attackCooldown = Mathf.Clamp(witchData.attackCooldown, 0.8f, 1.2f);
                attackWindup = 0.26f;
                attackRecovery = 0.36f;
                hoverHeight = 0f;
                break;

            case WitchType.Tall:
                attackRange = 2.2f;
                attackCooldown = Mathf.Clamp(witchData.attackCooldown, 1.0f, 1.4f);
                attackWindup = 0.45f;
                attackRecovery = 0.55f;
                hoverHeight = 0f;
                break;

            case WitchType.Final:
                attackRange = 2.0f;
                attackCooldown = Mathf.Clamp(witchData.attackCooldown, 0.9f, 1.3f);
                attackWindup = 0.34f;
                attackRecovery = 0.46f;
                hoverHeight = 0f;
                break;

            default: // Humanoid
                attackRange = 1.9f;
                attackCooldown = Mathf.Clamp(witchData.attackCooldown, 0.9f, 1.3f);
                attackWindup = 0.35f;
                attackRecovery = 0.45f;
                hoverHeight = 0f;
                break;
        }
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var st = gm.currentState;
        if (st != GameState.Gameplay && st != GameState.EscapePhase)
        {
            CancelAttack();
            if (_animator != null) _animator.speed = 0f;
            return;
        }

        // Perception decides whether the player's position is actually known.
        // Nothing below may read the player's live position unless this says so.
        UpdatePerception();

        // Emergency Weapon stun: planted, cannot attack, cannot chase.
        if (IsStunned)
        {
            CancelAttack();
            if (_animator != null) _animator.speed = 0f;
            SnapToGround();
            return;
        }

        if (!_hasTarget)
        {
            // Death can happen mid-swing; drop the attack immediately.
            CancelAttack();
            UpdateSearch();
            return;
        }

        Vector3 to = _target.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        Vector3 dir = dist > 0.0001f ? to / dist : transform.forward;

        _prevPos = transform.position;

        // An attack in progress owns the witch until impact + recovery completes.
        if (_attackPhase != AttackPhase.None)
        {
            RunAttackLifecycle(dist, dir);
            return;
        }

        if (Time.time - _lastAttackTime >= attackCooldown && dist <= attackRange)
        {
            BeginAttack();
            return;
        }

        bool cooldownReady = Time.time - _lastAttackTime >= attackCooldown;

        if (dist <= attackRange)
        {
            // In range but cooling down: hold at attack distance, never occupy
            // the player, and keep the face on the victim for the next swing.
            SetState(WitchState.Attack);
            FaceDirection(dir, turnSpeedDegrees);
            UpdateMeasuredSpeed();
            if (_animator != null) _animator.speed = 0f;
            SnapToGround();
        }
        else
        {
            SetState(WitchState.Chase);

            // --- attack-slot arbitration ---------------------------------
            // Only the closest witches press the attack; the rest hold a wider
            // ring so the player can still see what is coming. Local and
            // stateless: each witch only compares its own distance with its
            // pack mates, so no manager or physics query is needed.
            int slot = CountCloserChasers();
            if (VerbosePack && Time.time - _packLogTime >= 1f)
            {
                _packLogTime = Time.time;
                Debug.Log($"[PACK] {Name} dist={dist:F1} closer={slot} role={(slot < maxAttackers ? "attack" : "ring")} ring={HoldRingDistance():F1} nearest={NearestPackDistance():F1}");
            }
            Vector3 aimPoint = _target.position;
            float stopDistance = attackRange;

            if (slot >= maxAttackers)
            {
                Vector3 outward = transform.position - _target.position;
                outward.y = 0f;
                outward = outward.sqrMagnitude > 0.01f ? outward.normalized : -dir;
                outward = Quaternion.Euler(0f, (packSlot - 2.5f) * 24f, 0f) * outward;
                aimPoint = _target.position + outward * HoldRingDistance();
                stopDistance = 0.4f;   // stand on the ring, not on the victim
            }

            Vector3 toAim = aimPoint - transform.position;
            toAim.y = 0f;
            float aimDist = toAim.magnitude;
            Vector3 steer = aimDist > 0.001f ? toAim / aimDist : dir;

            // Fan the approach line. Identical straight lines make six chasers
            // arrive as one column; the bias fades out inside 6 m so it can
            // never stop a witch from reaching attack range.
            float fan = Mathf.Clamp01((dist - 6f) / 6f) * Mathf.Sin(packSlot * 1.7f) * 0.45f;
            if (Mathf.Abs(fan) > 0.001f)
                steer = (steer + Vector3.Cross(Vector3.up, steer) * fan).normalized;

            steer = AvoidObstacles(steer);

            // Body separation: cheap O(n) repulsion over the six witches, no
            // colliders and no physics solver.
            Vector3 push = SeparationPush();
            if (push.sqrMagnitude > 0.0001f) steer = (steer + push * 1.2f).normalized;

            // Clamped so the witch stops exactly at attack range instead of
            // walking through the target.
            float step = Mathf.Min(chaseSpeed * Time.deltaTime, aimDist - stopDistance);
            if (step > 0f) transform.position += steer * step;
            else if (push.sqrMagnitude > 0.0001f)
                transform.position += push * 2f * Time.deltaTime;   // unstick two idle bodies

            // Close in, keep the face on the victim: a chaser that looks at the
            // player reads far better than one staring at its steering vector.
            FaceDirection(dist < 8f ? dir : steer, turnSpeedDegrees);
            UpdateMeasuredSpeed();
            // The animator takes real metres/second, not a made-up multiplier,
            // so 6.5 m/s looks like a 6.5 m/s run.
            if (_animator != null) _animator.speed = _measuredSpeed;
            SnapToGround();
            RecoverIfStuck(steer);
            EnforceStandoff();
        }

        // Decide again with the post-move distance. Chasing a walking/sprinting
        // player made the pre-move distance always one frame larger than the stop
        // distance, so the clamped witch trailed forever and never swung.
        if (cooldownReady)
        {
            Vector3 after = _target.position - transform.position;
            after.y = 0f;
            if (after.magnitude <= attackRange + 0.05f)
            {
                BeginAttack();
                return;
            }
        }
    }

    // ------------------------------------------------------------- pack spacing

    /// <summary>Ring radius this witch holds while it is not one of the attackers.</summary>
    private float HoldRingDistance()
    {
        return holdRingBase + (packSlot % 3) * holdRingStep;
    }

    /// <summary>
    /// How many live pack mates are currently closer to my victim than I am.
    /// Distance ties are broken by instance id so exactly maxAttackers witches
    /// claim a slot instead of the whole pack flipping state every frame.
    /// </summary>
    private int CountCloserChasers()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.activeWitches == null) return 0;

        float myDist = FlatDistance(_target.position);
        int closer = 0;
        for (int i = 0; i < gm.activeWitches.Count; i++)
        {
            var w = gm.activeWitches[i];
            if (w == null || w == this || !w.isActiveAndEnabled) continue;
            float d = w.FlatDistanceTo(_target.position);
            if (d < myDist - 0.15f) closer++;
            else if (d <= myDist + 0.15f && w.packSlot < packSlot) closer++;
        }
        return closer;
    }

    /// <summary>Sum of repulsion from pack mates inside separationRadius.</summary>
    private Vector3 SeparationPush()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.activeWitches == null) return Vector3.zero;

        Vector3 push = Vector3.zero;
        for (int i = 0; i < gm.activeWitches.Count; i++)
        {
            var w = gm.activeWitches[i];
            if (w == null || w == this) continue;

            Vector3 d = transform.position - w.transform.position;
            d.y = 0f;
            float m = d.magnitude;
            if (m >= separationRadius) continue;

            if (m < 0.05f)
            {
                // Perfectly stacked: split along a stable per-slot direction.
                d = Quaternion.Euler(0f, packSlot * 60f, 0f) * Vector3.forward;
                m = 0.05f;
            }
            push += d / m * (1f - m / separationRadius);
        }
        return push;
    }

    /// <summary>
    /// Hard floor on horizontal distance to the victim. Sidesteps and pushes can
    /// otherwise walk a chaser into the camera, which is what made the view fill
    /// with monster geometry.
    /// </summary>
    private void EnforceStandoff()
    {
        if (_target == null) return;

        Vector3 d = transform.position - _target.position;
        d.y = 0f;
        float m = d.magnitude;
        float min = Mathf.Max(cameraClearance, attackRange * 0.8f);
        if (m >= min || m < 0.0001f) return;

        transform.position += d / m * (min - m);
    }

    /// <summary>Flat distance to the closest other witch (QA: proves nobody stacks).</summary>
    private float NearestPackDistance()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.activeWitches == null) return -1f;
        float best = float.MaxValue;
        for (int i = 0; i < gm.activeWitches.Count; i++)
        {
            var w = gm.activeWitches[i];
            if (w == null || w == this) continue;
            float d = FlatDistance(w.transform.position);
            if (d < best) best = d;
        }
        return best == float.MaxValue ? -1f : best;
    }

    /// <summary>Flat distance from this witch to an arbitrary point (QA + slot maths).</summary>
    public float FlatDistanceTo(Vector3 p) { return FlatDistance(p); }

    // ------------------------------------------------------------ perception

    /// <summary>
    /// Line of sight plus range. A visible player inside awarenessRange is seen;
    /// anyone inside hearingRange is heard through walls (sprinting is louder).
    /// Without either, the monster keeps only the last position it actually knew.
    /// </summary>
    private void UpdatePerception()
    {
        // QA-only (-nohunt): lets the harness measure how companions and the
        // player navigate without six hunters ending the round first. Never set
        // in normal play.
        if (HuntingDisabled)
        {
            _targetSurvivor = null;
            _targetPlayer = null;
            return;
        }

        _perceptionTimer -= Time.deltaTime;
        if (_perceptionTimer > 0f) return;
        _perceptionTimer = perceptionInterval;

        var gm = GameManager.Instance;
        if (gm == null) return;

        Transform best = null;
        PlayerController bestPlayer = null;
        SurvivorAI bestSurvivor = null;
        float bestDist = float.MaxValue;

        if (gm.player != null && !gm.player.IsDead && gm.player.currentHealth > 0f)
            ConsiderPerceived(gm.player.transform, gm.player, null, ref best, ref bestPlayer, ref bestSurvivor, ref bestDist);

        if (gm.survivors != null)
        {
            for (int i = 0; i < gm.survivors.Count; i++)
            {
                var s = gm.survivors[i];
                if (s == null || s.IsDead || s.healthState == SurvivorHealthState.Dead) continue;
                if (s.healthState == SurvivorHealthState.Downed) continue;
                if (!s.gameObject.activeInHierarchy) continue;
                ConsiderPerceived(s.transform, null, s, ref best, ref bestPlayer, ref bestSurvivor, ref bestDist);
            }
        }

        if (best != null)
        {
            _target = best;
            _targetPlayer = bestPlayer;
            _targetSurvivor = bestSurvivor;
            _hasTarget = true;
            _lastKnownPos = best.position;
            _searchPhase = 0;
        }
        else if (_hasTarget)
        {
            // Lost the trail: stop pretending we know where the player is.
            _hasTarget = false;
            BeginSearch();
        }
    }

    private void ConsiderPerceived(Transform t, PlayerController player, SurvivorAI survivor,
        ref Transform best, ref PlayerController bestPlayer, ref SurvivorAI bestSurvivor, ref float bestDist)
    {
        if (t == null) return;
        if (t == transform || t.IsChildOf(transform)) return;
        if (t.GetComponentInParent<WitchAI>() != null) return;

        Vector3 flat = t.position - transform.position;
        flat.y = 0f;
        float d = flat.magnitude;

        float hear = hearingRange;
        float see = awarenessRange;

        if (player != null)
        {
            if (player.IsSprinting) hear *= sprintHearingMultiplier;

            // ---- skill interaction -------------------------------------
            // Phase Shift makes the player untouchable, so it also makes them
            // undetectable. Invisibility leaves only a very short "they are
            // almost standing on me" radius, so it breaks a chase but is not a
            // free pass to walk through a witch.
            var sk = player.skillController;
            if (sk != null && sk.isSkillActive)
            {
                if (sk.IsPhasing)
                {
                    hear = 0f;
                    see = 0f;
                }
                else if (sk.IsInvisible)
                {
                    hear = Mathf.Min(hear, 3.0f);
                    see = Mathf.Min(see, 4.5f);
                }
            }
        }

        bool heard = hear > 0f && d <= hear;
        bool seen = see > 0f && d <= see && HasLineOfSight(t);
        if (!seen && !heard) return;

        if (d < bestDist)
        {
            bestDist = d;
            best = t;
            bestPlayer = player;
            bestSurvivor = survivor;
        }
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 eye = transform.position + Vector3.up * 1.55f;
        Vector3 chest = target.position + Vector3.up * 1.1f;
        RaycastHit hit;
        if (!Physics.Linecast(eye, chest, out hit, obstacleLayer, QueryTriggerInteraction.Ignore)) return true;
        if (hit.transform == null) return true;
        // Hitting the victim (or any character) is not an occlusion.
        return IsCharacterCollider(hit.transform) || hit.transform.IsChildOf(target);
    }

    // --------------------------------------------------------------- searching

    private void BeginSearch()
    {
        _searchPhase = 0;
        _searchPhaseTimer = 0f;
        _searchAroundTimer = 0f;
        _searchDestination = _lastKnownPos;
        Debug.Log($"[AI] {Name} lost the player -> moving to last known position {_lastKnownPos.ToString("F1")}");
    }

    private void UpdateSearch()
    {
        _searchPhaseTimer += Time.deltaTime;

        if (_searchPhase == 0)
        {
            if (FlatDistance(_searchDestination) < 2.5f || _searchPhaseTimer > 10f)
            {
                _searchPhase = 1;
                _searchPhaseTimer = 0f;
                _searchAroundTimer = 0f;
                PickSearchPoint();
            }
        }
        else if (_searchPhase == 1)
        {
            _searchAroundTimer += Time.deltaTime;
            if (FlatDistance(_searchDestination) < 2f || _searchPhaseTimer > 6f)
            {
                _searchPhaseTimer = 0f;
                PickSearchPoint();
            }
            if (_searchAroundTimer >= searchDuration)
            {
                _searchPhase = 2;
                _searchPhaseTimer = 0f;
                PickWanderPoint();
            }
        }
        else
        {
            if (FlatDistance(_searchDestination) < 2.5f || _searchPhaseTimer > 8f)
            {
                _searchPhaseTimer = 0f;
                PickWanderPoint();
            }
        }

        SetState(_searchPhase == 2 ? WitchState.Wander : WitchState.Search);
        MoveToward(_searchDestination, moveSpeed, 0.5f);
    }

    private void PickSearchPoint()
    {
        // Offset by the pack slot so six searchers do not sweep one identical arc.
        float angle = Random.Range(0f, 360f);
        float radius = Random.Range(searchRadius * 0.35f, searchRadius);
        Vector3 p = _lastKnownPos + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
        p.y = _lastKnownPos.y;
        _searchDestination = ClampToTown(p);
    }

    private void PickWanderPoint()
    {
        float angle = Random.Range(0f, 360f);
        float radius = Random.Range(6f, wanderRadius);
        Vector3 p = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
        p.y = 0f;
        _searchDestination = ClampToTown(p);
    }

    private static Vector3 ClampToTown(Vector3 p)
    {
        // Town square + the north open field: witches are allowed to follow the
        // player out into the open instead of stopping at the old wall line.
        return TownLayout.ClampToPlayable(p, 4f);
    }

    private float FlatDistance(Vector3 p)
    {
        Vector3 d = p - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private void MoveToward(Vector3 destination, float speed, float stopDistance)
    {
        float realDist = FlatDistance(destination);
        if (realDist <= stopDistance)
        {
            if (_animator != null) _animator.speed = 0f;
            return;
        }

        // Town <-> field travel is funnelled through the gate, so a witch never
        // grinds against the north wall on the way in or out.
        Vector3 aim = TownLayout.RouteViaGate(transform.position, destination);

        Vector3 to = aim - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.001f) return;

        Vector3 dir = to / dist;
        dir = AvoidObstacles(dir);

        float step = Mathf.Min(speed * Time.deltaTime, Mathf.Max(0f, realDist - stopDistance));
        if (step > 0f) transform.position += dir * step;

        FaceDirection(dir, turnSpeedDegrees);
        UpdateMeasuredSpeed();
        if (_animator != null) _animator.speed = _measuredSpeed;
        SnapToGround();
        RecoverIfStuck(dir);
    }

    // -------------------------------------------------------- obstacle handling

    /// <summary>
    /// Cheap local steering: if the straight line is blocked, try progressively
    /// wider angles until one is clear. Stops a chaser from grinding into a
    /// building corner without needing a full path solver.
    /// </summary>
    private Vector3 AvoidObstacles(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return dir;

        Vector3 origin = transform.position + Vector3.up * 0.9f;
        if (!Physics.SphereCast(origin, 0.4f, dir, out _, 2.2f, obstacleLayer, QueryTriggerInteraction.Ignore))
            return dir;

        float[] angles = { 35f, -35f, 70f, -70f, 105f, -105f };
        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 alt = Quaternion.Euler(0f, angles[i], 0f) * dir;
            if (!Physics.SphereCast(origin, 0.4f, alt, out _, 2.2f, obstacleLayer, QueryTriggerInteraction.Ignore))
                return alt;
        }

        return Quaternion.Euler(0f, 140f, 0f) * dir;
    }

    /// <summary>If a monster has barely moved for a second, sidestep for a moment.</summary>
    private void RecoverIfStuck(Vector3 dir)
    {
        if (_detourTimer > 0f)
        {
            _detourTimer -= Time.deltaTime;
            Vector3 sidestep = (_detourDir + dir).normalized;
            transform.position += sidestep * Mathf.Max(moveSpeed, chaseSpeed) * 0.6f * Time.deltaTime;
            return;
        }

        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < 1f) return;

        if (Vector3.Distance(transform.position, _stuckCheckPos) < 0.6f)
        {
            _detourDir = Vector3.Cross(Vector3.up, dir).normalized * (Random.value < 0.5f ? 1f : -1f);
            _detourTimer = 1.1f;
            if (VerboseCombat) Debug.Log($"[AI] {Name} stuck -> sidestep");
        }

        _stuckCheckPos = transform.position;
        _stuckTimer = 0f;
    }

    private void UpdateMeasuredSpeed()
    {
        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        float instant = Vector3.Distance(transform.position, _prevPos) / dt;
        _measuredSpeed = Mathf.Lerp(_measuredSpeed, instant, 12f * dt);
    }

    // ------------------------------------------------------------- targeting

    private bool IsTargetValid()
    {
        if (_target == null) return false;
        if (_target == transform || _target.IsChildOf(transform)) return false;
        if (_targetSurvivor != null) return !_targetSurvivor.IsDead && _targetSurvivor.healthState != SurvivorHealthState.Dead && _targetSurvivor.healthState != SurvivorHealthState.Downed && _targetSurvivor.gameObject.activeInHierarchy;
        if (_targetPlayer != null) return !_targetPlayer.IsDead && _targetPlayer.currentHealth > 0f;
        return true;
    }

    private void AcquireTarget()
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        _targetSurvivor = null;
        _targetPlayer = null;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // player (invalid the moment they die - witches then hunt survivors)
        if (gm.player != null && !gm.player.IsDead && gm.player.currentHealth > 0f)
            Consider(gm.player.transform, gm.player, null, ref best, ref bestDist);

        // survivors (downed teammates are left for a rescue, not finished off)
        if (gm.survivors != null)
        {
            for (int i = 0; i < gm.survivors.Count; i++)
            {
                var s = gm.survivors[i];
                if (s == null || s.IsDead || s.healthState == SurvivorHealthState.Dead) continue;
                if (s.healthState == SurvivorHealthState.Downed) continue;
                if (!s.gameObject.activeInHierarchy) continue;
                Consider(s.transform, null, s, ref best, ref bestDist);
            }
        }

        if (best != _target)
        {
            _target = best;
            if (best != null) OnSurvivorDetectedEvent?.Invoke(best);
        }
    }

    private void Consider(Transform t, PlayerController player, SurvivorAI survivor, ref Transform best, ref float bestDist)
    {
        if (t == null) return;
        if (t == transform || t.IsChildOf(transform)) return;   // never self
        if (t.GetComponentInParent<WitchAI>() != null) return;  // never another witch
        float d = (t.position - transform.position).sqrMagnitude;
        if (d < bestDist)
        {
            bestDist = d; best = t;
            _targetPlayer = player;
            _targetSurvivor = survivor;
        }
    }

    // -------------------------------------------------------------- movement

    private void FaceDirection(Vector3 dir, float speedDegreesPerSecond)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
        // Fast turn so the face never trails the run for long.
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, speedDegreesPerSecond * Time.deltaTime);
    }

    private void SnapToGround()
    {
        // Cast from above the witch's head downwards and take the highest
        // walkable hit. Two exclusions matter:
        //  - our own body, or the witch climbs into the air;
        //  - other characters and anything more than a step above the current
        //    foot height, or the witch stands on a player's head / a rooftop
        //    instead of continuing the chase on the road.
        float stepUp = 0.75f;
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 2f, Vector3.down, groundSnapHeight, ~0, QueryTriggerInteraction.Ignore);
        float best = float.NaN;
        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (IsCharacterCollider(t)) continue;
            if (hits[i].point.y > transform.position.y + stepUp) continue;
            if (float.IsNaN(best) || hits[i].point.y > best) best = hits[i].point.y;
        }
        if (!float.IsNaN(best))
        {
            Vector3 p = transform.position;
            // Winged witches visibly hover: feet never touch the road, and the
            // bob keeps the silhouette reading as "airborne" at medium range.
            float bob = hoverHeight > 0.01f ? Mathf.Sin(Time.time * 2.2f) * 0.08f : 0f;
            p.y = best + hoverHeight + bob;
            transform.position = p;
        }
    }

    private static bool IsCharacterCollider(Transform t)
    {
        return t.GetComponentInParent<PlayerController>() != null
            || t.GetComponentInParent<SurvivorAI>() != null
            || t.GetComponentInParent<WitchAI>() != null;
    }

    // ------------------------------------------------------- attack lifecycle

    private void BeginAttack()
    {
        _attackPhase = AttackPhase.Windup;
        _phaseTimer = 0f;
        SetState(WitchState.Attack);
        if (_animator != null)
        {
            _animator.PlayAttack(attackWindup, attackRecovery);
            _animator.speed = 0f;
        }
        if (VerboseCombat)
            Debug.Log($"[Combat] {Name} AttackStarted phase=Windup range={attackRange:F1} " +
                      $"windup={attackWindup:F2} target={TargetLabel()}");
    }

    private void RunAttackLifecycle(float dist, Vector3 dir)
    {
        // The witch is planted at its attack distance: face the victim, keep the
        // feet still, and let the procedural pose carry the swing.
        FaceDirection(dir, turnSpeedDegrees);
        UpdateMeasuredSpeed();
        if (_animator != null) _animator.speed = 0f;
        SnapToGround();

        _phaseTimer += Time.deltaTime;

        if (_attackPhase == AttackPhase.Windup)
        {
            if (_phaseTimer < attackWindup) return;
            ResolveImpact(dist);
            _attackPhase = AttackPhase.Recovery;
            _phaseTimer = 0f;
            return;
        }

        if (_phaseTimer >= attackRecovery)
        {
            _attackPhase = AttackPhase.None;
            _lastAttackTime = Time.time;
            if (VerboseCombat)
                Debug.Log($"[Combat] {Name} AttackRecovered cooldown={attackCooldown:F2}");
        }
    }

    /// <summary>
    /// Damage is resolved exactly once, on the impact frame, and only if the
    /// victim is still inside reach. Stepping away during the wind-up misses.
    /// </summary>
    private void ResolveImpact(float dist)
    {
        bool inRange = dist <= attackRange + impactRangeSlack;

        if (_targetSurvivor != null)
        {
            if (_targetSurvivor.IsDead || _targetSurvivor.healthState == SurvivorHealthState.Dead)
            {
                if (VerboseCombat) Debug.Log($"[Combat] {Name} AttackMissed target died before impact");
                return;
            }
            if (!inRange)
            {
                if (VerboseCombat) Debug.Log($"[Combat] {Name} AttackMissed dist={dist:F2} range={attackRange:F1} (escapable)");
                return;
            }

            float before = _targetSurvivor.currentHealth;
            _targetSurvivor.TakeDamage(attackDamage, transform);
            ImpactVFX.Spawn(_targetSurvivor.transform.position + Vector3.up * 1.1f, new Color(0.85f, 0.08f, 0.10f));

            // Blood feedback: spray away from the witch, stain the ground.
            Vector3 awayS = _targetSurvivor.transform.position - transform.position;
            awayS.y = 0f;
            BloodVFX.SpawnImpact(_targetSurvivor.transform.position + Vector3.up * 1.05f, awayS, 0.9f);
            if (Random.value < 0.55f)
                BloodVFX.SpawnGroundDecal(_targetSurvivor.transform.position, 0.7f);

            if (VerboseCombat)
                Debug.Log($"[Combat] {Name} AttackImpact target={_targetSurvivor.DisplayName} " +
                          $"damage={attackDamage:F0} hpBefore={before:F0} hpAfter={_targetSurvivor.currentHealth:F0} " +
                          $"dist={dist:F2} state={_targetSurvivor.healthState}");
            return;
        }

        if (_targetPlayer != null)
        {
            if (_targetPlayer.IsDead)
            {
                if (VerboseCombat) Debug.Log($"[Combat] {Name} AttackMissed target already dead");
                return;
            }
            if (!inRange)
            {
                if (VerboseCombat) Debug.Log($"[Combat] {Name} AttackMissed dist={dist:F2} range={attackRange:F1} (escapable)");
                return;
            }

            // Phase Shift / Invisibility block the hit
            var sk = _targetPlayer.skillController;
            if (sk != null && sk.isSkillActive && sk.equippedSkill != null &&
                (sk.equippedSkill.skillType == SkillType.Phase || sk.equippedSkill.skillType == SkillType.Invisibility))
            {
                if (VerboseCombat) Debug.Log($"[Combat] {Name} AttackBlocked by {sk.equippedSkill.skillName}");
                return;
            }

            float before = _targetPlayer.currentHealth;
            _targetPlayer.TakeDamage(attackDamage);
            ImpactVFX.Spawn(_targetPlayer.transform.position + Vector3.up * 1.1f, new Color(0.85f, 0.08f, 0.10f));

            // The blood spray and the screen wash are owned by PlayerController,
            // so every damage source (not just this one) produces the same
            // feedback and nothing is played twice.

            if (VerboseCombat)
                Debug.Log($"[Combat] {Name} AttackImpact target=Player damage={attackDamage:F0} " +
                          $"hpBefore={before:F0} hpAfter={_targetPlayer.currentHealth:F0} dist={dist:F2} " +
                          $"dead={_targetPlayer.IsDead}");
            OnAttackEvent?.Invoke(_targetPlayer);
        }
    }

    private void CancelAttack()
    {
        if (_attackPhase == AttackPhase.None) return;
        _attackPhase = AttackPhase.None;
        _phaseTimer = 0f;
        if (_animator != null) _animator.CancelAttack();
        if (VerboseCombat) Debug.Log($"[Combat] {Name} AttackCancelled target={TargetLabel()}");
    }

    private string TargetLabel()
    {
        if (_targetSurvivor != null) return _targetSurvivor.DisplayName;
        if (_targetPlayer != null) return "Player";
        return "NONE";
    }

    public string Name { get { return witchData != null ? witchData.witchName : name; } }

    public void SetState(WitchState newState)
    {
        if (currentState == newState) return;
        previousState = currentState;
        currentState = newState;
        OnStateChangedEvent?.Invoke(newState, previousState);
    }

    public void SetTarget(Transform target) { _target = target; }

    /// <summary>Diagnostic: how far this witch actually moved over the last second.</summary>
    public float DistanceMovedLastSecond { get { return _lastMoveSpeed; } }
    private float _lastMoveSpeed;
    private float _moveAccum;
    private float _moveTimer;
    private void LateUpdate()
    {
        float d = Vector3.Distance(transform.position, _lastPos);
        _lastPos = transform.position;
        _moveAccum += d;
        _moveTimer += Time.deltaTime;
        if (_moveTimer >= 1f) { _lastMoveSpeed = _moveAccum / _moveTimer; _moveAccum = 0f; _moveTimer = 0f; }
    }
}

public enum WitchState
{
    Idle,
    Patrol,
    Wander,
    HearSound,
    Investigate,
    DetectSurvivor,
    Chase,
    Attack,
    Search,
    ReturnToPatrol,
    SpecialAbility
}

public enum SoundType
{
    Footstep,
    Sprint,
    Generator,
    Interaction,
    Skill,
    Distress
}
