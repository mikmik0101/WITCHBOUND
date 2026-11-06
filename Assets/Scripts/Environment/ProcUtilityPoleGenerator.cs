using UnityEngine;

/// <summary>
/// Utility poles with crossbars, insulators and sagging overhead wires.
/// Wires use LineRenderers (very cheap) and are kept out of static batching.
/// </summary>
public static class ProcUtilityPoleGenerator
{
    private static Material wireMaterial;

    public static void Generate(Transform geometryParent, Transform systemsParent, System.Random rng)
    {
        wireMaterial = ProcMaterials.Line("wire", new Color(0.045f, 0.045f, 0.055f));

        // East-west line along the southern road sidewalk.
        var lineA = new[]
        {
            new Vector3(-42f, 0f, -16.4f),
            new Vector3(-28f, 0f, -16.4f),
            new Vector3(-14f, 0f, -16.4f),
            new Vector3(0f, 0f, -16.4f),
            new Vector3(14f, 0f, -16.4f),
            new Vector3(28f, 0f, -16.4f),
            new Vector3(42f, 0f, -16.4f)
        };
        BuildLine(geometryParent, systemsParent, lineA, 90f, rng, 2);

        // North-south line along the western road sidewalk.
        var lineB = new[]
        {
            new Vector3(-16.4f, 0f, -42f),
            new Vector3(-16.4f, 0f, -28f),
            new Vector3(-16.4f, 0f, -14f),
            new Vector3(-16.4f, 0f, 0f),
            new Vector3(-16.4f, 0f, 14f),
            new Vector3(-16.4f, 0f, 28f),
            new Vector3(-16.4f, 0f, 42f)
        };
        BuildLine(geometryParent, systemsParent, lineB, 0f, rng, 5);
    }

    private static void BuildLine(Transform geo, Transform systems, Vector3[] positions,
        float poleRotY, System.Random rng, int brokenIndex)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            bool broken = i == brokenIndex;
            CreatePole(geo, positions[i], poleRotY, broken, rng, i % 3 == 0);
        }

        // Perpendicular direction of the crossbar (used to offset the wires).
        Vector3 perp = Quaternion.Euler(0f, poleRotY, 0f) * Vector3.right;
        float[] offsets = { -1.05f, -0.35f, 0.35f, 1.05f };

        for (int i = 0; i < positions.Length - 1; i++)
        {
            Vector3 a = positions[i];
            Vector3 b = positions[i + 1];
            // Broken poles sag lower.
            float topA = i == brokenIndex ? 6.6f : 7.6f;
            float topB = i + 1 == brokenIndex ? 6.6f : 7.6f;
            float sag = ProcRoadGenerator.Rand(rng, 0.55f, 1.05f);

            for (int w = 0; w < offsets.Length; w++)
            {
                if (i == brokenIndex && w == 2) continue;
                CreateWire(systems, a + Vector3.up * topA + perp * offsets[w],
                    b + Vector3.up * topB + perp * offsets[w], sag);
            }
        }
    }

    private static void CreatePole(Transform parent, Vector3 position, float rotY,
        bool broken, System.Random rng, bool transformer)
    {
        var root = new GameObject(broken ? "UtilityPole_Broken" : "UtilityPole");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = position;
        t.rotation = Quaternion.Euler(0f, rotY, 0f);

        if (broken)
        {
            t.rotation = Quaternion.Euler(
                ProcRoadGenerator.Rand(rng, -10f, 10f),
                rotY,
                ProcRoadGenerator.Rand(rng, 7f, 15f));
        }

        var wood = ProcMaterials.Lit("pole_wood", new Color(0.155f, 0.130f, 0.105f));
        var metal = ProcMaterials.Lit("pole_metal", new Color(0.180f, 0.185f, 0.198f), 0.2f);
        var ceramic = ProcMaterials.Lit("insulator", new Color(0.380f, 0.378f, 0.365f), 0.35f);

        float height = broken ? 7.2f : 8.0f;
        ProcBuild.Cyl(t, "Pole", new Vector3(0f, height * 0.5f, 0f), 0.17f, height, wood);

        // Weathering bands.
        ProcBuild.Cyl(t, "Band", new Vector3(0f, 2.2f, 0f), 0.185f, 0.12f, metal);
        ProcBuild.Cyl(t, "Band", new Vector3(0f, 5.1f, 0f), 0.185f, 0.10f, metal);

        if (!broken)
        {
            ProcBuild.Box(t, "Crossbar", new Vector3(0f, 7.42f, 0f), new Vector3(2.8f, 0.16f, 0.16f), wood);
            ProcBuild.Box(t, "Brace", new Vector3(0f, 7.05f, 0.10f), new Vector3(1.9f, 0.07f, 0.07f), wood);

            float[] ins = { -1.18f, -0.6f, 0.6f, 1.18f };
            foreach (float x in ins)
            {
                ProcBuild.Cyl(t, "Insulator", new Vector3(x, 7.63f, 0f), 0.075f, 0.24f, ceramic);
            }

            if (transformer)
            {
                ProcBuild.Cyl(t, "Transformer", new Vector3(0.62f, 6.15f, 0f), 0.30f, 0.95f, metal);
                ProcBuild.Cyl(t, "TransformerCap", new Vector3(0.62f, 6.68f, 0f), 0.33f, 0.08f, metal);
            }
        }

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, height * 0.5f, 0f);
        col.radius = 0.22f;
        col.height = height;
    }

    private static void CreateWire(Transform systems, Vector3 a, Vector3 b, float sag)
    {
        var go = new GameObject("Wire");
        go.transform.SetParent(systems, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = wireMaterial;
        lr.startWidth = 0.035f;
        lr.endWidth = 0.035f;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;

        const int points = 9;
        lr.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float t = i / (float)(points - 1);
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y -= Mathf.Sin(t * Mathf.PI) * sag;
            lr.SetPosition(i, p);
        }
    }
}
