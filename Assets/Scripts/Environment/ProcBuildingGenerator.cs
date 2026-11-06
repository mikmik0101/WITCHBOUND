using UnityEngine;

public enum BuildingKind
{
    SmallHouse,
    TwoStoryHouse,
    Store,
    Warehouse,
    PublicBuilding,
    Ruin
}

/// <summary>
/// Procedural abandoned-town buildings. Six archetypes, built entirely from
/// primitives with shared materials. Each building gets a body, trim, roof,
/// door, window grid with per-window states (dark / lit / boarded / curtain /
/// broken) and damage details.
/// </summary>
public static class ProcBuildingGenerator
{
    private static readonly string[] WallKeys = { "wall_0", "wall_1", "wall_2", "wall_3", "wall_4" };
    private static readonly Color[] WallColors =
    {
        new Color(0.360f, 0.325f, 0.292f), // faded cream siding
        new Color(0.300f, 0.300f, 0.335f), // cold grey-blue siding
        new Color(0.390f, 0.340f, 0.270f), // sun-bleached tan brick
        new Color(0.285f, 0.320f, 0.315f), // mossy green board
        new Color(0.400f, 0.355f, 0.315f)  // dirty peach stucco
    };

    public static GameObject Generate(Transform parent, Vector3 position, float rotY, BuildingKind kind, System.Random rng)
    {
        var root = new GameObject("Building_" + kind);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;
        root.transform.localEulerAngles = new Vector3(0f, rotY, 0f);

        switch (kind)
        {
            case BuildingKind.SmallHouse: BuildHouse(root.transform, rng, false); break;
            case BuildingKind.TwoStoryHouse: BuildHouse(root.transform, rng, true); break;
            case BuildingKind.Store: BuildStore(root.transform, rng); break;
            case BuildingKind.Warehouse: BuildWarehouse(root.transform, rng); break;
            case BuildingKind.PublicBuilding: BuildPublicBuilding(root.transform, rng); break;
            case BuildingKind.Ruin: BuildRuin(root.transform, rng); break;
        }

        return root;
    }

    // ---------------------------------------------------------------- helpers

    private static Material Wall(System.Random rng)
    {
        int i = rng.Next(WallKeys.Length);
        return ProcMaterials.Lit(WallKeys[i], WallColors[i]);
    }

    private static Material Trim()
    {
        return ProcMaterials.Lit("trim_dark", new Color(0.165f, 0.155f, 0.145f));
    }

    private static Material RoofMat()
    {
        return ProcMaterials.Lit("roof", new Color(0.150f, 0.132f, 0.140f));
    }

    private static Material DoorMat()
    {
        return ProcMaterials.Lit("door", new Color(0.185f, 0.135f, 0.100f));
    }

    private static Material Wood()
    {
        return ProcMaterials.Lit("wood", new Color(0.255f, 0.195f, 0.135f));
    }

    private static Material Concrete()
    {
        return ProcMaterials.Lit("concrete", new Color(0.300f, 0.290f, 0.278f));
    }

