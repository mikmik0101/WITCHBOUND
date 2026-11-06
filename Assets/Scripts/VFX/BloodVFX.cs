using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight blood feedback for a dark horror chase on an Intel HD 620.
///
/// Everything here is pooled and self-limiting:
///   * one ParticleSystem handles every impact burst (max 240 particles),
///   * a fixed pool of 24 ground decals is reused in a ring buffer,
///   * no post-processing, no per-hit material instancing, no allocations
///     after the first few frames.
///
/// Nothing spawns on its own: every effect is requested by a real gameplay
/// event (a witch landing a hit, the player taking damage, a companion being
/// wounded), so the screen never fills with blood for no reason.
/// </summary>
public static class BloodVFX
{
    private const int MaxDecals = 24;
    private const int SplatVariants = 4;
    private const float DecalLife = 22f;
    private const float DecalFade = 2.5f;

    /// <summary>
    /// How far above the raycast surface a stain sits. The ray hits the ground
    /// *collider*, which sits below the visible ground mesh in places, so this
    /// has to clear the mesh rather than just avoid z-fighting.
    /// </summary>
    private const float DecalLift = 0.06f;

    /// <summary>QA-only: logs the ground raycast for every stain (set by NavProbe).</summary>
    public static bool VerboseGroundRay;

    private static GameObject _root;
    private static GameObject _decalRoot;
    private static ParticleSystem _impact;
    private static ParticleSystemRenderer _impactRenderer;

    private static readonly List<Transform> _decals = new List<Transform>(MaxDecals);
    private static readonly List<MeshRenderer> _decalRenderers = new List<MeshRenderer>(MaxDecals);
    private static readonly List<float> _decalBorn = new List<float>(MaxDecals);
    private static readonly List<float> _decalBaseScale = new List<float>(MaxDecals);
    private static int _decalCursor;
    private static Material[] _decalMaterials;
    private static Material _impactMaterial;

    private static readonly Color BloodDark = new Color(0.34f, 0.020f, 0.024f);
    private static readonly Color BloodBright = new Color(0.55f, 0.035f, 0.040f);

    // =====================================================================
    // public API
    // =====================================================================

    /// <summary>
    /// A short spray at the exact impact point. <paramref name="away"/> is the
    /// direction the blood should travel (usually from the victim to the
    /// attacker); <paramref name="scale"/> scales count and speed.
    /// </summary>
    public static void SpawnImpact(Vector3 worldPos, Vector3 away, float scale = 1f)
    {
        EnsureImpact();

        if (away.sqrMagnitude < 0.0001f) away = Vector3.up;
        away.Normalize();

        _root.transform.position = worldPos;
        _root.transform.rotation = Quaternion.LookRotation(away, Vector3.up);

        int count = Mathf.Clamp(Mathf.RoundToInt(18f * Mathf.Max(0.4f, scale)), 8, 60);
        _impact.Emit(count);
    }

    /// <summary>
    /// A small stain on the ground under an impact. The pool grows on demand
    /// and then ring-buffers, so a long round can never accumulate hundreds.
    /// </summary>
    public static void SpawnGroundDecal(Vector3 worldPos, float size = 1f)
    {
        EnsureDecals();

        float y = worldPos.y;
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(worldPos + Vector3.up * 2.5f, Vector3.down, out hit, 6f,
                ~0, QueryTriggerInteraction.Ignore);
        if (hitSomething)
            y = hit.point.y;

        if (VerboseGroundRay)
            Debug.Log("[BLOOD] ground ray from " + worldPos.ToString("0.00") + " hit=" +
                      (hitSomething ? hit.collider.name + " y=" + y.ToString("0.000") : "nothing") +
                      " -> decal y=" + (y + DecalLift).ToString("0.000"));

        int index;
        if (_decals.Count < MaxDecals)
        {
            index = _decals.Count;
            _decals.Add(CreateDecal(index));
            _decalBorn.Add(0f);
            _decalBaseScale.Add(1f);
        }
        else
        {
            index = _decalCursor;
            _decalCursor = (_decalCursor + 1) % MaxDecals;
        }

        Transform d = _decals[index];
        _decalBorn[index] = Time.time;

        // Small, irregular splatter rather than a broad oval. The texture only
        // fills the middle of the quad, so a 0.6-0.8 hit (a person taking
        // damage) leaves a stain roughly 30-45 cm across - big enough to read
        // from a few metres, far smaller than the old painted discs.
        float s = Mathf.Clamp(size, 0.30f, 1.60f) * Random.Range(0.85f, 1.30f);
        _decalBaseScale[index] = s;

        // Re-roll the splat shape on every spawn so repeated hits on the same
        // spot never stack identical silhouettes.
        var mr = _decalRenderers[index];
        if (mr != null && _decalMaterials != null && _decalMaterials.Length > 0)
            mr.sharedMaterial = _decalMaterials[Random.Range(0, _decalMaterials.Length)];

        d.gameObject.SetActive(true);
        d.position = new Vector3(worldPos.x, y + DecalLift, worldPos.z);
        d.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        d.localScale = new Vector3(s, s, 1f);
    }

