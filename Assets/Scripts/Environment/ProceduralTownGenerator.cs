using UnityEngine;

/// <summary>
/// Orchestrates the whole procedural town. Geometry is generated under one
/// root and then statically batched; lights, wires and particles live under a
/// separate systems root so batching stays clean.
/// </summary>
public static class ProceduralTownGenerator
{
    private struct Placement
    {
        public Vector3 position;
        public float rotation;
        public BuildingKind kind;

        public Placement(Vector3 p, float r, BuildingKind k)
        {
            position = p;
            rotation = r;
            kind = k;
        }
    }

    private static readonly Placement[] Buildings =
    {
        // Main blocks - one anchor building per block, all facing a street.
        // COMPACT: positions adjusted for 70x70 town (was 100x100)
        new Placement(new Vector3(-16f, 0f, -16.8f), 0f, BuildingKind.TwoStoryHouse),
        new Placement(new Vector3(0f, 0f, -16.8f), 0f, BuildingKind.Store),
        new Placement(new Vector3(16f, 0f, -16.8f), 0f, BuildingKind.Warehouse),
        new Placement(new Vector3(-16f, 0f, 16.8f), 180f, BuildingKind.PublicBuilding),
        new Placement(new Vector3(16f, 0f, 16.8f), 180f, BuildingKind.Ruin),
        new Placement(new Vector3(17f, 0f, 0f), 270f, BuildingKind.SmallHouse),

        // Slightly outside the grid to extend the town silhouette.
        new Placement(new Vector3(-30f, 0f, -12f), 90f, BuildingKind.SmallHouse),
        new Placement(new Vector3(30f, 0f, 12f), 270f, BuildingKind.TwoStoryHouse),
        new Placement(new Vector3(-30f, 0f, 12f), 90f, BuildingKind.Store),

        // ---- outer district (chase map extension) -------------------------
        // These sit in the band between the original core edge (~30 m) and the
        // ring road (48 m). They are landmarks for the outer loop and keep the
        // new space from being an empty ring.
        new Placement(new Vector3(38f, 0f, 0f), 270f, BuildingKind.Warehouse),
        new Placement(new Vector3(-38f, 0f, 0f), 90f, BuildingKind.Ruin),
        new Placement(new Vector3(0f, 0f, 38f), 180f, BuildingKind.Store),
        new Placement(new Vector3(0f, 0f, -38f), 0f, BuildingKind.TwoStoryHouse),
        new Placement(new Vector3(38f, 0f, 38f), 270f, BuildingKind.SmallHouse),
        new Placement(new Vector3(-38f, 0f, -38f), 90f, BuildingKind.PublicBuilding)
    };

    public static void Generate(int seed)
    {
        var rng = new System.Random(seed);
        ProcMaterials.ResetCache();

        var root = new GameObject("ProceduralTown");
        var geo = new GameObject("TownGeometry").transform;
        geo.SetParent(root.transform, false);
        var systems = new GameObject("TownSystems").transform;
        systems.SetParent(root.transform, false);

        var t0 = Time.realtimeSinceStartup;

        ProcRoadGenerator.Generate(geo, rng);
        BuildBuildings(geo, rng);
        BuildBoundary(geo);
        ProcFenceGenerator.Generate(geo, rng);
        ProcUtilityPoleGenerator.Generate(geo, systems, rng);
        ProcVehicleGenerator.GenerateAll(geo, rng);
        ProcVegetationGenerator.Generate(geo, rng);
        ProcPropGenerator.Generate(geo, rng);
        ProcHorrorDetailGenerator.Generate(geo, systems, rng);
        ProcLightingSetup.Generate(geo, systems, rng);
        ProcAtmosphereSetup.Generate(systems, rng);

        int meshCount = geo.GetComponentsInChildren<MeshRenderer>().Length;

        // Combine all static environment meshes. This collapses the town into
        // a handful of batches per shared material instead of thousands of
        // individual draw calls.
        StaticBatchingUtility.Combine(geo.gameObject);

        float elapsed = Time.realtimeSinceStartup - t0;
        Debug.Log($"[PROC] Procedural town generated: {meshCount} mesh renderers, " +
                  $"{Object.FindObjectsOfType<Light>().Length} lights, {elapsed:0.00}s (seed {seed}).");
    }

    private static void BuildBuildings(Transform parent, System.Random rng)
    {
        foreach (var p in Buildings)
        {
            ProcBuildingGenerator.Generate(parent, p.position, p.rotation, p.kind, rng);
        }

        BuildDistantSilhouettes(parent, rng);
    }

