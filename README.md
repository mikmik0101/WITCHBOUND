# WITCHBOUND

A first-person survival-horror game built in Unity. Set in an abandoned village at
night, you choose one of four survivors and must stay alive for **120 seconds**
while six witches hunt you through the streets.

- **Engine:** Unity **6000.6.7f1**
- **Platform:** Linux (Standalone x86-64); the project itself is platform-agnostic
- **Render pipeline:** Built-in Render Pipeline (no URP/HDRP package)
- **Input:** Legacy Input Manager (`activeInputHandler: 0`)
- **Scene:** a single scene — `Assets/Scenes/Game/MainGameScene.unity`

Everything the player sees is generated at runtime: the town, buildings, roads,
props, vegetation, character models, faces, and audio are all built procedurally
from code and small source assets. The repository therefore stays small — there
are no imported 3D model packs or texture libraries.

---

## 1. Opening the project

1. Install **Unity 6000.6.7f1** through Unity Hub.
2. In Unity Hub choose **Add → Add project from disk** and select this folder
   (the folder that contains `Assets/`, `Packages/` and `ProjectSettings/`).
3. Open it with **6000.6.7f1**. Unity will regenerate `Library/`, `Temp/` and the
   IDE project files on first import — this is expected and may take a minute.
4. Open `Assets/Scenes/Game/MainGameScene.unity` from the Project window.

> If Unity prompts to upgrade the project version, use 6000.6.7f1. Opening with a
> different major version may trigger an asset reimport and is not recommended.

### Required packages

`Packages/manifest.json` only uses packages that ship with the editor:

| Package | Version | Purpose |
|---|---|---|
| `com.unity.ugui` | 1.0.0 | UI (Canvas, Text, Buttons) |
| `com.unity.textmeshpro` | 3.0.6 | Text rendering |
| `com.unity.ide.rider` | 3.1.0 | IDE integration |
| `com.unity.ide.visualstudio` | 2.0.26 | IDE integration |
| Built-in modules | 1.0.0 | Physics, audio, animation, AI, video, etc. |

There are **no third-party, Asset Store, or paid dependencies**. Unity resolves
these automatically on first open; `Packages/packages-lock.json` pins the exact
versions.

---

## 2. Project structure

```
WITCHBOUND/
├── Assets/
│   ├── Audio/               Procedural audio setup + synthesized SFX/music
│   ├── Characters/          Character definitions, face configs, lore
│   ├── Editor/              Editor tooling (build, scene setup, data creators)
│   ├── Environment/         Procedural town, roads, buildings, props, lighting
│   ├── Faces/               Face source assets
│   ├── Materials/           Shared materials
│   ├── Prefabs/             Prefabs
│   ├── Resources/           Runtime-loaded data + character portraits
│   ├── Scenes/Game/         MainGameScene.unity (the only scene)
│   ├── ScriptableObjects/   GameConfig, survivor/witch/skill data assets
│   ├── Scripts/             All gameplay C# (see below)
│   ├── Shaders/             Custom shaders
│   ├── StreamingAssets/     Character images copied verbatim into the build
│   ├── UI/                  UI sprites and support assets
│   └── VFX/                 Visual-effect assets
├── Packages/                manifest.json + packages-lock.json
├── ProjectSettings/         Unity project settings (version, quality, input…)
└── .vscode/                 Optional VS Code attach-to-Unity config
```

### Code layout (`Assets/Scripts`)

| Folder | Responsibility |
|---|---|
| `Game/` | `GameManager` (state + 120 s timer), `GameplayBootstrap` (run assembly), `SceneSetup`, pause, UI setup, diagnostics |
| `Player/` | First-person `PlayerController`, `CameraController`, `PlayerSkillController` |
| `Witches/` | `WitchAI` chase behaviour, face-texture control |
| `Survivors/` | Companion `SurvivorAI` |
| `Characters/` | Procedural body/face generation, animation, portraits, lore |
| `Environment/` | Procedural town, buildings, roads, props, fences, vegetation, vehicles, lighting, atmosphere |
| `AI/` | Hybrid navigation and simple pathfinding |
| `Audio/` | Procedural/synthesized audio generation |
| `UI/` | Main menu, character select, character library, prologue, HUD, results, settings |
| `Objectives/` | Objective tracking (legacy generator/gate flow, unused by chase mode) |
| `Save/` | `SaveManager` (PlayerPrefs) and `CoinManager` |
| `Skills/` | Skill upgrade progression |
| `Generators/` | Generator props from the earlier game mode |
| `VFX/` | Blood, impact and skill effects |

