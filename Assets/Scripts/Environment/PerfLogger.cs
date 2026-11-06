using System;
using UnityEngine;

/// <summary>
/// Lightweight frame-rate logger for build verification. Only created when
/// the player is launched with "-fpslog"; it does nothing in a normal session.
/// Logs the average FPS over 5-second windows so the number reflects a steady
/// state rather than the first few (slow) frames.
/// </summary>
public class PerfLogger : MonoBehaviour
{
    private float windowTime;
    private int frames;
    private float elapsed;
    private const float Window = 5f;
    private const float TotalSeconds = 25f;

    public static bool Requested
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-fpslog") return true;
            return false;
        }
    }

    private void Update()
    {
        if (elapsed >= TotalSeconds) return;

        windowTime += Time.unscaledDeltaTime;
        elapsed += Time.unscaledDeltaTime;
        frames++;

        if (windowTime >= Window)
        {
            float fps = frames / windowTime;
            Debug.Log($"[PERF] {elapsed:0}s  avg {fps:0.0} fps  ({Time.deltaTime * 1000f:0.0} ms frame)");
            windowTime = 0f;
            frames = 0;
        }
    }
}
