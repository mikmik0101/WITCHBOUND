using UnityEngine;

/// <summary>
/// Sparse hand-placed-feeling abandoned props: benches, dumpsters, crates,
/// barrels, trash bags, shopping carts, bicycles, cones and street furniture.
/// None of these are randomly sprinkled - each cluster tells a small story.
/// </summary>
public static class ProcPropGenerator
{
    private static Material woodMat;
    private static Material woodDarkMat;
    private static Material metalMat;
    private static Material metalDarkMat;
    private static Material plasticMat;
    private static Material bagMat;
    private static Material paperMat;
    private static Material rustMat;
    private static Material orangeMat;
    private static Material redMat;
    private static Material whiteMat;

    public static void Generate(Transform parent, System.Random rng)
    {
        woodMat = ProcMaterials.Lit("prop_wood", new Color(0.220f, 0.172f, 0.122f));
        woodDarkMat = ProcMaterials.Lit("prop_wood_dark", new Color(0.140f, 0.110f, 0.082f));
        metalMat = ProcMaterials.Lit("prop_metal", new Color(0.200f, 0.208f, 0.218f), 0.25f);
        metalDarkMat = ProcMaterials.Lit("prop_metal_dark", new Color(0.105f, 0.108f, 0.118f), 0.15f);
        plasticMat = ProcMaterials.Lit("prop_plastic", new Color(0.095f, 0.118f, 0.110f), 0.3f);
        bagMat = ProcMaterials.Lit("trash_bag", new Color(0.045f, 0.045f, 0.050f), 0.15f);
        paperMat = ProcMaterials.Lit("paper", new Color(0.400f, 0.388f, 0.338f));
        rustMat = ProcMaterials.Lit("rust", new Color(0.290f, 0.150f, 0.085f));
        orangeMat = ProcMaterials.Lit("safety_orange", new Color(0.45f, 0.17f, 0.035f), 0.2f);
        redMat = ProcMaterials.Lit("hydrant_red", new Color(0.30f, 0.06f, 0.05f), 0.2f);
        whiteMat = ProcMaterials.Lit("paint_white", new Color(0.38f, 0.37f, 0.34f));

        // ---- plaza (player spawn) ----
        Bench(parent, new Vector3(-4.2f, 0f, 3.4f), 0f, false);
        Bench(parent, new Vector3(4.2f, 0f, 3.4f), 0f, false);
        Bench(parent, new Vector3(-4.6f, 0f, -4.2f), 22f, true);
        TrashCan(parent, new Vector3(6.1f, 0f, 2.9f), 0f, false);
        Hydrant(parent, new Vector3(7.0f, 0f, -3.0f));
        Bicycle(parent, new Vector3(4.6f, 0f, 1.4f), 64f, rng);
        Papers(parent, new Vector3(-6.0f, 0f, 0.8f), rng);

        // ---- park ----
        Bench(parent, new Vector3(3.6f, 0f, 23.4f), 184f, false);
        Bench(parent, new Vector3(-4.4f, 0f, 26.4f), 6f, false);
        TrashCan(parent, new Vector3(6.4f, 0f, 27.2f), 14f, true);
        FallenBranch(parent, new Vector3(2.0f, 0f, 22.2f), rng);

        // ---- store frontage ----
        Dumpster(parent, new Vector3(5.6f, 0f, -20.5f), 12f);
        TrashBag(parent, new Vector3(4.4f, 0f, -20.1f), rng);
        TrashBag(parent, new Vector3(3.9f, 0f, -20.6f), rng);
        ShoppingCart(parent, new Vector3(-3.4f, 0f, -20.2f), 108f);
        Bicycle(parent, new Vector3(-4.6f, 0f, -19.8f), 78f, rng);
        BrokenSign(parent, new Vector3(5.8f, 0f, -19.0f), 18f, rng);

        // ---- warehouse yard ----
        CrateStack(parent, new Vector3(29.2f, 0f, -19.4f), rng, 2);
        CrateStack(parent, new Vector3(30.0f, 0f, -20.6f), rng, 1);
        Barrel(parent, new Vector3(19.1f, 0f, -19.6f), false);
        Barrel(parent, new Vector3(19.9f, 0f, -20.4f), true);
        Pallet(parent, new Vector3(21.2f, 0f, -19.5f), 8f);
        Dumpster(parent, new Vector3(18.6f, 0f, -19.1f), 96f);

        // ---- public building frontage ----
        BrokenSign(parent, new Vector3(-18.6f, 0f, 19.3f), -24f, rng);
        TrashBag(parent, new Vector3(-29.0f, 0f, 19.2f), rng);
        TrashBag(parent, new Vector3(-28.6f, 0f, 18.6f), rng);
        Papers(parent, new Vector3(-22.0f, 0f, 19.0f), rng);
        Bench(parent, new Vector3(-20.6f, 0f, 19.4f), 176f, true);

        // ---- ruin ----
        Barrel(parent, new Vector3(19.0f, 0f, 18.8f), true);
        Barrel(parent, new Vector3(29.2f, 0f, 18.9f), false);
        CrateStack(parent, new Vector3(28.0f, 0f, 19.3f), rng, 1);
        RubblePile(parent, new Vector3(23.4f, 0f, 18.8f), rng, 7);
        FallenBranch(parent, new Vector3(27.2f, 0f, 19.6f), rng);

        // ---- parking lot / escape-gate block ----
        Dumpster(parent, new Vector3(-29.6f, 0f, -5.9f), 4f);
        TrashBag(parent, new Vector3(-18.7f, 0f, 5.2f), rng);
        RubblePile(parent, new Vector3(-18.4f, 0f, -5.4f), rng, 5);

        // ---- generator work zone (generator is at 10,0,0) ----
        TrafficCone(parent, new Vector3(8.7f, 0f, -1.7f));
        TrafficCone(parent, new Vector3(11.5f, 0f, 1.5f));
        TrafficCone(parent, new Vector3(9.0f, 0f, 1.9f));
        GraffitiWall(parent, new Vector3(9.8f, 0f, -2.1f), 12f);

        // ---- scattered street furniture ----
        TrashCan(parent, new Vector3(13.0f, 0f, 2.4f), 30f, true);
        Papers(parent, new Vector3(13.5f, 0f, -6.2f), rng);
        BrokenSign(parent, new Vector3(-13.4f, 0f, 13.5f), 72f, rng);
        TrafficCone(parent, new Vector3(-9.0f, 0f, -9.2f));
        RubblePile(parent, new Vector3(16.0f, 0f, -13.5f), rng, 4);
        FallenBranch(parent, new Vector3(-30.0f, 0f, -8.0f), rng);
        Hydrant(parent, new Vector3(-7.0f, 0f, 14.0f));
        TrashBag(parent, new Vector3(-6.2f, 0f, -14.0f), rng);

        // ---- a couple of distant details to break up empty blocks ----
        Bench(parent, new Vector3(-30.0f, 0f, -1.0f), 90f, true);
        CrateStack(parent, new Vector3(-18.3f, 0f, 1.0f), rng, 1);
    }

