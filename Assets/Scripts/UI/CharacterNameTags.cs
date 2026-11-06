using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floating name tags for every character that carries a CharacterVisual:
/// the three AI companions and all six witches. The player's own body has no
/// CharacterVisual (first person), so no tag is ever drawn in front of the
/// camera.
///
/// Design notes:
///  - one shared overlay canvas, sortingOrder 50 (below UI_Canvas at 100), so
///    menus and the HUD always draw on top of the tags;
///  - no GraphicRaycaster, so the tags can never intercept a click, a mouse-look
///    event or a button;
///  - labels are created once per character and reused, never per frame;
///  - names come from CharacterVisual.displayName, which the factory fills from
///    the same identity data the rest of the game uses.
/// </summary>
public class CharacterNameTags : MonoBehaviour
{
    public static CharacterNameTags Instance { get; private set; }

    [Header("Visibility")]
    public float maxDistance = 32f;
    public float fadeStartDistance = 26f;
    public float headOffset = 0.38f;
    public int fontSize = 16;

    private static readonly Color SurvivorColor = new Color(0.86f, 0.90f, 0.96f);
    private static readonly Color WitchColor = new Color(0.87f, 0.31f, 0.28f);
    private static readonly Color SurvivorRule = new Color(0.55f, 0.68f, 0.80f, 0.70f);
    private static readonly Color WitchRule = new Color(0.62f, 0.10f, 0.12f, 0.80f);

    private class Label
    {
        public GameObject root;
        public RectTransform rect;
        public CanvasGroup group;
        public Text text;
        public Image underline;
        public CharacterVisual target;
        public int stamp;
    }

    private GameObject _canvasGO;
    private Camera _camera;
    private float _nextCameraLookup;
    private readonly List<Label> _labels = new List<Label>();
    private readonly Dictionary<CharacterVisual, Label> _byTarget = new Dictionary<CharacterVisual, Label>();
    private int _stamp;
    private float _nextPrune;

    // =====================================================================
    // lifecycle
    // =====================================================================

    public static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("CharacterNameTags");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<CharacterNameTags>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _canvasGO = new GameObject("NameTagCanvas");
        _canvasGO.transform.SetParent(transform, false);

        var canvas = _canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = _canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Deliberately no GraphicRaycaster: tags must never block input.

