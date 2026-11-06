using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-100)]
public class SceneSetup : MonoBehaviour
{
    [Header("Auto-setup")]
    public bool setupOnStart = true;
    
    [Header("Data References (assigned in editor)")]
    public GameConfig gameConfig;
    public WitchData testWitchData;
    public SurvivorData testSurvivorData;
    public SkillData testSkillData;
    public GeneratorData generatorData;
    
    [Header("References (auto-filled)")]
    public GameManager gameManager;
    public AudioManager audioManager;
    public ObjectiveManager objectiveManager;
    public CoinManager coinManager;
    public SkillUpgradeManager skillUpgradeManager;
    public SaveManager saveManager;
    public UIManager uiManager;
    public PlayerController playerController;
    public CameraController cameraController;
    public PlayerSkillController skillController;
    public GeneratorController testGenerator;
    public EscapeGate escapeGate;
    public WitchAI testWitch;
    public EnvironmentBootstrap environmentBootstrap;
    public UISetup uiSetup;
    public ProceduralAudioSetup audioSetup;
    
    private void Awake()
    {
        if (gameConfig == null)
        {
#if UNITY_EDITOR
            gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
#endif
        }
        if (gameConfig == null)
        {
            gameConfig = Resources.Load<GameConfig>("GameConfig");
        }
        if (gameConfig == null)
        {
            gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            gameConfig.totalGenerators = 10;
            gameConfig.startingWitches = 1;
            gameConfig.maxWitches = 6;
            gameConfig.witchSpawnThresholds = new int[] { 0, 2, 4, 6, 8, 10 };
        }
#if UNITY_EDITOR
        if (testWitchData == null)
            testWitchData = AssetDatabase.LoadAssetAtPath<WitchData>("Assets/ScriptableObjects/Witch_MaraVeyne.asset");
        if (testSkillData == null)
            testSkillData = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/ScriptableObjects/Skill_Phase.asset");
        if (generatorData == null)
            generatorData = AssetDatabase.LoadAssetAtPath<GeneratorData>("Assets/ScriptableObjects/Generator_Standard.asset");
#endif
        
        if (setupOnStart)
        {
            SetupScene();
        }
    }
    
    [ContextMenu("Setup Scene")]
    public void SetupScene()
    {
        SetupManagers();
        SetupPlayer();
        SetupGenerator();
        SetupEscapeGate();
        SetupTestWitch();
        SetupEnvironment();
        SetupUI();
        SetupAudio();
        
        Debug.Log("Scene setup complete!");
    }
    
    private void SetupManagers()
    {
        var inputObj = new GameObject("InputStateManager");
        inputObj.AddComponent<InputStateManager>();
        
        var gmObj = new GameObject("GameManager");
        gameManager = gmObj.AddComponent<GameManager>();
        gameManager.gameConfig = gameConfig;
        
        var amObj = new GameObject("AudioManager");
        audioManager = amObj.AddComponent<AudioManager>();
        
        var omObj = new GameObject("ObjectiveManager");
        objectiveManager = omObj.AddComponent<ObjectiveManager>();
        objectiveManager.gameManager = gameManager;
        
        var cmObj = new GameObject("CoinManager");
        coinManager = cmObj.AddComponent<CoinManager>();
        
        var sumObj = new GameObject("SkillUpgradeManager");
        skillUpgradeManager = sumObj.AddComponent<SkillUpgradeManager>();
        
        var smObj = new GameObject("SaveManager");
        saveManager = smObj.AddComponent<SaveManager>();
        
        var uiObj = new GameObject("UIManager");
        uiManager = uiObj.AddComponent<UIManager>();
        
        // Single authoritative ESC/pause handler (runs even at timeScale 0).
        var pauseObj = new GameObject("PauseController");
        pauseObj.AddComponent<PauseController>();

        // Runtime presentation fixup: places characters in view + visual test.
        var bootstrapObj = new GameObject("GameplayBootstrap");
        bootstrapObj.AddComponent<GameplayBootstrap>();
        
        // Add WitchSpawner
        var witchSpawnerObj = new GameObject("WitchSpawner");
        var witchSpawner = witchSpawnerObj.AddComponent<WitchSpawner>();
        // Try to load witch data if available
        if (witchSpawner.witchDataArray == null || witchSpawner.witchDataArray.Length == 0)
        {
#if UNITY_EDITOR
            WitchData[] allWitchData = new WitchData[6];
            string[] paths = new string[] {
                "Assets/ScriptableObjects/Witch_MaraVeyne.asset",
                "Assets/ScriptableObjects/Witch_Seraphine.asset",
                "Assets/ScriptableObjects/Witch_Morrow.asset",
                "Assets/ScriptableObjects/Witch_Vespera.asset",
                "Assets/ScriptableObjects/Witch_MotherGrin.asset",
                "Assets/ScriptableObjects/Witch_Evelyn.asset"
            };
            int idx = 0;
            for (int p = 0; p < paths.Length && idx < allWitchData.Length; p++)
            {
                var wd = AssetDatabase.LoadAssetAtPath<WitchData>(paths[p]);
                if (wd != null) { allWitchData[idx++] = wd; }
            }
            if (idx == 0 && testWitchData != null)
            {
                allWitchData = new WitchData[1] { testWitchData };
            }
            witchSpawner.witchDataArray = allWitchData;
#endif
            if ((witchSpawner.witchDataArray == null || witchSpawner.witchDataArray.Length == 0) && testWitchData != null)
            {
                witchSpawner.witchDataArray = new WitchData[] { testWitchData };
            }
        }
    }
    
