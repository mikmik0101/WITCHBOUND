using UnityEngine;

/// <summary>
/// Stylised low-poly vegetation: gnarled dead trees, sparse broadleaf trees,
/// tiered conifers, bushes, weed clumps and dead grass patches.
/// </summary>
public static class ProcVegetationGenerator
{
    private static Material barkMat;
    private static Material barkDeadMat;
    private static Material foliageMatA;
    private static Material foliageMatB;
    private static Material foliageDeadMat;
    private static Material bushMat;
    private static Material weedMat;
    private static Material grassMat;

    public static void Generate(Transform parent, System.Random rng)
    {
        barkMat = ProcMaterials.Lit("bark", new Color(0.125f, 0.105f, 0.085f));
        barkDeadMat = ProcMaterials.Lit("bark_dead", new Color(0.105f, 0.092f, 0.078f));
        foliageMatA = ProcMaterials.Lit("foliage_a", new Color(0.060f, 0.095f, 0.065f));
        foliageMatB = ProcMaterials.Lit("foliage_b", new Color(0.078f, 0.118f, 0.070f));
        foliageDeadMat = ProcMaterials.Lit("foliage_dead", new Color(0.130f, 0.110f, 0.058f));
        bushMat = ProcMaterials.Lit("bush", new Color(0.068f, 0.100f, 0.062f));
        weedMat = ProcMaterials.Lit("weed", new Color(0.150f, 0.138f, 0.078f));
        grassMat = ProcMaterials.Lit("grass_patch", new Color(0.080f, 0.092f, 0.058f));

        // ---- park trees (block 0,24) ----
        Broadleaf(parent, new Vector3(5.0f, 0f, 20.0f), rng, 1.05f, true);
        Broadleaf(parent, new Vector3(-5.0f, 0f, 27.0f), rng, 0.95f, true);
        DeadTree(parent, new Vector3(3.2f, 0f, 29.4f), rng, 1.0f, true);
        Broadleaf(parent, new Vector3(-6.5f, 0f, 18.8f), rng, 0.85f, true);
        Bush(parent, new Vector3(7.5f, 0f, 26.0f), rng, 1.1f);
        Bush(parent, new Vector3(-2.5f, 0f, 21.0f), rng, 0.9f);

        // ---- yards ----
        DeadTree(parent, new Vector3(-30.2f, 0f, -19.4f), rng, 0.9f, true);
        Broadleaf(parent, new Vector3(30.2f, 0f, -19.0f), rng, 0.85f, true);
        DeadTree(parent, new Vector3(18.4f, 0f, 6.2f), rng, 0.8f, true);
        Broadleaf(parent, new Vector3(19.8f, 0f, -5.4f), rng, 0.9f, true);
        Bush(parent, new Vector3(-18.6f, 0f, -18.6f), rng, 1.0f);
        Bush(parent, new Vector3(18.2f, 0f, -18.2f), rng, 0.8f);
        Bush(parent, new Vector3(-18.6f, 0f, 18.2f), rng, 0.9f);
        Bush(parent, new Vector3(30.0f, 0f, 18.4f), rng, 0.85f);

        // ---- outer tree ring: hides the world edge and creates silhouettes ----
        // Trees are kept OUT of the open field: it has to stay a clear running
        // surface, and a tree line in the middle of it would defeat the point.
        for (int i = 0; i < 30; i++)
        {
            float angle = (i / 30f) * Mathf.PI * 2f + ProcRoadGenerator.Rand(rng, -0.09f, 0.09f);
            float radius = ProcRoadGenerator.Rand(rng, 50f, 72f);
            Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            if (TownLayout.InsideFieldArea(p, 3f))
            {
                // Push the tree out past the field's far wall instead of
                // dropping it, so the skyline stays dense.
                p.z = Mathf.Max(p.z, TownLayout.FieldEndZ + ProcRoadGenerator.Rand(rng, 4f, 18f));
            }

            if (rng.NextDouble() < 0.62)
                DeadTree(parent, p, rng, ProcRoadGenerator.Rand(rng, 0.9f, 1.35f), false);
            else
                Broadleaf(parent, p, rng, ProcRoadGenerator.Rand(rng, 0.9f, 1.3f), false);
        }

        // ---- roadside weeds along the curbs ----
        for (int i = 0; i < 46; i++)
        {
            float line = TownLayout.Lines[rng.Next(TownLayout.Lines.Length)];
            bool alongX = rng.Next(2) == 0;
            float along = ProcRoadGenerator.Rand(rng, -46f, 46f);
            float side = (rng.Next(2) == 0 ? -1f : 1f) * ProcRoadGenerator.Rand(rng, 4.4f, 5.4f);
            Vector3 p = alongX
                ? new Vector3(along, 0f, line + side)
                : new Vector3(line + side, 0f, along);
            WeedClump(parent, p, rng);
        }

        // ---- dead grass patches in yards and the park ----
        for (int i = 0; i < 26; i++)
        {
            int block = rng.Next(TownLayout.BlockCenters.Length);
            float cx = TownLayout.BlockCenters[block].x;
            float cz = TownLayout.BlockCenters[block].y;
            Vector3 p = new Vector3(cx + ProcRoadGenerator.Rand(rng, -5.5f, 5.5f), 0.052f,
                cz + ProcRoadGenerator.Rand(rng, -5.5f, 5.5f));
            ProcBuild.BoxRot(parent, "GrassPatch", p,
                new Vector3(ProcRoadGenerator.Rand(rng, 0.9f, 2.8f), 0.02f, ProcRoadGenerator.Rand(rng, 0.9f, 2.8f)),
                new Vector3(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 0f), grassMat);
        }
    }

