using UnityEngine;

/// <summary>
/// Procedural wooden and chain-link property fences. Fences are broken into
/// modular sections: posts, rails, pickets and (for chain-link) a procedurally
/// generated diamond-mesh texture on alpha-blended quads.
/// </summary>
public static class ProcFenceGenerator
{
    private static Texture2D chainTexture;

    public static void Generate(Transform parent, System.Random rng)
    {
        // ---- small house, front yard (with a gate-sized gap) ----
        WoodLine(parent, new Vector3(-30.0f, 0f, -17.7f), new Vector3(-25.5f, 0f, -17.7f), rng, true, true);
        WoodLine(parent, new Vector3(-22.5f, 0f, -17.7f), new Vector3(-18.0f, 0f, -17.7f), rng, true, true);
        WoodLine(parent, new Vector3(-30.0f, 0f, -30.0f), new Vector3(-30.0f, 0f, -17.7f), rng, false, true);

        // ---- ruined building, mostly collapsed picket fence ----
        WoodLine(parent, new Vector3(17.8f, 0f, 17.9f), new Vector3(21.0f, 0f, 17.9f), rng, true, false);
        WoodLine(parent, new Vector3(24.6f, 0f, 17.9f), new Vector3(30.0f, 0f, 17.9f), rng, true, false);

        // ---- chain-link around the parking lot / escape gate block ----
        ChainLine(parent, new Vector3(-30.4f, 0f, -6.5f), new Vector3(-30.4f, 0f, 6.5f), rng);
        ChainLine(parent, new Vector3(-30.4f, 0f, 6.5f), new Vector3(-18.2f, 0f, 6.5f), rng);
        ChainLine(parent, new Vector3(-30.4f, 0f, -6.5f), new Vector3(-22.2f, 0f, -6.5f), rng);

        // ---- chain-link side yard beside the public building ----
        ChainLine(parent, new Vector3(-30.55f, 0f, 17.6f), new Vector3(-30.55f, 0f, 23.2f), rng);
    }

    // ------------------------------------------------------------ wooden fence

    private static void WoodLine(Transform parent, Vector3 start, Vector3 end,
        System.Random rng, bool pickets, bool addCollider)
    {
        Vector3 dir = end - start;
        float len = dir.magnitude;
        if (len < 0.4f) return;

        var group = new GameObject("WoodFence");
        var gt = group.transform;
        gt.SetParent(parent, false);
        gt.position = (start + end) * 0.5f;
        gt.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        var wood = ProcMaterials.Lit("fence_wood", new Color(0.210f, 0.165f, 0.115f));
        var woodDark = ProcMaterials.Lit("fence_wood_dark", new Color(0.140f, 0.110f, 0.085f));

        float half = len * 0.5f;
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.7f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float t = posts == 1 ? 0f : (float)i / (posts - 1);
            float z = Mathf.Lerp(-half, half, t);
            bool leaning = rng.NextDouble() < 0.12;
            var post = ProcBuild.Box(gt, "Post", new Vector3(0f, 0.55f, z),
                new Vector3(0.13f, 1.1f, 0.13f), woodDark);
            if (leaning)
                post.transform.localEulerAngles = new Vector3(
                    ProcRoadGenerator.Rand(rng, -9f, 9f), 0f, ProcRoadGenerator.Rand(rng, -12f, 12f));
        }

        // Two horizontal rails (one may be missing on a broken section).
        ProcBuild.Box(gt, "RailTop", new Vector3(0f, 0.88f, 0f), new Vector3(0.07f, 0.09f, len), wood);
        if (rng.NextDouble() > 0.25)
            ProcBuild.Box(gt, "RailMid", new Vector3(0f, 0.45f, 0f), new Vector3(0.07f, 0.09f, len), wood);

