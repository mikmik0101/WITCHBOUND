using UnityEngine;

/// <summary>
/// Deliberate lighting hierarchy: cold moonlight as the key, sparse warm
/// sodium street lamps as the secondary layer, and a handful of cold/red
/// accents. Point lights never cast shadows so the Intel HD 620 target stays
/// playable.
/// </summary>
public static class ProcLightingSetup
{
    public static void Generate(Transform geometryParent, Transform systemsParent, System.Random rng)
    {
        SetupMoon();
        SetupQuality();

        // Warm street lamps. Mix of steady, flickering, weak and dead.
        Lamp(geometryParent, systemsParent, new Vector3(8.0f, 0f, 8.0f), 0.35f, LampMode.Steady, rng);
        Lamp(geometryParent, systemsParent, new Vector3(-8.0f, 0f, 8.0f), Mathf.PI * 0.5f, LampMode.Flicker, rng);
        Lamp(geometryParent, systemsParent, new Vector3(8.0f, 0f, -8.0f), Mathf.PI, LampMode.Weak, rng);
        Lamp(geometryParent, systemsParent, new Vector3(-8.0f, 0f, -8.0f), Mathf.PI * 1.5f, LampMode.Broken, rng);

        Lamp(geometryParent, systemsParent, new Vector3(16.4f, 0f, 16.4f), Mathf.PI * 1.5f, LampMode.Flicker, rng);
        Lamp(geometryParent, systemsParent, new Vector3(-16.4f, 0f, 16.4f), 0.2f, LampMode.Steady, rng);
        Lamp(geometryParent, systemsParent, new Vector3(16.4f, 0f, -16.4f), Mathf.PI, LampMode.Broken, rng);
        Lamp(geometryParent, systemsParent, new Vector3(-16.4f, 0f, -16.4f), Mathf.PI * 0.5f, LampMode.Steady, rng);

        Lamp(geometryParent, systemsParent, new Vector3(32.0f, 0f, 8.0f), Mathf.PI, LampMode.Flicker, rng);
        Lamp(geometryParent, systemsParent, new Vector3(-32.0f, 0f, -8.0f), 0f, LampMode.Weak, rng);

        // Emergency accents.
        EmergencyLight(systemsParent, new Vector3(10f, 2.5f, 0f), new Color(0.95f, 0.12f, 0.08f), 0.9f, 8f);
        EmergencyLight(systemsParent, new Vector3(-20f, 2.6f, 0f), new Color(0.35f, 0.72f, 1.0f), 0.8f, 10f);

        // Gate frame around the existing EscapeGate so it reads as a real exit.
        BuildGateFrame(geometryParent, systemsParent);
    }

