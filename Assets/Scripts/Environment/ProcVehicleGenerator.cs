using UnityEngine;

public enum VehicleKind
{
    Sedan,
    Van,
    Pickup
}

/// <summary>
/// Low-poly abandoned vehicles: body, cabin, glass, wheels, bumpers and lamps.
/// Each vehicle is roughly 40 primitives and shares its materials with the
/// rest of the town.
/// </summary>
public static class ProcVehicleGenerator
{
    private static readonly Color[] BodyColors =
    {
        new Color(0.290f, 0.115f, 0.095f), // faded rust red
        new Color(0.115f, 0.155f, 0.225f), // dull blue
        new Color(0.140f, 0.180f, 0.150f), // dead green
        new Color(0.225f, 0.225f, 0.232f), // primer grey
        new Color(0.300f, 0.272f, 0.215f)  // dirty beige
    };

    public static void GenerateAll(Transform parent, System.Random rng)
    {
        Create(parent, new Vector3(-27.0f, 0f, -4.0f), 24f, VehicleKind.Sedan, rng, 0, true);
        Create(parent, new Vector3(-29.5f, 0f, 3.6f), 104f, VehicleKind.Van, rng, 1, false);
        Create(parent, new Vector3(-22.0f, 0f, 4.8f), 78f, VehicleKind.Sedan, rng, 3, true);
        Create(parent, new Vector3(13.2f, 0f, -28.5f), 96f, VehicleKind.Pickup, rng, 2, true);
        Create(parent, new Vector3(-31.0f, 0f, -13.6f), 8f, VehicleKind.Sedan, rng, 4, false);
        Create(parent, new Vector3(28.2f, 0f, -10.7f), 183f, VehicleKind.Van, rng, 0, true);
    }

