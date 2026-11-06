using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns and spreads the chasing witches.
///
/// * Normal play: 1 witch at start, more as generators are repaired.
/// * Debug/test:  launch with "-allwitches" (or set the env var
///   WITCHBOUND_DEBUG_ALL_WITCHES=true) to spawn all six immediately, spread
///   around the town at ~25/40/50/60/70/80 m.
/// </summary>
public class WitchSpawner : MonoBehaviour
{
    public static WitchSpawner Instance { get; private set; }

    [Header("Data")]
    public GameObject witchPrefab;
    public WitchData[] witchDataArray;

    [Header("Spawn Settings")]
    // Compact town: witches spawn 20-45m from player
    public float minSpawnDistanceFromPlayer = 20f;
    public float spawnRadius = 45f;
    public float spawnCooldown = 10f;

    [Header("Debug")]
    public bool allWitchesImmediately = false;

    private static readonly string[] Order = { "Rona Tanyel", "Demonisa Belle", "Kyla Ni-Garcia", "Jannak", "Sandy Vill", "Beamoth" };
    // Chase mode: all six exist from the first frame. They are spread on a ring
    // wide enough that nobody starts on top of the player, and far enough apart
    // that the roster never stacks into one cluster.
    private static readonly float[] Distances = { 40f, 42f, 44f, 46f, 47f, 48f };
    private static readonly float[] Angles = { 20f, 80f, 140f, 200f, 260f, 320f };

    private List<WitchAI> spawnedWitches = new List<WitchAI>();
    private readonly List<string> usedWitchNames = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        EnsureWitchData();
        allWitchesImmediately = true;

