using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Subtle environmental horror: ritual circles, candle clusters, blood-like
/// stains, glowing wall runes and faint supernatural accent lights.
/// The effect is deliberately restrained - a few details, not a red rave.
/// </summary>
public static class ProcHorrorDetailGenerator
{
    private static Material runeRed;
    private static Material runeTeal;
    private static Material waxMat;
    private static Material flameMat;
    private static Material bloodMat;
    private static Material bloodWallMat;
    private static Material sigilLine;

    public static void Generate(Transform geometryParent, Transform systemsParent, System.Random rng)
    {
        runeRed = ProcMaterials.Emissive("rune_red",
            new Color(0.10f, 0.015f, 0.015f), new Color(1f, 0.10f, 0.06f), 0.55f);
        runeTeal = ProcMaterials.Emissive("rune_teal",
            new Color(0.02f, 0.07f, 0.07f), new Color(0.20f, 1f, 0.85f), 0.40f);
        waxMat = ProcMaterials.Lit("candle_wax", new Color(0.360f, 0.340f, 0.285f));
        flameMat = ProcMaterials.Emissive("candle_flame",
            new Color(0.55f, 0.30f, 0.08f), new Color(1f, 0.62f, 0.22f), 1.7f);
        bloodMat = ProcMaterials.Lit("blood_ground", new Color(0.140f, 0.020f, 0.024f), 0.35f);
        bloodWallMat = ProcMaterials.Lit("blood_wall", new Color(0.150f, 0.018f, 0.022f), 0.25f);
        sigilLine = ProcMaterials.Line("sigil_line", new Color(0.42f, 0.045f, 0.05f));

        // ---- plaza ritual (visible immediately from the spawn point) ----
        Ritual(geometryParent, systemsParent, new Vector3(0f, 0f, 4.6f), 2.0f, rng, true, false);

        // ---- park ritual beneath the dead trees ----
        Ritual(geometryParent, systemsParent, new Vector3(0f, 0f, 24.0f), 2.6f, rng, true, true);

        // ---- ritual at the ruined building ----
        Ritual(geometryParent, systemsParent, new Vector3(24.0f, 0f, 19.3f), 1.6f, rng, false, true);

        // ---- blood / corruption stains ----
        Stain(geometryParent, new Vector3(0.6f, 0f, -20.6f), 1.0f, rng);
        Stain(geometryParent, new Vector3(24.4f, 0f, -20.1f), 1.3f, rng);
        Stain(geometryParent, new Vector3(-23.6f, 0f, 20.5f), 0.9f, rng);
        Stain(geometryParent, new Vector3(-27.5f, 0f, -18.6f), 0.7f, rng);

        // ---- wall stains ----
        WallStain(geometryParent, new Vector3(2.2f, 1.25f, -20.62f), 0f, 0.9f, 1.4f);
        WallStain(geometryParent, new Vector3(25.8f, 1.6f, -20.08f), 0f, 0.7f, 1.8f);
        WallStain(geometryParent, new Vector3(-26.0f, 1.3f, -20.68f), 0f, 0.6f, 1.1f);

        // ---- glowing runes ----
        Rune(geometryParent, new Vector3(24.0f, 3.3f, -20.06f), 0f, runeRed, 1.1f);
        Rune(geometryParent, new Vector3(-24.0f, 4.4f, 20.10f), 180f, runeTeal, 1.0f);
        Rune(geometryParent, new Vector3(-27.2f, 2.2f, -20.64f), 0f, runeRed, 0.8f);
        Rune(geometryParent, new Vector3(-19.0f, 1.1f, 19.55f), 90f, runeTeal, 0.7f);

        // ---- weird hanging bundle under the store awning ----
        HangingBundle(geometryParent, new Vector3(2.4f, 0f, -20.0f), rng);

        // ---- faint cold glow in the ruin doorway ----
        var cold = new GameObject("RuinColdGlow");
        cold.transform.SetParent(systemsParent, false);
        cold.transform.position = new Vector3(24f, 1.6f, 24f);
        var cl = cold.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(0.25f, 0.55f, 0.65f);
        cl.intensity = 0.55f;
        cl.range = 7f;
        cl.shadows = LightShadows.None;
        cl.renderMode = LightRenderMode.ForcePixel;
    }

    // ------------------------------------------------------------------ ritual

