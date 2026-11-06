using UnityEngine;

/// <summary>
/// Shared town layout constants. Roads run on 4 grid lines per axis,
/// creating 9 blocks. The centre block is the plaza (player spawn),
/// block (-16,0) is a parking lot (escape gate), block (0,16) is a park.
/// COMPACT VERSION: ~70x70 town instead of 100x100.
/// </summary>
public static class TownLayout
{
    public static readonly float[] Lines = { -24f, -8f, 8f, 24f };
    public const float RoadWidth = 7f;
    public const float SidewalkWidth = 1.8f;
    // Chase mode: the original 70 m grid is kept exactly as it was and the map
    // is extended outward with an outer ring road, so the core town art and all
    // its dressing stay valid. 54 * 2 = ~108 m across, still bounded.
    public const float TownHalfExtent = 54f;
    public const float RingRoadOffset = 48f;
    public const float BoundaryOffset = 53f;

    // ---- north field: the big open chase space ---------------------------
    // The old +-53 m square is unchanged. North of the wall line the map now
    // continues into one large open field, reached through a 26 m gate in the
    // north wall. It is deliberately empty: this is the space the player needs
    // to run backwards while watching six witches chase.
    public const float FieldStartZ = 53f;        // = BoundaryOffset, the wall line
    public const float FieldEndZ = 128f;         // far wall
    public const float FieldHalfWidth = 53f;     // same width as the town square
    public const float GateHalfWidth = 13f;      // half of the 26 m opening
    public const float GateRouteX = 6f;          // AI funnel half-width through the gate

    public static readonly Vector2[] BlockCenters =
    {
        new Vector2(-16f, -16f), new Vector2(0f, -16f), new Vector2(16f, -16f),
        new Vector2(-16f, 0f), new Vector2(0f, 0f), new Vector2(16f, 0f),
        new Vector2(-16f, 16f), new Vector2(0f, 16f), new Vector2(16f, 16f)
    };

    public const float BlockHalfFree = 4.2f; // usable half-width inside sidewalks

    /// <summary>True when a world point is north of the town wall, i.e. in the field.</summary>
    public static bool InField(Vector3 p)
    {
        return p.z > FieldStartZ;
    }

    /// <summary>
    /// True when a point would land inside the open field (or the wall band
    /// around it). Decoration generators use this to keep the running space
    /// empty: no trees, no silhouettes, no props in the middle of the field.
    /// </summary>
    public static bool InsideFieldArea(Vector3 p, float margin)
    {
        return p.z > FieldStartZ - margin
            && p.z < FieldEndZ + margin
            && Mathf.Abs(p.x) < FieldHalfWidth + margin;
    }

    /// <summary>
    /// Clamps a point into the playable area: the original town square PLUS the
    /// north field. Used by the AI so nothing walks into a wall, and so witches
    /// can follow the player out into the open.
    /// </summary>
    public static Vector3 ClampToPlayable(Vector3 p, float margin)
    {
        float town = TownHalfExtent - margin;

        // The north wall has a real opening, so an agent lining up with the gate
        // must be allowed to travel through it. Without this exception the town
        // clamp pins anything south of the wall at z = town and a follower can
        // never reach the field.
        bool inGateLane = Mathf.Abs(p.x) <= GateHalfWidth - margin;

        if (p.z <= FieldStartZ)
        {
            p.x = Mathf.Clamp(p.x, -town, town);
            float maxZ = inGateLane ? FieldEndZ - margin : town;
            p.z = Mathf.Clamp(p.z, -town, maxZ);
            return p;
        }

        float fieldX = FieldHalfWidth - margin;
        p.x = Mathf.Clamp(p.x, -fieldX, fieldX);
        float minZ = inGateLane ? -town : FieldStartZ + margin;
        p.z = Mathf.Clamp(p.z, minZ, FieldEndZ - margin);
        return p;
    }