    private void SetupPlayer()
    {
        var playerObj = new GameObject("Player");
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(0, 2, 0); // Spawn higher to avoid clipping
        
        var controller = playerObj.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0, 0.9f, 0);
        
        playerController = playerObj.AddComponent<PlayerController>();
        // Keep the balance contract explicit even if the component defaults drift.
        playerController.moveSpeed = 4.5f;    // walk
        playerController.sprintSpeed = 7.0f;  // faster than witch chase (6.5)
        if (gameManager != null)
            gameManager.player = playerController;
        
        var camera = Camera.main;
        if (camera != null)
        {
            camera.transform.SetParent(playerObj.transform);
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            camera.transform.localRotation = Quaternion.identity;
            camera.tag = "MainCamera";
            
            cameraController = camera.gameObject.AddComponent<CameraController>();
            cameraController.playerBody = playerObj.transform;
        }
        
        skillController = playerObj.AddComponent<PlayerSkillController>();
        playerController.skillController = skillController;
        playerController.cameraController = cameraController;

        // First-person body (no head) so you can look down and see animated legs.
        var bodyModel = RuntimeCharacterFactory.BuildPlayerBody(new Color(0.20f, 0.22f, 0.28f), new Color(0.85f, 0.72f, 0.60f));
        bodyModel.transform.SetParent(playerObj.transform, false);
        bodyModel.transform.localPosition = new Vector3(0f, 0f, 0.14f); // slight forward offset so legs are visible when looking down
        playerController.bodyRoot = bodyModel.transform;
        playerController.bodyAnimator = bodyModel.GetComponent<ProceduralAnimator>();
        
        if (testSkillData != null)
        {
            skillController.EquipSkill(testSkillData);
        }
        