    private static void Window(Transform parent, Vector3 localPos, float rotY, System.Random rng)
    {
        var wroot = new GameObject("Window");
        var wt = wroot.transform;
        wt.SetParent(parent, false);
        wt.localPosition = localPos;
        wt.localEulerAngles = new Vector3(0f, rotY, 0f);

        var frameMat = Trim();
        ProcBuild.Box(wt, "Frame", Vector3.zero, new Vector3(1.42f, 1.62f, 0.10f), frameMat);
        ProcBuild.Box(wt, "Sill", new Vector3(0f, -0.86f, 0.06f), new Vector3(1.52f, 0.09f, 0.22f), frameMat);

        double roll = rng.NextDouble();

        if (roll < 0.10)
        {
            // Faint interior light
            var litMat = ProcMaterials.Emissive("window_lit",
                new Color(0.30f, 0.24f, 0.16f), new Color(1f, 0.78f, 0.50f), 0.55f);
            ProcBuild.Box(wt, "Glass", new Vector3(0f, 0f, 0.045f), new Vector3(1.05f, 1.24f, 0.05f), litMat);
            ProcBuild.Box(wt, "Mullion", new Vector3(0f, 0f, 0.065f), new Vector3(0.06f, 1.2f, 0.03f), frameMat);
        }
        else if (roll < 0.22)
        {
            // Curtain behind dark glass
            var darkGlass = ProcMaterials.Lit("glass_dark", new Color(0.055f, 0.065f, 0.085f), 0.35f);
            ProcBuild.Box(wt, "Glass", new Vector3(0f, 0f, 0.045f), new Vector3(1.05f, 1.24f, 0.05f), darkGlass);
            var curtain = ProcMaterials.Transparent("curtain", new Color(0.34f, 0.30f, 0.25f), 0.75f);
            ProcBuild.Box(wt, "Curtain", new Vector3(0f, 0f, 0.055f), new Vector3(0.95f, 1.1f, 0.02f), curtain);
            ProcBuild.Box(wt, "CurtainSplit", new Vector3(0f, 0f, 0.065f), new Vector3(0.05f, 1.1f, 0.02f), frameMat);
        }
        else if (roll < 0.36)
        {
            // Broken pane
            var darkGlass = ProcMaterials.Lit("glass_dark", new Color(0.055f, 0.065f, 0.085f), 0.35f);
            ProcBuild.Box(wt, "Glass", new Vector3(0f, 0f, 0.045f), new Vector3(1.05f, 1.24f, 0.05f), darkGlass);
            var shard = ProcMaterials.Lit("glass_shard", new Color(0.42f, 0.44f, 0.47f), 0.6f);
            ProcBuild.BoxRot(wt, "CrackA", new Vector3(0.1f, 0.05f, 0.07f),
                new Vector3(0.02f, 0.85f, 0.02f), new Vector3(0f, 0f, 24f), shard);
            ProcBuild.BoxRot(wt, "CrackB", new Vector3(-0.15f, -0.1f, 0.07f),
                new Vector3(0.02f, 0.62f, 0.02f), new Vector3(0f, 0f, -33f), shard);
        }
        else if (roll < 0.52)
        {
            // Boarded up
            var darkGlass = ProcMaterials.Lit("glass_void", new Color(0.022f, 0.024f, 0.030f));
            ProcBuild.Box(wt, "Void", new Vector3(0f, 0f, 0.045f), new Vector3(1.05f, 1.24f, 0.05f), darkGlass);
            var wood = Wood();
            ProcBuild.BoxRot(wt, "BoardA", new Vector3(0f, 0.3f, 0.09f),
                new Vector3(1.35f, 0.16f, 0.05f), new Vector3(0f, 0f, 6f), wood);
            ProcBuild.BoxRot(wt, "BoardB", new Vector3(0f, -0.12f, 0.09f),
                new Vector3(1.35f, 0.16f, 0.05f), new Vector3(0f, 0f, -4f), wood);
        }
        else
        {
            // Dark interior
            var darkGlass = ProcMaterials.Lit("glass_dark", new Color(0.055f, 0.065f, 0.085f), 0.35f);
            ProcBuild.Box(wt, "Glass", new Vector3(0f, 0f, 0.045f), new Vector3(1.05f, 1.24f, 0.05f), darkGlass);
            ProcBuild.Box(wt, "Mullion", new Vector3(0f, 0f, 0.065f), new Vector3(0.06f, 1.2f, 0.03f), frameMat);
            ProcBuild.Box(wt, "MullionH", new Vector3(0f, 0.15f, 0.065f), new Vector3(1.0f, 0.05f, 0.03f), frameMat);
        }
    }

