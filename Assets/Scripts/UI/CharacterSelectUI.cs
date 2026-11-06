using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Select / Bestiary. Shows the ACTUAL procedural 3D survivor models
/// (with faces) instead of text-only rows, and cycles the chaser roster so the
/// player can see what each enemy looks like.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    public static CharacterSelectUI Instance { get; private set; }

    [Header("References (auto-bound by name if null)")]
    public GameObject characterSelectPanel;
    public RawImage portraitImage;
    public RawImage chaserImage;
    public Text characterNameText;
    public Text characterDescriptionText;
    public Text skillNameText;
    public Text chaserNameText;
    public Button leftButton;
    public Button rightButton;
    public Button selectButton;
    public Button backButton;

    private struct Entry { public string name; public string role; public string skill; public WitchType witch; public bool chaser; }

    private Entry[] _survivors;
    private Entry[] _chasers;
    private int _survivorIndex = 0;
    private int _chaserIndex = 0;
    private float _chaserTimer = 0f;

    private CharacterPreviewStage _survivorStage;
    private CharacterPreviewStage _chaserStage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Names, roles and witch titles come from CharacterLore, so the select
        // screen and the Character Library can never disagree about the cast.
        var survivorNames = RuntimeCharacterFactory.SurvivorNames;

        _survivors = new Entry[survivorNames.Length];
        for (int i = 0; i < survivorNames.Length; i++)
        {
            int idx = CharacterLore.IndexOfName(survivorNames[i]);
            var lore = CharacterLore.Get(idx);
            _survivors[i] = new Entry
            {
                name = survivorNames[i],
                role = idx >= 0 ? lore.role : "Researcher  //  Survivor",
                // The panel text is derived from the same mapping the skill
                // controller uses, so it can never advertise the wrong ability.
                skill = CharacterLore.SkillNameFor(survivorNames[i])
            };
        }

        var chasers = new System.Collections.Generic.List<Entry>();
        for (int i = 0; i < CharacterLore.Count; i++)
        {
            var lore = CharacterLore.All[i];
            if (!lore.isMonster) continue;
            chasers.Add(new Entry { name = lore.name, role = lore.role, witch = lore.witchType, chaser = true });
        }
        _chasers = chasers.ToArray();
    }

    private void Start()
    {
        BindReferences();

        if (leftButton != null) { leftButton.onClick.RemoveAllListeners(); leftButton.onClick.AddListener(PreviousCharacter); }
        if (rightButton != null) { rightButton.onClick.RemoveAllListeners(); rightButton.onClick.AddListener(NextCharacter); }

        // Portraits are 2D (custom image -> procedural fallback). The old
        // off-screen 3D preview rendered unlit white mannequins on the target
        // GPU, so the card now uses the same face art as the Character Library.
        FixRosterLayout();
        ShowSurvivor(_survivorIndex);
        ShowChaser(_chaserIndex);
    }

    /// <summary>
    /// The saved scene draws the chaser caption on top of the chaser portrait,
    /// which made the title unreadable once a real photo filled the frame. The
    /// bottom strip of the frame is reserved for the caption instead, so the
    /// portrait, the survivor name and the chaser title never overlap.
    /// </summary>
    private void FixRosterLayout()
    {
        if (chaserImage != null)
        {
            // Enlarged witch (chaser) portrait: fills the wider/taller frame and
            // leaves only the bottom strip for the caption.
            var rt = chaserImage.rectTransform;
            rt.anchorMin = new Vector2(0.03f, 0.16f);
            rt.anchorMax = new Vector2(0.97f, 0.99f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (chaserNameText != null)
        {
            var tr = chaserNameText.rectTransform;
            tr.anchorMin = new Vector2(0.03f, 0.02f);
            tr.anchorMax = new Vector2(0.97f, 0.175f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            chaserNameText.alignment = TextAnchor.MiddleCenter;
            chaserNameText.fontSize = 16;
            chaserNameText.lineSpacing = 1.05f;
            chaserNameText.color = new Color(0.90f, 0.85f, 0.80f);
            chaserNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            chaserNameText.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    private void Update()
    {
        bool visible = characterSelectPanel != null && characterSelectPanel.activeInHierarchy;
        if (!visible) return;

        _chaserTimer += Time.unscaledDeltaTime;
        if (_chaserTimer >= 3f)
        {
            _chaserTimer = 0f;
            _chaserIndex = (_chaserIndex + 1) % _chasers.Length;
            ShowChaser(_chaserIndex);
        }
    }

    private void SetStageActive(bool active)
    {
        if (_survivorStage != null) _survivorStage.SetActive(active);
        if (_chaserStage != null) _chaserStage.SetActive(active);
    }

    private void BindReferences()
    {
        var canvas = GameObject.Find("UI_Canvas");
        if (canvas == null) return;
        var panel = FindDeep(canvas.transform, "CharacterSelect");
        if (panel != null) characterSelectPanel = panel.gameObject;

        if (portraitImage == null) portraitImage = FindComponent<RawImage>(canvas.transform, "PortraitImage");
        if (chaserImage == null) chaserImage = FindComponent<RawImage>(canvas.transform, "ChaserImage");
        if (characterNameText == null) characterNameText = FindComponent<Text>(canvas.transform, "CharNameText");
        if (characterDescriptionText == null) characterDescriptionText = FindComponent<Text>(canvas.transform, "CharRoleText");
        if (skillNameText == null) skillNameText = FindComponent<Text>(canvas.transform, "CharSkillText");
        if (chaserNameText == null) chaserNameText = FindComponent<Text>(canvas.transform, "ChaserNameText");
        if (leftButton == null) leftButton = FindComponent<Button>(canvas.transform, "PrevButton");
        if (rightButton == null) rightButton = FindComponent<Button>(canvas.transform, "NextButton");
    }

    private void PreviousCharacter()
    {
        _survivorIndex = (_survivorIndex - 1 + _survivors.Length) % _survivors.Length;
        ShowSurvivor(_survivorIndex);
    }

    private void NextCharacter()
    {
        _survivorIndex = (_survivorIndex + 1) % _survivors.Length;
        ShowSurvivor(_survivorIndex);
    }

    private void ShowSurvivor(int index)
    {
        var e = _survivors[index];
        if (characterNameText != null) characterNameText.text = e.name;
        if (characterDescriptionText != null) characterDescriptionText.text = e.role;
        if (skillNameText != null) skillNameText.text = "SKILL:  " + e.skill;

        // The selection is stored immediately, so every "start" path (Select
        // button, Play, restart) picks up the survivor that is on screen.
        if (GameManager.Instance != null) GameManager.Instance.selectedSurvivor = e.name;

        if (portraitImage != null)
        {
            var image = PortraitFor(e.name, false, WitchType.Humanoid);
            CharacterImageLibrary.ApplyCover(portraitImage, image);
            CharacterImageLibrary.LogApplied("character-select portrait", e.name);
        }
        Debug.Log($"[CHARSELECT] preview survivor {e.name}");
    }

    private void ShowChaser(int index)
    {
        var e = _chasers[index];
        if (chaserNameText != null) chaserNameText.text = e.name + "\n" + e.role;

        if (chaserImage != null)
        {
            var image = PortraitFor(e.name, true, e.witch);
            CharacterImageLibrary.ApplyCover(chaserImage, image, true);
            CharacterImageLibrary.LogApplied("character-select portrait", e.name);
        }
    }

    /// <summary>Custom image if present, procedural face otherwise. Never null.</summary>
    private static CharacterImage PortraitFor(string name, bool monster, WitchType type)
    {
        string id = null;
        for (int i = 0; i < CharacterLore.Count; i++)
            if (CharacterLore.All[i].name == name) { id = CharacterLore.All[i].id; break; }

        return CharacterImageLibrary.Get(id, name, monster ? type : WitchType.Humanoid);
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