    public static GameObject Create(Transform parent, Vector3 position, float rotY,
        VehicleKind kind, System.Random rng, int colorIndex, bool damaged)
    {
        var root = new GameObject("Vehicle_" + kind);
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = position;
        t.rotation = Quaternion.Euler(
            damaged ? ProcRoadGenerator.Rand(rng, -1.5f, 1.5f) : 0f,
            rotY + ProcRoadGenerator.Rand(rng, -6f, 6f),
            damaged ? ProcRoadGenerator.Rand(rng, 1.5f, 4.5f) : 0f);

        var body = ProcMaterials.Lit("car_" + colorIndex, BodyColors[colorIndex], 0.16f);
        var bodyDark = ProcMaterials.Lit("car_dark", new Color(0.090f, 0.090f, 0.098f), 0.1f);
        var glass = ProcMaterials.Lit("car_glass", new Color(0.048f, 0.060f, 0.075f), 0.55f);
        var glassBroken = ProcMaterials.Lit("car_glass_broken", new Color(0.085f, 0.092f, 0.100f), 0.25f);
        var tire = ProcMaterials.Lit("tire", new Color(0.050f, 0.050f, 0.055f));
        var chrome = ProcMaterials.Lit("chrome", new Color(0.320f, 0.320f, 0.328f), 0.5f);
        var headlight = ProcMaterials.Lit("headlight", new Color(0.520f, 0.520f, 0.450f), 0.65f);
        var taillight = ProcMaterials.Lit("taillight", new Color(0.24f, 0.035f, 0.035f), 0.35f);

        float bodyHeight;
        float bodyLength;

        switch (kind)
        {
            case VehicleKind.Van:
                bodyHeight = 1.02f;
                bodyLength = 4.5f;
                ProcBuild.Box(t, "Body", new Vector3(0f, 0.82f, 0f), new Vector3(1.92f, bodyHeight, bodyLength), body, true);
                ProcBuild.Box(t, "Cabin", new Vector3(0f, 1.62f, -0.15f), new Vector3(1.82f, 0.72f, 3.35f), body);
                ProcBuild.Box(t, "Roof", new Vector3(0f, 2.0f, -0.25f), new Vector3(1.7f, 0.08f, 3.0f), bodyDark);
                AddSideWindows(t, 1.82f, 1.55f, -0.2f, 2.5f, glass, damaged ? glassBroken : null, rng);
                AddGlass(t, new Vector3(0f, 1.55f, 1.52f), new Vector3(1.48f, 0.62f, 0.06f), new Vector3(-12f, 0f, 0f), glass);
                AddGlass(t, new Vector3(0f, 1.55f, -1.86f), new Vector3(1.52f, 0.58f, 0.06f), new Vector3(10f, 0f, 0f), glass);
                break;

            case VehicleKind.Pickup:
                bodyHeight = 0.66f;
                bodyLength = 4.4f;
                ProcBuild.Box(t, "Body", new Vector3(0f, 0.66f, 0f), new Vector3(1.86f, bodyHeight, bodyLength), body, true);
                ProcBuild.Box(t, "Hood", new Vector3(0f, 1.04f, 1.55f), new Vector3(1.8f, 0.26f, 1.15f), body);
                ProcBuild.Box(t, "Cabin", new Vector3(0f, 1.24f, 0.25f), new Vector3(1.72f, 0.66f, 1.62f), body);
                ProcBuild.Box(t, "Roof", new Vector3(0f, 1.60f, 0.2f), new Vector3(1.62f, 0.07f, 1.45f), bodyDark);
                AddGlass(t, new Vector3(0f, 1.3f, 1.09f), new Vector3(1.42f, 0.55f, 0.06f), new Vector3(-16f, 0f, 0f), glass);
                AddGlass(t, new Vector3(0f, 1.3f, -0.58f), new Vector3(1.45f, 0.5f, 0.06f), new Vector3(14f, 0f, 0f), glass);
                // Cargo bed walls
                ProcBuild.Box(t, "BedFloor", new Vector3(0f, 0.92f, -1.5f), new Vector3(1.74f, 0.1f, 1.55f), bodyDark);
                ProcBuild.Box(t, "BedSideL", new Vector3(-0.86f, 1.12f, -1.5f), new Vector3(0.09f, 0.42f, 1.55f), body);
                ProcBuild.Box(t, "BedSideR", new Vector3(0.86f, 1.12f, -1.5f), new Vector3(0.09f, 0.42f, 1.55f), body);
                ProcBuild.Box(t, "BedTail", new Vector3(0f, 1.12f, -2.25f), new Vector3(1.8f, 0.42f, 0.09f), body);
                break;

            default: // Sedan
                bodyHeight = 0.58f;
                bodyLength = 4.15f;
                ProcBuild.Box(t, "Body", new Vector3(0f, 0.64f, 0f), new Vector3(1.80f, bodyHeight, bodyLength), body, true);
                ProcBuild.Box(t, "Hood", new Vector3(0f, 0.94f, 1.52f), new Vector3(1.74f, 0.22f, 1.05f), body);
                ProcBuild.Box(t, "Trunk", new Vector3(0f, 0.94f, -1.55f), new Vector3(1.74f, 0.22f, 0.95f), body);
                ProcBuild.Box(t, "Cabin", new Vector3(0f, 1.18f, -0.1f), new Vector3(1.64f, 0.62f, 1.95f), body);
                ProcBuild.Box(t, "Roof", new Vector3(0f, 1.51f, -0.15f), new Vector3(1.54f, 0.07f, 1.72f), bodyDark);
                AddGlass(t, new Vector3(0f, 1.24f, 0.88f), new Vector3(1.36f, 0.52f, 0.06f), new Vector3(-24f, 0f, 0f), glass);
                AddGlass(t, new Vector3(0f, 1.24f, -1.07f), new Vector3(1.4f, 0.48f, 0.06f), new Vector3(22f, 0f, 0f), glass);
                AddSideWindows(t, 1.64f, 1.2f, -0.15f, 1.55f, glass, damaged ? glassBroken : null, rng);
                break;
        }

        // Bumpers
        ProcBuild.Box(t, "BumperFront", new Vector3(0f, 0.48f, 2.08f), new Vector3(1.86f, 0.16f, 0.18f), chrome);
        ProcBuild.Box(t, "BumperRear", new Vector3(0f, 0.48f, -2.08f), new Vector3(1.86f, 0.16f, 0.18f), chrome);

        // Lamps
        ProcBuild.Box(t, "HeadlightL", new Vector3(-0.52f, 0.78f, 2.06f), new Vector3(0.28f, 0.16f, 0.05f), headlight);
        ProcBuild.Box(t, "HeadlightR", new Vector3(0.52f, 0.78f, 2.06f), new Vector3(0.28f, 0.16f, 0.05f), headlight);
        ProcBuild.Box(t, "TaillightL", new Vector3(-0.56f, 0.80f, -2.06f), new Vector3(0.24f, 0.14f, 0.05f), taillight);
        ProcBuild.Box(t, "TaillightR", new Vector3(0.56f, 0.80f, -2.06f), new Vector3(0.24f, 0.14f, 0.05f), taillight);

        // Wheels
        float wheelZ = kind == VehicleKind.Van ? 1.42f : 1.38f;
        AddWheel(t, -0.87f, 0.34f, wheelZ, tire);
        AddWheel(t, 0.87f, 0.34f, wheelZ, tire);
        AddWheel(t, -0.87f, 0.34f, -wheelZ, tire);
        AddWheel(t, 0.87f, 0.34f, -wheelZ, tire);

        if (damaged)
        {
            // Missing wheel, dented door and a shattered window.
            var wheels = new[] { "Wheel_0", "Wheel_1", "Wheel_2", "Wheel_3" };
            var victim = root.transform.Find(wheels[rng.Next(wheels.Length)]);
            if (victim != null) Object.Destroy(victim.gameObject);

            ProcBuild.BoxRot(t, "Dent", new Vector3(-0.91f, 0.78f, ProcRoadGenerator.Rand(rng, -0.8f, 0.8f)),
                new Vector3(0.05f, 0.3f, 0.55f), new Vector3(0f, 0f, ProcRoadGenerator.Rand(rng, -6f, 6f)), bodyDark);

            if (rng.NextDouble() < 0.5)
                ProcBuild.Box(t, "BrokenGlass", new Vector3(0f, 0.42f, 1.1f),
                    new Vector3(1.1f, 0.02f, 0.5f), glassBroken);
        }

        // Collision: one box for the whole vehicle is enough.
        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.85f, 0f);
        col.size = new Vector3(1.95f, 1.75f, bodyLength + 0.2f);

        return root;
    }

    private static void AddWheel(Transform parent, float x, float y, float z, Material tire)
    {
        var w = ProcBuild.CylRot(parent, "Wheel", new Vector3(x, y, z), 0.34f, 0.24f,
            new Vector3(0f, 0f, 90f), tire);
        w.name = "Wheel_" + Mathf.RoundToInt((x + 1f) * 2f + z);
    }

    private static void AddGlass(Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
    {
        ProcBuild.BoxRot(parent, "Glass", pos, scale, euler, mat);
    }

    private static void AddSideWindows(Transform parent, float width, float y, float z,
        float length, Material glass, Material broken, System.Random rng)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            Material mat = (broken != null && rng.NextDouble() < 0.5) ? broken : glass;
            ProcBuild.Box(parent, "SideWindow",
                new Vector3(s * (width * 0.5f + 0.01f), y, z),
                new Vector3(0.04f, 0.44f, length), mat);
        }
    }
}