    private static void SetupMoon()
    {
        Light moon = null;
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                moon = l;
                break;
            }
        }

        if (moon == null)
        {
            var go = new GameObject("Moonlight");
            moon = go.AddComponent<Light>();
            moon.type = LightType.Directional;
        }

        moon.name = "Moonlight";
        moon.color = new Color(0.60f, 0.72f, 0.98f);
        moon.intensity = 0.90f;
        moon.shadows = LightShadows.Soft;
        moon.shadowStrength = 0.80f;
        moon.shadowBias = 0.05f;
        moon.shadowNormalBias = 0.35f;
        moon.shadowCustomResolution = 1024;
        moon.transform.rotation = Quaternion.Euler(40f, 207f, 0f);
        moon.transform.position = new Vector3(0f, 28f, 0f);

        RenderSettings.sun = moon;

        // A soft, shadowless cold fill from the opposite side keeps facades
        // and the street readable without flattening the moonlight key.
        // (Intel HD 620: one extra directional, no shadows.)
        var fill = new GameObject("MoonFill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.42f, 0.50f, 0.68f);
        fill.intensity = 0.42f;
        fill.shadows = LightShadows.None;
        fill.bounceIntensity = 0f;
        fill.renderMode = LightRenderMode.ForcePixel;
        fill.transform.rotation = Quaternion.Euler(18f, 24f, 0f);
    }

    private static void SetupQuality()
    {
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowDistance = 58f;
        QualitySettings.shadowCascades = 1;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.pixelLightCount = 4;
        QualitySettings.antiAliasing = 2;
    }

    // ------------------------------------------------------------------- lamps

    private enum LampMode
    {
        Steady,
        Flicker,
        Weak,
        Broken
    }

    private static void Lamp(Transform geometryParent, Transform systemsParent, Vector3 position,
        float yaw, LampMode mode, System.Random rng)
    {
        var root = new GameObject(mode == LampMode.Broken ? "StreetLamp_Broken" : "StreetLamp");
        var t = root.transform;
        t.SetParent(geometryParent, false);
        t.position = position;
        t.rotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg + ProcRoadGenerator.Rand(rng, -8f, 8f), 0f);

        var metal = ProcMaterials.Lit("lamp_metal", new Color(0.075f, 0.075f, 0.085f), 0.3f);
        var headMat = ProcMaterials.Lit("lamp_head", new Color(0.055f, 0.055f, 0.06f), 0.2f);

        // Wrought-iron pole with a slight taper illusion (two stacked cylinders).
        ProcBuild.Cyl(t, "PoleBase", new Vector3(0f, 0.22f, 0f), 0.19f, 0.44f, metal);
        ProcBuild.Cyl(t, "Pole", new Vector3(0f, 2.35f, 0f), 0.085f, 4.3f, metal);
        ProcBuild.Cyl(t, "PoleTop", new Vector3(0f, 4.52f, 0f), 0.075f, 0.3f, metal);

        // Arm reaching out over the pavement.
        ProcBuild.BoxRot(t, "Arm", new Vector3(0f, 4.62f, 0.55f), new Vector3(0.07f, 0.07f, 1.15f),
            new Vector3(8f, 0f, 0f), metal);
        ProcBuild.Box(t, "Head", new Vector3(0f, 4.46f, 1.10f), new Vector3(0.42f, 0.20f, 0.62f), headMat);
        ProcBuild.BoxRot(t, "HeadTrim", new Vector3(0f, 4.56f, 1.10f), new Vector3(0.5f, 0.05f, 0.7f),
            new Vector3(0f, 0f, 0f), metal);

        // Bulb: emissive only when the lamp is alive.
        bool alive = mode != LampMode.Broken;
        var bulbMat = alive
            ? ProcMaterials.Emissive("lamp_bulb_on",
                new Color(0.60f, 0.44f, 0.22f), new Color(1f, 0.74f, 0.40f), 1.25f)
            : ProcMaterials.Lit("lamp_bulb_off", new Color(0.07f, 0.07f, 0.06f));

        ProcBuild.Box(t, "Bulb", new Vector3(0f, 4.35f, 1.10f), new Vector3(0.30f, 0.06f, 0.48f), bulbMat);

        if (!alive) return;

        var lightObj = new GameObject("LampLight");
        lightObj.transform.SetParent(systemsParent, false);
        lightObj.transform.position = t.TransformPoint(new Vector3(0f, 4.18f, 1.10f));

        var l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.70f, 0.38f);
        l.range = mode == LampMode.Weak ? 8f : 14f;
        l.intensity = mode == LampMode.Weak ? 0.75f : 1.45f;
        l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForcePixel;
        l.bounceIntensity = 0f;

        if (mode == LampMode.Flicker)
        {
            var flicker = lightObj.AddComponent<FlickerLight>();
            flicker.target = l;
            flicker.baseIntensity = 1.45f;
            flicker.minIntensity = 0.22f;
            flicker.speed = ProcRoadGenerator.Rand(rng, 5f, 9f);
            flicker.seed = rng.Next(1, 9999);
        }
    }

    private static void EmergencyLight(Transform systemsParent, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject("EmergencyLight");
        go.transform.SetParent(systemsParent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForcePixel;
        l.bounceIntensity = 0f;

        var flicker = go.AddComponent<FlickerLight>();
        flicker.target = l;
        flicker.baseIntensity = intensity;
        flicker.minIntensity = intensity * 0.55f;
        flicker.speed = 1.2f;
        flicker.seed = Mathf.RoundToInt(pos.x * 17f + pos.z * 31f) & 0x7fff;
    }

    private static void BuildGateFrame(Transform geometryParent, Transform systemsParent)
    {
        var root = new GameObject("EscapeGateFrame");
        var t = root.transform;
        t.SetParent(geometryParent, false);
        t.position = new Vector3(-20f, 0f, 0f);

        var metal = ProcMaterials.Lit("gate_metal", new Color(0.09f, 0.095f, 0.10f), 0.35f);
        var glow = ProcMaterials.Emissive("gate_glow",
            new Color(0.04f, 0.12f, 0.15f), new Color(0.25f, 0.85f, 1f), 0.9f);

        ProcBuild.Box(t, "PostL", new Vector3(-2.25f, 2.1f, 0f), new Vector3(0.28f, 4.2f, 0.28f), metal);
        ProcBuild.Box(t, "PostR", new Vector3(2.25f, 2.1f, 0f), new Vector3(0.28f, 4.2f, 0.28f), metal);
        ProcBuild.Box(t, "Lintel", new Vector3(0f, 4.35f, 0f), new Vector3(4.8f, 0.28f, 0.28f), metal);
        ProcBuild.Box(t, "Sign", new Vector3(0f, 4.05f, 0.15f), new Vector3(2.4f, 0.42f, 0.08f), glow);
        ProcBuild.Box(t, "SignTrim", new Vector3(0f, 4.32f, 0.15f), new Vector3(2.6f, 0.08f, 0.12f), metal);

        // Cold strip lights on the posts.
        ProcBuild.Box(t, "StripL", new Vector3(-2.25f, 2.2f, 0.17f), new Vector3(0.07f, 1.6f, 0.04f), glow);
        ProcBuild.Box(t, "StripR", new Vector3(2.25f, 2.2f, 0.17f), new Vector3(0.07f, 1.6f, 0.04f), glow);
    }
}