    /// <summary>Clears every stain and stops the emitter (new round / restart).</summary>
    public static void ClearAll()
    {
        if (_impact != null) _impact.Clear();

        for (int i = 0; i < _decals.Count; i++)
            if (_decals[i] != null) _decals[i].gameObject.SetActive(false);

        _decalCursor = 0;
    }

    /// <summary>
    /// QA-only: drops a single stain quad in mid-air with no ground raycast.
    /// A ground stain that draws nothing could be a material fault or a depth
    /// fault (buried under a second ground mesh); this separates the two.
    /// </summary>
    public static void SpawnDebugQuad(Vector3 pos, Vector3 facing, float size)
    {
        EnsureDecals();

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BloodDebugQuad";
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sharedMaterial = _decalMaterials[0];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        go.transform.position = pos;
        if (facing.sqrMagnitude > 0.001f) go.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        go.transform.localScale = new Vector3(size, size, 1f);
        Debug.Log("[BLOOD] debug quad at " + pos.ToString("0.00") + " size=" + size.ToString("0.00") +
                  " visible=" + (r != null && r.isVisible));
    }

    // =====================================================================
    // setup
    // =====================================================================

    private static void EnsureRoot()
    {
        if (_root != null) return;

        _root = new GameObject("BloodVFX");
        Object.DontDestroyOnLoad(_root);
    }

    /// <summary>
    /// Stains live under their own root. The impact emitter is moved to each
    /// hit position, and anything parented to it would be dragged along with
    /// it - previously every stain in the world slid to the newest impact.
    /// </summary>
    private static Transform DecalRoot()
    {
        if (_decalRoot != null) return _decalRoot.transform;

        _decalRoot = new GameObject("BloodDecals");
        Object.DontDestroyOnLoad(_decalRoot);
        _decalRoot.transform.position = Vector3.zero;
        _decalRoot.transform.rotation = Quaternion.identity;
        return _decalRoot.transform;
    }

    private static void EnsureImpact()
    {
        EnsureRoot();

        if (_impact != null) return;

        var go = new GameObject("BloodImpact");
        go.transform.SetParent(_root.transform, false);

        _impact = go.AddComponent<ParticleSystem>();
        _impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _impact.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.085f);
        main.startColor = new ParticleSystem.MinMaxGradient(BloodDark, BloodBright);
        main.gravityModifier = 1.55f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 240;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = _impact.emission;
        emission.enabled = false;                 // bursts are explicit Emit() calls

        var shape = _impact.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 42f;
        shape.radius = 0.06f;

