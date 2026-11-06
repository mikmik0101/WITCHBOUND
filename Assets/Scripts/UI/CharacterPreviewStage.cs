using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Off-screen 3D preview used by Character Select / Bestiary.
/// Builds a dedicated camera + light on an isolated layer (31) and renders to a
/// RenderTexture shown in a RawImage, so the actual procedural models' faces can
/// be seen without them appearing anywhere in the world.
/// </summary>
public class CharacterPreviewStage
{
    public const int PreviewLayer = 31;

    public Camera cam;
    public RenderTexture rt;
    public Transform stageRoot;
    public GameObject current;

    private float _phase;

    public static CharacterPreviewStage Create(RawImage target)
    {
        var stage = new CharacterPreviewStage();

        // keep preview models invisible to the gameplay camera
        if (Camera.main != null)
            Camera.main.cullingMask &= ~(1 << PreviewLayer);

        var root = new GameObject("CharacterPreviewStage");
        root.transform.position = new Vector3(0f, -320f, 0f);
        stage.stageRoot = root.transform;

        var camGO = new GameObject("PreviewCamera");
        camGO.transform.SetParent(root.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 0.98f, -3.3f);
        camGO.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.035f, 0.04f, 0.055f, 1f);
        cam.cullingMask = 1 << PreviewLayer;
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.depth = -10;
        stage.cam = cam;

        stage.rt = new RenderTexture(512, 512, 16);
        stage.rt.Create();
        cam.targetTexture = stage.rt;

        var key = new GameObject("PreviewKey");
        key.transform.SetParent(root.transform, false);
        key.transform.localRotation = Quaternion.Euler(35f, 25f, 0f);
        var kl = key.AddComponent<Light>();
        kl.type = LightType.Directional;
        kl.intensity = 1.15f;
        kl.color = new Color(1f, 0.96f, 0.9f);
        kl.cullingMask = 1 << PreviewLayer;
        kl.shadows = LightShadows.None;

        var fill = new GameObject("PreviewFill");
        fill.transform.SetParent(root.transform, false);
        fill.transform.localRotation = Quaternion.Euler(10f, -140f, 0f);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.intensity = 0.55f;
        fl.color = new Color(0.6f, 0.7f, 0.95f);
        fl.cullingMask = 1 << PreviewLayer;
        fl.shadows = LightShadows.None;

        if (target != null) target.texture = stage.rt;
        return stage;
    }

    public void Show(GameObject model)
    {
        if (current != null) Object.Destroy(current);
        current = model;
        if (model == null) return;
        model.transform.SetParent(stageRoot, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // face the camera
        SetLayerRecursive(model, PreviewLayer);
        _phase = 0f;
    }

    public void Tick()
    {
        if (current == null) return;
        // gentle sway keeps the face visible while showing depth
        _phase += Time.unscaledDeltaTime;
        float yaw = Mathf.Sin(_phase * 0.7f) * 22f;
        current.transform.localRotation = Quaternion.Euler(0f, 180f + yaw, 0f);
    }

    public void SetActive(bool active)
    {
        if (cam != null) cam.enabled = active;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursive(t.gameObject, layer);
    }
}
