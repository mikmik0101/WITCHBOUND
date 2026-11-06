using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One-shot horror prologue shown before the main menu on a fresh launch.
///
/// It is deliberately self-contained: it builds its own overlay canvas, owns
/// its own audio sources and tears all of it down when the player skips or the
/// sequence finishes. The main menu is the existing one - this class only hides
/// it while the prologue is on screen and asks UIManager to show it again
/// afterwards. It never touches the session, the spawner or gameplay state.
///
/// It runs once per process (_shownThisSession), so returning to the main menu
/// later in the same session can never replay it.
/// </summary>
public class PrologueController : MonoBehaviour
{
    public static PrologueController Instance { get; private set; }

    /// <summary>True while the prologue owns the screen.</summary>
    public static bool IsShowing { get { return Instance != null; } }

    private static bool _shownThisSession;

    // ---- story ------------------------------------------------------------
    // The official opening narrative, broken into readable beats.
    // Every character is introduced ONCE, with the full official name.
    private static readonly string[] Beats =
    {
        "Four HANDSOME researchers entered a forgotten village in search of answers.\n" +
        "Luis Padilla, Mick Richards, Josh Garcia, and Vincent Reid thought they had discovered an abandoned settlement.",

        "They were wrong.",

        "Six witches lived there.\n" +
        "Rona Tanyel.   Demonisa Belle.   Kyla Ni-Garcia.   Jannak.   Sandy Vill.   And Beamoth.",

        "One by one, the streets changed.\n" +
        "Every road led back to the same place.\nEvery exit returned them to the village.",

        "The witches were hunting them.\n" +
        "And the village was hungry.",

        "There was only one way out:\n" +
        "survive the next two minutes.",

        "WELCOME TO WITCHBOUND."
    };

    private const string Tagline = "SIX WITCHES.   FOUR SURVIVORS.   NO WAY OUT.";

    // ---- tuning -----------------------------------------------------------
    private const float TitleFadeIn = 1.0f;
    private const float TitleHold = 1.8f;
    private const float BeatFadeIn = 0.55f;
    private const float BeatFadeOut = 0.40f;
    private const float BeatHold = 2.1f;
    private const float WelcomeHold = 2.6f;
    private const float TaglineFadeIn = 0.9f;
    private const float TaglineHold = 2.8f;
    private const float OutroFade = 0.6f;

    private CanvasGroup _rootGroup;
    private CanvasGroup _titleGroup;
    private CanvasGroup _storyGroup;
    private CanvasGroup _taglineGroup;
    private Text _storyText;
    private Text _taglineText;
    private RectTransform _fogLow;
    private RectTransform _fogHigh;
    private AudioSource _wind;
    private AudioSource _drone;
    private bool _finishing;
    private bool _skipped;
    private float _startedAt;

    // =====================================================================
    // entry points
    // =====================================================================

