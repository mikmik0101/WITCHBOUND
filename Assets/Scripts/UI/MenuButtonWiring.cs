using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Attaches all menu button callbacks at RUNTIME.
///
/// REQUIRED because SetupGameScene runs SceneSetup.SetupScene() only in the
/// EDITOR session: Button.onClick.AddListener closures are not serializable,
/// so the saved scene has no button callbacks in the built player.
/// UIManager (present in the saved scene) calls WireAll() during Start().
/// </summary>
public static class MenuButtonWiring
{
    public static void WireAll()
    {
        var canvasTr = GameObject.Find("UI_Canvas")?.transform;
        if (canvasTr == null)
        {
            Debug.LogError("[BTN WIRE] UI_Canvas not found - buttons cannot be wired!");
            return;
        }

        int wired = 0;

        // ---- MainMenu ----
        // Chase mode: Play goes to character selection so the run always starts
        // with a deliberate survivor choice (Main Menu -> Select -> Chase).
        wired += WireButton(canvasTr, "MainMenu", "PlayButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.CharacterSelect);
            else Debug.LogError("[BTN WIRE PLAY] GameManager.Instance is null!");
        });
        wired += WireButton(canvasTr, "MainMenu", "CharacterSelectButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.CharacterSelect);
            else Debug.LogError("[BTN WIRE CHARSELECT] GameManager.Instance is null!");
        });
        wired += WireButton(canvasTr, "MainMenu", "LibraryButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.CharacterLibrary);
        });
        wired += WireButton(canvasTr, "MainMenu", "SettingsButton", () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSettings();
            else Debug.LogError("[BTN WIRE SETTINGS] UIManager.Instance is null!");
        });
        wired += WireButton(canvasTr, "MainMenu", "QuitButton", () =>
        {
            Debug.Log("[BTN WIRE QUIT] Quitting application");
            Application.Quit();
        });

        // ---- CharacterSelect ----
        wired += WireButton(canvasTr, "CharacterSelect", "BackButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });
        wired += WireButton(canvasTr, "CharacterSelect", "SelectButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        });

        // ---- CharacterLibrary ----
        // Entry buttons are wired by CharacterLibraryUI itself; only the back
        // button belongs here so it works in the built player.
        wired += WireButton(canvasTr, "CharacterLibrary", "BackButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });

        // ---- SkillsMenu ----
        wired += WireButton(canvasTr, "SkillsMenu", "BackButton", () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.HideSkills();
        });

        // ---- PauseMenu ----
        wired += WireButton(canvasTr, "PauseMenu", "ResumeButton", () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.HidePauseMenu();
        });
        wired += WireButton(canvasTr, "PauseMenu", "SettingsButton", () =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSettings();
        });
        wired += WireButton(canvasTr, "PauseMenu", "RestartButton", () =>
        {
            // Must be a full session reset, not just a state change, or the
            // clock, monsters and player health all carry over.
            if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        });
        wired += WireButton(canvasTr, "PauseMenu", "MainMenuButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });

        // ---- ResultsScreen ----
        wired += WireButton(canvasTr, "ResultsScreen", "PlayAgainButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RequestNewGame();
        });
        wired += WireButton(canvasTr, "ResultsScreen", "MainMenuButton", () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameState(GameState.MainMenu);
        });

        // ---- SettingsMenu ----
        wired += WireButton(canvasTr, "SettingsMenu", "ApplyButton", () =>
        {
            if (SettingsMenu.Instance != null) SettingsMenu.Instance.ApplySettings();
            else Debug.LogError("[BTN WIRE APPLY] SettingsMenu.Instance is null!");
        });
        wired += WireButton(canvasTr, "SettingsMenu", "BackButton", () =>
        {
            if (SettingsMenu.Instance != null) SettingsMenu.Instance.BackToMenu();
            else if (UIManager.Instance != null) UIManager.Instance.HideSettings();
        });

        Debug.Log("[BTN WIRE] Total buttons wired: " + wired);
    }

    private static int WireButton(Transform canvas, string panelName, string buttonName, UnityEngine.Events.UnityAction action)
    {
        var panel = canvas.Find(panelName);
        if (panel == null)
        {
            Debug.LogWarning("[BTN WIRE] Panel not found: " + panelName);
            return 0;
        }

        var buttonTr = panel.Find(buttonName);
        if (buttonTr == null)
        {
            Debug.LogWarning("[BTN WIRE] Button '" + buttonName + "' not found in panel " + panelName);
            return 0;
        }

        var button = buttonTr.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning("[BTN WIRE] No Button component on: " + buttonName);
            return 0;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        button.onClick.AddListener(() => Debug.Log("[CLICK TRACE] " + buttonName + " onClick FIRED (wired listener)"));
        Debug.Log("[BTN WIRE] " + panelName + "/" + buttonName + " wired");
        return 1;
    }
}