    private static void Door(Transform parent, float x, float z, float rotY, System.Random rng, bool boarded)
    {
        var droot = new GameObject("Door");
        var dt = droot.transform;
        dt.SetParent(parent, false);
        dt.localPosition = new Vector3(x, 0f, z);
        dt.localEulerAngles = new Vector3(0f, rotY, 0f);

        var frameMat = Trim();
        ProcBuild.Box(dt, "Frame", new Vector3(0f, 1.12f, 0f), new Vector3(1.32f, 2.24f, 0.12f), frameMat);
        ProcBuild.Box(dt, "Panel", new Vector3(0f, 1.05f, 0.045f), new Vector3(1.04f, 2.0f, 0.06f), DoorMat());
        ProcBuild.Box(dt, "Step", new Vector3(0f, 0.09f, 0.28f), new Vector3(1.45f, 0.18f, 0.55f), Concrete());

        // Small lintel / awning
        if (rng.NextDouble() < 0.6)
        {
            ProcBuild.BoxRot(dt, "Awning", new Vector3(0f, 2.45f, 0.22f),
                new Vector3(1.6f, 0.06f, 0.6f), new Vector3(12f, 0f, 0f), RoofMat());
        }

        if (boarded || rng.NextDouble() < 0.3)
        {
            var wood = Wood();
            ProcBuild.BoxRot(dt, "BoardA", new Vector3(0f, 1.45f, 0.09f),
                new Vector3(1.3f, 0.17f, 0.05f), new Vector3(0f, 0f, 8f), wood);
            ProcBuild.BoxRot(dt, "BoardB", new Vector3(0f, 0.9f, 0.09f),
                new Vector3(1.3f, 0.17f, 0.05f), new Vector3(0f, 0f, -6f), wood);
        }
    }

    private static void WindowGrid(Transform parent, float width, float depth, float wallHeight,
        bool twoFloors, System.Random rng)
    {
        int cols = width > 8.5f ? 3 : 2;
        int rows = twoFloors ? 2 : 1;
        float spacing = width / (cols + 1);

        for (int r = 0; r < rows; r++)
        {
            float y = twoFloors ? (r == 0 ? 1.75f : wallHeight * 0.5f + 1.55f) : wallHeight * 0.52f;
            for (int c = 0; c < cols; c++)
            {
                float x = -width * 0.5f + spacing * (c + 1);
                Window(parent, new Vector3(x, y, depth * 0.5f + 0.06f), 0f, rng);
            }
        }

        // A couple of side windows
        int sideCount = twoFloors ? 2 : 1;
        for (int s = -1; s <= 1; s += 2)
        {
            for (int i = 0; i < sideCount; i++)
            {
                float z = (i - (sideCount - 1) * 0.5f) * 2.4f;
                float y = twoFloors ? 1.8f + i * 1.9f : wallHeight * 0.55f;
                Window(parent, new Vector3(s * (width * 0.5f + 0.06f), Mathf.Min(y, wallHeight - 1.1f), z),
                    s * 90f, rng);
            }
        }

        // Rear windows (fewer)
        if (rng.NextDouble() < 0.7)
        {
            Window(parent, new Vector3(0f, wallHeight * 0.55f, -depth * 0.5f - 0.06f), 180f, rng);
        }
    }