    /// <summary>
    /// Gate funnel. When the straight line from <paramref name="from"/> to
    /// <paramref name="to"/> would cross the north wall OUTSIDE the opening,
    /// this returns a waypoint just inside the gate. Both AI systems use it so
    /// nobody grinds against the wall on the way between town and field.
    /// Returns <paramref name="to"/> unchanged when no detour is needed.
    /// </summary>
    public static Vector3 RouteViaGate(Vector3 from, Vector3 to)
    {
        bool fromField = from.z > FieldStartZ;
        bool toField = to.z > FieldStartZ;
        if (fromField == toField) return to;

        float dz = to.z - from.z;
        if (Mathf.Abs(dz) < 0.0001f) return to;

        float t = Mathf.Clamp01((FieldStartZ - from.z) / dz);
        float crossX = Mathf.Lerp(from.x, to.x, t);

        if (Mathf.Abs(crossX) <= GateHalfWidth - 2f) return to;

        float side = toField ? 2.5f : -2.5f;
        return new Vector3(Mathf.Clamp(crossX, -GateRouteX, GateRouteX),
            to.y, FieldStartZ + side);
    }
}

/// <summary>Procedural road network: asphalt, sidewalks, curbs, markings, damage.</summary>
public static class ProcRoadGenerator
{
    public static void Generate(Transform root, System.Random rng)
    {
        var container = new GameObject("Roads").transform;
        container.SetParent(root, false);

        var earthMat = ProcMaterials.Lit("earth", new Color(0.075f, 0.072f, 0.078f));
        var asphaltMat = ProcMaterials.Lit("asphalt", new Color(0.115f, 0.113f, 0.120f));
        var patchMat = ProcMaterials.Lit("asphalt_patch", new Color(0.088f, 0.086f, 0.092f));
        var sidewalkMat = ProcMaterials.Lit("sidewalk", new Color(0.265f, 0.255f, 0.238f));
        var curbMat = ProcMaterials.Lit("curb", new Color(0.315f, 0.300f, 0.280f));
        var markingMat = ProcMaterials.Lit("marking", new Color(0.40f, 0.37f, 0.23f));
        var markingWhite = ProcMaterials.Lit("marking_white", new Color(0.35f, 0.34f, 0.315f));

        // Ground plane (keeps the CharacterController supported everywhere)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(container, false);
        ground.transform.localPosition = Vector3.zero;
        // 300 x 300: the town is ~108 x 108 and the north field reaches z=128,
        // so the plane has to cover it plus a margin. Still one quad.
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = earthMat;
        ground.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float span = TownLayout.TownHalfExtent;

        // ---- Asphalt strips ----
        for (int i = 0; i < TownLayout.Lines.Length; i++)
        {
            float line = TownLayout.Lines[i];

            // East-west road
            ProcBuild.Box(container, "Road_EW_" + i,
                new Vector3(0f, 0.02f, line),
                new Vector3(span * 2f, 0.04f, TownLayout.RoadWidth),
                asphaltMat);

            // North-south road (slightly higher to avoid coplanar z-fighting at intersections)
            ProcBuild.Box(container, "Road_NS_" + i,
                new Vector3(line, 0.024f, 0f),
                new Vector3(TownLayout.RoadWidth, 0.04f, span * 2f),
                asphaltMat);

            // ---- Sidewalks + curbs on both sides ----
            float offset = TownLayout.RoadWidth * 0.5f + TownLayout.SidewalkWidth * 0.5f;
            float curbOffset = TownLayout.RoadWidth * 0.5f + 0.08f;

            for (int s = -1; s <= 1; s += 2)
            {
                ProcBuild.Box(container, "Sidewalk_EW_" + i + "_" + s,
                    new Vector3(0f, 0.055f, line + offset * s),
                    new Vector3(span * 2f, 0.11f, TownLayout.SidewalkWidth),
                    sidewalkMat);

                ProcBuild.Box(container, "Curb_EW_" + i + "_" + s,
                    new Vector3(0f, 0.075f, line + curbOffset * s),
                    new Vector3(span * 2f, 0.15f, 0.16f),
                    curbMat);

                ProcBuild.Box(container, "Sidewalk_NS_" + i + "_" + s,
                    new Vector3(line + offset * s, 0.055f, 0f),
                    new Vector3(TownLayout.SidewalkWidth, 0.11f, span * 2f),
                    sidewalkMat);

                ProcBuild.Box(container, "Curb_NS_" + i + "_" + s,
                    new Vector3(line + curbOffset * s, 0.075f, 0f),
                    new Vector3(0.16f, 0.15f, span * 2f),
                    curbMat);
            }

            // ---- Faded centre line dashes ----
            for (float d = -span + 4f; d < span - 4f; d += 6.5f)
            {
                if (rng.NextDouble() < 0.22) continue; // missing dashes = wear

                ProcBuild.Box(container, "Dash_EW_" + i,
                    new Vector3(d, 0.055f, line),
                    new Vector3(2.6f, 0.012f, 0.16f),
                    markingMat);

                if (rng.NextDouble() < 0.85)
                {
                    ProcBuild.Box(container, "Dash_NS_" + i,
                        new Vector3(line, 0.058f, d),
                        new Vector3(0.16f, 0.012f, 2.6f),
                        markingMat);
                }
            }
        }

        BuildOuterRing(container, asphaltMat, sidewalkMat, curbMat, markingMat);
        BuildNorthField(container, asphaltMat, sidewalkMat, curbMat, markingMat, rng);

        // ---- Crosswalks at the four central intersections ----
        float[] crossLines = { -12f, 12f };
        foreach (float cx in crossLines)
        {
            foreach (float cz in crossLines)
            {
                bool skip = rng.NextDouble() < 0.4;
                if (skip) continue;

                for (int k = -2; k <= 2; k++)
                {
                    ProcBuild.Box(container, "Crosswalk_EW",
                        new Vector3(cx + k * 0.9f, 0.06f, cz + TownLayout.RoadWidth * 0.5f + 1.4f),
                        new Vector3(0.45f, 0.012f, 1.6f),
                        markingWhite);
                    ProcBuild.Box(container, "Crosswalk_NS",
                        new Vector3(cx + TownLayout.RoadWidth * 0.5f + 1.4f, 0.063f, cz + k * 0.9f),
                        new Vector3(1.6f, 0.012f, 0.45f),
                        markingWhite);
                }
            }
        }

        // ---- Road damage: patches, potholes, cracks ----
        for (int i = 0; i < 26; i++)
        {
            float x = Rand(rng, -span + 4f, span - 4f);
            float z = PickRoadCoord(rng);
            ProcBuild.BoxRot(container, "Patch",
                new Vector3(x, 0.045f, z),
                new Vector3(Rand(rng, 1.2f, 4.5f), 0.012f, Rand(rng, 0.8f, 2.8f)),
                new Vector3(0f, Rand(rng, 0f, 360f), 0f),
                patchMat);
        }

        for (int i = 0; i < 14; i++)
        {
            float x = Rand(rng, -span + 6f, span - 6f);
            float z = PickRoadCoord(rng);
            ProcBuild.Disc(container, "Pothole",
                new Vector3(x, 0.048f, z),
                Rand(rng, 0.35f, 1.0f),
                ProcMaterials.Lit("pothole", new Color(0.030f, 0.030f, 0.036f)));
        }

        for (int i = 0; i < 18; i++)
        {
            float x = Rand(rng, -span + 6f, span - 6f);
            float z = PickRoadCoord(rng);
            ProcBuild.BoxRot(container, "Crack",
                new Vector3(x, 0.052f, z),
                new Vector3(Rand(rng, 1.5f, 5f), 0.01f, 0.08f),
                new Vector3(0f, Rand(rng, 0f, 360f), 0f),
                ProcMaterials.Lit("crack", new Color(0.030f, 0.030f, 0.032f)));
        }

        // ---- Kerbside debris ----
        var rubbleMat = ProcMaterials.Lit("rubble", new Color(0.19f, 0.178f, 0.165f));
        for (int i = 0; i < 22; i++)
        {
            float x = Rand(rng, -span + 6f, span - 6f);
            float z = PickRoadCoord(rng) + (rng.Next(2) == 0 ? -1f : 1f) * Rand(rng, 3.6f, 4.8f);
            ProcBuild.BoxRot(container, "RoadRubble",
                new Vector3(x, 0.12f, z),
                new Vector3(Rand(rng, 0.18f, 0.55f), Rand(rng, 0.08f, 0.28f), Rand(rng, 0.18f, 0.55f)),
                new Vector3(Rand(rng, 0f, 30f), Rand(rng, 0f, 360f), Rand(rng, 0f, 30f)),
                rubbleMat);
        }

        BuildBlockSurfaces(container, rng);
    }

