using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds the runtime-generated HUD elements to UIManager's serialized fields.
///
/// The UI is created by UIBuilder at runtime, so the references cannot be
/// stored in the scene. Without this, health/objective/generator readouts stay
/// static and damage/interaction effects hit null references.
/// </summary>
public static class HudWiring
{
    public static void Wire(UIManager ui)
    {
        if (ui == null) return;

        var canvasTr = GameObject.Find("UI_Canvas")?.transform;
        if (canvasTr == null) return;

        // Panels: the scene's UI is rebuilt from UIBuilder, so the serialized
        // references are always re-bound by name at runtime.
        ui.mainMenuPanel = Panel(canvasTr, "MainMenu");
        ui.characterSelectPanel = Panel(canvasTr, "CharacterSelect");
        ui.hudPanel = Panel(canvasTr, "GameplayHUD");
        ui.pauseMenuPanel = Panel(canvasTr, "PauseMenu");
        ui.resultsPanel = Panel(canvasTr, "ResultsScreen");
        ui.settingsPanel = Panel(canvasTr, "SettingsMenu");
        ui.skillsPanel = Panel(canvasTr, "SkillsMenu");
        ui.libraryPanel = Panel(canvasTr, "CharacterLibrary");

        var hud = canvasTr.Find("GameplayHUD");
        if (hud == null) return;

        ui.survivalText = FindText(hud, "TimerText");
        ui.speedText = FindText(hud, "SpeedText");
        var warn = FindDeep(hud, "ThreatWarning");
        if (warn != null) ui.threatWarning = warn.gameObject;

        ui.healthText = FindText(hud, "HealthText");
        ui.objectiveText = FindText(hud, "ObjectiveText");
        ui.generatorsRemainingText = FindText(hud, "GeneratorsText");
        ui.healthBar = FindImage(hud, "HealthBarFill");
        ui.skillText = FindText(hud, "SkillText");

        var promptShade = hud.Find("PromptShade");
        if (promptShade != null)
        {
            ui.interactionPrompt = promptShade.gameObject;
            ui.interactionText = FindText(promptShade, "InteractionPrompt");
        }

        var vignette = hud.Find("DamageVignette");
        if (vignette != null) ui.damageVignette = vignette.gameObject;

        // ---- teammate rows (text + health bar) ----
        var rows = new GameObject[3];
        var bars = new Image[3];
        var texts = new Text[3];
        var dots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var row = FindDeep(hud, "Teammate" + (i + 1) + "Row");
            if (row == null) continue;
            rows[i] = row.gameObject;
            texts[i] = FindText(row, "Teammate" + (i + 1));
            bars[i] = FindImage(row, "Teammate" + (i + 1) + "HealthFill");
            dots[i] = FindImage(row, "Dot");
        }
        ui.teammateIndicators = rows;
        ui.teammateHealthBars = bars;
        ui.teammateTexts = texts;
        ui.teammateDots = dots;

        // ---- death overlay ----
        var death = canvasTr.Find("DeathOverlay");
        if (death != null)
        {
            ui.deathOverlay = death.gameObject;
            ui.deathFadeImage = FindImage(death, "DeathFade");
            ui.deathText = FindText(death, "DeathText");
        }

        // ---- results panel (static texts built by UIBuilder) ----
        var results = canvasTr.Find("ResultsScreen");
        if (results != null)
        {
            ui.resultsPanel = results.gameObject;
            var card = FindDeep(results, "ResultsCard") ?? results;
            ui.resultTitleText = FindText(results, "Title");
            ui.resultTimeText = FindText(card, "ResultTimeText");
            ui.resultMonstersText = FindText(card, "ResultMonstersText");
            ui.resultHintText = FindText(card, "ResultHintText");
        }

        int bound = 0;
        if (ui.survivalText != null) bound++;
        if (ui.speedText != null) bound++;
        if (ui.threatWarning != null) bound++;
        if (ui.healthText != null) bound++;
        if (ui.objectiveText != null) bound++;
        if (ui.generatorsRemainingText != null) bound++;
        if (ui.healthBar != null) bound++;
        if (ui.interactionPrompt != null) bound++;
        if (ui.damageVignette != null) bound++;
        if (ui.teammateTexts != null) bound++;
        if (ui.deathFadeImage != null) bound++;
        if (ui.resultTitleText != null) bound++;
        if (ui.generatorsRepairedText != null) bound++;

        Debug.Log("[HUD WIRE] Bound " + bound + " HUD references. rows=" +
            (ui.teammateTexts?[0] != null) + "/" + (ui.teammateTexts?[1] != null) + "/" + (ui.teammateTexts?[2] != null));
    }

    private static Text FindText(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<Text>() : null;
    }

    private static Image FindImage(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static GameObject Panel(Transform canvas, string name)
    {
        var t = canvas.Find(name);
        return t != null ? t.gameObject : null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