    private static void GableRoof(Transform parent, float width, float depth, float wallHeight, System.Random rng)
    {
        float rise = ProcRoadGenerator.Rand(rng, 1.5f, 2.3f);
        float overhang = 0.45f;
        float halfW = width * 0.5f + overhang;
        float slopeLen = Mathf.Sqrt(halfW * halfW + rise * rise);
        float angle = Mathf.Atan2(rise, halfW) * Mathf.Rad2Deg;
        var mat = RoofMat();

        ProcBuild.BoxRot(parent, "RoofLeft",
            new Vector3(-halfW * 0.5f, wallHeight + rise * 0.5f + 0.04f, 0f),
            new Vector3(slopeLen, 0.18f, depth + 0.9f),
            new Vector3(0f, 0f, angle), mat);

        ProcBuild.BoxRot(parent, "RoofRight",
            new Vector3(halfW * 0.5f, wallHeight + rise * 0.5f + 0.04f, 0f),
            new Vector3(slopeLen, 0.18f, depth + 0.9f),
            new Vector3(0f, 0f, -angle), mat);

        // Simple gable faces
        ProcBuild.Box(parent, "GableFront",
            new Vector3(0f, wallHeight + rise * 0.30f, depth * 0.5f + 0.02f),
            new Vector3(width * 0.52f, rise * 0.58f, 0.14f), Trim());
        ProcBuild.Box(parent, "GableBack",
            new Vector3(0f, wallHeight + rise * 0.30f, -depth * 0.5f - 0.02f),
            new Vector3(width * 0.52f, rise * 0.58f, 0.14f), Trim());

        // Attic vent
        ProcBuild.Box(parent, "AtticVent",
            new Vector3(0f, wallHeight + rise * 0.28f, depth * 0.5f + 0.10f),
            new Vector3(0.42f, 0.30f, 0.05f),
            ProcMaterials.Lit("vent", new Color(0.060f, 0.058f, 0.062f)));
    }

    private static void FlatRoof(Transform parent, float width, float depth, float wallHeight, System.Random rng)
    {
        var mat = RoofMat();
        ProcBuild.Box(parent, "RoofSlab",
            new Vector3(0f, wallHeight + 0.10f, 0f),
            new Vector3(width + 0.35f, 0.22f, depth + 0.35f), mat);

        float parapetH = ProcRoadGenerator.Rand(rng, 0.35f, 0.65f);
        float ph = wallHeight + 0.10f + parapetH * 0.5f;
        ProcBuild.Box(parent, "ParapetF", new Vector3(0f, ph, depth * 0.5f + 0.1f),
            new Vector3(width + 0.35f, parapetH, 0.16f), mat);
        ProcBuild.Box(parent, "ParapetB", new Vector3(0f, ph, -depth * 0.5f - 0.1f),
            new Vector3(width + 0.35f, parapetH, 0.16f), mat);
        ProcBuild.Box(parent, "ParapetL", new Vector3(-width * 0.5f - 0.1f, ph, 0f),
            new Vector3(0.16f, parapetH, depth + 0.35f), mat);
        ProcBuild.Box(parent, "ParapetR", new Vector3(width * 0.5f + 0.1f, ph, 0f),
            new Vector3(0.16f, parapetH, depth + 0.35f), mat);
    }

    private static void Damage(Transform parent, float width, float depth, float wallHeight, System.Random rng)
    {
        // Wall cracks
        int cracks = rng.Next(0, 3);
        var crackMat = ProcMaterials.Lit("wall_crack", new Color(0.055f, 0.052f, 0.052f));
        for (int i = 0; i < cracks; i++)
        {
            float x = ProcRoadGenerator.Rand(rng, -width * 0.42f, width * 0.42f);
            float y = ProcRoadGenerator.Rand(rng, 0.5f, wallHeight - 0.6f);
            ProcBuild.BoxRot(parent, "Crack",
                new Vector3(x, y, depth * 0.5f + 0.055f),
                new Vector3(0.035f, ProcRoadGenerator.Rand(rng, 0.5f, 1.6f), 0.02f),
                new Vector3(0f, 0f, ProcRoadGenerator.Rand(rng, -25f, 25f)),
                crackMat);
        }

        // Leaning scrap plank
        if (rng.NextDouble() < 0.55)
        {
            ProcBuild.BoxRot(parent, "LeaningBoard",
                new Vector3(ProcRoadGenerator.Rand(rng, -width * 0.4f, width * 0.4f), 0.8f, depth * 0.5f + 0.25f),
                new Vector3(0.22f, 1.6f, 0.05f),
                new Vector3(ProcRoadGenerator.Rand(rng, -18f, 18f), ProcRoadGenerator.Rand(rng, -25f, 25f), ProcRoadGenerator.Rand(rng, 8f, 20f)),
                Wood());
        }

        // Rubble at the base
        int rubble = rng.Next(2, 5);
        var rubbleMat = ProcMaterials.Lit("rubble", new Color(0.215f, 0.200f, 0.185f));
        for (int i = 0; i < rubble; i++)
        {
            ProcBuild.BoxRot(parent, "BaseRubble",
                new Vector3(ProcRoadGenerator.Rand(rng, -width * 0.5f, width * 0.5f), 0.1f, depth * 0.5f + ProcRoadGenerator.Rand(rng, 0.3f, 0.9f)),
                new Vector3(ProcRoadGenerator.Rand(rng, 0.2f, 0.5f), ProcRoadGenerator.Rand(rng, 0.08f, 0.22f), ProcRoadGenerator.Rand(rng, 0.2f, 0.5f)),
                new Vector3(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 0f),
                rubbleMat);
        }
    }