    // ----------------------------------------------------------------- helpers

    private static void Bench(Transform parent, Vector3 pos, float rotY, bool broken)
    {
        var root = new GameObject(broken ? "Bench_Broken" : "Bench");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, 0f);

        ProcBuild.Box(t, "Seat", new Vector3(0f, 0.48f, 0f), new Vector3(1.9f, 0.10f, 0.56f), woodMat);
        for (int i = 0; i < 3; i++)
        {
            ProcBuild.Box(t, "Slat", new Vector3(0f, 0.52f, -0.18f + i * 0.18f),
                new Vector3(1.86f, 0.04f, 0.13f), woodDarkMat);
        }

        var back = ProcBuild.BoxRot(t, "Back", new Vector3(0f, 0.80f, -0.28f),
            new Vector3(1.9f, 0.5f, 0.07f),
            new Vector3(broken ? 18f : 4f, 0f, broken ? 7f : 0f), woodMat);
        back.name = "Back";

        for (int s = -1; s <= 1; s += 2)
        {
            if (broken && s > 0) continue;
            ProcBuild.Box(t, "Leg", new Vector3(s * 0.78f, 0.24f, 0f),
                new Vector3(0.08f, 0.48f, 0.48f), metalDarkMat);
        }

        if (!broken)
        {
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1.95f, 1.0f, 0.62f);
        }
    }

    private static void TrashCan(Transform parent, Vector3 pos, float rotY, bool tipped)
    {
        var root = new GameObject("TrashCan");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos + (tipped ? new Vector3(0f, 0.29f, 0f) : Vector3.zero);
        t.rotation = Quaternion.Euler(0f, rotY, tipped ? 78f : 0f);

        ProcBuild.Cyl(t, "Body", new Vector3(0f, 0.42f, 0f), 0.29f, 0.84f, metalDarkMat);
        ProcBuild.Cyl(t, "Rim", new Vector3(0f, 0.84f, 0f), 0.31f, 0.06f, metalMat);
        if (!tipped)
            ProcBuild.Cyl(t, "Lid", new Vector3(0.04f, 0.88f, 0.04f), 0.29f, 0.05f, metalMat);
        else
            ProcBuild.Cyl(t, "Lid", new Vector3(0.55f, 0.10f, 0.22f), 0.29f, 0.05f, metalMat);
    }

    private static void TrashBag(Transform parent, Vector3 pos, System.Random rng)
    {
        var root = new GameObject("TrashBag");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 0f);

        float s = ProcRoadGenerator.Rand(rng, 0.4f, 0.62f);
        ProcBuild.Sphere(t, "Bag", new Vector3(0f, 0.20f, 0f), new Vector3(s, s * 0.75f, s * 0.9f), bagMat);
        ProcBuild.Sphere(t, "Knot", new Vector3(0.06f, 0.42f, 0f), new Vector3(0.12f, 0.10f, 0.12f), bagMat);
    }

    private static void Dumpster(Transform parent, Vector3 pos, float rotY)
    {
        var root = new GameObject("Dumpster");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, 0f);

        ProcBuild.Box(t, "Body", new Vector3(0f, 0.72f, 0f), new Vector3(2.2f, 1.3f, 1.25f), plasticMat);
        ProcBuild.Box(t, "LidL", new Vector3(-0.55f, 1.42f, 0.06f), new Vector3(1.08f, 0.10f, 1.3f),
            ProcMaterials.Lit("dumpster_lid", new Color(0.045f, 0.055f, 0.052f), 0.25f));
        ProcBuild.BoxRot(t, "LidR", new Vector3(0.58f, 1.48f, 0.04f), new Vector3(1.06f, 0.10f, 1.3f),
            new Vector3(0f, 0f, -9f), metalDarkMat);
        ProcBuild.Box(t, "SideL", new Vector3(-1.11f, 0.72f, 0f), new Vector3(0.06f, 1.15f, 1.15f), metalDarkMat);
        ProcBuild.Box(t, "SideR", new Vector3(1.11f, 0.72f, 0f), new Vector3(0.06f, 1.15f, 1.15f), metalDarkMat);
        for (int i = 0; i < 3; i++)
            ProcBuild.Box(t, "Rib", new Vector3(-0.7f + i * 0.7f, 0.72f, 0.64f), new Vector3(0.08f, 1.2f, 0.04f), metalMat);
        ProcBuild.CylRot(t, "Wheel", new Vector3(-0.8f, 0.12f, 0.55f), 0.12f, 0.1f, new Vector3(90f, 0f, 0f), metalDarkMat);
        ProcBuild.CylRot(t, "Wheel", new Vector3(0.8f, 0.12f, 0.55f), 0.12f, 0.1f, new Vector3(90f, 0f, 0f), metalDarkMat);

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.78f, 0f);
        col.size = new Vector3(2.3f, 1.6f, 1.35f);
    }

    private static void CrateStack(Transform parent, Vector3 pos, System.Random rng, int layers)
    {
        var root = new GameObject("Crates");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 0f);

        for (int i = 0; i < layers; i++)
        {
            float y = 0.35f + i * 0.72f;
            var c = ProcBuild.Box(t, "Crate", new Vector3(0f, y, 0f), new Vector3(0.7f, 0.7f, 0.7f), woodMat);
            // Corner slats
            for (int s = -1; s <= 1; s += 2)
            {
                ProcBuild.Box(t, "Slat", new Vector3(s * 0.36f, y, 0f), new Vector3(0.04f, 0.72f, 0.72f), woodDarkMat);
                ProcBuild.Box(t, "Slat", new Vector3(0f, y, s * 0.36f), new Vector3(0.72f, 0.72f, 0.04f), woodDarkMat);
            }
            c.transform.localEulerAngles = new Vector3(0f, ProcRoadGenerator.Rand(rng, -8f, 8f), 0f);
        }

        if (layers > 0)
        {
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, layers * 0.36f, 0f);
            col.size = new Vector3(0.76f, layers * 0.72f, 0.76f);
        }
    }

    private static void Barrel(Transform parent, Vector3 pos, bool tipped)
    {
        var root = new GameObject("Barrel");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos + (tipped ? new Vector3(0.45f, 0.34f, 0f) : Vector3.zero);
        t.rotation = Quaternion.Euler(0f, 0f, tipped ? 88f : 0f);

        var barrelMat = ProcMaterials.Lit("barrel", new Color(0.215f, 0.132f, 0.085f), 0.2f);
        ProcBuild.Cyl(t, "Body", new Vector3(0f, 0.45f, 0f), 0.33f, 0.9f, barrelMat);
        ProcBuild.Cyl(t, "Ring", new Vector3(0f, 0.20f, 0f), 0.345f, 0.06f, rustMat);
        ProcBuild.Cyl(t, "Ring", new Vector3(0f, 0.70f, 0f), 0.345f, 0.06f, rustMat);
        ProcBuild.Cyl(t, "Lid", new Vector3(0f, 0.91f, 0f), 0.33f, 0.04f, metalDarkMat);

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.45f, 0f);
        col.size = new Vector3(0.7f, 0.9f, 0.7f);
    }

    private static void BrokenSign(Transform parent, Vector3 pos, float rotY, System.Random rng)
    {
        var root = new GameObject("BrokenSign");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(
            ProcRoadGenerator.Rand(rng, -8f, 8f), rotY, ProcRoadGenerator.Rand(rng, 5f, 17f));

        ProcBuild.Box(t, "Post", new Vector3(0f, 1.0f, 0f), new Vector3(0.09f, 2.0f, 0.09f), metalDarkMat);
        var board = ProcBuild.BoxRot(t, "Board", new Vector3(0f, 1.85f, 0.05f),
            new Vector3(1.05f, 0.55f, 0.06f), new Vector3(0f, 0f, ProcRoadGenerator.Rand(rng, -15f, 15f)),
            ProcMaterials.Lit("sign_face", new Color(0.250f, 0.235f, 0.195f)));
        board.name = "Board";
        ProcBuild.Box(t, "Stripe", new Vector3(0f, 1.85f, 0.09f), new Vector3(0.9f, 0.07f, 0.01f),
            ProcMaterials.Lit("sign_stripe", new Color(0.36f, 0.33f, 0.25f)));
        ProcBuild.BoxRot(t, "Crack", new Vector3(0.12f, 1.85f, 0.095f),
            new Vector3(0.02f, 0.5f, 0.01f), new Vector3(0f, 0f, 26f), metalDarkMat);
    }

    private static void Bicycle(Transform parent, Vector3 pos, float rotY, System.Random rng)
    {
        var root = new GameObject("Bicycle");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, ProcRoadGenerator.Rand(rng, 4f, 12f));

        var frameMat = ProcMaterials.Lit("bike_frame", new Color(0.160f, 0.162f, 0.175f), 0.3f);
        var tire = ProcMaterials.Lit("bike_tire", new Color(0.060f, 0.060f, 0.065f));

        for (int s = -1; s <= 1; s += 2)
            ProcBuild.CylRot(t, "Wheel", new Vector3(s * 0.56f, 0.33f, 0f), 0.33f, 0.045f,
                new Vector3(90f, 0f, 0f), tire);

        ProcBuild.BoxRot(t, "TopTube", new Vector3(0f, 0.62f, 0f),
            new Vector3(0.82f, 0.04f, 0.04f), new Vector3(0f, 0f, 8f), frameMat);
        ProcBuild.BoxRot(t, "DownTube", new Vector3(-0.1f, 0.48f, 0f),
            new Vector3(0.70f, 0.04f, 0.04f), new Vector3(0f, 0f, 38f), frameMat);
        ProcBuild.BoxRot(t, "SeatTube", new Vector3(-0.22f, 0.55f, 0f),
            new Vector3(0.04f, 0.55f, 0.04f), new Vector3(0f, 0f, -18f), frameMat);
        ProcBuild.BoxRot(t, "Fork", new Vector3(0.48f, 0.53f, 0f),
            new Vector3(0.04f, 0.52f, 0.04f), new Vector3(0f, 0f, 22f), frameMat);
        ProcBuild.Box(t, "Handlebar", new Vector3(0.55f, 0.82f, 0f), new Vector3(0.05f, 0.05f, 0.42f), frameMat);
        ProcBuild.Box(t, "Seat", new Vector3(-0.24f, 0.85f, 0f), new Vector3(0.24f, 0.05f, 0.12f), metalDarkMat);
    }

    private static void ShoppingCart(Transform parent, Vector3 pos, float rotY)
    {
        var root = new GameObject("ShoppingCart");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, 6f);

        var basket = ProcMaterials.Lit("cart_metal", new Color(0.235f, 0.245f, 0.258f), 0.35f);
        ProcBuild.Box(t, "Floor", new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.04f, 0.86f), basket);
        ProcBuild.Box(t, "SideL", new Vector3(-0.31f, 0.62f, 0f), new Vector3(0.03f, 0.42f, 0.86f), basket);
        ProcBuild.Box(t, "SideR", new Vector3(0.31f, 0.62f, 0f), new Vector3(0.03f, 0.42f, 0.86f), basket);
        ProcBuild.Box(t, "EndF", new Vector3(0f, 0.62f, 0.43f), new Vector3(0.62f, 0.42f, 0.03f), basket);
        ProcBuild.Box(t, "EndB", new Vector3(0f, 0.62f, -0.43f), new Vector3(0.62f, 0.42f, 0.03f), basket);
        ProcBuild.BoxRot(t, "Handle", new Vector3(0f, 0.90f, -0.52f), new Vector3(0.7f, 0.04f, 0.05f),
            new Vector3(-14f, 0f, 0f), metalDarkMat);

        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                ProcBuild.CylRot(t, "CartWheel", new Vector3(sx * 0.25f, 0.10f, sz * 0.34f),
                    0.09f, 0.05f, new Vector3(90f, 0f, 0f), metalDarkMat);
    }

    private static void TrafficCone(Transform parent, Vector3 pos)
    {
        var root = new GameObject("TrafficCone");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        ProcBuild.Cyl(t, "Base", new Vector3(0f, 0.03f, 0f), 0.24f, 0.06f, orangeMat);
        ProcBuild.Cyl(t, "Tier1", new Vector3(0f, 0.17f, 0f), 0.16f, 0.22f, orangeMat);
        ProcBuild.Cyl(t, "Tier2", new Vector3(0f, 0.36f, 0f), 0.11f, 0.18f, orangeMat);
        ProcBuild.Cyl(t, "Tier3", new Vector3(0f, 0.51f, 0f), 0.06f, 0.14f, orangeMat);
        ProcBuild.Cyl(t, "Stripe", new Vector3(0f, 0.27f, 0f), 0.14f, 0.05f, whiteMat);
    }

    private static void RubblePile(Transform parent, Vector3 pos, System.Random rng, int count)
    {
        var root = new GameObject("Rubble");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        var concrete = ProcMaterials.Lit("rubble_prop", new Color(0.225f, 0.215f, 0.200f));
        for (int i = 0; i < count; i++)
        {
            Vector3 off = new Vector3(ProcRoadGenerator.Rand(rng, -0.55f, 0.55f), 0.08f,
                ProcRoadGenerator.Rand(rng, -0.55f, 0.55f));
            ProcBuild.BoxRot(t, "Chunk", off,
                new Vector3(ProcRoadGenerator.Rand(rng, 0.15f, 0.48f), ProcRoadGenerator.Rand(rng, 0.08f, 0.26f),
                    ProcRoadGenerator.Rand(rng, 0.15f, 0.48f)),
                new Vector3(ProcRoadGenerator.Rand(rng, -20f, 20f), ProcRoadGenerator.Rand(rng, 0f, 360f),
                    ProcRoadGenerator.Rand(rng, -20f, 20f)), concrete);
        }
    }

    private static void FallenBranch(Transform parent, Vector3 pos, System.Random rng)
    {
        var root = new GameObject("FallenBranch");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        for (int i = 0; i < 3; i++)
        {
            float len = ProcRoadGenerator.Rand(rng, 0.8f, 2.0f);
            Vector3 dir = Quaternion.Euler(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 84f) * Vector3.up;
            ProcBuild.CylRot(t, "Branch", dir * (len * 0.35f) + Vector3.up * 0.07f,
                ProcRoadGenerator.Rand(rng, 0.03f, 0.07f), len,
                Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, woodDarkMat);
        }
    }

    private static void Pallet(Transform parent, Vector3 pos, float rotY)
    {
        var root = new GameObject("Pallet");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, 0f);

        for (int i = 0; i < 4; i++)
            ProcBuild.Box(t, "Plank", new Vector3(0f, 0.10f, -0.45f + i * 0.3f),
                new Vector3(1.1f, 0.05f, 0.18f), woodMat);
        for (int s = -1; s <= 1; s += 2)
            ProcBuild.Box(t, "Runner", new Vector3(s * 0.45f, 0.06f, 0f),
                new Vector3(0.12f, 0.10f, 1.15f), woodDarkMat);
    }

    private static void Papers(Transform parent, Vector3 pos, System.Random rng)
    {
        var root = new GameObject("Papers");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        for (int i = 0; i < 5; i++)
        {
            ProcBuild.BoxRot(t, "Paper",
                new Vector3(ProcRoadGenerator.Rand(rng, -0.8f, 0.8f), 0.035f, ProcRoadGenerator.Rand(rng, -0.8f, 0.8f)),
                new Vector3(ProcRoadGenerator.Rand(rng, 0.18f, 0.32f), 0.006f, ProcRoadGenerator.Rand(rng, 0.24f, 0.4f)),
                new Vector3(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 0f), paperMat);
        }
    }

    private static void Hydrant(Transform parent, Vector3 pos)
    {
        var root = new GameObject("Hydrant");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        ProcBuild.Cyl(t, "Body", new Vector3(0f, 0.34f, 0f), 0.13f, 0.55f, redMat);
        ProcBuild.Sphere(t, "Cap", new Vector3(0f, 0.64f, 0f), new Vector3(0.28f, 0.20f, 0.28f), redMat);
        ProcBuild.CylRot(t, "NozzleL", new Vector3(-0.17f, 0.40f, 0f), 0.055f, 0.16f,
            new Vector3(0f, 0f, 90f), metalDarkMat);
        ProcBuild.CylRot(t, "NozzleR", new Vector3(0.17f, 0.40f, 0f), 0.055f, 0.16f,
            new Vector3(0f, 0f, 90f), metalDarkMat);
        ProcBuild.Cyl(t, "Base", new Vector3(0f, 0.04f, 0f), 0.19f, 0.08f, metalDarkMat);
    }

    private static void GraffitiWall(Transform parent, Vector3 pos, float rotY)
    {
        // Small barrier/roadworks sign leaning against the generator zone.
        var root = new GameObject("WorkSign");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, 7f);

        ProcBuild.Box(t, "LegA", new Vector3(-0.28f, 0.46f, 0f), new Vector3(0.05f, 0.95f, 0.05f), metalMat);
        ProcBuild.Box(t, "LegB", new Vector3(0.28f, 0.46f, 0f), new Vector3(0.05f, 0.95f, 0.05f), metalMat);
        ProcBuild.Box(t, "Board", new Vector3(0f, 0.92f, 0f), new Vector3(0.82f, 0.45f, 0.05f), orangeMat);
        ProcBuild.Box(t, "Stripe", new Vector3(0f, 0.92f, 0.04f), new Vector3(0.74f, 0.10f, 0.01f), whiteMat);
    }
}