    /// <summary>Statics must not survive an editor play-mode restart.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        _shownThisSession = false;
    }

    /// <summary>
    /// Starts the prologue the first time this process launches.
    /// Returns true when the prologue took the screen, so the caller keeps the
    /// main menu hidden until it finishes.
    /// </summary>
    public static bool ShowOnLaunch()
    {
        if (_shownThisSession) return false;
        _shownThisSession = true;

        // Automated QA runs pass -noprologue so a harness can reach the menus
        // without waiting out the sequence.
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "-noprologue") return false;

        if (Instance != null) return false;

        var go = new GameObject("Prologue");
        go.AddComponent<PrologueController>();
        return Instance != null;
    }

    // =====================================================================
    // lifecycle
    // =====================================================================

    private void Awake()
    {
        Instance = this;
        _startedAt = Time.realtimeSinceStartup;
        BuildUI();
        BuildAudio();
        StartCoroutine(PlayRoutine());
        Debug.Log("[PROLOGUE] started");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_wind != null) _wind.Stop();
        if (_drone != null) _drone.Stop();
    }

    private void Update()
    {
        if (_finishing) return;

        // Keyboard escape hatch alongside the visible SKIP button.
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            Skip();
    }

    /// <summary>Called by the SKIP button (and the keyboard shortcuts).</summary>
    public void Skip()
    {
        if (_finishing) return;
        _skipped = true;
        Debug.Log("[PROLOGUE] skipped");
        Finish();
    }

    // =====================================================================
    // sequence
    // =====================================================================

    private IEnumerator PlayRoutine()
    {
        // Slow fog drift runs independently of the text beats.
        StartCoroutine(DriftRoutine());

        yield return Fade(_titleGroup, 0f, 1f, TitleFadeIn);
        yield return new WaitForSecondsRealtime(TitleHold);

        for (int i = 0; i < Beats.Length; i++)
        {
            bool welcome = i == Beats.Length - 1;
            Debug.Log($"[PROLOGUE] beat {i + 1}/{Beats.Length}");

            _storyText.text = Beats[i];
            // Longer beats shrink slightly so the full official names always fit
            // the story block on a 1366x768 laptop screen.
            int size = welcome ? 40 : (Beats[i].Length > 150 ? 26 : 30);
            _storyText.fontSize = size;
            _storyText.color = welcome
                ? new Color(0.80f, 0.16f, 0.15f)
                : new Color(0.85f, 0.84f, 0.79f);

            yield return Fade(_storyGroup, 0f, 1f, BeatFadeIn);
            yield return new WaitForSecondsRealtime(welcome ? WelcomeHold : BeatHold);
            yield return Fade(_storyGroup, 1f, 0f, BeatFadeOut);
        }

        _taglineText.gameObject.SetActive(true);
        yield return Fade(_taglineGroup, 0f, 1f, TaglineFadeIn);
        yield return new WaitForSecondsRealtime(TaglineHold);

        Finish();
    }

    /// <summary>Very cheap parallax: two translucent bands sliding past each other.</summary>
    private IEnumerator DriftRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            if (_fogLow != null)
                _fogLow.anchoredPosition = new Vector2(Mathf.Sin(t * 0.09f) * 70f, 0f);
            if (_fogHigh != null)
                _fogHigh.anchoredPosition = new Vector2(Mathf.Sin(t * 0.06f + 1.7f) * 110f, 0f);
            yield return null;
        }
    }

    /// <summary>Fades the prologue out, silences its audio and hands over to the menu.</summary>
    private void Finish()
    {
        if (_finishing) return;
        _finishing = true;
        StopAllCoroutines();
        StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        float startWind = _wind != null ? _wind.volume : 0f;
        float startDrone = _drone != null ? _drone.volume : 0f;

        float t = 0f;
        while (t < OutroFade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / OutroFade);
            if (_rootGroup != null) _rootGroup.alpha = 1f - k;
            if (_wind != null) _wind.volume = Mathf.Lerp(startWind, 0f, k);
            if (_drone != null) _drone.volume = Mathf.Lerp(startDrone, 0f, k);
            yield return null;
        }

        if (_wind != null) _wind.Stop();
        if (_drone != null) _drone.Stop();

        // The prologue never changes game state; it only re-exposes the menu it
        // hid at launch. If anything else already moved the game on, leave it be.
        var gm = GameManager.Instance;
        if (gm != null && gm.currentState == GameState.MainMenu && UIManager.Instance != null)
            UIManager.Instance.ShowMainMenu();

        Debug.Log($"[PROLOGUE] finished -> main menu (skipped={_skipped}, elapsed={Time.realtimeSinceStartup - _startedAt:F1}s)");
        Instance = null;
        Destroy(gameObject);
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        if (duration <= 0f) { group.alpha = to; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }

    // =====================================================================
    // audio
    // =====================================================================

    private void BuildAudio()
    {
        // Existing synthesized audio: no new assets, no downloads.
        _wind = gameObject.AddComponent<AudioSource>();
        _wind.clip = SynthesizedAudioGenerator.GenerateWind(6f);
        _wind.loop = true;
        _wind.playOnAwake = false;
        _wind.spatialBlend = 0f;
        _wind.volume = 0.20f;
        _wind.Play();

        _drone = gameObject.AddComponent<AudioSource>();
        _drone.clip = SynthesizedAudioGenerator.GenerateTone(48f, 5f, 0.55f, 0);
        _drone.loop = true;
        _drone.playOnAwake = false;
        _drone.spatialBlend = 0f;
        _drone.volume = 0.14f;
        _drone.Play();
    }

    // =====================================================================
    // UI construction
    // =====================================================================

    private void BuildUI()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGO = new GameObject("PrologueCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ---- full screen backdrop -----------------------------------------
        var panel = MakeRect("ProloguePanel", canvasGO.transform, Vector2.zero, Vector2.one);
        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.006f, 0.008f, 0.014f, 0.92f);
        bg.raycastTarget = true; // swallows stray clicks aimed at the menu behind

        _rootGroup = panel.gameObject.AddComponent<CanvasGroup>();

        // Subtle red wash + edge shadow: cheap, no post-processing.
        MakeImage(panel, "RedWash", new Color(0.30f, 0.02f, 0.03f, 0.10f), Vector2.zero, Vector2.one);
        MakeImage(panel, "VignetteTop", new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, 0.82f), new Vector2(1f, 1f));
        MakeImage(panel, "VignetteBottom", new Color(0f, 0f, 0f, 0.62f), new Vector2(0f, 0f), new Vector2(1f, 0.18f));
        MakeImage(panel, "BloodEdge_L", new Color(0.26f, 0.03f, 0.04f, 0.30f), new Vector2(0f, 0f), new Vector2(0.010f, 1f));
        MakeImage(panel, "BloodEdge_R", new Color(0.26f, 0.03f, 0.04f, 0.30f), new Vector2(0.990f, 0f), new Vector2(1f, 1f));

        // Drifting fog bands (moved by DriftRoutine).
        _fogLow = MakeImage(panel, "FogLow", new Color(0.55f, 0.58f, 0.63f, 0.035f),
            new Vector2(-0.10f, 0.26f), new Vector2(1.10f, 0.50f)).rectTransform;
        _fogHigh = MakeImage(panel, "FogHigh", new Color(0.42f, 0.45f, 0.52f, 0.028f),
            new Vector2(-0.10f, 0.54f), new Vector2(1.10f, 0.70f)).rectTransform;

        // ---- title block ----------------------------------------------------
        var titleGroup = MakeRect("TitleBlock", panel, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.90f));
        _titleGroup = titleGroup.gameObject.AddComponent<CanvasGroup>();
        _titleGroup.alpha = 0f;

        var title = MakeText(titleGroup, "PrologueTitle", "WITCHBOUND", 96,
            new Color(0.64f, 0.09f, 0.11f), TextAnchor.MiddleCenter, true);
        title.fontStyle = FontStyle.Bold;

        var subtitle = MakeText(titleGroup, "PrologueSubtitle",
            "T H E   V I L L A G E   T H A T   N E V E R   L E T S   G O", 21,
            new Color(0.66f, 0.64f, 0.58f), TextAnchor.MiddleCenter, false);
        var subRect = subtitle.rectTransform;
        subRect.anchorMin = new Vector2(0f, 0.10f);
        subRect.anchorMax = new Vector2(1f, 0.24f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        MakeImage(panel, "TitleRule", new Color(0.45f, 0.07f, 0.08f, 0.90f),
            new Vector2(0.40f, 0.612f), new Vector2(0.60f, 0.6145f));

        // ---- story ----------------------------------------------------------
        var storyGroup = MakeRect("StoryBlock", panel, new Vector2(0.13f, 0.24f), new Vector2(0.87f, 0.58f));
        _storyGroup = storyGroup.gameObject.AddComponent<CanvasGroup>();
        _storyGroup.alpha = 0f;

        _storyText = MakeText(storyGroup, "StoryText", "", 30,
            new Color(0.85f, 0.84f, 0.79f), TextAnchor.MiddleCenter, true);
        _storyText.lineSpacing = 1.30f;
        _storyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _storyText.verticalOverflow = VerticalWrapMode.Overflow;

        // ---- tagline --------------------------------------------------------
        var tagGroup = MakeRect("TaglineBlock", panel, new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.52f));
        _taglineGroup = tagGroup.gameObject.AddComponent<CanvasGroup>();
        _taglineGroup.alpha = 0f;
        _taglineText = MakeText(tagGroup, "TaglineText", Tagline, 34,
            new Color(0.72f, 0.13f, 0.14f), TextAnchor.MiddleCenter, true);
        _taglineText.fontStyle = FontStyle.Bold;
        _taglineText.gameObject.SetActive(false);

        // ---- skip -----------------------------------------------------------
        MakeButton(panel, "SkipButton", "SKIP   \u25B8",
            new Vector2(0.855f, 0.905f), new Vector2(0.972f, 0.960f), Skip);

        var hint = MakeText(panel, "SkipHint", "SKIP TO ENTER THE VILLAGE", 12,
            new Color(0.58f, 0.57f, 0.52f, 0.55f), TextAnchor.MiddleCenter, false);
        var hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.38f, 0.035f);
        hintRect.anchorMax = new Vector2(0.62f, 0.075f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        Debug.Log("[PROLOGUE] UI built (canvas sortingOrder=200)");
    }

    // ---- tiny UI helpers ---------------------------------------------------

    private static Font _font;
    private static Font UIFont
    {
        get
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image MakeImage(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var rect = MakeRect(name, parent, min, max);
        var img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Text MakeText(Transform parent, string name, string text, int size, Color color,
        TextAnchor anchor, bool outlined)
    {
        var rect = MakeRect(name, parent, Vector2.zero, Vector2.one);
        var txt = rect.gameObject.AddComponent<Text>();
        txt.text = text;
        txt.font = UIFont;
        txt.fontSize = size;
        txt.color = color;
        txt.alignment = anchor;
        txt.raycastTarget = false;
        txt.supportRichText = true;

        if (outlined)
        {
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
        }
        return txt;
    }

    private static Button MakeButton(Transform parent, string name, string label,
        Vector2 min, Vector2 max, UnityEngine.Events.UnityAction onClick)
    {
        var rect = MakeRect(name, parent, min, max);
        var img = rect.gameObject.AddComponent<Image>();
        img.color = new Color(0.075f, 0.060f, 0.070f, 0.90f);
        img.raycastTarget = true;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        button.onClick.AddListener(onClick);

        var accent = MakeImage(rect, "Accent", new Color(0.34f, 0.06f, 0.08f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, 1f));
        accent.rectTransform.sizeDelta = new Vector2(4f, 0f);

        var txt = MakeText(rect, "Text", label, 18, new Color(0.80f, 0.79f, 0.73f),
            TextAnchor.MiddleCenter, true);
        txt.fontStyle = FontStyle.Bold;

        return button;
    }
}
