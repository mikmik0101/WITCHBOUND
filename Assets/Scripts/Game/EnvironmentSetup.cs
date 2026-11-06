using UnityEngine;

[DefaultExecutionOrder(-50)]
public class EnvironmentSetup : MonoBehaviour
{
    [Header("Auto-setup")]
    public bool setupOnStart = true;
    
    [Header("Environment")]
    public GameObject[] buildingPrefabs;
    public GameObject[] treePrefabs;
    public GameObject[] propPrefabs;
    
    [Header("Settings")]
    public float townRadius = 50f;
    public float fogDensity = 0.02f;
    public Color fogColor = new Color(0.1f, 0.1f, 0.15f);
    public Color ambientColor = new Color(0.05f, 0.05f, 0.1f);
    
    private void Awake()
    {
        if (setupOnStart)
        {
            SetupEnvironment();
        }
    }
    
    [ContextMenu("Setup Environment")]
    public void SetupEnvironment()
    {
        SetupFog();
        SetupLighting();
        SetupAmbientAudio();
        CreateTownLayout();
        CreateForest();
        CreateBloodDecals();
        CreateEnvironmentalClues();
        
        Debug.Log("Environment setup complete!");
    }
    
    private void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = 0f;
        RenderSettings.fogEndDistance = 100f;
    }
    
    private void SetupLighting()
    {
        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientSkyColor = ambientColor;
        RenderSettings.ambientEquatorColor = ambientColor * 0.8f;
        RenderSettings.ambientGroundColor = ambientColor * 0.6f;
        RenderSettings.ambientIntensity = 0.3f;
        
        // Directional light (moonlight)
        var light = GameObject.Find("Directional Light");
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var lightComp = light.GetComponent<Light>();
            if (lightComp != null)
            {
                lightComp.type = LightType.Directional;
                lightComp.color = new Color(0.7f, 0.75f, 0.9f);
                lightComp.intensity = 0.3f;
                lightComp.shadows = LightShadows.Soft;
            }
        }
    }
    
    private void SetupAmbientAudio()
    {
        var audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayAmbience();
            audioManager.PlayMusic("ambient");
        }
    }
    
    private void CreateTownLayout()
    {
        // Create main road
        CreateRoad(new Vector3(-50, 0, 0), new Vector3(50, 0, 0), 4f);
        
        // Create side roads
        CreateRoad(new Vector3(0, 0, -30), new Vector3(0, 0, 30), 3f);
        CreateRoad(new Vector3(-20, 0, -50), new Vector3(-20, 0, 50), 3f);
        CreateRoad(new Vector3(20, 0, -50), new Vector3(20, 0, 50), 3f);
        
        // Create buildings
        CreateBuilding(new Vector3(5, 0, 5), new Vector3(3, 3, 3), Color.gray);
        CreateBuilding(new Vector3(-5, 0, 5), new Vector3(4, 2.5f, 3), Color.darkGray);
        CreateBuilding(new Vector3(15, 0, -10), new Vector3(5, 4, 4), Color.gray);
        CreateBuilding(new Vector3(-15, 0, -10), new Vector3(3, 2, 3), Color.darkGray);
        CreateBuilding(new Vector3(0, 0, 15), new Vector3(6, 3.5f, 5), Color.gray);
        CreateBuilding(new Vector3(0, 0, -15), new Vector3(4, 3, 4), Color.darkGray);
        
        // Create ruins
        CreateRuins(new Vector3(30, 0, 30), new Vector3(6, 2, 6));
        CreateRuins(new Vector3(-30, 0, 30), new Vector3(5, 1.5f, 5));
        CreateRuins(new Vector3(30, 0, -30), new Vector3(7, 3, 7));
        CreateRuins(new Vector3(-30, 0, -30), new Vector3(4, 2, 4));
    }
    
    private void CreateRoad(Vector3 start, Vector3 end, float width)
    {
        var roadObj = new GameObject("Road");
        var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "RoadSegment";
        road.transform.SetParent(roadObj.transform);
        
        Vector3 dir = (end - start).normalized;
        float length = Vector3.Distance(start, end);
        
        road.transform.position = (start + end) / 2f + Vector3.up * 0.05f;
        road.transform.rotation = Quaternion.LookRotation(dir);
        road.transform.localScale = new Vector3(width, 0.1f, length);
        
        var roadMat = new Material(Shader.Find("Standard"));
        roadMat.color = new Color(0.15f, 0.15f, 0.15f);
        road.GetComponent<Renderer>().material = roadMat;
    }
    
    private void CreateBuilding(Vector3 position, Vector3 size, Color color)
    {
        var buildingObj = new GameObject("Building");
        buildingObj.transform.position = position;
        
        var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "BuildingModel";
        building.transform.SetParent(buildingObj.transform);
        building.transform.localPosition = Vector3.up * size.y * 0.5f;
        building.transform.localScale = size;
        
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        building.GetComponent<Renderer>().material = mat;
        
        // Add windows
        var window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "Window";
        window.transform.SetParent(buildingObj.transform);
        window.transform.localPosition = new Vector3(size.x * 0.3f, size.y * 0.6f, size.z * 0.5f + 0.01f);
        window.transform.localScale = new Vector3(0.5f, 0.8f, 0.05f);
        
        var windowMat = new Material(Shader.Find("Standard"));
        windowMat.color = new Color(0.8f, 0.8f, 0.5f);
        windowMat.EnableKeyword("_EMISSION");
        windowMat.SetColor("_EmissionColor", new Color(0.8f, 0.8f, 0.5f) * 0.5f);
        window.GetComponent<Renderer>().material = windowMat;
    }
    
    private void CreateRuins(Vector3 position, Vector3 size)
    {
        var ruinsObj = new GameObject("Ruins");
        ruinsObj.transform.position = position;
        
        // Create broken walls
        for (int i = 0; i < 3; i++)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall_{i}";
            wall.transform.SetParent(ruinsObj.transform);
            wall.transform.localPosition = new Vector3(
                (i - 1) * size.x * 0.3f,
                size.y * 0.3f,
                0
            );
            wall.transform.localScale = new Vector3(size.x * 0.2f, size.y * 0.6f, size.z * 0.1f);
            
            var wallMat = new Material(Shader.Find("Standard"));
            wallMat.color = new Color(0.3f, 0.25f, 0.2f);
            wall.GetComponent<Renderer>().material = wallMat;
        }
        
        // Debris
        for (int i = 0; i < 5; i++)
        {
            var debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = $"Debris_{i}";
            debris.transform.SetParent(ruinsObj.transform);
            debris.transform.localPosition = new Vector3(
                Random.Range(-size.x * 0.5f, size.x * 0.5f),
                0.2f,
                Random.Range(-size.z * 0.5f, size.z * 0.5f)
            );
            debris.transform.localScale = new Vector3(
                Random.Range(0.3f, 1f),
                Random.Range(0.2f, 0.5f),
                Random.Range(0.3f, 1f)
            );
            debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            var debrisMat = new Material(Shader.Find("Standard"));
            debrisMat.color = new Color(0.3f, 0.25f, 0.2f);
            debris.GetComponent<Renderer>().material = debrisMat;
        }
    }
    
    private void CreateForest()
    {
        for (int i = 0; i < 30; i++)
        {
            float angle = Random.Range(0f, 360f);
            float radius = Random.Range(30f, 60f);
            Vector3 pos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );
            
            CreateTree(pos);
        }
    }
    
    private void CreateTree(Vector3 position)
    {
        var treeObj = new GameObject("Tree");
        treeObj.transform.position = position;
        
        // Trunk
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(treeObj.transform);
        trunk.transform.localPosition = Vector3.up * 2f;
        trunk.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
        
        var trunkMat = new Material(Shader.Find("Standard"));
        trunkMat.color = new Color(0.3f, 0.2f, 0.1f);
        trunk.GetComponent<Renderer>().material = trunkMat;
        
        // Foliage
        var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.name = "Foliage";
        foliage.transform.SetParent(treeObj.transform);
        foliage.transform.localPosition = Vector3.up * 4.5f;
        foliage.transform.localScale = new Vector3(2.5f, 2f, 2.5f);
        
        var foliageMat = new Material(Shader.Find("Standard"));
        foliageMat.color = new Color(0.1f, 0.2f, 0.1f);
        foliage.GetComponent<Renderer>().material = foliageMat;
    }
    
    private void CreateBloodDecals()
    {
        for (int i = 0; i < 10; i++)
        {
            var decal = GameObject.CreatePrimitive(PrimitiveType.Plane);
            decal.name = $"BloodDecal_{i}";
            decal.transform.position = new Vector3(
                Random.Range(-40f, 40f),
                0.01f,
                Random.Range(-40f, 40f)
            );
            decal.transform.rotation = Quaternion.Euler(90f, Random.Range(0, 360), 0);
            decal.transform.localScale = new Vector3(
                Random.Range(0.5f, 2f),
                1f,
                Random.Range(0.5f, 2f)
            );
            
            var decalMat = new Material(Shader.Find("Standard"));
            decalMat.color = new Color(0.4f, 0.05f, 0.05f);
            decalMat.EnableKeyword("_EMISSION");
            decalMat.SetColor("_EmissionColor", new Color(0.1f, 0.01f, 0.01f));
            decal.GetComponent<Renderer>().material = decalMat;
        }
    }
    
    private void CreateEnvironmentalClues()
    {
        // Research notes
        for (int i = 0; i < 5; i++)
        {
            var note = GameObject.CreatePrimitive(PrimitiveType.Cube);
            note.name = $"ResearchNote_{i}";
            note.transform.position = new Vector3(
                Random.Range(-30f, 30f),
                1f,
                Random.Range(-30f, 30f)
            );
            note.transform.localScale = new Vector3(0.2f, 0.01f, 0.15f);
            note.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            var noteMat = new Material(Shader.Find("Standard"));
            noteMat.color = new Color(0.9f, 0.85f, 0.7f);
            note.GetComponent<Renderer>().material = noteMat;
        }
        
        // Destroyed equipment
        for (int i = 0; i < 3; i++)
        {
            var equip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            equip.name = $"DestroyedEquipment_{i}";
            equip.transform.position = new Vector3(
                Random.Range(-30f, 30f),
                0.5f,
                Random.Range(-30f, 30f)
            );
            equip.transform.localScale = new Vector3(1.5f, 1f, 1f);
            equip.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            var equipMat = new Material(Shader.Find("Standard"));
            equipMat.color = new Color(0.3f, 0.3f, 0.3f);
            equip.GetComponent<Renderer>().material = equipMat;
        }
        
        // Ritual symbols (on ground)
        for (int i = 0; i < 3; i++)
        {
            var symbol = GameObject.CreatePrimitive(PrimitiveType.Plane);
            symbol.name = $"RitualSymbol_{i}";
            symbol.transform.position = new Vector3(
                Random.Range(-20f, 20f),
                0.02f,
                Random.Range(-20f, 20f)
            );
            symbol.transform.rotation = Quaternion.Euler(90f, Random.Range(0, 360), 0);
            symbol.transform.localScale = new Vector3(2f, 1f, 2f);
            
            var symbolMat = new Material(Shader.Find("Standard"));
            symbolMat.color = new Color(0.6f, 0.1f, 0.1f);
            symbolMat.EnableKeyword("_EMISSION");
            symbolMat.SetColor("_EmissionColor", new Color(0.3f, 0.05f, 0.05f));
            symbol.GetComponent<Renderer>().material = symbolMat;
        }
    }
}