    // ------------------------------------------------------------- archetypes

    private static void BuildHouse(Transform root, System.Random rng, bool twoStory)
    {
        float w = ProcRoadGenerator.Rand(rng, twoStory ? 8f : 7.5f, twoStory ? 10f : 9.2f);
        float d = ProcRoadGenerator.Rand(rng, 7.5f, 9.8f);
        float h = twoStory ? ProcRoadGenerator.Rand(rng, 6.2f, 7.2f) : ProcRoadGenerator.Rand(rng, 3.4f, 4.3f);

        ProcBuild.Box(root, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), Wall(rng), true);
        ProcBuild.Box(root, "Skirt", new Vector3(0f, 0.18f, 0f), new Vector3(w + 0.12f, 0.36f, d + 0.12f), Trim());
        ProcBuild.Box(root, "Cornice", new Vector3(0f, h - 0.10f, 0f), new Vector3(w + 0.26f, 0.22f, d + 0.26f), Trim());

        if (twoStory)
        {
            ProcBuild.Box(root, "FloorBand", new Vector3(0f, h * 0.5f, 0f),
                new Vector3(w + 0.14f, 0.16f, d + 0.14f), Trim());
        }

        GableRoof(root, w, d, h, rng);
        WindowGrid(root, w, d, h, twoStory, rng);