        // Create 3 AI survivors as teammates, arranged in a visible front arc.
        string[] survivorNames = new string[] { "Luis Padilla", "Mick Richards", "Josh Garcia" };
        Vector3 origin = playerObj.transform.position;
        Vector3[] survivorPositions = new Vector3[]
        {
            origin + new Vector3(-2.6f, 0f, 4f),  // left-front
            origin + new Vector3( 2.6f, 0f, 4f),  // right-front
            origin + new Vector3( 0f,   0f, 6f)   // centre-front
        };
        for (int i = 0; i < 3; i++)
        {
            var survObj = new GameObject($"AI_Survivor_{survivorNames[i].Replace(" ", "_")}");
            survObj.transform.position = survivorPositions[i];
            survObj.tag = "Player";
            
            var survAgent = survObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            survAgent.speed = 4f;
            survAgent.angularSpeed = 120f;
            survAgent.acceleration = 8f;
            survAgent.baseOffset = 0f;
            
            var survAI = survObj.AddComponent<SurvivorAI>();
            survAI.isPlayerTeammate = true;
            survAI.displayName = survivorNames[i];
            if (testSurvivorData != null)
                survAI.survivorData = testSurvivorData;
            
            // Full low-poly humanoid with a distinct face (built by the factory so
            // the player can actually tell the survivors apart at night).
            var model = RuntimeCharacterFactory.BuildSurvivor(survivorNames[i], Vector3.zero, 0f);
            model.transform.SetParent(survObj.transform, false);
            model.transform.localPosition = Vector3.zero;
            var visual = model.GetComponent<CharacterVisual>();
            if (visual != null) visual.displayName = survivorNames[i];
        }
    }
    
    private void SetupGenerator()
    {
        // Create 10 generators placed around the town
        Vector3[] positions = new Vector3[10]
        {
            new Vector3(10, 0.5f, 0),
            new Vector3(-10, 0.5f, 10),
            new Vector3(15, 0.5f, -15),
            new Vector3(-15, 0.5f, -10),
            new Vector3(0, 0.5f, -20),
            new Vector3(25, 0.5f, 5),
            new Vector3(-25, 0.5f, 5),
            new Vector3(5, 0.5f, 25),
            new Vector3(-5, 0.5f, -25),
            new Vector3(20, 0.5f, -20)
        };
        
        for (int i = 0; i < 10; i++)
        {
            var genObj = new GameObject($"Generator_{i+1:00}");
            genObj.transform.position = positions[i];
            genObj.tag = "Interactable";
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0) genObj.layer = interactableLayer;
            
            var genController = genObj.AddComponent<GeneratorController>();
            genController.generatorId = $"Gen_{i+1:00}";
            genController.generatorData = generatorData;
            if (generatorData != null)
                genController.repairTime = generatorData.repairTime;
            else
                genController.repairTime = 20f;
            
            // Recognizable portable industrial generator + distance beacon.
            var refs = GeneratorVisualBuilder.Build(genObj.transform);
            genController.generatorLight = refs.indicatorLight;
            genController.indicatorRenderer = refs.indicator;
            genController.beacon = refs.beaconScript;

            // Single interaction collider on the root (children are collider-free).
            var genCol = genObj.AddComponent<BoxCollider>();
            genCol.center = new Vector3(0f, 0.85f, 0f);
            genCol.size = new Vector3(1.5f, 1.7f, 2.1f);
            
            var genAudio = genObj.AddComponent<AudioSource>();
            genAudio.spatialBlend = 1f;
            genAudio.maxDistance = 20f;
            genController.audioSource = genAudio;
            
            if (i == 0) testGenerator = genController;
        }
    }
    
    private void SetupEscapeGate()
    {
        var gateObj = new GameObject("EscapeGate");
        gateObj.transform.position = new Vector3(-20, 0, 0);
        gateObj.tag = "Interactable";
        int gateLayer = LayerMask.NameToLayer("Interactable");
        if (gateLayer >= 0) gateObj.layer = gateLayer;
        
        escapeGate = gateObj.AddComponent<EscapeGate>();
        escapeGate.openTime = 5f;
        
        var gateModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateModel.name = "GateModel";
        gateModel.transform.SetParent(gateObj.transform);
        gateModel.transform.localPosition = new Vector3(0, 2, 0);
        gateModel.transform.localScale = new Vector3(4, 4, 0.5f);
        
        var gateMat = new Material(Shader.Find("Standard"));
        gateMat.color = Color.gray;
        gateModel.GetComponent<Renderer>().material = gateMat;
        
        var gateAudio = gateObj.AddComponent<AudioSource>();
        gateAudio.spatialBlend = 1f;
        gateAudio.maxDistance = 30f;
    }
    
    private void SetupTestWitch()
    {
        var witchObj = new GameObject("TestWitch_MaraVeyne");
        witchObj.transform.position = new Vector3(0, 0, 20); // 20 m down the open street
        witchObj.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) witchObj.layer = enemyLayer;
        
        var witchAgent = witchObj.AddComponent<NavMeshAgent>();
        witchAgent.speed = 3f;
        witchAgent.angularSpeed = 120f;
        witchAgent.acceleration = 8f;
        witchAgent.baseOffset = 0f;
        
        testWitch = witchObj.AddComponent<WitchAI>();
        testWitch.witchData = testWitchData;
        testWitch.survivorLayer = 1 << 0; // Default (player + survivors)
        testWitch.obstacleLayer = 1 << 0; // buildings block line of sight
        
        string witchName = testWitchData != null ? testWitchData.witchName : "Rona Tanyel";
        WitchType wtype = testWitchData != null ? testWitchData.witchType : WitchType.Humanoid;

        // Full per-type chaser model with a visible face + glowing eyes.
        // IMPORTANT: the model is authored facing +Z (face/snout/claws), so it
        // must be parented with identity rotation. The old 180f yaw here made
        // every witch run backwards (face pointing away from its target).
        var chaserModel = RuntimeCharacterFactory.BuildWitch(witchName, wtype, Vector3.zero, 0f);
        chaserModel.transform.SetParent(witchObj.transform, false);
        chaserModel.transform.localPosition = Vector3.zero;
        chaserModel.transform.localRotation = Quaternion.identity;
        var cvisual = chaserModel.GetComponent<CharacterVisual>();
        if (cvisual != null)
        {
            cvisual.displayName = witchName;
            testWitch.headTransform = cvisual.headTransform;
            testWitch.faceRenderer = cvisual.faceRenderer;
            testWitch.faceMaterial = cvisual.faceMaterial;
        }
    }
    
    private void SetupEnvironment()
    {
        // The entire town is generated at runtime by EnvironmentBootstrap.
        // Editor-time procedural meshes/materials do not survive scene
        // serialization, so only the bootstrap component is saved here.
        var envObj = new GameObject("Environment");
        environmentBootstrap = envObj.AddComponent<EnvironmentBootstrap>();
        environmentBootstrap.seed = 20261024;
        environmentBootstrap.generateOnAwake = true;

        CreateShaderProbe();
    }

    /// <summary>
    /// Runtime shaders are only included in a player build when something in
    /// the build references them. These hidden, off-screen renderers guarantee
    /// Shader.Find(...) succeeds in the standalone build.
    /// </summary>
    private void CreateShaderProbe()
    {
        var probe = new GameObject("ShaderProbe");
        probe.transform.position = new Vector3(0f, -800f, 0f);
        probe.hideFlags = HideFlags.HideInHierarchy;

        string[] shaderNames =
        {
            "Standard",
            "Unlit/Color",
            "Unlit/Texture",
            "Sprites/Default",
            "WITCHBOUND/NightSky",
            "WITCHBOUND/SoftFog"
        };

        for (int i = 0; i < shaderNames.Length; i++)
        {
            var shader = Shader.Find(shaderNames[i]);
            if (shader == null)
            {
                Debug.LogError("[PROC] Shader not found for build probe: " + shaderNames[i]);
                continue;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Probe_" + shaderNames[i].Replace('/', '_');
            go.transform.SetParent(probe.transform, false);
            go.transform.localPosition = new Vector3(i * 2f, 0f, 0f);
            go.transform.localScale = Vector3.one * 0.01f;
            go.hideFlags = HideFlags.HideInHierarchy;

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var mat = new Material(shader);
            mat.name = "ProbeMat_" + shaderNames[i].Replace('/', '_');
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
    
    private void SetupNavMesh()
    {
        var navObj = new GameObject("NavMeshDataHolder");
        navObj.AddComponent<NavMeshDataHolder>();
    }
    
    private void SetupUI()
    {
        var uiObj = new GameObject("UISetup");
        uiSetup = uiObj.AddComponent<UISetup>();
        uiSetup.setupOnStart = false;
        uiSetup.SetupUI();

        // Character Select / Bestiary logic (drives the 3D model previews).
        var csObj = new GameObject("CharacterSelectUI");
        csObj.transform.SetParent(uiObj.transform);
        csObj.AddComponent<CharacterSelectUI>();

        // Wire runtime-built UI panels into UIManager.
        // UIBuilder creates panels as children of "UI_Canvas"; without this,
        // UIManager's panel references stay null and state changes do nothing on screen.
        if (uiManager != null)
        {
            var canvasTr = GameObject.Find("UI_Canvas")?.transform;
            if (canvasTr != null)
            {
                uiManager.mainMenuPanel = FindPanel(canvasTr, "MainMenu");
                uiManager.characterSelectPanel = FindPanel(canvasTr, "CharacterSelect");
                uiManager.hudPanel = FindPanel(canvasTr, "GameplayHUD");
                uiManager.pauseMenuPanel = FindPanel(canvasTr, "PauseMenu");
                uiManager.resultsPanel = FindPanel(canvasTr, "ResultsScreen");
                uiManager.settingsPanel = FindPanel(canvasTr, "SettingsMenu");
                uiManager.skillsPanel = FindPanel(canvasTr, "SkillsMenu");

                Debug.Log($"[UI WIRING] MainMenu={(uiManager.mainMenuPanel != null)} " +
                          $"CharacterSelect={(uiManager.characterSelectPanel != null)} " +
                          $"HUD={(uiManager.hudPanel != null)} " +
                          $"Pause={(uiManager.pauseMenuPanel != null)} " +
                          $"Results={(uiManager.resultsPanel != null)} " +
                          $"Settings={(uiManager.settingsPanel != null)} " +
                          $"Skills={(uiManager.skillsPanel != null)}");
            }
            else
            {
                Debug.LogError("[UI WIRING] UI_Canvas not found - panels could not be wired!");
            }
        }
        else
        {
            Debug.LogError("[UI WIRING] UIManager is null - panels could not be wired!");
        }

        Debug.Log($"[UI WIRING] GameManager.Instance={(GameManager.Instance != null)} UIManager.Instance={(UIManager.Instance != null)}");
    }

    private static GameObject FindPanel(Transform canvas, string name)
    {
        var child = canvas.Find(name);
        return child != null ? child.gameObject : null;
    }
    
    private void SetupAudio()
    {
        var audioObj = new GameObject("AudioSetup");
        audioSetup = audioObj.AddComponent<ProceduralAudioSetup>();
        audioSetup.setupOnStart = false;
        audioSetup.SetupAudio();
    }
}