    // --------------------------------------------------------------- archetypes

    private static void DeadTree(Transform parent, Vector3 pos, System.Random rng, float scale, bool collider)
    {
        var root = new GameObject("DeadTree");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        float h = ProcRoadGenerator.Rand(rng, 3.8f, 6.8f) * scale;
        float radius = ProcRoadGenerator.Rand(rng, 0.16f, 0.28f) * scale;

        ProcBuild.Cyl(t, "Trunk", new Vector3(0f, h * 0.5f, 0f), radius, h, barkDeadMat, collider);

        int branches = rng.Next(4, 8);
        for (int i = 0; i < branches; i++)
        {
            float t0 = ProcRoadGenerator.Rand(rng, 0.38f, 0.92f);
            float startY = h * t0;
            float len = ProcRoadGenerator.Rand(rng, 1.1f, 2.9f) * scale;
            float br = ProcRoadGenerator.Rand(rng, 0.045f, 0.10f) * scale;

            Vector3 dir = Quaternion.Euler(
                ProcRoadGenerator.Rand(rng, -58f, 58f),
                ProcRoadGenerator.Rand(rng, 0f, 360f),
                ProcRoadGenerator.Rand(rng, -58f, 58f)) * Vector3.up;

            Vector3 mid = new Vector3(0f, startY, 0f) + dir * (len * 0.5f);
            ProcBuild.CylRot(t, "Branch", mid, br, len,
                Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, barkDeadMat);

            // Occasional forked twig.
            if (rng.NextDouble() < 0.45)
            {
                Vector3 tip = new Vector3(0f, startY, 0f) + dir * len;
                Vector3 dir2 = Quaternion.Euler(
                    ProcRoadGenerator.Rand(rng, -50f, 50f),
                    ProcRoadGenerator.Rand(rng, 0f, 360f),
                    ProcRoadGenerator.Rand(rng, -50f, 50f)) * Vector3.up;
                float len2 = len * ProcRoadGenerator.Rand(rng, 0.35f, 0.65f);
                ProcBuild.CylRot(t, "Twig", tip + dir2 * (len2 * 0.5f), br * 0.65f, len2,
                    Quaternion.FromToRotation(Vector3.up, dir2).eulerAngles, barkDeadMat);
            }
        }

        // A couple of dead twigs at the base.
        for (int i = 0; i < 2; i++)
        {
            Vector3 dir = Quaternion.Euler(
                ProcRoadGenerator.Rand(rng, 55f, 80f),
                ProcRoadGenerator.Rand(rng, 0f, 360f), 0f) * Vector3.up;
            float len = ProcRoadGenerator.Rand(rng, 0.7f, 1.5f) * scale;
            ProcBuild.CylRot(t, "Root", dir * (len * 0.4f), 0.04f * scale, len,
                Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, barkDeadMat);
        }
    }