        float doorX = ProcRoadGenerator.Rand(rng, -w * 0.28f, w * 0.28f);
        Door(root, doorX, d * 0.5f + 0.06f, 0f, rng, false);
        Damage(root, w, d, h, rng);
    }

    private static void BuildStore(Transform root, System.Random rng)
    {
        float w = ProcRoadGenerator.Rand(rng, 9.5f, 12f);
        float d = ProcRoadGenerator.Rand(rng, 8f, 10f);
        float h = ProcRoadGenerator.Rand(rng, 4.4f, 5.4f);

        ProcBuild.Box(root, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), Wall(rng), true);
        ProcBuild.Box(root, "Skirt", new Vector3(0f, 0.16f, 0f), new Vector3(w + 0.12f, 0.32f, d + 0.12f), Trim());
        FlatRoof(root, w, d, h, rng);

        // Shopfront: two wide windows + central door
        var glass = ProcMaterials.Lit("glass_store", new Color(0.065f, 0.080f, 0.095f), 0.4f);
        var frame = Trim();
        for (int s = -1; s <= 1; s += 2)
        {
            ProcBuild.Box(root, "ShopWindow",
                new Vector3(s * (w * 0.24f), 1.75f, d * 0.5f + 0.06f),
                new Vector3(w * 0.30f, 2.1f, 0.08f), glass);
            ProcBuild.Box(root, "ShopFrame",
                new Vector3(s * (w * 0.24f), 1.75f, d * 0.5f + 0.03f),
                new Vector3(w * 0.33f, 2.3f, 0.10f), frame);
        }

        Door(root, 0f, d * 0.5f + 0.06f, 0f, rng, rng.NextDouble() < 0.35);

        // Sign board above the door
        var signMat = ProcMaterials.Emissive("store_sign",
            new Color(0.15f, 0.12f, 0.08f), new Color(0.9f, 0.65f, 0.35f), 0.30f);
        ProcBuild.Box(root, "SignBoard",
            new Vector3(0f, h - 0.75f, d * 0.5f + 0.16f),
            new Vector3(w * 0.55f, 0.95f, 0.14f), signMat);

        // Awning
        if (rng.NextDouble() < 0.6)
        {
            var awningMat = ProcMaterials.Lit("awning", new Color(0.22f, 0.10f, 0.10f));
            ProcBuild.BoxRot(root, "Awning",
                new Vector3(0f, 3.25f, d * 0.5f + 0.85f),
                new Vector3(w * 0.7f, 0.08f, 1.7f),
                new Vector3(10f, 0f, 0f), awningMat);
        }

        // Upper side windows
        Window(root, new Vector3(-w * 0.3f, h - 1.3f, d * 0.5f + 0.06f), 0f, rng);
        Window(root, new Vector3(w * 0.3f, h - 1.3f, d * 0.5f + 0.06f), 0f, rng);

        Damage(root, w, d, h, rng);
    }

    private static void BuildWarehouse(Transform root, System.Random rng)
    {
        // Kept inside the town block footprint (13.4 units of usable width).
        float w = ProcRoadGenerator.Rand(rng, 10.4f, 11.6f);
        float d = ProcRoadGenerator.Rand(rng, 8.8f, 10.2f);
        float h = ProcRoadGenerator.Rand(rng, 6.4f, 7.4f);
        var wall = Wall(rng);

        ProcBuild.Box(root, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall, true);
        FlatRoof(root, w, d, h, rng);

        // Corrugated wall ribs (front)
        int ribs = 10;
        for (int i = 0; i < ribs; i++)
        {
            float x = -w * 0.5f + (w / (ribs - 1)) * i + w * 0.5f / ribs;
            ProcBuild.Box(root, "Rib",
                new Vector3(Mathf.Clamp(x, -w * 0.48f, w * 0.48f), h * 0.5f, d * 0.5f + 0.03f),
                new Vector3(0.14f, h, 0.06f), Trim());
        }

        // Roll-up loading door
        ProcBuild.Box(root, "RollDoor",
            new Vector3(0f, 2.0f, d * 0.5f + 0.06f),
            new Vector3(3.4f, 4.0f, 0.08f),
            ProcMaterials.Lit("metal_door", new Color(0.185f, 0.190f, 0.198f)));
        for (int i = 0; i < 6; i++)
        {
            ProcBuild.Box(root, "RollSlat",
                new Vector3(0f, 0.35f + i * 0.68f, d * 0.5f + 0.11f),
                new Vector3(3.3f, 0.06f, 0.03f), Trim());
        }

        // Loading dock + steps
        ProcBuild.Box(root, "Dock",
            new Vector3(0f, 0.32f, d * 0.5f + 1.2f),
            new Vector3(w * 0.45f, 0.55f, 2.4f), Concrete());
        ProcBuild.Box(root, "DockStep",
            new Vector3(0f, 0.09f, d * 0.5f + 2.4f),
            new Vector3(w * 0.30f, 0.18f, 0.9f), Concrete());

        // Roof vents
        ProcBuild.Cyl(root, "Vent",
            new Vector3(-w * 0.25f, h + 0.55f, 0f), 0.35f, 0.8f,
            ProcMaterials.Lit("metal", new Color(0.160f, 0.162f, 0.175f)));
        ProcBuild.Cyl(root, "Vent",
            new Vector3(w * 0.22f, h + 0.45f, -d * 0.2f), 0.28f, 0.6f,
            ProcMaterials.Lit("metal", new Color(0.160f, 0.162f, 0.175f)));

        // Small office window + door on the side
        Window(root, new Vector3(w * 0.5f + 0.06f, 2.6f, -d * 0.2f), 90f, rng);
        Damage(root, w, d, h, rng);
    }

    private static void BuildPublicBuilding(Transform root, System.Random rng)
    {
        float w = ProcRoadGenerator.Rand(rng, 10.5f, 12f);
        float d = ProcRoadGenerator.Rand(rng, 9f, 10.5f);
        float h = ProcRoadGenerator.Rand(rng, 8f, 9f);

        ProcBuild.Box(root, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), Wall(rng), true);
        ProcBuild.Box(root, "FloorBand", new Vector3(0f, h * 0.5f, 0f),
            new Vector3(w + 0.16f, 0.20f, d + 0.16f), Trim());
        ProcBuild.Box(root, "Cornice", new Vector3(0f, h - 0.15f, 0f),
            new Vector3(w + 0.45f, 0.35f, d + 0.45f), Trim());
        FlatRoof(root, w, d, h, rng);

        // Portico columns
        float colY = 2.1f;
        for (int c = -2; c <= 2; c += 2)
        {
            float x = c * 1.15f;
            ProcBuild.Cyl(root, "Column",
                new Vector3(x, colY, d * 0.5f + 1.6f), 0.24f, 4.2f,
                ProcMaterials.Lit("column", new Color(0.24f, 0.23f, 0.22f)));
            ProcBuild.Box(root, "ColumnBase",
                new Vector3(x, 0.15f, d * 0.5f + 1.6f),
                new Vector3(0.65f, 0.3f, 0.65f), Concrete());
        }

        // Entablature above columns
        ProcBuild.Box(root, "Entablature",
            new Vector3(0f, 4.45f, d * 0.5f + 1.6f),
            new Vector3(w * 0.62f, 0.5f, 1.9f), Trim());

        // Wide steps
        for (int i = 0; i < 3; i++)
        {
            ProcBuild.Box(root, "Steps",
                new Vector3(0f, 0.10f + i * 0.16f, d * 0.5f + 3.1f - i * 0.5f),
                new Vector3(w * 0.5f - i * 0.4f, 0.18f, 2.0f - i * 0.35f), Concrete());
        }

        Door(root, 0f, d * 0.5f + 0.06f, 0f, rng, false);

        // Tall windows, two rows
        for (int r = 0; r < 2; r++)
        {
            float y = 1.9f + r * 3.1f;
            for (int c = -2; c <= 2; c++)
            {
                if (c == 0) continue;
                Window(root, new Vector3(c * (w * 0.16f), y, d * 0.5f + 0.06f), 0f, rng);
            }
        }

        // Plaque
        var plaqueMat = ProcMaterials.Emissive("plaque",
            new Color(0.16f, 0.15f, 0.10f), new Color(0.7f, 0.6f, 0.35f), 0.18f);
        ProcBuild.Box(root, "Plaque",
            new Vector3(w * 0.30f, 5.35f, d * 0.5f + 0.10f),
            new Vector3(2.1f, 0.7f, 0.08f), plaqueMat);

        Damage(root, w, d, h, rng);
    }

    private static void BuildRuin(Transform root, System.Random rng)
    {
        float w = ProcRoadGenerator.Rand(rng, 9f, 12f);
        float d = ProcRoadGenerator.Rand(rng, 8f, 11f);
        var wall = Wall(rng);
        var crackMat = ProcMaterials.Lit("wall_crack", new Color(0.055f, 0.052f, 0.052f));

        // Floor slab
        ProcBuild.Box(root, "FloorSlab", new Vector3(0f, 0.06f, 0f),
            new Vector3(w, 0.12f, d), Concrete());

        // Back wall (mostly standing)
        float backH = ProcRoadGenerator.Rand(rng, 3.2f, 5.2f);
        ProcBuild.Box(root, "WallBack", new Vector3(0f, backH * 0.5f, -d * 0.5f),
            new Vector3(w, backH, 0.35f), wall, true);
        ProcBuild.BoxRot(root, "WallBackBreak",
            new Vector3(w * 0.28f, backH - 0.35f, -d * 0.5f),
            new Vector3(w * 0.36f, 0.7f, 0.36f), new Vector3(0f, 0f, 7f), wall);

        // Side walls, jagged
        for (int s = -1; s <= 1; s += 2)
        {
            float sh = ProcRoadGenerator.Rand(rng, 1.6f, 4.4f);
            ProcBuild.Box(root, "WallSide",
                new Vector3(s * w * 0.5f, sh * 0.5f, 0f),
                new Vector3(0.35f, sh, d * 0.9f), wall, true);

            if (rng.NextDouble() < 0.6)
            {
                ProcBuild.BoxRot(root, "WallSideChunk",
                    new Vector3(s * w * 0.5f, sh + 0.2f, d * 0.18f),
                    new Vector3(0.36f, ProcRoadGenerator.Rand(rng, 0.5f, 1.2f), d * 0.42f),
                    new Vector3(0f, 0f, ProcRoadGenerator.Rand(rng, -9f, 9f)), wall);
            }
        }

        // Front wall: two broken segments
        float segH = ProcRoadGenerator.Rand(rng, 1.2f, 3.0f);
        ProcBuild.Box(root, "WallFrontL",
            new Vector3(-w * 0.32f, segH * 0.5f, d * 0.5f),
            new Vector3(w * 0.34f, segH, 0.35f), wall, true);
        ProcBuild.Box(root, "WallFrontR",
            new Vector3(w * 0.34f, (segH * 0.7f) * 0.5f, d * 0.5f),
            new Vector3(w * 0.30f, segH * 0.7f, 0.35f), wall, true);

        // Exposed beams
        for (int i = 0; i < 3; i++)
        {
            ProcBuild.BoxRot(root, "Beam",
                new Vector3(ProcRoadGenerator.Rand(rng, -w * 0.35f, w * 0.35f), 1.2f, ProcRoadGenerator.Rand(rng, -d * 0.3f, d * 0.3f)),
                new Vector3(0.16f, 2.4f, 0.16f),
                new Vector3(ProcRoadGenerator.Rand(rng, -35f, 35f), ProcRoadGenerator.Rand(rng, 0f, 360f), ProcRoadGenerator.Rand(rng, -35f, 35f)),
                Wood());
        }

        // Rubble field
        var rubbleMat = ProcMaterials.Lit("rubble", new Color(0.215f, 0.200f, 0.185f));
        for (int i = 0; i < 9; i++)
        {
            ProcBuild.BoxRot(root, "Rubble",
                new Vector3(ProcRoadGenerator.Rand(rng, -w * 0.45f, w * 0.45f), 0.15f, ProcRoadGenerator.Rand(rng, -d * 0.45f, d * 0.45f)),
                new Vector3(ProcRoadGenerator.Rand(rng, 0.2f, 0.7f), ProcRoadGenerator.Rand(rng, 0.1f, 0.35f), ProcRoadGenerator.Rand(rng, 0.2f, 0.7f)),
                new Vector3(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 0f),
                rubbleMat);
        }

        // Charred patch + cracks
        ProcBuild.Disc(root, "Charred",
            new Vector3(0f, 0.09f, 0f), ProcRoadGenerator.Rand(rng, 1.2f, 2.4f),
            ProcMaterials.Lit("charred", new Color(0.040f, 0.038f, 0.040f)));

        for (int i = 0; i < 3; i++)
        {
            ProcBuild.BoxRot(root, "Crack",
                new Vector3(ProcRoadGenerator.Rand(rng, -w * 0.3f, w * 0.3f), ProcRoadGenerator.Rand(rng, 0.8f, 2.4f), d * 0.5f + 0.19f),
                new Vector3(0.035f, ProcRoadGenerator.Rand(rng, 0.6f, 1.5f), 0.02f),
                new Vector3(0f, 0f, ProcRoadGenerator.Rand(rng, -20f, 20f)), crackMat);
        }
    }
}
