using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Deterministic screenshot harness for build verification.
///
/// Only activated when the player is launched with "-visualprobe". It renders
/// a fixed set of viewpoints into a RenderTexture and writes PNGs, then quits.
/// It never runs in a normal play session and never touches input, gameplay
/// systems or the UI.
/// </summary>
public class VisualProbe : MonoBehaviour
{
    private struct Shot
    {
        public string name;
        public Vector3 position;
        public Vector3 lookAt;
        public float fov;

        public Shot(string n, Vector3 p, Vector3 l, float f)
        {
            name = n;
            position = p;
            lookAt = l;
            fov = f;
        }
    }

    private static readonly Shot[] Shots =
    {
        new Shot("01_plaza",      new Vector3(0f, 1.70f, -7f),    new Vector3(0f, 1.45f, 12f),   66f),
        new Shot("02_street_ew",  new Vector3(-34f, 1.70f, -11.6f), new Vector3(30f, 1.40f, -12f), 66f),
        new Shot("03_park",       new Vector3(-1f, 1.70f, 12f),   new Vector3(2f, 3.0f, 28f),    66f),
        new Shot("04_warehouse",  new Vector3(24f, 1.70f, -10f),  new Vector3(24f, 3.2f, -25f),  66f),
        new Shot("05_ruin",       new Vector3(14f, 1.70f, 12f),   new Vector3(24f, 3.0f, 24f),   66f),
        new Shot("06_overview",   new Vector3(0f, 26f, -44f),     new Vector3(0f, 0f, 2f),       58f),
        // North field extension (z 53..128) - gate, interior and perimeter.
        new Shot("07_gate",       new Vector3(0f, 1.70f, 34f),    new Vector3(0f, 1.60f, 70f),   66f),
        new Shot("08_field_entry",new Vector3(0f, 1.70f, 58f),    new Vector3(0f, 1.60f, 100f),  66f),
        new Shot("09_field_mid",  new Vector3(0f, 1.70f, 92f),    new Vector3(12f, 1.60f, 126f), 66f),
        new Shot("10_field_back", new Vector3(0f, 1.70f, 124f),   new Vector3(0f, 1.60f, 60f),   66f),
        new Shot("11_field_wide", new Vector3(0f, 40f, 40f),      new Vector3(0f, 0f, 100f),     70f),
        new Shot("12_field_corner",new Vector3(-46f, 1.70f, 70f), new Vector3(40f, 1.60f, 112f), 66f)
    };

    private string outputDir;
    private Camera probeCamera;
    private Camera mainCamera;

    public static bool Requested
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-visualprobe") return true;
            return false;
        }
    }

    private void Start()
    {
        outputDir = "/tmp/opencode/shots";
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
        catch (Exception e) { Debug.LogError("[PROBE] Cannot create output dir: " + e.Message); }

        Debug.Log("[PROBE] Visual probe requested, writing to " + outputDir);
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // Let generation, GI, particle prewarm and the first frames settle.
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(1.2f);

        mainCamera = Camera.main;
        if (mainCamera != null) mainCamera.enabled = false;

        var camObj = new GameObject("VisualProbeCamera");
        probeCamera = camObj.AddComponent<Camera>();
        probeCamera.enabled = false;
        probeCamera.clearFlags = CameraClearFlags.Skybox;
        probeCamera.nearClipPlane = 0.08f;
        probeCamera.farClipPlane = 260f;
        probeCamera.cullingMask = ~0;
        probeCamera.allowHDR = false;
        probeCamera.allowMSAA = true;

        const int width = 1280;
        const int height = 720;

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        rt.Create();

        probeCamera.targetTexture = rt;

        for (int i = 0; i < Shots.Length; i++)
        {
            var s = Shots[i];
            probeCamera.transform.position = s.position;
            probeCamera.transform.rotation = Quaternion.LookRotation(
                (s.lookAt - s.position).normalized, Vector3.up);
            probeCamera.fieldOfView = s.fov;

            yield return null;
            yield return null;

            probeCamera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply(false, false);
            RenderTexture.active = null;

            var path = Path.Combine(outputDir, "probe_" + s.name + ".png");
            try
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log("[PROBE] Wrote " + path);
            }
            catch (Exception e)
            {
                Debug.LogError("[PROBE] Failed writing " + path + ": " + e.Message);
            }

            Destroy(tex);
            yield return null;
        }

        probeCamera.targetTexture = null;
        Destroy(rt);

        Debug.Log("[PROBE] Complete (" + Shots.Length + " shots).");
        yield return new WaitForSecondsRealtime(0.4f);

        Application.Quit();
    }
}
