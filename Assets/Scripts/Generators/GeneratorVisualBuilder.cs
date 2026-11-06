using UnityEngine;

/// <summary>
/// Builds a clearly recognizable portable industrial generator so the player
/// can identify repair targets at a glance: engine body, housing, fuel tank,
/// control panel with switches, indicator light, cables, wheels, legs and an
/// exhaust pipe, plus a subtle pulsing beacon visible from a distance.
/// </summary>
public static class GeneratorVisualBuilder
{
    public struct Refs
    {
        public Renderer indicator;
        public Light indicatorLight;
        public Renderer beacon;
        public Light beaconLight;
        public Transform body;
        public GeneratorBeacon beaconScript;
    }

    public static Refs Build(Transform parent)
    {
        var refs = new Refs();

        Color metal = new Color(0.30f, 0.31f, 0.34f);
        Color darkMetal = new Color(0.18f, 0.19f, 0.22f);
        Color trim = new Color(0.42f, 0.30f, 0.10f);

        // base skid / support legs
        Part(parent, "Base", new Vector3(0f, 0.16f, 0f), new Vector3(1.30f, 0.14f, 1.90f), darkMetal);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Part(parent, "Leg", new Vector3(sx * 0.52f, 0.10f, sz * 0.78f), new Vector3(0.14f, 0.20f, 0.14f), trim);

        // main engine body
        Part(parent, "GeneratorBody", new Vector3(0f, 0.62f, 0f), new Vector3(1.10f, 0.80f, 1.70f), metal);

        // engine housing (top)
        Part(parent, "EngineHousing", new Vector3(0f, 1.10f, 0.10f), new Vector3(0.90f, 0.30f, 1.20f), darkMetal);

        // fuel tank (cylinder lying along X)
        var tank = Cyl(parent, "FuelTank", new Vector3(0f, 1.20f, -0.55f), new Vector3(0.34f, 0.60f, 0.34f), trim);
        tank.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // control panel with switches
        Part(parent, "ControlPanel", new Vector3(0.62f, 0.72f, 0f), new Vector3(0.10f, 0.60f, 0.90f), darkMetal);
        for (int i = 0; i < 3; i++)
            Part(parent, "Switch" + i, new Vector3(0.70f, 0.84f - i * 0.16f, 0.12f), new Vector3(0.05f, 0.07f, 0.07f), new Color(0.85f, 0.85f, 0.88f));

        // indicator light (red -> green when repaired)
        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "IndicatorBulb";
        bulb.transform.SetParent(parent, false);
        bulb.transform.localPosition = new Vector3(0.70f, 0.95f, -0.22f);
        bulb.transform.localScale = Vector3.one * 0.13f;
        Object.DestroyImmediate(bulb.GetComponent<Collider>());
        var bulbMat = RuntimeCharacterFactory.Emissive(new Color(1f, 0.20f, 0.15f), 2.2f, true);
        var bulbR = bulb.GetComponent<Renderer>();
        bulbR.sharedMaterial = bulbMat;
        refs.indicator = bulbR;

        var indLight = new GameObject("IndicatorLight").AddComponent<Light>();
        indLight.transform.SetParent(parent, false);
        indLight.transform.localPosition = new Vector3(0.75f, 0.95f, -0.22f);
        indLight.type = LightType.Point;
        indLight.color = new Color(1f, 0.25f, 0.15f);
        indLight.range = 4f;
        indLight.intensity = 1.1f;
        indLight.shadows = LightShadows.None;
        indLight.renderMode = LightRenderMode.ForceVertex;
        refs.indicatorLight = indLight;

        // cables to the ground
        var cable = Cyl(parent, "Cable", new Vector3(0.35f, 0.30f, -0.90f), new Vector3(0.06f, 0.55f, 0.06f), new Color(0.05f, 0.05f, 0.06f));
        cable.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

        // wheels
        var wheelA = Cyl(parent, "Wheel_L", new Vector3(-0.58f, 0.14f, 0.45f), new Vector3(0.26f, 0.10f, 0.26f), new Color(0.07f, 0.07f, 0.08f));
        wheelA.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        var wheelB = Cyl(parent, "Wheel_R", new Vector3(0.58f, 0.14f, 0.45f), new Vector3(0.26f, 0.10f, 0.26f), new Color(0.07f, 0.07f, 0.08f));
        wheelB.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // exhaust pipe
        var pipe = Cyl(parent, "Exhaust", new Vector3(-0.35f, 1.25f, -0.35f), new Vector3(0.07f, 0.45f, 0.07f), darkMetal);
        pipe.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);

        // interaction marker (flat glowing disc under the generator)
        var marker = Cyl(parent, "InteractionMarker", new Vector3(0f, 0.03f, 0f), new Vector3(1.5f, 0.01f, 1.5f), new Color(1f, 0.75f, 0.10f));
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        var markerMat = RuntimeCharacterFactory.Emissive(new Color(1f, 0.75f, 0.10f), 1.2f, true);
        marker.GetComponent<Renderer>().sharedMaterial = markerMat;

        // beacon: thin pole + pulsing light, visible from a distance
        var pole = Cyl(parent, "BeaconPole", new Vector3(0f, 1.55f, -0.80f), new Vector3(0.04f, 1.1f, 0.04f), darkMetal);
        pole.transform.localPosition = new Vector3(0f, 1.85f, -0.80f);
        var beaconOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beaconOrb.name = "BeaconOrb";
        beaconOrb.transform.SetParent(parent, false);
        beaconOrb.transform.localPosition = new Vector3(0f, 2.45f, -0.80f);
        beaconOrb.transform.localScale = Vector3.one * 0.20f;
        Object.DestroyImmediate(beaconOrb.GetComponent<Collider>());
        var beaconMat = RuntimeCharacterFactory.Emissive(new Color(1f, 0.55f, 0.10f), 2.0f, true);
        var beaconR = beaconOrb.GetComponent<Renderer>();
        beaconR.sharedMaterial = beaconMat;
        refs.beacon = beaconR;

        var beaconLight = new GameObject("BeaconLight").AddComponent<Light>();
        beaconLight.transform.SetParent(parent, false);
        beaconLight.transform.localPosition = new Vector3(0f, 2.45f, -0.80f);
        beaconLight.type = LightType.Point;
        beaconLight.color = new Color(1f, 0.6f, 0.15f);
        beaconLight.range = 10f;
        beaconLight.intensity = 1.0f;
        beaconLight.shadows = LightShadows.None;
        beaconLight.renderMode = LightRenderMode.ForceVertex;
        refs.beaconLight = beaconLight;

        var script = parent.gameObject.AddComponent<GeneratorBeacon>();
        script.orb = beaconR;
        script.beaconLight = beaconLight;
        refs.beaconScript = script;
        refs.body = parent;

        return refs;
    }

    private static GameObject Part(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = RuntimeCharacterFactory.Surface(color);
        return obj;
    }

    private static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = RuntimeCharacterFactory.Surface(color);
        return obj;
    }
}

