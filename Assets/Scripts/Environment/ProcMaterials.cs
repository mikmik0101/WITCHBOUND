using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Shared, cached material library for all procedural environment content.
/// Materials are created once per key and reused to keep draw calls low
/// (critical for the Intel HD 620 target).
/// </summary>
public static class ProcMaterials
{
    private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    private static Shader standardShader;
    private static Shader unlitShader;
    private static Shader lineShader;
    private static Shader softFogShader;
    private static Shader nightSkyShader;

    public static void ResetCache()
    {
        foreach (var m in cache.Values)
        {
            if (m != null) Object.Destroy(m);
        }
        cache.Clear();

        standardShader = Shader.Find("Standard");
        unlitShader = Shader.Find("Unlit/Color");
        lineShader = Shader.Find("Sprites/Default");
        softFogShader = Shader.Find("WITCHBOUND/SoftFog");
        nightSkyShader = Shader.Find("WITCHBOUND/NightSky");

        if (standardShader == null)
            Debug.LogError("[PROC] Standard shader not found - environment will render pink.");
        if (softFogShader == null)
            Debug.LogWarning("[PROC] WITCHBOUND/SoftFog shader not found - fog will be disabled.");
        if (nightSkyShader == null)
            Debug.LogWarning("[PROC] WITCHBOUND/NightSky shader not found - using flat sky.");
    }

    private static Shader StandardShader
    {
        get
        {
            if (standardShader == null) standardShader = Shader.Find("Standard");
            return standardShader;
        }
    }

    public static Material Lit(string key, Color color, float smoothness = 0.04f)
    {
        if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

        var mat = new Material(StandardShader);
        mat.name = "Proc_" + key;
        mat.color = color;
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        mat.enableInstancing = true;
        cache[key] = mat;
        return mat;
    }

    public static Material Emissive(string key, Color color, Color emission, float intensity)
    {
        if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

        var mat = new Material(StandardShader);
        mat.name = "Proc_" + key;
        mat.color = color;
        mat.SetFloat("_Glossiness", 0.1f);
        mat.SetFloat("_Metallic", 0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        mat.enableInstancing = true;
        cache[key] = mat;
        return mat;
    }

    public static Material Unlit(string key, Color color)
    {
        if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

        var shader = unlitShader != null ? unlitShader : Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.name = "Proc_" + key;
        mat.color = color;
        cache[key] = mat;
        return mat;
    }

    /// <summary>Soft alpha-blended Standard material (curtains, glass, cloth).</summary>
    public static Material Transparent(string key, Color color, float alpha)
    {
        if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

        var mat = new Material(StandardShader);
        mat.name = "Proc_" + key;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_Mode", 3f); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetFloat("_Glossiness", 0f);
        mat.SetFloat("_Metallic", 0f);
        var c = color;
        c.a = alpha;
        mat.color = c;
        cache[key] = mat;
        return mat;
    }

    /// <summary>Alpha-blended Standard material with a generated texture (chain-link).</summary>
    public static Material TransparentTextured(string key, Color color, float alpha, Texture2D texture)
    {
        if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

        var mat = new Material(StandardShader);
        mat.name = "Proc_" + key;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetFloat("_Glossiness", 0.1f);
        mat.SetFloat("_Metallic", 0.1f);
        mat.mainTexture = texture;
        var c = color;
        c.a = alpha;
        mat.color = c;
        cache[key] = mat;
        return mat;
    }

    /// <summary>Soft, unlit, radial-falloff sheet material for fog pockets.</summary>
    public static Material SoftFog(string key, Color color, float falloff = 1.8f, bool additive = false)
    {
        if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

        var shader = softFogShader != null ? softFogShader : Shader.Find("WITCHBOUND/SoftFog");
        if (shader == null) shader = standardShader;

        var mat = new Material(shader);
        mat.name = "Proc_" + key;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        else mat.color = color;
        if (mat.HasProperty("_Falloff")) mat.SetFloat("_Falloff", falloff);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)(additive
                ? UnityEngine.Rendering.BlendMode.One
                : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        cache[key] = mat;
        return mat;
    }

    public static Material Line(string key, Color color)
    {
        if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

        var shader = lineShader != null ? lineShader : Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.name = "Proc_" + key;
        mat.color = color;
        cache[key] = mat;
        return mat;
    }

    public static Material NightSky()
    {
        var shader = nightSkyShader != null ? nightSkyShader : Shader.Find("WITCHBOUND/NightSky");
        if (shader == null) return null;

        var mat = new Material(shader);
        mat.name = "Proc_NightSky";
        return mat;
    }
}