        var colorOverLifetime = _impact.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.55f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = _impact.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0.45f));

        _impactRenderer = go.GetComponent<ParticleSystemRenderer>();
        _impactRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        _impactRenderer.material = ImpactMaterial();
        _impactRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _impactRenderer.receiveShadows = false;

        _impact.Play();
    }

    private static Material ImpactMaterial()
    {
        if (_impactMaterial != null) return _impactMaterial;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        _impactMaterial = new Material(shader);
        _impactMaterial.name = "BloodImpactMat";
        if (_impactMaterial.HasProperty("_Color")) _impactMaterial.SetColor("_Color", Color.white);
        _impactMaterial.renderQueue = 3000;
        return _impactMaterial;
    }

    private static void EnsureDecals()
    {
        EnsureRoot();

        if (_decalMaterials == null || _decalMaterials.Length != SplatVariants)
        {
            _decalMaterials = new Material[SplatVariants];
            for (int i = 0; i < SplatVariants; i++)
            {
                var tex = BuildSplatterTexture(i);
                _decalMaterials[i] = DecalMaterial("BloodSplatMat_" + i, tex);
            }
        }

        if (_root.GetComponent<BloodVFXRunner>() == null)
            _root.AddComponent<BloodVFXRunner>();
    }

    private static Transform CreateDecal(int index)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BloodDecal_" + index;
        go.transform.SetParent(DecalRoot(), false);

        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sharedMaterial = _decalMaterials != null && _decalMaterials.Length > 0
                ? _decalMaterials[index % _decalMaterials.Length]
                : DecalMaterial("BloodSplatMat_fallback", BuildSplatterTexture(0));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        _decalRenderers.Add(r);
        return go.transform;
    }

    /// <summary>
    /// Unlit alpha-blended material. The scene is almost black at night, so a
    /// lit Standard material turned the stains invisible in unlit streets; an
    /// unlit one keeps a dark, non-glowing splat readable everywhere.
    /// </summary>
    private static Material DecalMaterial(string name, Texture2D texture)
    {
        // A project shader, not a built-in one: Unlit/Transparent is stripped
        // out of the player build, and Sprites/Default multiplies by the mesh
        // vertex colour, which the built-in quad does not have, so both
        // rendered nothing. WITCHBOUND/BloodDecal samples the texture directly
        // and is registered in Always Included Shaders so it always ships.
        var shader = Shader.Find("WITCHBOUND/BloodDecal");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        var mat = new Material(shader);
        mat.name = name;
        mat.mainTexture = texture;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_AlphaMul")) mat.SetFloat("_AlphaMul", 1f);
        mat.renderQueue = 3000;
        Debug.Log("[BLOOD] decal material '" + name + "' shader=" + (shader != null ? shader.name : "NULL") +
                  " centre=" + texture.GetPixel(texture.width / 2, texture.height / 2).ToString("0.00") +
                  " corner=" + texture.GetPixel(1, 1).ToString("0.00"));
        return mat;
    }

    /// <summary>
    /// Builds one 64x64 splatter. A stain from a hit is directional - a body
    /// dragged along the throw with thin tails and droplets trailing off it -
    /// so the shape is built from three shrinking lobes on one axis, five thin
    /// streaks thrown outward, and a scatter of detached droplets. The whole
    /// silhouette is then eroded with value noise so no edge is a clean arc.
    /// Colour is baked in; four variants is 16 KB of texture in total.
    /// </summary>
    private static Texture2D BuildSplatterTexture(int variant)
    {
        const int Size = 64;
        var rng = new System.Random(variant * 7919 + 104729);

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
        tex.name = "BloodSplat_" + variant;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // Direction the blood was thrown.
        float dir = (float)(rng.NextDouble() * System.Math.PI * 2.0);
        Vector2 axis = new Vector2(Mathf.Cos(dir), Mathf.Sin(dir));

        // --- body: three shrinking lobes along the throw direction ------
        const int Lobes = 3;
        var lc = new Vector2[Lobes];
        var lr = new float[Lobes];
        var lw = new float[Lobes];
        for (int i = 0; i < Lobes; i++)
        {
            float t = i / (float)(Lobes - 1);
            float along = Mathf.Lerp(-0.025f, 0.10f, t);
            float side = ((float)rng.NextDouble() - 0.5f) * 0.05f;
            lc[i] = new Vector2(0.5f + axis.x * along - axis.y * side,
                                0.5f + axis.y * along + axis.x * side);
            lr[i] = Mathf.Lerp(0.125f, 0.052f, t) + (float)rng.NextDouble() * 0.020f;
            lw[i] = 0.60f + (float)rng.NextDouble() * 0.22f;   // squashed sideways
        }

        // --- tails: thin streaks thrown out of the body -----------------
        const int Tails = 5;
        var tc = new Vector2[Tails];
        var tu = new Vector2[Tails];
        var tl = new float[Tails];
        var tt = new float[Tails];
        for (int i = 0; i < Tails; i++)
        {
            float a = dir + ((float)rng.NextDouble() - 0.5f) * 2.6f;
            tu[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float len = 0.10f + (float)rng.NextDouble() * 0.13f;
            float start = 0.05f + (float)rng.NextDouble() * 0.04f;
            tc[i] = new Vector2(0.5f, 0.5f) + tu[i] * (start + len);
            tl[i] = len;
            tt[i] = 0.014f + (float)rng.NextDouble() * 0.014f;
        }

        // --- detached droplets -------------------------------------------
        const int Drops = 9;
        var dc = new Vector2[Drops];
        var dr = new float[Drops];
        for (int i = 0; i < Drops; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            float dist = 0.19f + (float)rng.NextDouble() * 0.22f;
            dc[i] = new Vector2(0.5f + Mathf.Cos(a) * dist, 0.5f + Mathf.Sin(a) * dist);
            dr[i] = 0.009f + (float)rng.NextDouble() * 0.026f;
        }

        var px = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            float v = (y + 0.5f) / Size;
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size;
                var p = new Vector2(u, v);

                float field = 0f;

                for (int i = 0; i < Lobes; i++)
                {
                    Vector2 d = p - lc[i];
                    float alongC = d.x * axis.x + d.y * axis.y;
                    float sideC = -d.x * axis.y + d.y * axis.x;
                    float k = Mathf.Sqrt(alongC * alongC + (sideC / lw[i]) * (sideC / lw[i])) / lr[i];
                    if (1f - k > field) field = 1f - k;
                }

                for (int i = 0; i < Tails; i++)
                {
                    Vector2 d = p - tc[i];
                    float a = d.x * tu[i].x + d.y * tu[i].y;
                    float b = -d.x * tu[i].y + d.y * tu[i].x;
                    float k = Mathf.Sqrt((a / tl[i]) * (a / tl[i]) + (b / tt[i]) * (b / tt[i]));
                    if (1f - k > field) field = 1f - k;
                }

                for (int i = 0; i < Drops; i++)
                {
                    Vector2 d = p - dc[i];
                    float k = d.magnitude / dr[i];
                    if (1f - k > field) field = 1f - k;
                }

                if (field <= 0f)
                {
                    px[y * Size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                // Ragged rim plus a little internal mottling, so the stain is
                // never a flat disc.
                float n = Hash(u * 41.7f + variant * 3.1f, v * 47.3f + variant * 7.7f);
                float n2 = Hash(u * 11.3f - variant * 5.3f, v * 13.9f + variant * 2.1f);

                float shaped = field * (0.62f + 0.52f * n) - 0.020f * n2;
                float alpha = Mathf.Clamp01(shaped / 0.22f);
                alpha = alpha * alpha * (3f - 2f * alpha);          // smoothstep

                // Denser core, thinner rim, so it reads as a stain, not a decal.
                alpha *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(field * 2f));
                alpha *= 0.82f + 0.18f * n2;
                if (alpha < 0.02f)
                {
                    px[y * Size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                float t = Mathf.Clamp01(field);
                px[y * Size + x] = new Color32(
                    (byte)(66f + 34f * t),
                    (byte)(8f + 6f * t),
                    (byte)(9f + 7f * t),
                    (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);
        return tex;
    }

    private static float Hash(float x, float y)
    {
        float h = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    /// <summary>
    /// Ages the stains and shrinks them away. Called every frame by
    /// <see cref="BloodVFXRunner"/>, which only exists once a stain has spawned.
    /// </summary>
    internal static void TickDecals()
    {
        float now = Time.time;

        for (int i = 0; i < _decals.Count; i++)
        {
            var d = _decals[i];
            if (d == null || !d.gameObject.activeSelf) continue;

            float age = now - _decalBorn[i];
            if (age < DecalLife) continue;

            float k = Mathf.Clamp01((age - DecalLife) / DecalFade);
            if (k >= 1f)
            {
                d.gameObject.SetActive(false);
                continue;
            }

            float s = _decalBaseScale[i] * (1f - k);
            d.localScale = new Vector3(s, s, 1f);
        }
    }
}
