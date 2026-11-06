using UnityEngine;

/// <summary>
/// Lightweight, guaranteed-visible melee impact flash.
///
/// One emissive sphere + one short-lived point light, both removed after ~0.25 s.
/// Deliberately much cheaper than SkillVFX so up to six witches can connect
/// without spawning dozens of primitives per second on Intel-class GPUs. The
/// shared material is created once, so hits do not leak materials.
/// </summary>
public class ImpactVFX : MonoBehaviour
{
    private static Material _sharedMaterial;

    private float _t;
    private const float Duration = 0.25f;
    private Transform _sphere;
    private Light _light;

    public static void Spawn(Vector3 position, Color color)
    {
        var root = new GameObject("WitchImpact");
        root.transform.position = position;
        root.AddComponent<ImpactVFX>().Build(color);
    }

    private void Build(Color color)
    {
        if (_sharedMaterial == null)
        {
            var shader = Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");
            _sharedMaterial = new Material(shader) { color = Color.white };
        }
        // Impact colour is the same blood red for every hit; sharing one material
        // keeps the cost flat instead of instantiating a material per attack.
        _sharedMaterial.color = color;

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Flash";
        var col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * 0.55f;
        var rend = sphere.GetComponent<Renderer>();
        rend.sharedMaterial = _sharedMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        _sphere = sphere.transform;

        var lgo = new GameObject("Light");
        lgo.transform.SetParent(transform, false);
        _light = lgo.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.color = color;
        _light.range = 5f;
        _light.intensity = 3.5f;
        _light.shadows = LightShadows.None;
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / Duration);

        if (_sphere != null)
        {
            // quick pop then collapse
            float s = k < 0.35f ? Mathf.Lerp(0.45f, 1.15f, k / 0.35f)
                               : Mathf.Lerp(1.15f, 0.05f, (k - 0.35f) / 0.65f);
            _sphere.localScale = Vector3.one * s;
        }
        if (_light != null) _light.intensity = Mathf.Lerp(3.5f, 0f, k);

        if (_t >= Duration) Destroy(gameObject);
    }
}