`Assets/Editor` contains menu commands under the **WITCHBOUND** menu, including
scene setup helpers, data-asset creators, and the Linux build command.

---

## 3. Running the game

**In the editor:** open `MainGameScene.unity` and press **Play**.

**Standalone build:** the compiled Linux build is not part of this source
repository. If you have it, run the `WITCHBOUND` executable from the build
folder; the game writes its save data through `PlayerPrefs`.

### Controls

| Input | Action |
|---|---|
| `W A S D` | Move |
| Mouse | Look |
| `Left Shift` / `Right Shift` | Sprint |
| `Q` | Activate survivor ability |
| `E` | Interact |
| `Esc` | Pause / resume |
| Mouse | UI navigation (menus, character select, library) |

---

## 4. Gameplay

### Objective

Survive for **120 seconds**.

- **Victory:** the timer reaches `00:00`.
- **Defeat:** the player's health reaches zero.

A short skippable prologue plays once on launch, then the main menu appears. A
run goes main menu → character select → chase. There is no generator-repair or
escape-gate win condition in the current chase mode (the generator/gate classes
remain in the project but are not used by the active mode).

### Survivors (playable)

The player picks one survivor; the other three accompany the run as AI
companions. Each ability is activated with `Q` and has a **10-second cooldown**.

| Survivor | Ability | Effect |
|---|---|---|
| Luis Padilla | Phase | Becomes intangible for 3 s — witches cannot see or hit you |
| Mick Richards | Emergency Weapon | Stuns and knocks back nearby witches for 8 s |
| Josh Garcia | Speed Burst | +45% movement speed for 4 s |
| Vincent Reid | Invisibility | Witches lose track of you for 5 s |

### Witches

Six witches hunt the player. In chase mode the full roster is present from the
start (the spawner places and spreads all six): **Rona Tanyel, Demonisa Belle,
Kyla Ni-Garcia, Jannak, Sandy Vill, Beamoth**. Each has a distinct look and
chase behaviour driven by `WitchAI`.

> Internal asset identifiers (for example `mara_veyne`, `seraphine_blackroot`,
> `ethan_vale`) are stable legacy IDs and intentionally differ from the visible
> display names. Do not rename them — scenes, prefabs and save data reference
> them.

### Main systems

- **Procedural town generation** — roads, buildings, fences, props, vegetation,
  vehicles, lighting and atmosphere assembled at runtime.
- **Procedural characters** — bodies, faces, portraits and animation built in
  code, with character-specific configuration and lore.
- **Witch AI** — navigation, pursuit, line-of-sight and attack behaviour.
- **Companion AI** — three AI survivors that follow and react during the run.
- **Abilities & cooldowns** — per-survivor skills with HUD state, VFX and upgrade
  levels.
- **Health, damage and VFX** — blood/impact effects and death handling.
- **UI flow** — main menu, character select, character library, prologue, HUD,
  pause, settings, results.
- **Save & progression** — coins, skill levels/unlocks, survivor progress and
  settings stored in `PlayerPrefs` under the key `WITCHBOUND_SaveData`.
- **Diagnostics** — performance logging and QA/diagnostic helpers used during
  development.

---

## 5. Building the game

### Option A — editor menu

Use **WITCHBOUND → Build Linux** (`Assets/Editor/BuildGame.cs`). It builds
`MainGameScene.unity` for `StandaloneLinux64`.

> The output path in `BuildGame.cs` is currently hard-coded to
> `/home/mikmik/Desktop/WITCHBOUND/Builds/WITCHBOUND`. If the project lives
> elsewhere, change that path in the script or use Option B.

### Option B — Build Settings

1. **File → Build Settings**.
2. Add `Assets/Scenes/Game/MainGameScene.unity` to **Scenes In Build**
   (the scene list in `EditorBuildSettings.asset` is intentionally empty).
3. Select **Linux** (or your target platform) and click **Build**.

Build output folders (`Builds/`, `Library/`, `Temp/`, `Logs/`) are ignored by
Git and can be regenerated at any time.

---

## 6. Repository notes

- `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/` are Unity-generated and
  excluded — they rebuild on first open.
- `*.csproj`, `*.sln`, `*.slnx` are generated by Unity/your IDE and excluded.
- Compiled builds (`Builds/`), the presentation video project
  (`VideoPresentation/`), narration audio (`Audio/`) and historical source
  snapshots (`Backups/`) are kept locally but are **not** part of this source
  repository.
- No credentials, API keys or `.env` files are required by this project.

---

## 7. Asset & licensing note

All code, procedural generation, character art and audio in this repository were
created for WITCHBOUND. No third-party asset packs are included.