    private static void Ritual(Transform geo, Transform systems, Vector3 center, float radius,
        System.Random rng, bool redLight, bool candles)
    {
        var root = new GameObject("RitualCircle");
        root.transform.SetParent(systems, false);
        root.transform.position = center;

        // Dark burnt ground disc.
        ProcBuild.Disc(geo, "RitualGround", center + Vector3.up * 0.058f, radius * 1.25f,
            ProcMaterials.Lit("ritual_ground", new Color(0.055f, 0.040f, 0.036f)), false);

        // Outer ring.
        CreateCircle(root.transform, radius, 0.42f, 0.06f, sigilLine, 0.075f);

        // Inner star.
        var star = new GameObject("SigilStar");
        star.transform.SetParent(root.transform, false);
        var lr = star.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = sigilLine;
        lr.startWidth = 0.055f;
        lr.endWidth = 0.055f;
        lr.loop = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        int points = 5;
        lr.positionCount = points + 1;
        for (int i = 0; i <= points; i++)
        {
            float a = (i * 2 * Mathf.PI * 2f / points) + Mathf.PI * 0.5f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * 0.72f, 0.05f, Mathf.Sin(a) * radius * 0.72f));
        }

        // Faint unnatural glow ring on the ground.
        ProcBuild.Disc(geo, "RitualGlow", center + Vector3.up * 0.062f, radius * 0.55f,
            ProcMaterials.Emissive("ritual_glow",
                new Color(0.10f, 0.012f, 0.012f), new Color(0.9f, 0.05f, 0.04f), 0.30f), false);

        if (candles)
        {
            int count = Mathf.RoundToInt(radius * 3.2f);
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + ProcRoadGenerator.Rand(rng, -0.2f, 0.2f);
                float r = radius * ProcRoadGenerator.Rand(rng, 0.95f, 1.18f);
                Vector3 p = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Candle(geo, p, rng);
            }
        }

        if (redLight)
        {
            var lightObj = new GameObject("RitualLight");
            lightObj.transform.SetParent(systems, false);
            lightObj.transform.position = center + Vector3.up * 0.8f;
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.85f, 0.12f, 0.08f);
            l.intensity = 0.65f;
            l.range = radius * 4.5f;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
            var flicker = lightObj.AddComponent<FlickerLight>();
            flicker.target = l;
            flicker.baseIntensity = 0.65f;
            flicker.minIntensity = 0.35f;
            flicker.speed = 2.4f;
            flicker.seed = rng.Next(1, 9999);
        }
    }

    private static void CreateCircle(Transform parent, float radius, float y, float width,
        Material mat, float lineWidth)
    {
        var go = new GameObject("SigilCircle");
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = mat;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.loop = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        const int segments = 40;
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, y * 0.1f, Mathf.Sin(a) * radius));
        }
    }

    private static void Candle(Transform parent, Vector3 pos, System.Random rng)
    {
        var root = new GameObject("Candle");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, ProcRoadGenerator.Rand(rng, 0f, 360f), 0f);

        float h = ProcRoadGenerator.Rand(rng, 0.10f, 0.24f);
        ProcBuild.Cyl(t, "Wax", new Vector3(0f, h * 0.5f + 0.04f, 0f), 0.035f, h, waxMat);
        ProcBuild.Box(t, "Flame", new Vector3(0f, h + 0.10f, 0f), new Vector3(0.045f, 0.09f, 0.045f), flameMat);
    }

    private static void Stain(Transform parent, Vector3 pos, float radius, System.Random rng)
    {
        int splats = 3;
        for (int i = 0; i < splats; i++)
        {
            Vector3 off = new Vector3(ProcRoadGenerator.Rand(rng, -radius * 0.5f, radius * 0.5f), 0f,
                ProcRoadGenerator.Rand(rng, -radius * 0.5f, radius * 0.5f));
            float r = radius * ProcRoadGenerator.Rand(rng, 0.45f, 0.8f);
            ProcBuild.Disc(parent, "BloodStain", pos + off + Vector3.up * 0.064f, r, bloodMat);
        }
    }

    private static void WallStain(Transform parent, Vector3 pos, float rotY, float width, float height)
    {
        var root = new GameObject("WallStain");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, 0f);

        ProcBuild.Box(t, "StainA", new Vector3(0f, 0f, 0f), new Vector3(width, height, 0.02f), bloodWallMat);
        ProcBuild.BoxRot(t, "StainDrip", new Vector3(width * 0.25f, -height * 0.4f, 0f),
            new Vector3(0.08f, height * 0.7f, 0.02f), new Vector3(0f, 0f, 6f), bloodWallMat);
    }

    private static void Rune(Transform parent, Vector3 pos, float rotY, Material mat, float scale)
    {
        var root = new GameObject("WallRune");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, rotY, 0f);
        t.localScale = Vector3.one * scale;

        // Abstract angular glyph: three strokes plus a dot.
        ProcBuild.Box(t, "StrokeA", new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.62f, 0.015f), mat);
        ProcBuild.BoxRot(t, "StrokeB", new Vector3(0.12f, 0.12f, 0f), new Vector3(0.04f, 0.42f, 0.015f),
            new Vector3(0f, 0f, 48f), mat);
        ProcBuild.BoxRot(t, "StrokeC", new Vector3(-0.10f, -0.14f, 0f), new Vector3(0.04f, 0.34f, 0.015f),
            new Vector3(0f, 0f, -36f), mat);
        ProcBuild.Box(t, "Dot", new Vector3(0f, 0.38f, 0f), new Vector3(0.09f, 0.09f, 0.015f), mat);
    }

    private static void HangingBundle(Transform parent, Vector3 pos, System.Random rng)
    {
        var root = new GameObject("HangingBundle");
        var t = root.transform;
        t.SetParent(parent, false);
        t.position = pos;

        var rope = ProcMaterials.Lit("rope", new Color(0.180f, 0.150f, 0.115f));
        ProcBuild.Cyl(t, "Rope", new Vector3(0f, 2.1f, 0f), 0.015f, 1.4f, rope);
        ProcBuild.Box(t, "Bundle", new Vector3(0f, 1.32f, 0f), new Vector3(0.22f, 0.32f, 0.16f), rope);
        for (int i = 0; i < 3; i++)
        {
            ProcBuild.CylRot(t, "Twig", new Vector3(ProcRoadGenerator.Rand(rng, -0.08f, 0.08f), 1.18f,
                    ProcRoadGenerator.Rand(rng, -0.08f, 0.08f)),
                0.012f, ProcRoadGenerator.Rand(rng, 0.18f, 0.34f),
                new Vector3(ProcRoadGenerator.Rand(rng, -25f, 25f), 0f, ProcRoadGenerator.Rand(rng, -25f, 25f)), rope);
        }
    }
}
