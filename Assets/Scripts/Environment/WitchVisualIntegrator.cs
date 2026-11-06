using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Adds a faint supernatural aura to witches without touching WitchAI,
/// detection, navigation or attack logic. Only the nearest witch within range
/// keeps a live light, so the extra cost is capped at one point light.
/// </summary>
public class WitchVisualIntegrator : MonoBehaviour
{
    private class Tracked
    {
        public WitchAI ai;
        public Light aura;
    }

    private readonly List<Tracked> tracked = new List<Tracked>();
    private float scanTimer;
    private float refreshTimer;

    private void Start()
    {
        Scan();
    }

    private void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= 4f)
        {
            scanTimer = 0f;
            Scan();
        }

        refreshTimer += Time.deltaTime;
        if (refreshTimer < 0.2f) return;
        refreshTimer = 0f;
        RefreshNearestAura();
    }

    private void Scan()
    {
        var witches = Object.FindObjectsOfType<WitchAI>();
        for (int i = 0; i < witches.Length; i++)
        {
            if (HasTracked(witches[i])) continue;
            AddAura(witches[i]);
        }
    }

    private bool HasTracked(WitchAI ai)
    {
        for (int i = 0; i < tracked.Count; i++)
        {
            if (tracked[i] == null) continue;
            if (tracked[i].ai == ai) return true;
        }
        return false;
    }

    private void AddAura(WitchAI ai)
    {
        if (ai == null) return;

        Transform parent = ai.headTransform != null ? ai.headTransform : ai.transform;
        var auraObj = new GameObject("WitchAura");
        auraObj.transform.SetParent(parent, false);
        auraObj.transform.localPosition = ai.headTransform != null
            ? new Vector3(0f, 0f, 0.35f)
            : new Vector3(0f, 2.1f, 0f);

        var light = auraObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.85f, 0.10f, 0.09f);
        light.intensity = 0f;
        light.range = 6.5f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        light.bounceIntensity = 0f;

        var flicker = auraObj.AddComponent<FlickerLight>();
        flicker.target = light;
        flicker.baseIntensity = 0.55f;
        flicker.minIntensity = 0.18f;
        flicker.speed = 3.2f;
        flicker.seed = 137 + tracked.Count * 97;

        tracked.Add(new Tracked { ai = ai, aura = light });
    }

    private void RefreshNearestAura()
    {
        Vector3 playerPos = Vector3.zero;
        bool hasPlayer = GameManager.Instance != null && GameManager.Instance.player != null;
        if (hasPlayer) playerPos = GameManager.Instance.player.transform.position;

        Tracked nearest = null;
        float nearestDist = 26f;

        for (int i = 0; i < tracked.Count; i++)
        {
            var t = tracked[i];
            if (t == null || t.ai == null || t.aura == null) continue;

            // Only one aura is allowed to be a live light at a time.
            t.aura.enabled = false;

            float d = hasPlayer ? Vector3.Distance(playerPos, t.ai.transform.position) : 999f;
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = t;
            }
        }

        if (nearest != null && nearest.aura != null)
        {
            // Let the flicker component own the intensity while nearby.
            nearest.aura.enabled = true;
            var flicker = nearest.aura.GetComponent<FlickerLight>();
            if (flicker == null && nearest.aura.intensity <= 0.001f)
                nearest.aura.intensity = 0.45f;
        }
    }
}