    private static void Broadleaf(Transform parent, Vector3 pos, System.Random rng, float scale, bool collider)
    {
        var root = new GameObject("Tree_Broadleaf");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        float h = ProcRoadGenerator.Rand(rng, 2.6f, 4.4f) * scale;
        float radius = ProcRoadGenerator.Rand(rng, 0.16f, 0.26f) * scale;

        ProcBuild.Cyl(t, "Trunk", new Vector3(0f, h * 0.5f, 0f), radius, h, barkMat, collider);

        // Visible branches under the crown.
        int branches = rng.Next(2, 4);
        for (int i = 0; i < branches; i++)
        {
            float startY = h * ProcRoadGenerator.Rand(rng, 0.55f, 0.88f);
            float len = ProcRoadGenerator.Rand(rng, 0.9f, 1.9f) * scale;
            Vector3 dir = Quaternion.Euler(
                ProcRoadGenerator.Rand(rng, -45f, 45f),
                ProcRoadGenerator.Rand(rng, 0f, 360f),
                ProcRoadGenerator.Rand(rng, -45f, 45f)) * Vector3.up;
            ProcBuild.CylRot(t, "Branch", new Vector3(0f, startY, 0f) + dir * (len * 0.5f),
                0.05f * scale, len, Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, barkMat);
        }

        // Irregular crown made of a few flattened blobs.
        int blobs = rng.Next(3, 5);
        for (int i = 0; i < blobs; i++)
        {
            float ang = (i / (float)blobs) * Mathf.PI * 2f + ProcRoadGenerator.Rand(rng, -0.5f, 0.5f);
            float off = ProcRoadGenerator.Rand(rng, 0.3f, 1.4f) * scale;
            Vector3 bp = new Vector3(Mathf.Cos(ang) * off, h + ProcRoadGenerator.Rand(rng, -0.2f, 1.1f) * scale,
                Mathf.Sin(ang) * off);
            Vector3 sc = new Vector3(
                ProcRoadGenerator.Rand(rng, 1.8f, 3.0f) * scale,
                ProcRoadGenerator.Rand(rng, 1.2f, 2.0f) * scale,
                ProcRoadGenerator.Rand(rng, 1.8f, 3.0f) * scale);
            ProcBuild.Sphere(t, "Crown", bp, sc, rng.NextDouble() < 0.5 ? foliageMatA : foliageMatB);
        }
    }

    private static void Bush(Transform parent, Vector3 pos, System.Random rng, float scale)
    {
        var root = new GameObject("Bush");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        int blobs = rng.Next(2, 4);
        for (int i = 0; i < blobs; i++)
        {
            Vector3 off = new Vector3(ProcRoadGenerator.Rand(rng, -0.5f, 0.5f), 0f,
                ProcRoadGenerator.Rand(rng, -0.5f, 0.5f));
            Vector3 sc = new Vector3(
                ProcRoadGenerator.Rand(rng, 0.8f, 1.5f) * scale,
                ProcRoadGenerator.Rand(rng, 0.45f, 0.85f) * scale,
                ProcRoadGenerator.Rand(rng, 0.8f, 1.5f) * scale);
            ProcBuild.Sphere(t, "BushBlob", off + Vector3.up * (sc.y * 0.45f), sc,
                rng.NextDouble() < 0.5 ? bushMat : foliageDeadMat);
        }
    }

    private static void WeedClump(Transform parent, Vector3 pos, System.Random rng)
    {
        var root = new GameObject("Weeds");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        int blades = rng.Next(3, 6);
        for (int i = 0; i < blades; i++)
        {
            float h = ProcRoadGenerator.Rand(rng, 0.18f, 0.55f);
            Vector3 off = new Vector3(ProcRoadGenerator.Rand(rng, -0.28f, 0.28f), h * 0.5f,
                ProcRoadGenerator.Rand(rng, -0.28f, 0.28f));
            ProcBuild.BoxRot(t, "Blade", off,
                new Vector3(0.035f, h, 0.035f),
                new Vector3(ProcRoadGenerator.Rand(rng, -14f, 14f), ProcRoadGenerator.Rand(rng, 0f, 360f),
                    ProcRoadGenerator.Rand(rng, -14f, 14f)), weedMat);
        }
    }
}