    /// <summary>
    /// The outer loop that turns the compact core into a chase map: four long
    /// straights at +-48 joined at the corners, giving the player a full lap
    /// around the town plus four extra crossings with the core streets.
    /// </summary>
    private static void BuildOuterRing(Transform parent, Material asphalt, Material sidewalk,
        Material curb, Material marking)
    {
        float r = TownLayout.RingRoadOffset;
        float span = TownLayout.TownHalfExtent;
        float w = TownLayout.RoadWidth;

        for (int s = -1; s <= 1; s += 2)
        {
            ProcBuild.Box(parent, "RingRoad_EW_" + s,
                new Vector3(0f, 0.026f, r * s), new Vector3(span * 2f, 0.04f, w), asphalt);
            ProcBuild.Box(parent, "RingRoad_NS_" + s,
                new Vector3(r * s, 0.028f, 0f), new Vector3(w, 0.04f, span * 2f), asphalt);

            // Sidewalks on the town-facing side only (the outer side is the
            // boundary strip, which stays as dark earth).
            ProcBuild.Box(parent, "RingWalk_EW_" + s,
                new Vector3(0f, 0.055f, r * s - w * 0.5f * s - 0.9f * s),
                new Vector3(span * 2f, 0.11f, 1.8f), sidewalk);
            ProcBuild.Box(parent, "RingWalk_NS_" + s,
                new Vector3(r * s - w * 0.5f * s - 0.9f * s, 0.055f, 0f),
                new Vector3(1.8f, 0.11f, span * 2f), sidewalk);
        }

        // Faded centre dashes around the loop.
        for (float d = -span + 4f; d < span - 4f; d += 7f)
        {
            ProcBuild.Box(parent, "RingDash_EW",
                new Vector3(d, 0.056f, r), new Vector3(2.4f, 0.012f, 0.14f), marking);
            ProcBuild.Box(parent, "RingDash_NS",
                new Vector3(r, 0.058f, d), new Vector3(0.14f, 0.012f, 2.4f), marking);
        }

        // Outer verge slabs so the boundary strip is not one flat plane.
        var verge = ProcMaterials.Lit("verge_dirt", new Color(0.085f, 0.075f, 0.062f));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                ProcBuild.Box(parent, "OuterVerge",
                    new Vector3(sx * 50.5f, 0.020f, sz * 50.5f),
                    new Vector3(7f, 0.04f, 7f), verge);
    }

