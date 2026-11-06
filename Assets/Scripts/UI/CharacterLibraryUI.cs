using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Library: a browse-only codex of the four survivors and six
/// monsters. It reads CharacterLore and CharacterImageLibrary (the same picture
/// pipeline gameplay uses) and never touches the session, the AI or the spawner.
/// </summary>
public class CharacterLibraryUI : MonoBehaviour
{
    public static CharacterLibraryUI Instance { get; private set; }

    [Header("Panel (auto-bound by name)")]
    public GameObject libraryPanel;
    public RawImage portraitImage;
    public Text nameText;
    public Text roleText;
    public Text factionText;
    public Text traitsText;
    public Text bioText;
    public Text threatText;

    private readonly Button[] _entries = new Button[10];
    private readonly Text[] _entryLabels = new Text[10];
    private int _index;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        Bind();
        ApplySectionHeaders();
        LayoutEntries();
        BuildEntryButtons();
        ShowEntry(0);

        var scroll = bioText != null ? bioText.GetComponentInParent<ScrollRect>(true) : null;
        Debug.Log($"[LIBRARY] entries={CharacterLore.Count} survivors={CharacterLore.SurvivorCount} " +
                  $"witches={CharacterLore.WitchCount} scroll={(scroll != null)} " +
                  $"buttons={System.Array.FindAll(_entries, b => b != null).Length}");
    }

    /// <summary>
    /// Two clearly labelled sections with live counts, so the library reads as
    /// a full cast list (4 + 6) rather than a list of whatever is in the round.
    /// </summary>
    private void ApplySectionHeaders()
    {
        if (libraryPanel == null) return;

        var survivors = FindComponent<Text>(libraryPanel.transform, "SurvivorHeader");
        if (survivors != null)
            survivors.text = "SURVIVORS  \u2014  " + CharacterLore.SurvivorCount + " CHARACTERS";

        var witches = FindComponent<Text>(libraryPanel.transform, "MonsterHeader");
        if (witches != null)
        {
            witches.text = "THE WITCHES  \u2014  " + CharacterLore.WitchCount + " CHARACTERS";
            witches.color = new Color(0.90f, 0.45f, 0.40f);
        }

        // Slightly larger than the scene default so the biography stays
        // comfortable to read on a 720p window without changing the layout.
        if (bioText != null)
        {
            bioText.fontSize = 18;
            bioText.lineSpacing = 1.12f;
        }

        var subtitle = FindComponent<Text>(libraryPanel.transform, "Subtitle");
        if (subtitle != null)
            subtitle.text = "FOUR SURVIVORS   //   SIX WITCHES   //   ONE VILLAGE";
    }

    private void Bind()
    {
        var canvas = GameObject.Find("UI_Canvas");
        if (canvas == null) return;

        var panel = FindDeep(canvas.transform, "CharacterLibrary");
        if (panel != null) libraryPanel = panel.gameObject;

        if (portraitImage == null) portraitImage = FindComponent<RawImage>(canvas.transform, "LibraryPortraitImage");
        if (nameText == null) nameText = FindComponent<Text>(canvas.transform, "LibraryName");
        if (roleText == null) roleText = FindComponent<Text>(canvas.transform, "LibraryRole");
        if (factionText == null) factionText = FindComponent<Text>(canvas.transform, "LibraryFaction");
        if (traitsText == null) traitsText = FindComponent<Text>(canvas.transform, "LibraryTraits");
        if (bioText == null) bioText = FindComponent<Text>(canvas.transform, "LibraryBio");
        if (threatText == null) threatText = FindComponent<Text>(canvas.transform, "LibraryThreat");
    }

    /// <summary>Called by UIManager when the library state is entered.</summary>
    public void Show()
    {
        if (libraryPanel != null) libraryPanel.SetActive(true);
        ShowEntry(_index);
    }

    /// <summary>
    /// The saved scene was laid out for a taller list than the one the library
    /// now carries, which pushed the tenth entry off the bottom edge. The ten
    /// rows are re-anchored here so all four survivors and all six witches fit
    /// on a 720p window without scrolling the roster.
    /// </summary>
    private void LayoutEntries()
    {
        if (libraryPanel == null) return;

        var list = FindDeep(libraryPanel.transform, "LibraryList");
        if (list == null) return;

        var survivors = FindComponent<Text>(list, "SurvivorHeader");
        if (survivors != null) SetAnchors(survivors.rectTransform, 0.952f, 0.996f);

        var witches = FindComponent<Text>(list, "MonsterHeader");
        if (witches != null) SetAnchors(witches.rectTransform, 0.545f, 0.589f);

        const float pitch = 0.088f;
        const float height = 0.078f;

        for (int i = 0; i < CharacterLore.Count && i < 10; i++)
        {
            var tr = FindDeep(list, "EntryButton_" + i) as RectTransform;
            if (tr == null) continue;

            float top = i < 4 ? 0.945f - i * pitch : 0.535f - (i - 4) * pitch;
            SetAnchors(tr, top - height, top);
        }
    }

    private static void SetAnchors(RectTransform rect, float bottom, float top)
    {
        rect.anchorMin = new Vector2(0.045f, bottom);
        rect.anchorMax = new Vector2(0.955f, top);
        rect.offsetMin = new Vector2(10f, 2f);
        rect.offsetMax = new Vector2(-10f, -2f);
    }

    private void BuildEntryButtons()
    {
        if (libraryPanel == null) return;

        for (int i = 0; i < CharacterLore.Count && i < _entries.Length; i++)
        {
            var tr = FindDeep(libraryPanel.transform, "EntryButton_" + i);
            if (tr == null) continue;

            var button = tr.GetComponent<Button>();
            if (button == null) continue;

            var entry = CharacterLore.Get(i);
            int captured = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowEntry(captured));

            var label = FindComponent<Text>(tr, "Text");
            if (label != null)
            {
                label.text = entry.name.ToUpperInvariant();
                label.alignment = TextAnchor.MiddleLeft;
                label.color = entry.isMonster ? new Color(0.92f, 0.64f, 0.60f) : new Color(0.70f, 0.88f, 0.74f);
            }

            _entries[i] = button;
            _entryLabels[i] = label;
        }
    }

    public void ShowEntry(int index)
    {
        if (CharacterLore.Count == 0) return;
        _index = Mathf.Clamp(index, 0, CharacterLore.Count - 1);
        var e = CharacterLore.Get(_index);

        if (nameText != null)
        {
            nameText.text = e.name.ToUpperInvariant();
            nameText.color = e.isMonster ? new Color(0.96f, 0.73f, 0.69f) : new Color(0.90f, 0.89f, 0.83f);
        }
        if (roleText != null) roleText.text = e.role;
        if (factionText != null)
        {
            factionText.text = e.isMonster ? "MONSTER" : "SURVIVOR";
            factionText.color = e.isMonster ? new Color(0.92f, 0.38f, 0.34f) : new Color(0.60f, 0.86f, 0.65f);
        }
        if (traitsText != null) traitsText.text = e.traits;
        if (bioText != null) bioText.text = e.bio;
        if (threatText != null) threatText.text = e.threat;

        // A new profile always starts at the top of its biography.
        if (bioText != null)
        {
            var scroll = bioText.GetComponentInParent<ScrollRect>(true);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        if (portraitImage != null)
        {
            // Same resolver as gameplay: custom image first, procedural face
            // second, cover-cropped so a portrait is never squashed.
            var image = CharacterImageLibrary.GetFor(e);
            CharacterImageLibrary.ApplyCover(portraitImage, image, e.isMonster);
            CharacterImageLibrary.LogApplied("library portrait", e.name);
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var f = FindDeep(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    private static T FindComponent<T>(Transform root, string name) where T : Component
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<T>() : null;
    }
}
