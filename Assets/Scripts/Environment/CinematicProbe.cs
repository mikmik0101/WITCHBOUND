using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Cinematic camera-path capture harness.
///
/// Only activated with "-cinemaprobe". It starts a real round (so the actual
/// survivors, companions and witches are in the streets), disables the player
/// camera and the HUD, then renders smooth spline camera moves over the real
/// procedural village to a PNG sequence at 1920x1080 / 30 fps.
///
/// Nothing here runs in a normal session and no gameplay system is modified:
/// the probe only reads the scene. The result is genuine engine footage of the
/// actual environment, not a mockup.
/// </summary>
public class CinematicProbe : MonoBehaviour
{
    private struct Key
    {
        public Vector3 pos;
        public Vector3 look;
        public float fov;

        public Key(float px, float py, float pz, float lx, float ly, float lz, float f)
        {
            pos = new Vector3(px, py, pz);
            look = new Vector3(lx, ly, lz);
            fov = f;
        }
    }

    private class CamPath
    {
        public string name;
        public Key[] keys;
        public float duration;

        public CamPath(string n, float d, params Key[] k)
        {
            name = n;
            duration = d;
            keys = k;
        }
    }

    private const int Width = 1920;
    private const int Height = 1080;
    private const int Fps = 30;

    private string outputDir;
    private Camera cam;