    /// <summary>
    /// THE OPEN FIELD. North of the town wall the map continues into one large,
    /// deliberately empty running surface (~106 x 75 m). No buildings, no
    /// fences, no vehicles, no debris inside it - only ground treatment and a
    /// dirt track - so the player can sprint backwards, dodge and turn while
    /// six witches chase, without hitting anything.
    ///
    /// The transition is a 26 m gate in the north wall with an asphalt apron
    /// that flows out of the ring road, so it reads as the same street system
    /// rather than a separate level.
    /// </summary>
    private static void BuildNorthField(Transform parent, Material asphalt, Material sidewalk,
        Material curb, Material marking, System.Random rng)
    {
        float z0 = TownLayout.FieldStartZ;
        float z1 = TownLayout.FieldEndZ;
        float halfW = TownLayout.FieldHalfWidth;
        float gate = TownLayout.GateHalfWidth;

        float depth = z1 - z0;
        float midZ = (z0 + z1) * 0.5f;

        // ---- 1. ground: dead grass, same palette family as the town lots ----
        var fieldMat = ProcMaterials.Lit("field_ground", new Color(0.062f, 0.072f, 0.049f));
        var fieldDry = ProcMaterials.Lit("field_ground_dry", new Color(0.082f, 0.078f, 0.048f));
        var fieldDirt = ProcMaterials.Lit("field_dirt", new Color(0.105f, 0.089f, 0.068f));

        var slab = ProcBuild.Box(parent, "NorthFieldGround",
            new Vector3(0f, 0.021f, midZ),
            new Vector3(halfW * 2f, 0.042f, depth), fieldMat);
        slab.GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        // ---- 2. gate apron: the ring road visibly continues through the gate --
        // Runs from just inside the ring road to a few metres into the field.
        ProcBuild.Box(parent, "GateApron",
            new Vector3(0f, 0.045f, (TownLayout.RingRoadOffset + z0 + 4f) * 0.5f),
            new Vector3(gate * 2f, 0.012f,
                (z0 + 4f) - TownLayout.RingRoadOffset), asphalt);

        // Short kerbs flanking the apron so it reads as a street mouth. They stop
        // short of the wall line so nothing pokes through the gate pillars.
        float walkFrom = TownLayout.RingRoadOffset;
        float walkTo = z0 - 1.2f;
        float walkLen = walkTo - walkFrom;
        float walkMid = (walkFrom + walkTo) * 0.5f;

        for (int s = -1; s <= 1; s += 2)
        {
            ProcBuild.Box(parent, "GateKerb",
                new Vector3((gate + 0.35f) * s, 0.075f, walkMid),
                new Vector3(0.3f, 0.15f, walkLen), curb);
            ProcBuild.Box(parent, "GateWalk",
                new Vector3((gate + 1.35f) * s, 0.055f, walkMid),
                new Vector3(1.7f, 0.11f, walkLen), sidewalk);
        }

        // ---- 3. a worn dirt track down the middle: a visual guide line ------
        ProcBuild.Box(parent, "FieldTrack",
            new Vector3(0f, 0.044f, midZ + 2f),
            new Vector3(9f, 0.012f, depth - 10f), fieldDirt);
        ProcBuild.Box(parent, "FieldTrackBranch",
            new Vector3(0f, 0.045f, z0 + 18f),
            new Vector3(halfW * 1.55f, 0.012f, 4.5f), fieldDirt);

        // ---- 4. tonal patches: no colliders, purely to break the flat quad ---
        for (int i = 0; i < 16; i++)
        {
            float x = Rand(rng, -halfW + 4f, halfW - 4f);
            float z = Rand(rng, z0 + 5f, z1 - 5f);
            // Keep the central running lane clear of even the flat patches.
            if (Mathf.Abs(x) < 7f && i % 2 == 0) x += (x < 0f ? -8f : 8f);

            ProcBuild.BoxRot(parent, "FieldPatch",
                new Vector3(x, 0.043f, z),
                new Vector3(Rand(rng, 2.5f, 7f), 0.01f, Rand(rng, 2.5f, 7f)),
                new Vector3(0f, Rand(rng, 0f, 360f), 0f),
                i % 3 == 0 ? fieldDry : (i % 3 == 1 ? fieldDirt : fieldMat));
        }

        // ---- 5. faded paint on the apron: it is still a street --------------
        for (float d = TownLayout.RingRoadOffset + 1f; d < z0 + 6f; d += 2.6f)
        {
            ProcBuild.Box(parent, "GateDash",
                new Vector3(0f, 0.052f, d), new Vector3(0.16f, 0.012f, 1.3f), marking);
        }

        // ---- 6. outer edge dressing ONLY (never inside the running space) ---
        // Low rubble and dead grass hug the walls, well outside the play area,
        // so the field still belongs to the village without becoming an obstacle
        // course.
        var rubble = ProcMaterials.Lit("field_rubble", new Color(0.15f, 0.142f, 0.132f));
        for (int i = 0; i < 14; i++)
        {
            int side = (i % 2 == 0) ? -1 : 1;
            float x = side * Rand(rng, halfW - 1.6f, halfW - 0.4f);
            float z = Rand(rng, z0 + 6f, z1 - 6f);
            ProcBuild.BoxRot(parent, "FieldEdgeRubble",
                new Vector3(x, 0.10f, z),
                new Vector3(Rand(rng, 0.25f, 0.6f), Rand(rng, 0.1f, 0.3f), Rand(rng, 0.25f, 0.6f)),
                new Vector3(0f, Rand(rng, 0f, 360f), 0f), rubble);
        }
        for (int i = 0; i < 10; i++)
        {
            float z = (i < 5) ? Rand(rng, z0 + 3f, z0 + 8f) : Rand(rng, z1 - 6f, z1 - 2f);
            ProcBuild.BoxRot(parent, "FieldEdgeGrass",
                new Vector3(Rand(rng, -halfW + 2f, halfW - 2f), 0.046f, z),
                new Vector3(Rand(rng, 2f, 5f), 0.014f, Rand(rng, 1.2f, 3f)),
                new Vector3(0f, Rand(rng, 0f, 360f), 0f), fieldDry);
        }
    }

