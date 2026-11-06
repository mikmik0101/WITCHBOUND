using UnityEngine;

/// <summary>
/// Night-time atmosphere: gradient sky with moon and stars, linear distance
/// fog, low blue ambient, camera grading and a handful of soft ground-fog
/// pockets. All effects are built-in-pipeline friendly and cheap enough for
/// integrated graphics.
/// </summary>
public static class ProcAtmosphereSetup
{
    public static void Generate(Transform systemsParent, System.Random rng)
    {
        SetupSky();
        SetupFogAndAmbient();
        SetupCamera();
        CreateGroundMist(systemsParent, rng);
        CreateDust(systemsParent);
    }

    private static void SetupSky()
    {
        var skyMat = ProcMaterials.NightSky();
        if (skyMat != null)
        {
            skyMat.SetColor("_TopColor", new Color(0.010f, 0.016f, 0.034f));
            skyMat.SetColor("_HorizonColor", new Color(0.055f, 0.070f, 0.110f));
            skyMat.SetColor("_GroundColor", new Color(0.014f, 0.018f, 0.028f));
            skyMat.SetColor("_MoonColor", new Color(0.80f, 0.86f, 0.99f));
            skyMat.SetVector("_MoonDir", new Vector4(0.42f, 0.48f, 0.77f, 0f).normalized);
            skyMat.SetFloat("_MoonSize", 0.042f);
            skyMat.SetFloat("_StarIntensity", 0.28f);
            RenderSettings.skybox = skyMat;
        }
        else
        {
            RenderSettings.skybox = null;
        }
    }

    private static void SetupFogAndAmbient()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.062f, 0.076f, 0.110f);
        RenderSettings.fogStartDistance = 9f;
        RenderSettings.fogEndDistance = 95f;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.145f, 0.165f, 0.215f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.reflectionIntensity = 0.06f;

        DynamicGI.UpdateEnvironment();
    }

    private static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.012f, 0.016f, 0.030f);
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 260f;
        cam.fieldOfView = 66f;
        cam.allowHDR = false;
        cam.allowMSAA = true;

        var listener = cam.GetComponent<AudioListener>();
        if (listener == null)
        {
            // AudioListener is normally on the camera already; only add if the
            // scene is missing one so 3D audio keeps working.
            if (Object.FindObjectOfType<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();
        }
    }

    private static void CreateGroundMist(Transform parent, System.Random rng)
    {
        // Mist is delivered entirely through billboarded particles. Static
        // horizontal sheets look like a solid pale wash when viewed from eye
        // height (grazing angle), which hid the plaza and the ritual circle.
        var psObj = new GameObject("MistParticles");
        psObj.transform.SetParent(parent, false);
        psObj.transform.position = new Vector3(0f, 1.7f, 0f);

        var ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 20f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 20f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(4.5f, 9f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(0.55f, 0.60f, 0.72f, 0.055f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 1.6f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(74f, 1.6f, 74f);

        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = ProcMaterials.SoftFog("fog_particle",
            new Color(0.52f, 0.58f, 0.70f, 0.05f), 1.5f, false);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 0;
    }

    private static void CreateDust(Transform parent)
    {
        var psObj = new GameObject("DustMotes");
        psObj.transform.SetParent(parent, false);
        psObj.transform.position = new Vector3(0f, 2.0f, 0f);

        var ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 16f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.startColor = new Color(0.75f, 0.78f, 0.88f, 0.45f);
        main.maxParticles = 70;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.002f;

        var emission = ps.emission;
        emission.rateOverTime = 5.5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(46f, 3f, 46f);

        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = ProcMaterials.SoftFog("dust_particle",
            new Color(0.70f, 0.74f, 0.88f, 0.40f), 2.4f, true);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
