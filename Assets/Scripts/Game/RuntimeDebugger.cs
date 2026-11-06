using UnityEngine;
using System.Collections.Generic;

public class RuntimeDebugger : MonoBehaviour
{
    private float debugTimer = 0f;
    private bool loggedInitial = false;
    
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    private void Update()
    {
        debugTimer += Time.deltaTime;
        if (debugTimer >= 3f)
        {
            debugTimer = 0f;
            LogRuntimeSummary();
            LogGenerators();
            LogWitches();
            LogSurvivors();
            LogSpawn();
        }
        
        if (!loggedInitial && Time.time > 5f)
        {
            loggedInitial = true;
            LogRuntimeSummary();
            LogGenerators();
            LogWitches();
            LogSurvivors();
            LogSpawn();
        }
    }
    
    private void LogRuntimeSummary()
    {
        PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindObjectOfType<PlayerController>();
        SurvivorAI[] allSurvivors = FindObjectsOfType<SurvivorAI>();
        GeneratorController[] gens = FindObjectsOfType<GeneratorController>();
        WitchAI[] witches = FindObjectsOfType<WitchAI>();
        Camera[] cams = FindObjectsOfType<Camera>();
        UnityEngine.EventSystems.EventSystem[] events = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        
        int aiSurvivors = 0;
        for (int i = 0; i < allSurvivors.Length; i++)
        {
            if (allSurvivors[i] != null) aiSurvivors++;
        }
        
        Debug.Log($"[WITCHBOUND RUNTIME] Player: {(player != null ? 1 : 0)} AI Survivors: {aiSurvivors} Generators: {gens.Length} Witches: {witches.Length} Main Cameras: {cams.Length} EventSystems: {events.Length} AudioListeners: {listeners.Length}");
        Debug.Log($"[WITCHBOUND RUNTIME] Expected: Player = 1 AI Survivors = 3 Generators = 10 Witches >= 1 Main Cameras = 1 EventSystems = 1 AudioListeners = 1");
    }
    
    private void LogGenerators()
    {
        GeneratorController[] gens = FindObjectsOfType<GeneratorController>();
        Debug.Log($"[GENERATOR DEBUG] Generator count: {gens.Length}");
        for (int i = 0; i < gens.Length; i++)
        {
            if (gens[i] != null)
            {
                var g = gens[i];
                Renderer[] rends = g.GetComponentsInChildren<Renderer>(true);
                Collider[] cols = g.GetComponentsInChildren<Collider>(true);
                bool hasVisibleRenderer = false;
                for (int r = 0; r < rends.Length; r++)
                {
                    if (rends[r] != null && rends[r].enabled && rends[r].gameObject.activeInHierarchy)
                        hasVisibleRenderer = true;
                }
                float distFromPlayer = 0f;
                if (GameManager.Instance != null && GameManager.Instance.player != null)
                    distFromPlayer = Vector3.Distance(g.transform.position, GameManager.Instance.player.transform.position);
                Debug.Log($"[GENERATOR DEBUG] Generator #{i+1} pos:{g.transform.position} active:{g.gameObject.activeInHierarchy} rendererEnabled:{hasVisibleRenderer} colliderEnabled:{(cols.Length > 0 && cols[0] != null && cols[0].enabled)} distFromPlayer:{distFromPlayer:F1} hasController:{g != null}");
            }
        }
    }
    
    private void LogWitches()
    {
        WitchAI[] witches = FindObjectsOfType<WitchAI>();
        WitchSpawner spawner = FindObjectOfType<WitchSpawner>();
        int maxWitches = -1;
        if (GameManager.Instance != null && GameManager.Instance.gameConfig != null)
            maxWitches = GameManager.Instance.gameConfig.maxWitches;
        Debug.Log($"[WITCH DEBUG] WitchSpawner active: {(spawner != null && spawner.gameObject.activeInHierarchy)} Current witch count: {witches.Length} Maximum witch count: {maxWitches}");
        if (GameManager.Instance != null)
        {
            Debug.Log($"[WITCH DEBUG] Generators repaired: {GameManager.Instance.generatorsRepaired}");
        }
        for (int i = 0; i < witches.Length; i++)
        {
            if (witches[i] != null)
            {
                var w = witches[i];
                Renderer[] rends = w.GetComponentsInChildren<Renderer>(true);
                bool hasRend = rends.Length > 0 && rends[0] != null && rends[0].enabled;
                Collider[] cols = w.GetComponentsInChildren<Collider>(true);
                float dist = 0f;
                if (GameManager.Instance != null && GameManager.Instance.player != null)
                    dist = Vector3.Distance(w.transform.position, GameManager.Instance.player.transform.position);
                UnityEngine.AI.NavMeshAgent agent = w.GetComponent<UnityEngine.AI.NavMeshAgent>();
                Debug.Log($"[WITCH DEBUG] Witch #{i+1} name:{w.name} active:{w.gameObject.activeInHierarchy} pos:{w.transform.position} dist:{dist:F1} AI:{(w.enabled)} rend:{hasRend} coll:{(cols.Length>0&&cols[0].enabled)} nav:{(agent!=null&&agent.enabled)} state:{w.currentState}");
                
                if (rends.Length > 0 && rends[0] != null)
                {
                    Debug.Log($"[WITCH VISUAL DEBUG] Name:{w.name} Position:{w.transform.position} Distance:{dist:F1} Scale:{w.transform.lossyScale} Renderer count:{rends.Length} Renderer enabled:{rends[0].enabled} Bounds center:{rends[0].bounds.center} Bounds size:{rends[0].bounds.size} ActiveInHierarchy:{w.gameObject.activeInHierarchy} WitchAI enabled:{w.enabled} Collider enabled:{(cols.Length>0&&cols[0]!=null&&cols[0].enabled)} Current AI state:{w.currentState}");
                }
                else
                {
                    Debug.Log($"[WITCH VISUAL DEBUG] Name:{w.name} Position:{w.transform.position} Distance:{dist:F1} Scale:{w.transform.lossyScale} Renderer count:0");
                }
            }
        }
    }
    
