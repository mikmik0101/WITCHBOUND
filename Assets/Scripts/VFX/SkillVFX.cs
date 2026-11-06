using UnityEngine;

/// <summary>
/// Lightweight, guaranteed-visible skill VFX built from primitives so it works
/// without any imported particle assets or package shaders (Intel HD 620
/// friendly). An expanding cyan ring, a rising glow column, an outward shard
/// burst and a brief light flash. Self-destructs after ~1.1s.
/// </summary>
public class SkillVFX : MonoBehaviour
{
    private float _t;
    private float _duration = 1.1f;
    private Transform _ring;
    private Transform _column;
    private Transform[] _shards;
    private Vector3[] _shardDirs;
    private Light _light;
    private Material _ringMat, _columnMat, _shardMat;

    public static GameObject Spawn(Vector3 position, Color color)
    {
        var root = new GameObject("SkillVFX_PhaseShift");
        root.transform.position = position;
        var vfx = root.AddComponent<SkillVFX>();
        vfx.Build(color);
        return root;
    }

    private void Build(Color color)
    {
        Color core = Color.Lerp(color, Color.white, 0.55f);

        // expanding ground ring
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        ring.transform.localScale = new Vector3(0.4f, 0.02f, 0.4f);
        StripCol(ring);
        _ring = ring.transform;
        _ringMat = RuntimeCharacterFactory.Emissive(core, 1f, true);
        ring.GetComponent<Renderer>().sharedMaterial = _ringMat;
        ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // rising glow column
        var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        col.name = "Column";
        col.transform.SetParent(transform, false);
        col.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        col.transform.localScale = new Vector3(0.30f, 0.9f, 0.30f);
        StripCol(col);
        _column = col.transform;
        _columnMat = RuntimeCharacterFactory.Emissive(core, 1f, true);
        col.GetComponent<Renderer>().sharedMaterial = _columnMat;
        col.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // outward shard burst
        int n = 12;
        _shards = new Transform[n];
        _shardDirs = new Vector3[n];
        _shardMat = RuntimeCharacterFactory.Emissive(core, 1f, true);
        for (int i = 0; i < n; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.name = "Shard" + i;
            s.transform.SetParent(transform, false);
            s.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            s.transform.localScale = Vector3.one * 0.07f;
            StripCol(s);
            s.GetComponent<Renderer>().sharedMaterial = _shardMat;
            s.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float a = (i / (float)n) * Mathf.PI * 2f;
            _shardDirs[i] = new Vector3(Mathf.Cos(a), 0.15f + Random.value * 0.35f, Mathf.Sin(a));
            _shards[i] = s.transform;
        }

        // flash light
        var lgo = new GameObject("Flash");
        lgo.transform.SetParent(transform, false);
        lgo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        _light = lgo.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.color = core;
        _light.range = 14f;
        _light.intensity = 6f;
        _light.shadows = LightShadows.None;

        Debug.Log("[VFX] Phase Shift VFX spawned at " + transform.position);
    }

    private static void StripCol(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _duration);

        if (_ring != null)
        {
            float s = Mathf.Lerp(1.0f, 8.5f, k);
            _ring.localScale = new Vector3(s, 0.03f, s);
        }
        if (_column != null)
        {
            float s = Mathf.Lerp(0.30f, 0.8f, k);
            _column.localScale = new Vector3(s, Mathf.Lerp(0.9f, 2.0f, k), s);
        }
        if (_shards != null)
        {
            for (int i = 0; i < _shards.Length; i++)
            {
                if (_shards[i] == null) continue;
                _shards[i].localPosition += _shardDirs[i] * (4f * Time.deltaTime);
                _shards[i].localScale = Vector3.one * Mathf.Lerp(0.08f, 0.01f, k);
            }
        }
        if (_light != null) _light.intensity = Mathf.Lerp(6f, 0f, k);

        // fade out by shrinking the whole effect near the end
        if (k > 0.65f)
        {
            float f = Mathf.Lerp(1f, 0.02f, (k - 0.65f) / 0.35f);
            transform.localScale = Vector3.one * f;
        }

        if (_t >= _duration) Destroy(gameObject);
    }
}