        if (pickets)
        {
            float step = 0.34f;
            int count = Mathf.FloorToInt(len / step);
            for (int i = 0; i <= count; i++)
            {
                float z = -half + i * step;
                // Missing or snapped pickets.
                if (rng.NextDouble() < 0.14) continue;
                float h = ProcRoadGenerator.Rand(rng, 0.82f, 1.02f);
                var p = ProcBuild.Box(gt, "Picket", new Vector3(0f, 0.42f, z),
                    new Vector3(0.05f, h, 0.16f), wood);
                if (rng.NextDouble() < 0.10)
                    p.transform.localEulerAngles = new Vector3(
                        ProcRoadGenerator.Rand(rng, -16f, 16f), 0f, ProcRoadGenerator.Rand(rng, -14f, 14f));
            }

            // Pointed tops are the strongest visual cue for a wooden picket fence.
            if (rng.NextDouble() < 0.55)
            {
                for (int i = 0; i <= count; i += 2)
                {
                    float z = -half + i * step;
                    if (rng.NextDouble() < 0.2) continue;
                    ProcBuild.BoxRot(gt, "PicketTip", new Vector3(0f, 0.95f, z),
                        new Vector3(0.05f, 0.16f, 0.16f), new Vector3(0f, 0f, 45f), wood);
                }
            }
        }

        // A couple of collapsed panels leaning against the line.
        if (rng.NextDouble() < 0.35)
        {
            ProcBuild.BoxRot(gt, "BrokenPanel",
                new Vector3(ProcRoadGenerator.Rand(rng, -0.3f, 0.3f), 0.35f, ProcRoadGenerator.Rand(rng, -half, half)),
                new Vector3(0.06f, 0.9f, 1.1f),
                new Vector3(ProcRoadGenerator.Rand(rng, 55f, 78f), 0f, ProcRoadGenerator.Rand(rng, -12f, 12f)),
                wood);
        }

        if (addCollider)
        {
            var col = group.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.62f, 0f);
            col.size = new Vector3(0.28f, 1.24f, len);
        }
    }

    // -------------------------------------------------------- chain-link fence

    private static void ChainLine(Transform parent, Vector3 start, Vector3 end, System.Random rng)
    {
        Vector3 dir = end - start;
        float len = dir.magnitude;
        if (len < 0.5f) return;

        var group = new GameObject("ChainLinkFence");
        var gt = group.transform;
        gt.SetParent(parent, false);
        gt.position = (start + end) * 0.5f;
        gt.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        var metal = ProcMaterials.Lit("fence_metal", new Color(0.235f, 0.245f, 0.258f), 0.25f);
        var meshMat = ProcMaterials.TransparentTextured("chainlink",
            new Color(0.42f, 0.45f, 0.48f), 0.62f, GetChainTexture());

        float half = len * 0.5f;
        int segments = Mathf.Max(1, Mathf.RoundToInt(len / 2.0f));
        float segLen = len / segments;

        for (int i = 0; i <= segments; i++)
        {
            float z = -half + i * segLen;
            bool lean = rng.NextDouble() < 0.09;
            var post = ProcBuild.Cyl(gt, "ChainPost", new Vector3(0f, 0.95f, z), 0.055f, 1.9f, metal);
            if (lean)
                post.transform.localEulerAngles = new Vector3(
                    ProcRoadGenerator.Rand(rng, -7f, 7f), 0f, ProcRoadGenerator.Rand(rng, -10f, 10f));
        }

        ProcBuild.Box(gt, "TopRail", new Vector3(0f, 1.92f, 0f), new Vector3(0.05f, 0.05f, len), metal);

        for (int i = 0; i < segments; i++)
        {
            if (rng.NextDouble() < 0.10) continue; // torn-out panel

            float z = -half + segLen * (i + 0.5f);
            float h = 1.82f;
            // Two back-to-back quads so the mesh is visible from both sides.
            ProcBuild.Quad(gt, "ChainMesh", new Vector3(0f, 0.95f, z),
                new Vector2(segLen - 0.12f, h), new Vector3(0f, 90f, 0f), meshMat);
            ProcBuild.Quad(gt, "ChainMesh", new Vector3(0f, 0.95f, z),
                new Vector2(segLen - 0.12f, h), new Vector3(0f, 270f, 0f), meshMat);
        }

        var col = group.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.98f, 0f);
        col.size = new Vector3(0.12f, 1.96f, len);
    }

    private static Texture2D GetChainTexture()
    {
        if (chainTexture != null) return chainTexture;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "ProcChainLink";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Two crossing diagonal wire families.
                int a = (x + y) % 16;
                int b = (x - y + size * 4) % 16;
                bool wire = a < 2 || b < 2;
                byte alpha = wire ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        chainTexture = tex;
        return tex;
    }
}