    public static bool Requested
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-cinemaprobe") return true;
            return false;
        }
    }

    private static bool HasFlag(string flag)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (args[i] == flag) return true;
        return false;
    }

    private void Start()
    {
        outputDir = "/tmp/wb_cine";
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-probeout")
            {
                outputDir = args[i + 1];
                break;
            }
        }

        try { Directory.CreateDirectory(outputDir); }
        catch (Exception e) { Debug.LogError("[CINE] cannot create output dir: " + e.Message); }

        Debug.Log("[CINE] cinematic probe requested -> " + outputDir);
        StartCoroutine(Run());
    }

    private static CamPath[] BuildPaths()
    {
        var paths = new System.Collections.Generic.List<CamPath>();

        // High descent toward the village centre.
        paths.Add(new CamPath("01_descent", 10f,
            new Key(0f, 68f, -86f, 0f, 0f, 6f, 38f),
            new Key(0f, 44f, -62f, 0f, 0f, 4f, 44f),
            new Key(0f, 26f, -46f, 0f, 1.5f, 2f, 50f),
            new Key(0f, 17f, -34f, 0f, 2f, 6f, 55f)));

        // Slow forward travel above the rooftops.
        paths.Add(new CamPath("02_rooftops", 10f,
            new Key(-52f, 19f, -44f, -30f, 6f, -18f, 50f),
            new Key(-26f, 17f, -26f, -6f, 5f, -6f, 52f),
            new Key(2f, 16f, 2f, 10f, 4f, 22f, 54f),
            new Key(26f, 15f, 26f, 30f, 4f, 44f, 56f)));

        // Near-overhead drift along the main street.
        paths.Add(new CamPath("03_overhead", 9f,
            new Key(-6f, 58f, -34f, 0f, 0f, -14f, 42f),
            new Key(0f, 56f, -6f, 0f, 0f, 12f, 44f),
            new Key(4f, 54f, 22f, 2f, 0f, 40f, 46f)));

        // Slow orbit around the plaza.
        var orbit = new System.Collections.Generic.List<Key>();
        float[] ts = { 0f, 0.34f, 0.67f, 1f };
        for (int i = 0; i < ts.Length; i++)
        {
            float a = Mathf.Deg2Rad * (198f + 132f * ts[i]);
            float r = 30f - 3f * ts[i];
            float y = 16f + 8f * ts[i];
            orbit.Add(new Key(Mathf.Sin(a) * r, y, 4f + Mathf.Cos(a) * r,
                              0f, 1.5f, 4f, 46f + 6f * ts[i]));
        }
        paths.Add(new CamPath("04_orbit", 11f, orbit.ToArray()));

        // Rising pull-away that reveals the whole town.
        paths.Add(new CamPath("05_pullaway", 9f,
            new Key(1.5f, 3f, -9f, 0f, 2f, 8f, 60f),
            new Key(0.5f, 12f, -26f, 0f, 1.5f, 6f, 52f),
            new Key(0f, 30f, -52f, 0f, 0.5f, 4f, 45f),
            new Key(0f, 46f, -74f, 0f, 0f, 2f, 40f)));

        // Wide travel over the north field.
        paths.Add(new CamPath("06_field", 8f,
            new Key(0f, 34f, 54f, 0f, 0f, 96f, 44f),
            new Key(0f, 28f, 70f, 0f, 0f, 104f, 48f),
            new Key(0f, 22f, 88f, 0f, 0f, 116f, 52f)));

        // Low crane rising out of the street.
        paths.Add(new CamPath("07_crane", 7f,
            new Key(-2f, 1.8f, -16f, 0f, 2.2f, 4f, 60f),
            new Key(0f, 6f, -14f, 0f, 2f, 6f, 55f),
            new Key(2f, 14f, -12f, 0f, 1.8f, 8f, 50f)));

        return paths.ToArray();
    }

    private IEnumerator Run()
    {
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(1.0f);

        // Start a genuine round so the cast is present in the streets.
        if (!HasFlag("-nostart") && GameManager.Instance != null)
        {
            GameManager.Instance.RequestNewGame();
            yield return new WaitForSecondsRealtime(2.2f);
        }

        // Aerial shots need to see the far side of the town: widen the fog
        // range for the capture only (the probe quits afterwards).
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 240f;

        var mainCamera = Camera.main;
        if (mainCamera != null) mainCamera.enabled = false;

        var camObj = new GameObject("CinematicProbeCamera");
        cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 400f;
        cam.cullingMask = ~0;
        cam.allowHDR = false;
        cam.allowMSAA = true;

        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        rt.Create();
        cam.targetTexture = rt;

        var paths = BuildPaths();
        for (int p = 0; p < paths.Length; p++)
        {
            var path = paths[p];
            int frames = Mathf.RoundToInt(path.duration * Fps);
            string dir = Path.Combine(outputDir, path.name);
            Directory.CreateDirectory(dir);

            for (int f = 0; f < frames; f++)
            {
                float t = frames > 1 ? (float)f / (frames - 1) : 0f;
                Key k = Sample(path.keys, t);

                // Very subtle organic drift so the move never feels like a
                // linear keyframe ramp.
                float tt = (float)f / Fps;
                Vector3 drift = new Vector3(
                    Mathf.Sin(tt * 0.7f) * 0.035f,
                    Mathf.Sin(tt * 0.53f + 1.3f) * 0.030f,
                    Mathf.Cos(tt * 0.61f) * 0.035f);

                cam.transform.position = k.pos + drift;
                cam.transform.rotation = Quaternion.LookRotation(
                    (k.look - k.pos).normalized, Vector3.up);
                cam.fieldOfView = k.fov;

                yield return null;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply(false, false);
                RenderTexture.active = null;

                File.WriteAllBytes(Path.Combine(dir, "f" + f.ToString("D4") + ".png"),
                                   tex.EncodeToPNG());
                Destroy(tex);
            }

            Debug.Log("[CINE] path " + path.name + " done (" + frames + " frames)");
        }

        cam.targetTexture = null;
        Destroy(rt);
        Debug.Log("[CINE] complete");
        yield return new WaitForSecondsRealtime(0.3f);
        Application.Quit();
    }

    /// <summary>Catmull-Rom through the keys, with eased end points.</summary>
    private static Key Sample(Key[] k, float t)
    {
        int n = k.Length;
        if (n == 1) return k[0];

        float scaled = Mathf.Clamp01(t) * (n - 1);
        int i = Mathf.Min(Mathf.FloorToInt(scaled), n - 2);
        float u = scaled - i;

        Key p0 = k[Mathf.Max(i - 1, 0)];
        Key p1 = k[i];
        Key p2 = k[i + 1];
        Key p3 = k[Mathf.Min(i + 2, n - 1)];

        Key r;
        r.pos = Catmull(p0.pos, p1.pos, p2.pos, p3.pos, u);
        r.look = Catmull(p0.look, p1.look, p2.look, p3.look, u);
        r.fov = Mathf.Lerp(p1.fov, p2.fov, Mathf.SmoothStep(0f, 1f, u));
        return r;
    }

    private static Vector3 Catmull(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u)
    {
        float u2 = u * u;
        float u3 = u2 * u;
        return 0.5f * ((2f * b) + (-a + c) * u +
                       (2f * a - 5f * b + 4f * c - d) * u2 +
                       (-a + 3f * b - 3f * c + d) * u3);
    }
}