    private void LogSurvivors()
    {
        SurvivorAI[] survivors = FindObjectsOfType<SurvivorAI>();
        PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindObjectOfType<PlayerController>();
        Debug.Log($"[SURVIVOR DEBUG] Player survivor: {(player != null ? 1 : 0)} AI survivor count: {survivors.Length}");
        for (int i = 0; i < survivors.Length; i++)
        {
            if (survivors[i] != null)
            {
                var s = survivors[i];
                Renderer[] rends = s.GetComponentsInChildren<Renderer>(true);
                bool hasRend = rends.Length > 0 && rends[0] != null && rends[0].enabled;
                float dist = 0f;
                if (player != null)
                    dist = Vector3.Distance(s.transform.position, player.transform.position);
                Debug.Log($"[SURVIVOR DEBUG] Survivor #{i+1} name:{s.name} pos:{s.transform.position} dist:{dist:F1} active:{s.gameObject.activeInHierarchy} rend:{hasRend} AI:{s.enabled} state:{s.currentState} health:{s.currentHealth}");
                
                // Detailed visual debug
                if (rends.Length > 0 && rends[0] != null)
                {
                    Debug.Log($"[SURVIVOR VISUAL DEBUG] Name:{s.name} Position:{s.transform.position} Distance:{dist:F1} ActiveInHierarchy:{s.gameObject.activeInHierarchy} Scale:{s.transform.lossyScale} Renderer count:{rends.Length} Renderer enabled:{rends[0].enabled} Bounds center:{rends[0].bounds.center} Bounds size:{rends[0].bounds.size} Material:{rends[0].sharedMaterial?.name} Layer:{LayerMask.LayerToName(s.gameObject.layer)} Current AI state:{s.currentState}");
                }
                else
                {
                    Debug.Log($"[SURVIVOR VISUAL DEBUG] Name:{s.name} Position:{s.transform.position} Distance:{dist:F1} ActiveInHierarchy:{s.gameObject.activeInHierarchy} Scale:{s.transform.lossyScale} Renderer count:0");
                }
            }
        }
    }
    
    private void LogSpawn()
    {
        PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Debug.Log($"[SPAWN DEBUG] Player spawn position: {player.transform.position}");
            // rough ground check
            RaycastHit hit;
            if (Physics.Raycast(player.transform.position + Vector3.up * 10f, Vector3.down, out hit, 50f))
            {
                Debug.Log($"[SPAWN DEBUG] Ground height approx at raycast hit: {hit.point.y}, distance to ground: {player.transform.position.y - hit.point.y}");
            }
            GeneratorController[] gens = FindObjectsOfType<GeneratorController>();
            if (gens.Length > 0)
            {
                float minDist = float.MaxValue;
                for (int i = 0; i < gens.Length; i++) if (gens[i] != null) minDist = Mathf.Min(minDist, Vector3.Distance(player.transform.position, gens[i].transform.position));
                Debug.Log($"[SPAWN DEBUG] Distance to nearest generator: {minDist:F1}");
            }
            SurvivorAI[] survivors = FindObjectsOfType<SurvivorAI>();
            if (survivors.Length > 0)
            {
                float minDist = float.MaxValue;
                for (int i = 0; i < survivors.Length; i++) if (survivors[i] != null) minDist = Mathf.Min(minDist, Vector3.Distance(player.transform.position, survivors[i].transform.position));
                Debug.Log($"[SPAWN DEBUG] Distance to nearest survivor: {minDist:F1}");
            }
            WitchAI[] witches = FindObjectsOfType<WitchAI>();
            if (witches.Length > 0)
            {
                float minDist = float.MaxValue;
                for (int i = 0; i < witches.Length; i++) if (witches[i] != null) minDist = Mathf.Min(minDist, Vector3.Distance(player.transform.position, witches[i].transform.position));
                Debug.Log($"[SPAWN DEBUG] Distance to nearest witch: {minDist:F1}");
            }
        }
    }
}