    /// <summary>
    /// Solid perimeter around the playable map. Chase mode needs a hard edge:
    /// the player must not be able to run out of the town and keep going across
    /// the ground plane.
    ///
    /// The north wall now has a 26 m gate in the middle, and the perimeter
    /// continues around the open field beyond it, so the field is part of the
    /// playable area instead of a hole in the boundary.
    /// </summary>
    private static void BuildBoundary(Transform parent)
    {
        var concrete = ProcMaterials.Lit("boundary_wall", new Color(0.115f, 0.112f, 0.108f));
        var cap = ProcMaterials.Lit("boundary_cap", new Color(0.150f, 0.146f, 0.140f));

        float b = TownLayout.BoundaryOffset;
        float len = b * 2f + 2f;
        float h = 2.7f;

        var root = new GameObject("TownBoundary").transform;
        root.SetParent(parent, false);

        // Colliders ON: this is the hard playable boundary.
        ProcBuild.Box(root, "Wall_S", new Vector3(0f, h * 0.5f, -b), new Vector3(len, h, 1.1f), concrete, true);
        ProcBuild.Box(root, "Wall_E", new Vector3(b, h * 0.5f, 0f), new Vector3(1.1f, h, len), concrete, true);
        ProcBuild.Box(root, "Wall_W", new Vector3(-b, h * 0.5f, 0f), new Vector3(1.1f, h, len), concrete, true);

        ProcBuild.Box(root, "Cap_S", new Vector3(0f, h + 0.08f, -b), new Vector3(len, 0.16f, 1.5f), cap);
        ProcBuild.Box(root, "Cap_E", new Vector3(b, h + 0.08f, 0f), new Vector3(1.5f, 0.16f, len), cap);
        ProcBuild.Box(root, "Cap_W", new Vector3(-b, h + 0.08f, 0f), new Vector3(1.5f, 0.16f, len), cap);

        // ---- north wall: split around the gate into the open field ----------
        float gate = TownLayout.GateHalfWidth;
        float segLen = b - gate;                  // 53 - 13 = 40
        float segCenter = gate + segLen * 0.5f;   // 33

        for (int s = -1; s <= 1; s += 2)
        {
            ProcBuild.Box(root, "Wall_N", new Vector3(segCenter * s, h * 0.5f, b),
                new Vector3(segLen, h, 1.1f), concrete, true);
            ProcBuild.Box(root, "Cap_N", new Vector3(segCenter * s, h + 0.08f, b),
                new Vector3(segLen, 0.16f, 1.5f), cap);

            // Gate pillar: marks the opening and matches the wall collider.
            ProcBuild.Box(root, "GatePillar", new Vector3(gate * s, (h + 0.5f) * 0.5f, b),
                new Vector3(1.1f, h + 0.5f, 1.6f), concrete, true);
            ProcBuild.Box(root, "GatePillarCap", new Vector3(gate * s, h + 0.58f, b),
                new Vector3(1.5f, 0.16f, 2.0f), cap);
        }

        // ---- perimeter around the north field -------------------------------
        float fz0 = TownLayout.FieldStartZ;
        float fz1 = TownLayout.FieldEndZ;
        float fw = TownLayout.FieldHalfWidth;
        float fieldDepth = fz1 - fz0;
        float fieldMidZ = (fz0 + fz1) * 0.5f;

        for (int s = -1; s <= 1; s += 2)
        {
            ProcBuild.Box(root, "FieldWall_Side", new Vector3(fw * s, h * 0.5f, fieldMidZ),
                new Vector3(1.1f, h, fieldDepth), concrete, true);
            ProcBuild.Box(root, "FieldCap_Side", new Vector3(fw * s, h + 0.08f, fieldMidZ),
                new Vector3(1.5f, 0.16f, fieldDepth), cap);
        }

        ProcBuild.Box(root, "FieldWall_North", new Vector3(0f, h * 0.5f, fz1),
            new Vector3(fw * 2f + 2f, h, 1.1f), concrete, true);
        ProcBuild.Box(root, "FieldCap_North", new Vector3(0f, h + 0.08f, fz1),
            new Vector3(fw * 2f + 2f, 0.16f, 1.5f), cap);
    }

    private static void BuildDistantSilhouettes(Transform parent, System.Random rng)
    {
        var silhouette = ProcMaterials.Lit("silhouette", new Color(0.045f, 0.052f, 0.068f));
        var silhouetteDark = ProcMaterials.Lit("silhouette_dark", new Color(0.030f, 0.034f, 0.046f));
        var dimWindow = ProcMaterials.Emissive("distant_window",
            new Color(0.10f, 0.08f, 0.05f), new Color(1f, 0.65f, 0.32f), 0.18f);

        var root = new GameObject("DistantSilhouettes").transform;
        root.SetParent(parent, false);

        int count = 10;
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f + ProcRoadGenerator.Rand(rng, -0.08f, 0.08f);
            float radius = ProcRoadGenerator.Rand(rng, 66f, 82f);
            Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            // Never drop a silhouette inside the open field: it must stay clear.
            if (TownLayout.InsideFieldArea(p, 4f))
                p.z = TownLayout.FieldEndZ + ProcRoadGenerator.Rand(rng, 6f, 26f);

            float w = ProcRoadGenerator.Rand(rng, 6f, 12f);
            float d = ProcRoadGenerator.Rand(rng, 6f, 11f);
            float h = ProcRoadGenerator.Rand(rng, 5f, 11f);

            var b = new GameObject("DistantBuilding");
            var t = b.transform;
            t.SetParent(root, false);
            t.position = p;
            t.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);

            ProcBuild.Box(t, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), silhouette, false, false);

            // A slightly oversized roof slab reads as a shadowed silhouette.
            ProcBuild.Box(t, "Roof", new Vector3(0f, h + 0.22f, 0f), new Vector3(w + 0.8f, 0.45f, d + 0.8f),
                silhouetteDark, false, false);

            if (rng.NextDouble() < 0.45)
            {
                int windows = rng.Next(1, 3);
                for (int wi = 0; wi < windows; wi++)
                {
                    ProcBuild.Box(t, "Window",
                        new Vector3(ProcRoadGenerator.Rand(rng, -w * 0.3f, w * 0.3f),
                            ProcRoadGenerator.Rand(rng, h * 0.3f, h * 0.8f), d * 0.5f + 0.03f),
                        new Vector3(0.9f, 1.2f, 0.04f), dimWindow, false, false);
                }
            }

            // Occasional chimney / roof vent for silhouette variety.
            if (rng.NextDouble() < 0.5)
            {
                ProcBuild.Box(t, "Chimney",
                    new Vector3(ProcRoadGenerator.Rand(rng, -w * 0.25f, w * 0.25f), h + 0.9f, 0f),
                    new Vector3(0.7f, 1.8f, 0.7f), silhouetteDark, false, false);
            }
        }
    }
}
