using System;
using UnityEngine;

/// <summary>
/// QA-only navigation logger. Only created when the player is launched with
/// "-navlog". It prints the player's world position, horizontal speed and
/// stuck state every 0.5 s so a harness run can prove that the north field is
/// really walkable, that no invisible wall stops movement, and that the
/// gate connection is passable. It never changes gameplay or input.
/// </summary>
public class NavProbe : MonoBehaviour
{
    private const float Interval = 0.5f;

    private PlayerController player;
    private float timer;
    private float elapsed;
    private Vector3 lastPos;
    private float travelled;
    private int stuckFrames;
    private bool spawnApplied;
    private Vector3 navSpawn;
    private bool hasNavSpawn;

    // ---- companion tracking ------------------------------------------------
    private SurvivorAI[] _companions;
    private Vector3[] _compLastPos;
    private float[] _compStuck;
    private bool[] _compCrossed;
    private float[] _compCrossTime;
    private int[] _compZone;
    private float _compTimer;
    private float _compLogTimer;
    private int _compCrossedCount;
    private float _roundStart = -1f;
    private bool _roundWasLive;
    private bool _splatTest;
    private bool _splatSpawned;
    private float _splatVerifyAt = -1f;

    public static bool Requested
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-navlog") return true;
            return false;
        }
    }

    private void Start()
    {
        var gm = GameManager.Instance;
        player = gm != null ? gm.player : FindObjectOfType<PlayerController>();
        if (player != null) lastPos = player.transform.position;

        // "-navspawn X Z" drops the player on a chosen spot once the round goes
        // live. Navigation of the empty field cannot be measured reliably while
        // six witches converge on the spawn point, so this lets the harness test
        // the surface itself. It never runs unless the flag is passed.
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "-splattest") _splatTest = true;

        for (int i = 0; i < args.Length - 2; i++)
        {
            if (args[i] != "-navspawn") continue;
            float x, z;
            if (float.TryParse(args[i + 1], out x) && float.TryParse(args[i + 2], out z))
            {
                navSpawn = new Vector3(x, 0f, z);
                hasNavSpawn = true;
            }
        }

        Debug.Log("[NAV] NavProbe attached (player=" + (player != null) + ")" +
                  (hasNavSpawn ? " spawn=" + navSpawn.ToString("0.0") : ""));
    }

    private void ApplyNavSpawn()
    {
        if (!hasNavSpawn || spawnApplied || player == null) return;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Gameplay) return;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = navSpawn + Vector3.up * 0.1f;
        if (cc != null) cc.enabled = true;

        spawnApplied = true;
        lastPos = player.transform.position;
        travelled = 0f;
        Debug.Log("[NAV] spawn override applied -> " + player.transform.position.ToString("0.00"));
    }

    private void Update()
    {
        if (player == null)
        {
            var gm = GameManager.Instance;
            player = gm != null ? gm.player : FindObjectOfType<PlayerController>();
            if (player == null) return;
        }

        // Re-arm the override for the next round when a run ends.
        if (hasNavSpawn && GameManager.Instance != null &&
            GameManager.Instance.currentState != GameState.Gameplay)
            spawnApplied = false;

        ApplyNavSpawn();

        bool live = GameManager.Instance != null &&
                    GameManager.Instance.currentState == GameState.Gameplay;
        if (live && !_roundWasLive)
        {
            _roundStart = Time.time;
            _companions = null;          // roster is rebuilt every round
            _compCrossedCount = 0;
            Debug.Log("[NAV] round start observed; companion tracking armed");
        }
        _roundWasLive = live;

        if (live) TickCompanions();
        if (live) RunSplatTest();
        if (live) VerifySplatTest();

        elapsed += Time.deltaTime;
        timer += Time.deltaTime;
        if (timer < Interval) return;
        timer = 0f;

        Vector3 p = player.transform.position;
        Vector3 flat = new Vector3(p.x - lastPos.x, 0f, p.z - lastPos.z);
        float step = flat.magnitude;
        travelled += step;

        bool keyMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                         Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                         PlayerController.ForceForwardForQA;

        // A held direction key with no displacement over 1.5 s means a blocker.
        if (keyMoving && step < 0.05f) stuckFrames++;
        else stuckFrames = 0;

        string zone = p.z > TownLayout.FieldStartZ
            ? (p.z > TownLayout.FieldEndZ ? "OUTSIDE" : "FIELD")
            : "TOWN";

        Debug.Log(string.Format(
            "[NAV] t={0:0.0}s pos=({1:0.00},{2:0.00},{3:0.00}) zone={4} step={5:0.00} " +
            "speed={6:0.00} sprint={7} dead={8} travelled={9:0.0} stuck={10}",
            elapsed, p.x, p.y, p.z, zone, step, player.CurrentSpeed,
            player.IsSprinting, player.IsDead, travelled, stuckFrames));

        if (stuckFrames == 3)
        {
            Debug.LogWarning("[NAV] STUCK: direction held but no movement at (" +
                             p.x.ToString("0.00") + ", " + p.z.ToString("0.00") + ")");

            // Name whatever is holding the player, so a harness run can tell a
            // real prop from an invisible barrier.
            Vector3 dir = player.transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 1.0f, dir, out hit, 3.0f))
            {
                Debug.LogWarning("[NAV] BLOCKER: '" + hit.collider.name + "' at " +
                                 hit.collider.transform.position.ToString("0.00") +
                                 " dist=" + hit.distance.ToString("0.00") +
                                 " parent='" + (hit.collider.transform.parent != null
                                     ? hit.collider.transform.parent.name : "none") + "'");
            }
            else
            {
                Debug.LogWarning("[NAV] BLOCKER: no collider within 3 m (possible invisible barrier)");
            }
        }

        lastPos = p;
    }

    // =====================================================================
    // splatter appearance check
    // =====================================================================

    /// <summary>
    /// QA-only (-splattest): lays one row of ground splatters and one impact
    /// burst in front of the player at the start of a round, so the decal art
    /// can be photographed without having to wait to be mauled. The row sits
    /// six metres out because anything closer projects under the HUD bar.
    /// </summary>
    private void RunSplatTest()
    {
        if (!_splatTest || _splatSpawned || player == null) return;

        Vector3 fwd = player.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        BloodVFX.VerboseGroundRay = true;

        // A full ring, not a forward row: if the player transform and the
        // camera ever disagree about which way is "ahead", a row can land
        // entirely behind the view and look exactly like a rendering bug.
        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            Vector3 spot = player.transform.position + dir * 4.5f;
            BloodVFX.SpawnGroundDecal(spot, 1.0f);
            Debug.Log("[NAV] splattest decal " + i + " requested at " + spot.ToString("0.00"));
        }

        BloodVFX.SpawnImpact(player.transform.position + fwd * 6.0f + Vector3.up * 1.1f, fwd, 1.1f);

        _splatSpawned = true;
        _splatVerifyAt = Time.time + 1.2f;
        Debug.Log("[NAV] splattest: 8 decals in a 4.5 m ring + 1 impact 6.0 m ahead");
    }

    /// <summary>
    /// Confirms the stains are still enabled and actually being drawn a second
    /// after they spawn. Without this a "no stains on screen" screenshot is
    /// ambiguous: it could be a material problem or a visibility problem.
    /// </summary>
    private void VerifySplatTest()
    {
        if (_splatVerifyAt < 0f || Time.time < _splatVerifyAt) return;
        _splatVerifyAt = -1f;

        var root = GameObject.Find("BloodDecals");
        if (root == null)
        {
            Debug.LogWarning("[NAV] splattest verify: BloodDecals root missing");
            return;
        }

        int active = 0, visible = 0;
        MeshRenderer sample = null;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            active++;
            var mr = child.GetComponent<MeshRenderer>();
            if (mr != null && mr.isVisible) visible++;
            if (sample == null) sample = mr;
        }

        string shaderName = "none";
        string texName = "none";
        if (sample != null && sample.sharedMaterial != null)
        {
            shaderName = sample.sharedMaterial.shader != null ? sample.sharedMaterial.shader.name : "NULL";
            texName = sample.sharedMaterial.mainTexture != null ? sample.sharedMaterial.mainTexture.name : "NULL";
        }

        Debug.Log("[NAV] splattest verify: active=" + active + " visible=" + visible +
                  " shader=" + shaderName + " tex=" + texName +
                  (sample != null ? " pos=" + sample.transform.position.ToString("0.00") +
                                    " scale=" + sample.transform.localScale.ToString("0.00") : ""));
    }

    // =====================================================================
    // companion tracking
    // =====================================================================

    private static int ZoneOf(Vector3 p)
    {
        return p.z > TownLayout.FieldStartZ ? 1 : 0;
    }

    private void AllocateCompanions(System.Collections.Generic.List<SurvivorAI> list)
    {
        int n = list.Count;
        _companions = new SurvivorAI[n];
        _compLastPos = new Vector3[n];
        _compStuck = new float[n];
        _compCrossed = new bool[n];
        _compCrossTime = new float[n];
        _compZone = new int[n];

        for (int i = 0; i < n; i++)
        {
            _companions[i] = list[i];
            if (_companions[i] == null) continue;
            _compLastPos[i] = _companions[i].transform.position;
            _compZone[i] = ZoneOf(_compLastPos[i]);
        }

        _compCrossedCount = 0;
        Debug.Log("[NAV] tracking " + n + " companion(s)");
    }

    private bool SameRoster(System.Collections.Generic.List<SurvivorAI> list)
    {
        if (_companions == null || list == null || _companions.Length != list.Count) return false;
        for (int i = 0; i < _companions.Length; i++)
            if (!ReferenceEquals(_companions[i], list[i])) return false;
        return true;
    }

    /// <summary>
    /// Follows the three AI survivors and records the exact moment each one
    /// steps past the town wall into the field, so "did they get through the
    /// gate, and how long did it take" is measured rather than guessed.
    /// </summary>
    private void TickCompanions()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var list = gm.survivors;
        if (list == null || list.Count == 0) return;

        if (!SameRoster(list)) AllocateCompanions(list);
        if (_companions == null) return;

        _compLogTimer += Time.deltaTime;
        bool doLog = _compLogTimer >= 1.0f;
        if (doLog) _compLogTimer = 0f;

        float roundT = _roundStart > 0f ? Time.time - _roundStart : 0f;
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

        for (int i = 0; i < _companions.Length; i++)
        {
            var c = _companions[i];
            if (c == null) continue;

            Vector3 p = c.transform.position;
            float moved = new Vector2(p.x - _compLastPos[i].x, p.z - _compLastPos[i].z).magnitude;

            // A body that stopped moving because it is dead or downed is not a
            // navigation failure, so it must not raise the stuck warning.
            if (c.IsDead || c.currentState == SurvivorState.Downed) _compStuck[i] = 0f;
            else if (moved < 0.03f) _compStuck[i] += Time.deltaTime;
            else _compStuck[i] = 0f;

            int zone = ZoneOf(p);
            if (zone == 1 && _compZone[i] == 0 && !_compCrossed[i])
            {
                _compCrossed[i] = true;
                _compCrossTime[i] = roundT;
                _compCrossedCount++;
                Debug.Log($"[NAV] companion {c.DisplayName} CROSSED the gate at round t={roundT:F1}s " +
                          $"pos=({p.x:F1},{p.z:F1})");
                if (_compCrossedCount >= _companions.Length)
                    Debug.Log($"[NAV] ALL {_companions.Length} companions through the gate " +
                              $"(times: {CrossSummary()})");
            }
            _compZone[i] = zone;

            if (doLog)
            {
                float nearestWitch = NearestWitchDistance(p);
                Debug.Log($"[NAV] companion {c.DisplayName} pos=({p.x:F1},{p.z:F1}) " +
                          $"zone={(zone == 1 ? "FIELD" : "TOWN")} moved={moved:F2} " +
                          $"stuck={_compStuck[i]:F1}s distPlayer={Vector3.Distance(p, playerPos):F1} " +
                          $"state={c.currentState} witch={nearestWitch:F1} dead={c.IsDead}");
            }

            // Idle in town while the player is already north of the wall.
            if (_compStuck[i] > 3f && zone == 0 && ZoneOf(playerPos) == 1)
            {
                Debug.LogWarning($"[NAV] COMPANION STUCK {c.DisplayName} at ({p.x:F1},{p.z:F1}) " +
                                 $"for {_compStuck[i]:F1}s while the player is in the field");
                _compStuck[i] = 0f;
            }

            _compLastPos[i] = p;
        }
    }

    private static float NearestWitchDistance(Vector3 from)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.activeWitches == null) return -1f;

        float best = float.MaxValue;
        for (int i = 0; i < gm.activeWitches.Count; i++)
        {
            var w = gm.activeWitches[i];
            if (w == null) continue;
            float d = Vector3.Distance(from, w.transform.position);
            if (d < best) best = d;
        }
        return best == float.MaxValue ? -1f : best;
    }

    private string CrossSummary()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _companions.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(_companions[i] != null ? _companions[i].DisplayName : "?");
            sb.Append(_compCrossed[i] ? " " + _compCrossTime[i].ToString("F1") + "s" : " NOT-YET");
        }
        return sb.ToString();
    }
}