    /// <summary>
    /// Each grid block gets its own ground treatment instead of a single
    /// uniform plane: dead grass yards, dirt lots, an asphalt parking lot, a
    /// paved plaza and a park lawn. This is what stops the town from reading
    /// as "grey plane with cubes".
    /// </summary>
    private static void BuildBlockSurfaces(Transform parent, System.Random rng)
    {
        var grass = ProcMaterials.Lit("lot_grass", new Color(0.055f, 0.078f, 0.042f));
        var grassDead = ProcMaterials.Lit("lot_grass_dead", new Color(0.086f, 0.082f, 0.045f));
        var dirt = ProcMaterials.Lit("lot_dirt", new Color(0.115f, 0.095f, 0.070f));
        var asphalt = ProcMaterials.Lit("lot_asphalt", new Color(0.100f, 0.098f, 0.105f));
        var paving = ProcMaterials.Lit("lot_paving", new Color(0.240f, 0.228f, 0.212f));
        var concrete = ProcMaterials.Lit("lot_concrete", new Color(0.190f, 0.185f, 0.175f));

        float half = (24f - TownLayout.RoadWidth) * 0.5f; // 8.5

        Material[] mats =
        {
            grassDead, grassDead, dirt,      // south row: yards / store front / warehouse yard
            asphalt,   paving,    grass,     // middle row: parking lot / plaza / house yard
            concrete,  grass,     dirt       // north row: public building / park / ruin
        };

        for (int i = 0; i < TownLayout.BlockCenters.Length; i++)
        {
            var c = TownLayout.BlockCenters[i];
            var slab = ProcBuild.Box(parent, "BlockSurface_" + i,
                new Vector3(c.x, 0.022f, c.y),
                new Vector3(half * 2f, 0.045f, half * 2f), mats[i]);
            slab.GetComponent<MeshRenderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            // Sparse tonal patches so large lawns don't look like one flat quad.
            if (i == 6 || i == 4 || i == 7 || i == 8) continue;
            for (int p = 0; p < 5; p++)
            {
                var patchMat = (i == 3) ? asphalt : (i == 4 ? paving : dirt);
                ProcBuild.BoxRot(parent, "GroundPatch",
                    new Vector3(c.x + Rand(rng, -half + 1f, half - 1f), 0.046f,
                        c.y + Rand(rng, -half + 1f, half - 1f)),
                    new Vector3(Rand(rng, 1.4f, 4.2f), 0.012f, Rand(rng, 1.2f, 3.6f)),
                    new Vector3(0f, Rand(rng, 0f, 360f), 0f),
                    p % 2 == 0 ? dirt : grassDead);
            }
        }

        // ---- parking lot: faded bay markings ----
        var lot = TownLayout.BlockCenters[3];
        for (int i = -3; i <= 3; i++)
        {
            ProcBuild.Box(parent, "ParkingLine",
                new Vector3(lot.x + i * 2.1f, 0.05f, lot.y - 5.2f),
                new Vector3(0.09f, 0.012f, 3.4f), ProcMaterials.Lit("parking_line",
                    new Color(0.32f, 0.31f, 0.26f)));
        }

        // ---- park: dirt footpaths crossing the lawn ----
        var park = TownLayout.BlockCenters[7];
        ProcBuild.Box(parent, "ParkPath_NS",
            new Vector3(park.x, 0.048f, park.y), new Vector3(1.5f, 0.02f, half * 2f), dirt);
        ProcBuild.Box(parent, "ParkPath_EW",
            new Vector3(park.x, 0.049f, park.y), new Vector3(half * 2f, 0.02f, 1.4f), dirt);
    }

    private static float PickRoadCoord(System.Random rng)
    {
        return TownLayout.Lines[rng.Next(TownLayout.Lines.Length)];
    }

    public static float Rand(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