        // No objective or time gate: the full roster is present from the start.
        if (FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude).Length == 0) SpawnAllAndSpread();

        Debug.Log($"[WITCH SPAWNER] chase mode: spawned={spawnedWitches.Count} " +
                  $"names=[{string.Join(", ", LiveWitchNames().ToArray())}]");
    }

    private bool WantsAllWitches()
    {
        try
        {
            if (System.Environment.GetEnvironmentVariable("WITCHBOUND_DEBUG_ALL_WITCHES") == "true") return true;
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-allwitches" || a == "-visualtest") return true;
        }
        catch { }
        return false;
    }

    private void EnsureWitchData()
    {
        var loaded = Resources.LoadAll<WitchData>("");
        var merged = new List<WitchData>();
        foreach (var name in Order)
        {
            WitchData found = null;
            // Match on the official name so a data asset still carrying a
            // previous name resolves to the same witch instead of being dropped.
            if (witchDataArray != null)
                foreach (var d in witchDataArray) if (d != null && CharacterLore.OfficialName(d.witchName) == name) { found = d; break; }
            if (found == null && loaded != null)
                foreach (var d in loaded) if (d != null && CharacterLore.OfficialName(d.witchName) == name) { found = d; break; }
            if (found != null) merged.Add(found);
        }
        if (merged.Count > 0) witchDataArray = merged.ToArray();
        Debug.Log($"[WITCH SPAWNER] witch data resolved: {merged.Count}/6 ({string.Join(", ", merged.ConvertAll(d => d.witchName).ToArray())})");
    }

    // ------------------------------------------------------------ public API

    public void RemoveWitch(WitchAI witch) { spawnedWitches.Remove(witch); }
    public int GetSpawnedWitchCount() { return spawnedWitches.Count; }
    public int MaxWitches { get { return GameManager.Instance != null && GameManager.Instance.gameConfig != null ? GameManager.Instance.gameConfig.maxWitches : 6; } }
    public int StartingWitches { get { return GameManager.Instance != null && GameManager.Instance.gameConfig != null ? GameManager.Instance.gameConfig.startingWitches : 1; } }

    /// <summary>
    /// Destroys every witch from the previous run and starts the roster again:
    /// one witch in normal play, all six in debug/-visualtest mode. Deactivate
    /// first so old AI cannot act during the frame before Destroy resolves.
    /// </summary>
    public void ResetForNewSession()
    {
        var old = FindObjectsByType<WitchAI>(FindObjectsInactive.Include);
        for (int i = 0; i < old.Length; i++)
        {
            var w = old[i];
            if (w == null || w.gameObject == null) continue;
            w.gameObject.SetActive(false);
            Destroy(w.gameObject);
        }

        spawnedWitches.Clear();
        usedWitchNames.Clear();
        if (GameManager.Instance != null) GameManager.Instance.activeWitches.Clear();

        EnsureWitchData();
        allWitchesImmediately = true;

        SpawnAllAndSpread();

        Debug.Log($"[WITCH SPAWNER] reset: spawned={spawnedWitches.Count} " +
                  $"names=[{string.Join(", ", LiveWitchNames().ToArray())}]");
    }

    // ------------------------------------------------------------- spawning

    private void SpawnInitialWitch()
    {
        if (witchDataArray == null || witchDataArray.Length == 0) return;
        var w = CreateWitch(witchDataArray[0], Vector3.zero);
        if (w != null)
        {
            PlaceAt(w, 0, 20f);
            if (!spawnedWitches.Contains(w)) spawnedWitches.Add(w);
            usedWitchNames.Add(witchDataArray[0].witchName);
        }
    }

    private void SpawnAllAndSpread()
    {
        var nodes = new List<WitchAI>(FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude));

        if (witchDataArray != null)
        {
            foreach (var data in witchDataArray)
            {
                if (data == null) continue;
                bool exists = nodes.Exists(w => w != null && w.witchData != null &&
                    CharacterLore.OfficialName(w.witchData.witchName) == CharacterLore.OfficialName(data.witchName));
                if (!exists)
                {
                    var created = CreateWitch(data, Vector3.zero);
                    if (created != null) nodes.Add(created);
                }
            }
        }

        // Order them Rona..Beamoth and place on the spread ring.
        for (int i = 0; i < Order.Length; i++)
        {
            var node = nodes.Find(w => w != null && w.witchData != null && CharacterLore.OfficialName(w.witchData.witchName) == Order[i]);
            if (node == null) continue;
            node.packSlot = i;
            PlaceAt(node, i, Distances[i]);
            if (!spawnedWitches.Contains(node)) spawnedWitches.Add(node);
        }
    }

    private WitchAI CreateWitch(WitchData data, Vector3 pos)
    {
        if (data == null) return null;

        GameObject go;
        if (witchPrefab != null) go = Instantiate(witchPrefab, pos, Quaternion.identity);
        else
        {
            go = new GameObject("Witch_" + data.witchName.Replace(" ", "_"));
            go.transform.position = pos;
            SafeTag(go, "Enemy");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) go.layer = enemyLayer;

            var model = RuntimeCharacterFactory.BuildWitch(data.witchName, data.witchType, Vector3.zero, 0f);
            model.transform.SetParent(go.transform, false);
            model.transform.localPosition = Vector3.zero;
        }

        var ai = go.GetComponent<WitchAI>();
        if (ai == null) ai = go.AddComponent<WitchAI>();
        ai.witchData = data;

        var vis = go.GetComponentInChildren<CharacterVisual>();
        if (vis != null)
        {
            ai.headTransform = vis.headTransform;
            ai.faceRenderer = vis.faceRenderer;
            ai.faceMaterial = vis.faceMaterial;
        }
        ai.survivorLayer = 1 << 0;
        ai.obstacleLayer = 1 << 0;

        Debug.Log($"[WITCH SPAWN] {data.witchName} ({data.witchType}) at {go.transform.position}");
        return ai;
    }

    private void PlaceAt(WitchAI witch, int index, float distance)
    {
        Vector3 origin = GameManager.Instance != null && GameManager.Instance.player != null
            ? GameManager.Instance.player.transform.position : Vector3.zero;

        float baseAngle = Angles[index % Angles.Length];
        Vector3 pos = ClampToTown(origin + Quaternion.Euler(0f, baseAngle, 0f) * Vector3.forward * distance);
        pos.y = GroundY(pos);

        if (Blocked(pos))
        {
            bool found = false;
            for (int j = 0; j < 12 && !found; j++)
            {
                // Widen and rotate the search so a monster never starts inside a
                // building: alternate sides and walk the ring in 15 degree steps.
                float a = baseAngle + ((j % 2 == 0) ? 1 : -1) * ((j + 1) / 2) * 15f;
                float d = distance + ((j / 2) % 3) * 3f;
                Vector3 cand = ClampToTown(origin + Quaternion.Euler(0f, a, 0f) * Vector3.forward * d);
                cand.y = GroundY(cand);
                if (!Blocked(cand)) { pos = cand; found = true; }
            }

            // Last resort: pull straight back toward the player until clear.
            if (!found)
            {
                for (float d = distance - 4f; d >= 12f; d -= 4f)
                {
                    Vector3 cand = ClampToTown(origin + Quaternion.Euler(0f, baseAngle, 0f) * Vector3.forward * d);
                    cand.y = GroundY(cand);
                    if (!Blocked(cand)) { pos = cand; break; }
                }
            }
        }

        pos.y = GroundY(pos);
        witch.transform.position = pos;
        Vector3 look = origin - pos; look.y = 0f;
        if (look.sqrMagnitude > 0.01f) witch.transform.rotation = Quaternion.LookRotation(look);
        Debug.Log($"[WITCH SPAWN] placed {witch.witchData.witchName} dist={Vector3.Distance(origin, pos):F0}");
    }

    private static void SafeTag(GameObject go, string tag)
    {
        try { go.tag = tag; } catch { /* tag not defined in this project */ }
    }

    private static Vector3 ClampToTown(Vector3 p)
    {
        // Town square + the north open field, so witches can spawn around a
        // player who has run out into the open.
        return TownLayout.ClampToPlayable(p, 6f);
    }

    private float GroundY(Vector3 p)
    {
        RaycastHit hit;
        if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return 0f;
    }

    private bool Blocked(Vector3 pos)
    {
        return Physics.CheckSphere(pos + Vector3.up * 1.0f, 0.7f, ~0, QueryTriggerInteraction.Ignore);
    }

    // -------------------------------------------------------- progression

    private void OnGeneratorRepaired(int current, int total) { CheckAndSpawnWitches(current); }

    private void CheckAndSpawnWitches(int generatorsRepaired)
    {
        if (allWitchesImmediately) return;
        if (GameManager.Instance == null || GameManager.Instance.gameConfig == null) return;
        if (witchDataArray == null || witchDataArray.Length == 0) return;

        var config = GameManager.Instance.gameConfig;
        int targetCount = config.startingWitches;
        for (int i = 0; i < config.witchSpawnThresholds.Length; i++)
            if (generatorsRepaired >= config.witchSpawnThresholds[i]) targetCount = i + 1;
        targetCount = Mathf.Clamp(targetCount, config.startingWitches, config.maxWitches);

        // Count the witches that actually exist (including the one SceneSetup
        // created) and top up only the difference. This is what prevents the
        // old duplicate-witch bug where nextWitchIndex restarted at 0 and the
        // same data asset was spawned twice.
        int live = CountLiveWitches();
        int before = live;
        int safety = 0;
        while (live < targetCount && safety++ < 12)
        {
            var data = FindUnusedWitchData();
            if (data == null) break;

            var w = CreateWitch(data, Vector3.zero);
            if (w == null) break;

            int slot = Mathf.Clamp(live, 0, Distances.Length - 1);
            PlaceAt(w, slot, Distances[slot]);
            if (!spawnedWitches.Contains(w)) spawnedWitches.Add(w);
            usedWitchNames.Add(data.witchName);
            live++;
        }

        Debug.Log($"[PROGRESSION] generators={generatorsRepaired}/{GameManager.Instance.totalGenerators} " +
                  $"thresholdTarget={targetCount} liveBefore={before} liveAfter={CountLiveWitches()} " +
                  $"names=[{string.Join(", ", LiveWitchNames().ToArray())}]");
    }

    private int CountLiveWitches()
    {
        int count = 0;
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
            if (w != null && w.gameObject.activeInHierarchy) count++;
        return count;
    }

    private List<string> LiveWitchNames()
    {
        var names = new List<string>();
        foreach (var w in FindObjectsByType<WitchAI>(FindObjectsInactive.Exclude))
        {
            if (w == null || w.witchData == null) continue;
            if (!names.Contains(w.witchData.witchName)) names.Add(w.witchData.witchName);
        }
        return names;
    }

    /// <summary>
    /// Picks the first data asset whose witch is not already alive. Never
    /// returns a name twice, so wheels 1-6 are always six distinct witches.
    /// </summary>
    private WitchData FindUnusedWitchData()
    {
        var liveNames = LiveWitchNames();
        for (int i = 0; i < witchDataArray.Length; i++)
        {
            var d = witchDataArray[i];
            if (d == null) continue;
            if (liveNames.Contains(d.witchName)) continue;
            if (usedWitchNames.Contains(d.witchName)) continue;
            return d;
        }
        // Fallback: allow a name that was used but is no longer alive.
        for (int i = 0; i < witchDataArray.Length; i++)
        {
            var d = witchDataArray[i];
            if (d == null) continue;
            if (!liveNames.Contains(d.witchName)) return d;
        }
        return null;
    }
}
