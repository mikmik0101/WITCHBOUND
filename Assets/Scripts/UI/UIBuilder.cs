using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public static class UIBuilder
{
    // ---- palette -----------------------------------------------------------
    private static readonly Color Ink = new Color(0.012f, 0.015f, 0.024f, 0.78f);
    private static readonly Color InkSolid = new Color(0.010f, 0.012f, 0.020f, 0.92f);
    private static readonly Color Blood = new Color(0.66f, 0.10f, 0.12f, 1f);
    private static readonly Color BloodDim = new Color(0.34f, 0.06f, 0.08f, 1f);
    private static readonly Color Bone = new Color(0.80f, 0.79f, 0.73f, 1f);
    private static readonly Color BoneDim = new Color(0.60f, 0.59f, 0.54f, 1f);
    private static readonly Color PaleGreen = new Color(0.62f, 0.86f, 0.66f, 1f);
    private static readonly Color Amber = new Color(0.82f, 0.68f, 0.42f, 1f);
    private static readonly Color Cold = new Color(0.55f, 0.74f, 0.82f, 1f);
    private static readonly Color ButtonNormal = new Color(0.075f, 0.060f, 0.070f, 0.88f);
    private static readonly Color ButtonHover = new Color(0.230f, 0.070f, 0.085f, 0.96f);
    private static readonly Color ButtonPressed = new Color(0.360f, 0.100f, 0.110f, 1f);

    private static Font uiFont;
    private static Font Font
    {
        get
        {
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return uiFont;
        }
    }

    /// <summary>Create a complete Canvas with all UI elements.</summary>
    public static GameObject BuildCompleteUI()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvasObj = new GameObject("UI_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        BuildMainMenu(canvasObj.transform);
        BuildCharacterSelect(canvasObj.transform);
        BuildCharacterLibrary(canvasObj.transform);
        BuildGameplayHUD(canvasObj.transform);
        BuildDeathOverlay(canvasObj.transform);
        BuildPauseMenu(canvasObj.transform);
        BuildResultsScreen(canvasObj.transform);
        BuildSettingsMenu(canvasObj.transform);
        BuildSkillsMenu(canvasObj.transform);

        return canvasObj;
    }

    // --------------------------------------------------------------- helpers

    private static GameObject CreatePanel(string name, Transform parent, Color bgColor,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.color = bgColor;
        image.raycastTarget = false;
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, bool raycast = false)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize,
        Color color, Vector2 anchorMin, Vector2 anchorMax,
        TextAnchor alignment = TextAnchor.MiddleCenter, bool outlined = false)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.color = color;
        textComp.alignment = alignment;
        textComp.font = Font;
        textComp.raycastTarget = false;

        if (outlined)
        {
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        return textComp;
    }

    private static Button CreateButton(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, UnityAction onClick)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        var rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(10, 5);
        rect.offsetMax = new Vector2(-10, -5);

        var image = buttonObj.AddComponent<Image>();
        image.color = ButtonNormal;
        image.raycastTarget = true;

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        // Thin blood-red accent on the left edge of every menu button.
        var accent = CreateImage("Accent", buttonObj.transform, BloodDim,
            new Vector2(0f, 0f), new Vector2(0f, 1f), false);
        var accentRect = accent.rectTransform;
        accentRect.sizeDelta = new Vector2(5f, 0f);
        accentRect.anchoredPosition = new Vector2(2.5f, 0f);
        accentRect.offsetMin = new Vector2(0f, 0f);
        accentRect.offsetMax = new Vector2(5f, 0f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 0f);
        textRect.offsetMax = Vector2.zero;

        var textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.fontSize = 25;
        textComp.color = Bone;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.font = Font;
        textComp.raycastTarget = false;

        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return button;
    }

    private static void Divider(Transform parent, float y, float width, Color color)
    {
        CreateImage("Divider", parent, color,
            new Vector2(0.5f - width * 0.5f, y), new Vector2(0.5f + width * 0.5f, y + 0.002f));
    }

    // ------------------------------------------------------------- main menu

    private static void BuildMainMenu(Transform parent)
    {
        // Deliberately translucent so the generated night town is visible
        // behind the menu. Most of the darkness is supplied by the scene.
        var panel = CreatePanel("MainMenu", parent, new Color(0.010f, 0.013f, 0.022f, 0.44f),
            Vector2.zero, Vector2.one);

        // Readability shade behind the button column only.
        CreateImage("MenuShade", panel.transform, new Color(0.008f, 0.010f, 0.018f, 0.52f),
            new Vector2(0.30f, 0.015f), new Vector2(0.70f, 0.62f));

        var title = CreateText("Title", panel.transform, "WITCHBOUND", 86, Blood,
            new Vector2(0.12f, 0.70f), new Vector2(0.88f, 0.88f), TextAnchor.MiddleCenter, true);
        title.fontStyle = FontStyle.Bold;

        CreateText("Subtitle", panel.transform, "A   N I G H T M A R E   I N   A S H W O O D", 19,
            BoneDim, new Vector2(0.20f, 0.635f), new Vector2(0.80f, 0.695f));

        Divider(panel.transform, 0.607f, 0.30f, new Color(0.55f, 0.09f, 0.11f, 0.85f));

        // Buttons keep their exact original names and anchors. MenuButtonWiring
        // attaches their runtime callbacks by name.
        CreateButton("PlayButton", panel.transform, "PLAY",
            new Vector2(0.35f, 0.455f), new Vector2(0.65f, 0.545f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.CharacterSelect);
        });

        CreateButton("CharacterSelectButton", panel.transform, "CHARACTER SELECT",
            new Vector2(0.35f, 0.360f), new Vector2(0.65f, 0.450f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.CharacterSelect);
        });

        CreateButton("LibraryButton", panel.transform, "CHARACTER LIBRARY",
            new Vector2(0.35f, 0.265f), new Vector2(0.65f, 0.355f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.CharacterLibrary);
        });

        CreateButton("SettingsButton", panel.transform, "SETTINGS",
            new Vector2(0.35f, 0.170f), new Vector2(0.65f, 0.260f), () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSettings();
        });

        CreateButton("QuitButton", panel.transform, "QUIT",
            new Vector2(0.35f, 0.075f), new Vector2(0.65f, 0.165f), () =>
        {
            Application.Quit();
        });

        CreateText("FooterLeft", panel.transform, "SURVIVE  2  MINUTES", 15,
            new Color(0.55f, 0.54f, 0.49f, 0.75f),
            new Vector2(0.025f, 0.015f), new Vector2(0.30f, 0.055f), TextAnchor.MiddleLeft);
        CreateText("FooterRight", panel.transform, "ASHWOOD COUNTY  //  OCTOBER 1998", 14,
            new Color(0.50f, 0.50f, 0.47f, 0.65f),
            new Vector2(0.70f, 0.015f), new Vector2(0.975f, 0.055f), TextAnchor.MiddleRight);

        panel.SetActive(true);
    }

    // -------------------------------------------------------- character select

    private static void BuildCharacterSelect(Transform parent)
    {
        var panel = CreatePanel("CharacterSelect", parent, new Color(0.010f, 0.013f, 0.022f, 0.86f),
            Vector2.zero, Vector2.one);

        CreateText("Title", panel.transform, "CHARACTER SELECT", 46, Bone,
            new Vector2(0.28f, 0.88f), new Vector2(0.72f, 0.96f), TextAnchor.MiddleCenter, true);
        CreateText("Subtitle", panel.transform, "FOUR SURVIVORS.  ONE TOWN.  NO WAY OUT.", 16,
            BoneDim, new Vector2(0.28f, 0.845f), new Vector2(0.72f, 0.885f));

        var card = CreateImage("RosterCard", panel.transform, new Color(0.015f, 0.018f, 0.028f, 0.82f),
            new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.825f));

        // --- survivor 3D preview (left) ---
        CreateText("SurvivorLabel", card.transform, "SURVIVOR", 15, Blood,
            new Vector2(0.03f, 0.90f), new Vector2(0.31f, 0.98f), TextAnchor.MiddleCenter);
        var portraitFrame = CreateImage("PortraitFrame", card.transform, new Color(0.03f, 0.035f, 0.05f, 0.95f),
            new Vector2(0.035f, 0.12f), new Vector2(0.315f, 0.89f));
        CreateRawImage("PortraitImage", portraitFrame.transform);

        // --- middle: name / role / skill ---
        CreateText("CharNameText", card.transform, "LUIS PADILLA", 34, Bone,
            new Vector2(0.335f, 0.60f), new Vector2(0.665f, 0.74f), TextAnchor.MiddleCenter, true);
        CreateText("CharRoleText", card.transform, "Methodical researcher", 20, BoneDim,
            new Vector2(0.335f, 0.50f), new Vector2(0.665f, 0.60f), TextAnchor.MiddleCenter);
        CreateText("CharSkillText", card.transform, "SKILL:  PHASE SHIFT", 18, Cold,
            new Vector2(0.335f, 0.40f), new Vector2(0.665f, 0.50f), TextAnchor.MiddleCenter);
        CreateText("SelectHint", card.transform, "<  PREV      NEXT  >", 15,
            new Color(0.55f, 0.54f, 0.49f, 0.8f),
            new Vector2(0.335f, 0.28f), new Vector2(0.665f, 0.36f), TextAnchor.MiddleCenter);

        // --- chaser bestiary preview (right) ---
        CreateText("ChaserLabel", card.transform, "CHASERS", 15, Blood,
            new Vector2(0.667f, 0.915f), new Vector2(0.945f, 0.985f), TextAnchor.MiddleCenter);
        var chaserFrame = CreateImage("ChaserFrame", card.transform, new Color(0.04f, 0.028f, 0.05f, 0.95f),
            new Vector2(0.662f, 0.10f), new Vector2(0.945f, 0.905f));
        CreateRawImage("ChaserImage", chaserFrame.transform);
        CreateText("ChaserNameText", chaserFrame.transform, "RONA TANYEL", 15, Bone,
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.16f), TextAnchor.MiddleCenter, true);

        // --- navigation ---
        CreateButton("BackButton", panel.transform, "BACK",
            new Vector2(0.03f, 0.90f), new Vector2(0.18f, 0.975f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });

        CreateButton("PrevButton", panel.transform, "< PREV",
            new Vector2(0.24f, 0.05f), new Vector2(0.40f, 0.14f), () => { });

        CreateButton("SelectButton", panel.transform, "ENTER THE TOWN",
            new Vector2(0.41f, 0.05f), new Vector2(0.59f, 0.14f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        });

        CreateButton("NextButton", panel.transform, "NEXT >",
            new Vector2(0.60f, 0.05f), new Vector2(0.76f, 0.14f), () => { });

        panel.SetActive(false);
    }

    private static RawImage CreateRawImage(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.03f, 0.03f);
        rect.anchorMax = new Vector2(0.97f, 0.97f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var raw = obj.AddComponent<RawImage>();
        raw.raycastTarget = false;
        raw.color = Color.white;
        return raw;
    }

    // ------------------------------------------------------------------- HUD

    // ------------------------------------------------------- character library

    /// <summary>
    /// Browsable codex for all four survivors and all six monsters. Pure
    /// presentation: it never starts a run, spawns anything, or touches the
    /// session. Entry buttons are named EntryButton_0..9 and are labelled and
    /// wired by CharacterLibraryUI at runtime.
    /// </summary>
    private static void BuildCharacterLibrary(Transform parent)
    {
        var panel = CreatePanel("CharacterLibrary", parent, new Color(0.010f, 0.013f, 0.022f, 0.94f),
            Vector2.zero, Vector2.one);

        CreateText("Title", panel.transform, "CHARACTER LIBRARY", 44, Bone,
            new Vector2(0.28f, 0.905f), new Vector2(0.72f, 0.965f), TextAnchor.MiddleCenter, true);
        CreateText("Subtitle", panel.transform, "FOUR SURVIVORS   //   SIX WITCHES   //   ONE VILLAGE", 14,
            BoneDim, new Vector2(0.22f, 0.875f), new Vector2(0.78f, 0.908f));

        // ---- left: the roster list ----
        var list = CreateImage("LibraryList", panel.transform, new Color(0.015f, 0.018f, 0.028f, 0.86f),
            new Vector2(0.030f, 0.055f), new Vector2(0.345f, 0.862f));

        CreateText("SurvivorHeader", list.transform, "SURVIVORS  \u2014  4 CHARACTERS", 12, PaleGreen,
            new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.985f), TextAnchor.MiddleLeft);
        CreateText("MonsterHeader", list.transform, "THE WITCHES  \u2014  6 CHARACTERS", 12, Blood,
            new Vector2(0.05f, 0.548f), new Vector2(0.95f, 0.598f), TextAnchor.MiddleLeft);

        for (int i = 0; i < 10; i++)
        {
            float top = i < 4 ? 0.905f - i * 0.078f : 0.525f - (i - 4) * 0.078f;
            CreateButton("EntryButton_" + i, list.transform, "—",
                new Vector2(0.045f, top - 0.068f), new Vector2(0.955f, top), () => { });
        }

        // ---- right: the detail view ----
        var detail = CreateImage("LibraryDetail", panel.transform, new Color(0.015f, 0.018f, 0.028f, 0.86f),
            new Vector2(0.362f, 0.055f), new Vector2(0.970f, 0.862f));

        var portraitFrame = CreateImage("LibraryPortrait", detail.transform,
            new Color(0.030f, 0.035f, 0.050f, 0.96f),
            new Vector2(0.025f, 0.500f), new Vector2(0.330f, 0.955f));
        CreateRawImage("LibraryPortraitImage", portraitFrame.transform);

        CreateText("LibraryFaction", detail.transform, "", 12, Blood,
            new Vector2(0.025f, 0.440f), new Vector2(0.330f, 0.490f), TextAnchor.MiddleCenter);

        CreateText("LibraryName", detail.transform, "", 34, Bone,
            new Vector2(0.330f, 0.855f), new Vector2(0.975f, 0.950f), TextAnchor.MiddleLeft, true);
        CreateText("LibraryRole", detail.transform, "", 16, Cold,
            new Vector2(0.330f, 0.795f), new Vector2(0.975f, 0.855f), TextAnchor.MiddleLeft);
        CreateText("LibraryTraits", detail.transform, "", 14, Amber,
            new Vector2(0.330f, 0.700f), new Vector2(0.975f, 0.790f), TextAnchor.UpperLeft);

        CreateText("BioHeader", detail.transform, "BACKGROUND", 11, BoneDim,
            new Vector2(0.330f, 0.650f), new Vector2(0.975f, 0.692f), TextAnchor.MiddleLeft);
        CreateScrollText("LibraryBio", detail.transform,
            new Vector2(0.330f, 0.150f), new Vector2(0.975f, 0.648f), 16, Bone);

        CreateText("ThreatHeader", detail.transform, "IN THE CHASE", 11, BoneDim,
            new Vector2(0.030f, 0.105f), new Vector2(0.975f, 0.145f), TextAnchor.MiddleLeft);
        CreateText("LibraryThreat", detail.transform, "", 14, BoneDim,
            new Vector2(0.030f, 0.020f), new Vector2(0.975f, 0.105f), TextAnchor.UpperLeft);

        CreateButton("BackButton", panel.transform, "BACK",
            new Vector2(0.030f, 0.878f), new Vector2(0.160f, 0.965f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });

        panel.SetActive(false);
    }

    /// <summary>
    /// A vertical ScrollRect holding one wrapping text block. Used for the
    /// biography so long entries stay readable on smaller windows.
    /// </summary>
    private static Text CreateScrollText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
    {
        var root = new GameObject(name + "Scroll");
        root.transform.SetParent(parent, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = anchorMin;
        rootRect.anchorMax = anchorMax;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(root.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        var vpImage = viewport.AddComponent<Image>();
        vpImage.color = new Color(1f, 1f, 1f, 0.004f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Named after the caller so CharacterLibraryUI can find the Text by name
        // (the component lives on the scroll content object).
        var content = new GameObject(name);
        content.transform.SetParent(viewport.transform, false);
        var cRect = content.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0f, 1f);
        cRect.anchorMax = new Vector2(1f, 1f);
        cRect.pivot = new Vector2(0.5f, 1f);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(-12f, 0f);

        var text = content.AddComponent<Text>();
        text.text = "";
        text.font = Font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = true;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = vpRect;
        scroll.content = cRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        return text;
    }

    private static void BuildGameplayHUD(Transform parent)
    {
        var panel = CreatePanel("GameplayHUD", parent, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);

        // ---- top left: objective ----
        var objectiveShade = CreateImage("ObjectiveShade", panel.transform,
            new Color(0.008f, 0.010f, 0.018f, 0.62f),
            new Vector2(0.018f, 0.905f), new Vector2(0.335f, 0.978f));
        CreateImage("Accent", objectiveShade.transform, Blood,
            new Vector2(0f, 0f), new Vector2(0.008f, 1f));
        CreateText("ObjectiveLabel", objectiveShade.transform, "OBJECTIVE", 12,
            Blood, new Vector2(0.035f, 0.55f), new Vector2(0.30f, 0.92f), TextAnchor.MiddleLeft);
        CreateText("ObjectiveText", objectiveShade.transform, "Survive 2 minutes", 19, Bone,
            new Vector2(0.035f, 0.06f), new Vector2(0.97f, 0.58f), TextAnchor.MiddleLeft, true);

        // ---- top centre: the survival countdown (the only win condition) ----
        var timerShade = CreateImage("TimerShade", panel.transform,
            new Color(0.008f, 0.010f, 0.018f, 0.66f),
            new Vector2(0.393f, 0.898f), new Vector2(0.607f, 0.982f));
        CreateImage("AccentTop", timerShade.transform, Blood,
            new Vector2(0f, 0.978f), new Vector2(1f, 1f));
        CreateText("TimerLabel", timerShade.transform, "SURVIVE", 12, BoneDim,
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.96f), TextAnchor.MiddleCenter);
        CreateText("TimerText", timerShade.transform, "01:00", 38, Bone,
            new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.74f), TextAnchor.MiddleCenter, true);

        // Proximity warning: UIManager shows this when a monster is close.
        var warn = CreateText("ThreatWarning", panel.transform, "SOMETHING IS CLOSE", 20, Blood,
            new Vector2(0.30f, 0.840f), new Vector2(0.70f, 0.892f), TextAnchor.MiddleCenter, true);
        warn.gameObject.SetActive(false);

        // ---- bottom: speed readout (acceleration level) ----
        var speedShade = CreateImage("SpeedShade", panel.transform,
            new Color(0.008f, 0.010f, 0.018f, 0.60f),
            new Vector2(0.262f, 0.022f), new Vector2(0.360f, 0.098f));
        CreateText("SpeedLabel", speedShade.transform, "SPEED", 11, Cold,
            new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f), TextAnchor.MiddleCenter);
        CreateText("SpeedText", speedShade.transform, "5.0", 18, Bone,
            new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.55f), TextAnchor.MiddleCenter, true);

        // ---- bottom left: vitals ----
        var vitals = CreateImage("VitalsShade", panel.transform,
            new Color(0.008f, 0.010f, 0.018f, 0.60f),
            new Vector2(0.018f, 0.022f), new Vector2(0.255f, 0.098f));
        CreateText("VitalsLabel", vitals.transform, "VITALS", 11, Blood,
            new Vector2(0.05f, 0.55f), new Vector2(0.30f, 0.92f), TextAnchor.MiddleLeft);

        var healthBg = CreateImage("HealthBarBg", vitals.transform, new Color(0.10f, 0.03f, 0.03f, 0.9f),
            new Vector2(0.30f, 0.48f), new Vector2(0.93f, 0.80f));
        var healthFill = CreateImage("HealthBarFill", healthBg.transform, new Color(0.52f, 0.09f, 0.10f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillAmount = 1f;

        CreateText("HealthText", vitals.transform, "100/100", 17, Bone,
            new Vector2(0.30f, 0.02f), new Vector2(0.93f, 0.46f), TextAnchor.MiddleLeft, true);

        // ---- bottom center: skill ----
        var skillShade = CreateImage("SkillShade", panel.transform,
            new Color(0.008f, 0.010f, 0.018f, 0.60f),
            new Vector2(0.365f, 0.022f), new Vector2(0.635f, 0.098f));
        CreateText("SkillLabel", skillShade.transform, "SKILL  [Q]", 11, Cold,
            new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f), TextAnchor.MiddleCenter);
        CreateText("SkillText", skillShade.transform, "Phase Shift", 18, Bone,
            new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.55f), TextAnchor.MiddleCenter, true);

        // ---- bottom right: interaction prompt ----
        var promptShade = CreateImage("PromptShade", panel.transform,
            new Color(0.008f, 0.010f, 0.018f, 0.60f),
            new Vector2(0.700f, 0.022f), new Vector2(0.982f, 0.098f));
        CreateText("InteractionPrompt", promptShade.transform, "", 18, Amber,
            new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.90f), TextAnchor.MiddleRight, true);

        // Full-screen damage flash. Hidden until PlayerController.TakeDamage.
        var vignette = CreateImage("DamageVignette", panel.transform,
            new Color(0.42f, 0.020f, 0.028f, 0.22f), Vector2.zero, Vector2.one);
        vignette.gameObject.SetActive(false);

        panel.SetActive(false);
    }

    private static void CreateTeammateRow(Transform parent, string name, string text, float y, Color color)
    {
        var row = CreateImage(name + "Row", parent, new Color(0.05f, 0.055f, 0.065f, 0.45f),
            new Vector2(0.05f, y), new Vector2(0.95f, y + 0.20f));
        CreateImage("Dot", row.transform, color,
            new Vector2(0.03f, 0.40f), new Vector2(0.09f, 0.78f));
        CreateText(name, row.transform, text, 13, color,
            new Vector2(0.14f, 0.28f), new Vector2(0.98f, 1f), TextAnchor.MiddleLeft);

        // Live health bar (driven by UIManager.UpdateTeammateStatus).
        var barBg = CreateImage(name + "HealthBg", row.transform, new Color(0.10f, 0.03f, 0.03f, 0.9f),
            new Vector2(0.14f, 0.12f), new Vector2(0.98f, 0.26f));
        var fill = CreateImage(name + "HealthFill", barBg.transform, color,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
    }

    /// <summary>
    /// Full-screen fade used by the player death sequence, plus the DEFEATED
    /// caption. Sits above the HUD but below the menus/results panel.
    /// </summary>
    private static void BuildDeathOverlay(Transform parent)
    {
        var overlay = CreatePanel("DeathOverlay", parent, new Color(0f, 0f, 0f, 0f),
            Vector2.zero, Vector2.one);
        // The panel helper adds a dark background; force this one transparent -
        // PlayerController drives only the alpha of the fade image.
        var bg = overlay.GetComponent<Image>();
        if (bg != null) bg.raycastTarget = false;

        var fade = CreateImage("DeathFade", overlay.transform, new Color(0f, 0f, 0f, 0f),
            Vector2.zero, Vector2.one);
        fade.raycastTarget = false;

        var txt = CreateText("DeathText", overlay.transform, "DEFEATED", 72, Blood,
            new Vector2(0.20f, 0.40f), new Vector2(0.80f, 0.60f), TextAnchor.MiddleCenter, true);
        txt.gameObject.SetActive(false);

        overlay.SetActive(false);
    }

    // ------------------------------------------------------------ pause menu

    private static void BuildPauseMenu(Transform parent)
    {
        var panel = CreatePanel("PauseMenu", parent, new Color(0.008f, 0.010f, 0.018f, 0.84f),
            Vector2.zero, Vector2.one);

        CreateText("Title", panel.transform, "PAUSED", 56, Bone,
            new Vector2(0.30f, 0.70f), new Vector2(0.70f, 0.85f), TextAnchor.MiddleCenter, true);
        CreateText("Hint", panel.transform, "THE TOWN IS STILL WAITING", 16, BoneDim,
            new Vector2(0.30f, 0.65f), new Vector2(0.70f, 0.70f));
        Divider(panel.transform, 0.635f, 0.18f, BloodDim);

        CreateButton("ResumeButton", panel.transform, "RESUME",
            new Vector2(0.35f, 0.50f), new Vector2(0.65f, 0.60f), () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.HidePauseMenu();
        });

        CreateButton("SettingsButton", panel.transform, "SETTINGS",
            new Vector2(0.35f, 0.40f), new Vector2(0.65f, 0.50f), () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSettings();
        });

        CreateButton("RestartButton", panel.transform, "RESTART",
            new Vector2(0.35f, 0.30f), new Vector2(0.65f, 0.40f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        });

        CreateButton("MainMenuButton", panel.transform, "MAIN MENU",
            new Vector2(0.35f, 0.20f), new Vector2(0.65f, 0.30f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });

        panel.SetActive(false);
    }

    // ---------------------------------------------------------- results menu

    private static void BuildResultsScreen(Transform parent)
    {
        var panel = CreatePanel("ResultsScreen", parent, new Color(0.010f, 0.012f, 0.020f, 0.90f),
            Vector2.zero, Vector2.one);

        CreateText("Title", panel.transform, "RESULTS", 64, Blood,
            new Vector2(0.28f, 0.80f), new Vector2(0.72f, 0.95f), TextAnchor.MiddleCenter, true);
        Divider(panel.transform, 0.785f, 0.22f, BloodDim);

        var card = CreateImage("ResultsCard", panel.transform, new Color(0.02f, 0.024f, 0.034f, 0.75f),
            new Vector2(0.30f, 0.33f), new Vector2(0.70f, 0.77f));

        CreateText("ResultTimeText", card.transform, "Survived: 0.0s", 26, Bone,
            new Vector2(0.10f, 0.62f), new Vector2(0.90f, 0.80f), TextAnchor.MiddleLeft);
        CreateText("ResultMonstersText", card.transform, "Monsters hunting: 6", 20, BoneDim,
            new Vector2(0.10f, 0.44f), new Vector2(0.90f, 0.60f), TextAnchor.MiddleLeft);
        CreateText("ResultHintText", card.transform, "Survive the full 2 minutes to escape Ashwood.",
            16, BoneDim, new Vector2(0.10f, 0.20f), new Vector2(0.90f, 0.40f), TextAnchor.MiddleLeft);

        CreateButton("PlayAgainButton", panel.transform, "TRY AGAIN",
            new Vector2(0.35f, 0.20f), new Vector2(0.65f, 0.29f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.Gameplay);
        });

        CreateButton("MainMenuButton", panel.transform, "MAIN MENU",
            new Vector2(0.35f, 0.10f), new Vector2(0.65f, 0.19f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });

        panel.SetActive(false);
    }

    // --------------------------------------------------------- settings menu

    private static void BuildSettingsMenu(Transform parent)
    {
        var panel = CreatePanel("SettingsMenu", parent, new Color(0.010f, 0.012f, 0.020f, 0.88f),
            Vector2.zero, Vector2.one);

        CreateText("Title", panel.transform, "SETTINGS", 50, Bone,
            new Vector2(0.30f, 0.85f), new Vector2(0.70f, 0.95f), TextAnchor.MiddleCenter, true);
        Divider(panel.transform, 0.83f, 0.20f, BloodDim);

        var card = CreateImage("SettingsCard", panel.transform, new Color(0.02f, 0.024f, 0.034f, 0.72f),
            new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.80f));

        CreateText("AudioHeader", card.transform, "AUDIO", 13, Blood,
            new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.92f), TextAnchor.MiddleLeft);
        CreateText("MasterLabel", card.transform, "Master Volume", 20, Bone,
            new Vector2(0.08f, 0.68f), new Vector2(0.55f, 0.78f), TextAnchor.MiddleLeft);
        CreateText("MusicLabel", card.transform, "Music Volume", 20, Bone,
            new Vector2(0.08f, 0.56f), new Vector2(0.55f, 0.66f), TextAnchor.MiddleLeft);
        CreateText("SFXLabel", card.transform, "SFX Volume", 20, Bone,
            new Vector2(0.08f, 0.44f), new Vector2(0.55f, 0.54f), TextAnchor.MiddleLeft);

        CreateText("ControlsHeader", card.transform, "CONTROLS", 13, Blood,
            new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.44f), TextAnchor.MiddleLeft);
        CreateText("SensLabel", card.transform, "Mouse Sensitivity", 20, Bone,
            new Vector2(0.08f, 0.20f), new Vector2(0.55f, 0.30f), TextAnchor.MiddleLeft);

        CreateText("ValueHint", card.transform,
            "Saved settings apply immediately.", 14, BoneDim,
            new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.14f), TextAnchor.MiddleLeft);

        CreateButton("ApplyButton", panel.transform, "APPLY",
            new Vector2(0.35f, 0.20f), new Vector2(0.65f, 0.29f), () =>
        {
            if (SettingsMenu.Instance != null) SettingsMenu.Instance.ApplySettings();
        });

        CreateButton("BackButton", panel.transform, "BACK",
            new Vector2(0.35f, 0.10f), new Vector2(0.65f, 0.19f), () =>
        {
            if (SettingsMenu.Instance != null) SettingsMenu.Instance.BackToMenu();
            else if (UIManager.Instance != null) UIManager.Instance.HideSettings();
        });

        panel.SetActive(false);
    }

    // ------------------------------------------------------------ skills menu

    private static void BuildSkillsMenu(Transform parent)
    {
        var panel = CreatePanel("SkillsMenu", parent, new Color(0.010f, 0.012f, 0.020f, 0.88f),
            Vector2.zero, Vector2.one);

        CreateText("Title", panel.transform, "SKILLS", 50, Bone,
            new Vector2(0.30f, 0.85f), new Vector2(0.70f, 0.95f), TextAnchor.MiddleCenter, true);
        CreateText("Subtitle", panel.transform, "POWERS BOUGHT IN COIN AND BLOOD", 17, BoneDim,
            new Vector2(0.25f, 0.80f), new Vector2(0.75f, 0.85f));
        Divider(panel.transform, 0.785f, 0.20f, BloodDim);

        var card = CreateImage("SkillsCard", panel.transform, new Color(0.02f, 0.024f, 0.034f, 0.72f),
            new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.76f));

        // One control for all four abilities: Q. Each survivor carries the
        // skill shown on their Character Select card.
        SkillRow(card.transform, 0.78f, "PHASE SHIFT  -  Luis Padilla", "[Q]  Untouchable: witches cannot see or hit you", Cold);
        SkillRow(card.transform, 0.60f, "EMERGENCY WEAPON  -  Mick Richards", "[Q]  Stuns and knocks back nearby witches", Blood);
        SkillRow(card.transform, 0.42f, "SPEED BURST  -  Josh Garcia", "[Q]  +45% movement speed for a few seconds", Amber);
        SkillRow(card.transform, 0.24f, "INVISIBILITY  -  Vincent Reid", "[Q]  Witches lose track of you", PaleGreen);

        // Preserve the original narrative hint fields.
        CreateText("Skill1", panel.transform, "Each survivor starts with the skill shown on their card", 17,
            BoneDim, new Vector2(0.22f, 0.14f), new Vector2(0.78f, 0.20f), TextAnchor.MiddleCenter);
        CreateText("Skill2", panel.transform, "Press Q in a round. The HUD shows READY, ACTIVE and the cooldown.", 15,
            new Color(0.55f, 0.54f, 0.49f), new Vector2(0.22f, 0.10f), new Vector2(0.78f, 0.15f),
            TextAnchor.MiddleCenter);

        CreateButton("BackButton", panel.transform, "BACK",
            new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.10f), () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.HideSkills();
        });

        panel.SetActive(false);
    }

    private static void SkillRow(Transform parent, float top, string name, string description, Color accent)
    {
        var row = CreateImage(name + "Row", parent, new Color(0.05f, 0.055f, 0.065f, 0.50f),
            new Vector2(0.05f, top - 0.13f), new Vector2(0.95f, top));
        CreateImage("Accent", row.transform, accent,
            new Vector2(0f, 0f), new Vector2(0.008f, 1f));
        CreateText("Name", row.transform, name, 22, Bone,
            new Vector2(0.04f, 0.46f), new Vector2(0.70f, 0.94f), TextAnchor.MiddleLeft);
        CreateText("Description", row.transform, description, 15, BoneDim,
            new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.45f), TextAnchor.MiddleLeft);
    }
}