        Debug.Log("[NAMETAGS] ready");
    }

    private void LateUpdate()
    {
        var gm = GameManager.Instance;
        bool show = gm != null
                    && (gm.currentState == GameState.Gameplay || gm.currentState == GameState.EscapePhase)
                    && !PrologueController.IsShowing;

        if (!show)
        {
            HideAll();
            return;
        }

        if (_camera == null && Time.unscaledTime >= _nextCameraLookup)
        {
            _nextCameraLookup = Time.unscaledTime + 1f;
            _camera = Camera.main;
            if (_camera == null) _camera = FindAnyObjectByType<Camera>();
        }
        if (_camera == null) { HideAll(); return; }

        _stamp++;

        var active = CharacterVisual.Active;
        for (int i = 0; i < active.Count; i++)
        {
            var v = active[i];
            if (v == null || !v.isActiveAndEnabled) continue;
            if (string.IsNullOrEmpty(v.displayName)) continue;

            // The selected survivor is the first-person camera: never label it.
            if (gm.player != null && v.transform.IsChildOf(gm.player.transform)) continue;

            Vector3 anchor = (v.headTransform != null ? v.headTransform.position : v.transform.position)
                             + Vector3.up * headOffset;

            float distance = Vector3.Distance(_camera.transform.position, anchor);
            if (distance > maxDistance) continue;

            Vector3 screen = _camera.WorldToScreenPoint(anchor);
            if (screen.z <= 0.05f) continue;
            if (screen.x < -60f || screen.x > Screen.width + 60f) continue;
            if (screen.y < -60f || screen.y > Screen.height + 60f) continue;

            var label = GetLabel(v);
            if (label == null) continue;

            label.stamp = _stamp;
            if (!label.root.activeSelf) label.root.SetActive(true);

            // Screen-space labels always face the camera by construction.
            label.rect.position = new Vector3(screen.x, screen.y, 0f);
            label.group.alpha = distance <= fadeStartDistance
                ? 1f
                : Mathf.InverseLerp(maxDistance, fadeStartDistance, distance);
        }

        for (int i = 0; i < _labels.Count; i++)
        {
            var l = _labels[i];
            if (l == null || l.stamp == _stamp) continue;
            if (l.root.activeSelf) l.root.SetActive(false);
        }

        if (Time.unscaledTime >= _nextPrune) Prune();
    }

    private void HideAll()
    {
        for (int i = 0; i < _labels.Count; i++)
        {
            var l = _labels[i];
            if (l != null && l.root.activeSelf) l.root.SetActive(false);
        }
    }

    /// <summary>Drops labels whose character was destroyed (restart, respawn).</summary>
    private void Prune()
    {
        _nextPrune = Time.unscaledTime + 2f;

        List<CharacterVisual> dead = null;
        foreach (var kv in _byTarget)
        {
            if (kv.Key != null) continue;
            if (dead == null) dead = new List<CharacterVisual>();
            dead.Add(kv.Key);
        }
        if (dead == null) return;

        for (int i = 0; i < dead.Count; i++)
        {
            Label label;
            if (!_byTarget.TryGetValue(dead[i], out label)) continue;
            _byTarget.Remove(dead[i]);
            if (label == null) continue;
            _labels.Remove(label);
            if (label.root != null) Destroy(label.root);
        }
    }

    // =====================================================================
    // labels
    // =====================================================================

    private Label GetLabel(CharacterVisual v)
    {
        Label label;
        if (_byTarget.TryGetValue(v, out label) && label != null && label.text != null)
        {
            if (label.text.text != v.displayName) ApplyText(label, v);
            return label;
        }

        label = CreateLabel();
        if (label == null) return null;
        _byTarget[v] = label;
        _labels.Add(label);
        ApplyText(label, v);
        Debug.Log($"[NAMETAGS] label created: {v.displayName} ({(v.isChaser ? "witch" : "survivor")})");
        return label;
    }

    private void ApplyText(Label label, CharacterVisual v)
    {
        label.target = v;
        label.text.text = v.displayName;
        label.text.color = v.isChaser ? WitchColor : SurvivorColor;
        label.underline.color = v.isChaser ? WitchRule : SurvivorRule;

        float width = Mathf.Clamp(label.text.preferredWidth + 20f, 60f, 340f);
        label.rect.sizeDelta = new Vector2(width, fontSize + 10f);
    }

    private Label CreateLabel()
    {
        if (_canvasGO == null) return null;

        var go = new GameObject("NameTag");
        go.transform.SetParent(_canvasGO.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(140f, fontSize + 10f);

        var group = go.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // dark pill behind the text
        var bg = MakeImage(go.transform, "Backdrop", new Color(0.015f, 0.018f, 0.026f, 0.62f));
        bg.rectTransform.offsetMin = Vector2.zero;
        bg.rectTransform.offsetMax = Vector2.zero;

        // faction underline
        var ruleGO = new GameObject("Rule");
        ruleGO.transform.SetParent(go.transform, false);
        var ruleRect = ruleGO.AddComponent<RectTransform>();
        ruleRect.anchorMin = new Vector2(0.10f, 0f);
        ruleRect.anchorMax = new Vector2(0.90f, 0f);
        ruleRect.pivot = new Vector2(0.5f, 0f);
        ruleRect.sizeDelta = new Vector2(0f, 1.5f);
        ruleRect.anchoredPosition = new Vector2(0f, 1.5f);
        var rule = ruleGO.AddComponent<Image>();
        rule.color = SurvivorRule;
        rule.raycastTarget = false;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(9f, 3f);
        textRect.offsetMax = new Vector2(-9f, -2f);

        var text = textGO.AddComponent<Text>();
        text.font = Font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = SurvivorColor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;

        var outline = textGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        return new Label
        {
            root = go,
            rect = rect,
            group = group,
            text = text,
            underline = rule
        };
    }

    private static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Font _font;
    private static Font Font
    {
        get
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }
}
