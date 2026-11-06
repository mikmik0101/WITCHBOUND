using UnityEngine;

/// <summary>
/// Primitive construction helpers. Everything in the town is built from
/// Unity built-in primitives (extremely low poly and always serializable).
/// Colliders are opt-in so decorative geometry stays cheap.
/// </summary>
public static class ProcBuild
{
    public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 scale,
        Material mat, bool collider = false, bool castShadows = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        t.localScale = scale;

        if (!collider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static GameObject BoxRot(Transform parent, string name, Vector3 localPos, Vector3 scale,
        Vector3 euler, Material mat, bool collider = false, bool castShadows = true)
    {
        var go = Box(parent, name, localPos, scale, mat, collider, castShadows);
        go.transform.localEulerAngles = euler;
        return go;
    }

    public static GameObject Cyl(Transform parent, string name, Vector3 localPos, float radius, float height,
        Material mat, bool collider = false, bool castShadows = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        t.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

        if (!collider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static GameObject CylRot(Transform parent, string name, Vector3 localPos, float radius, float height,
        Vector3 euler, Material mat, bool collider = false, bool castShadows = true)
    {
        var go = Cyl(parent, name, localPos, radius, height, mat, collider, castShadows);
        go.transform.localEulerAngles = euler;
        return go;
    }

    public static GameObject Sphere(Transform parent, string name, Vector3 localPos, Vector3 scale,
        Material mat, bool collider = false, bool castShadows = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        t.localScale = scale;

        if (!collider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static GameObject Quad(Transform parent, string name, Vector3 localPos, Vector2 size,
        Vector3 euler, Material mat, bool castShadows = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        t.localScale = new Vector3(size.x, size.y, 1f);
        t.localEulerAngles = euler;

        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    /// <summary>Flat disc on the ground (uses a squashed cylinder).</summary>
    public static GameObject Disc(Transform parent, string name, Vector3 localPos, float radius,
        Material mat, bool shadows = false)
    {
        return Cyl(parent, name, localPos, radius, 0.03f, mat, false, shadows);
    }
}
