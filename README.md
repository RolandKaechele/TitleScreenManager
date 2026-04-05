# TitleScreenManager

A modular title screen manager for Unity.  
Orchestrates main menu navigation, new/continue/load game flow, options, credits, and extras.  
Gallery panel navigation is built in; gallery content management is delegated to **GalleryManager** (optional integration).  
Optionally integrates with SaveManager, GalleryManager, MapLoaderFramework, EventManager, and Unity Localization.


## Features

- **Main menu** — New Game, Continue, Load Game, Gallery, Options, Credits, Extras, Quit
- **Continue button** — shown only when at least one save slot has data (requires `TITLESCREEN_SM`)
- **Load Game panel** — slot-selection UI wired to `LoadSlot(int)` (requires `TITLESCREEN_SM`)
- **Gallery panel** — navigation to gallery panel built in; content managed by GalleryManager (activated via `TITLESCREEN_GM`)
- **Options** — master/music/SFX volume via `AudioMixer`, graphics quality, fullscreen, language; persisted via `PlayerPrefs`
- **Language switching** — applies to Unity Localization `SelectedLocale` (activated via `TITLESCREEN_LOC`)
- **Panel system** — each panel is a `CanvasGroup`; toggle visibility, interactability, and raycasts in one call
- **SaveManager integration** — continue/load visibility (activated via `TITLESCREEN_SM`)
- **GalleryManager integration** — gallery content and unlock state delegated to GalleryManager (activated via `TITLESCREEN_GM`)
- **MapLoaderFramework integration** — scene loading delegated to MapLoader (activated via `TITLESCREEN_MLF`)
- **EventManager integration** — menu transitions broadcast as named `GameEvent`s (activated via `TITLESCREEN_EM`)
- **Custom Inspector** — validation warnings, live panel switcher, live options display, gallery overview (via GalleryManager)
- **DOTween Pro integration** — `CanvasGroup.DOFade` and `RectTransform.DOAnchorPos` drive menu panel slide-in and fade transitions (activated via `TITLESCREEN_DOTWEEN`)
- **Odin Inspector integration** — `SerializedMonoBehaviour` base for full Inspector serialization of complex types; runtime-display fields marked `[ReadOnly]` in Play Mode (activated via `ODIN_INSPECTOR`)


## Installation

### Option A — Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL…**
3. Enter:

   ```
   https://github.com/RolandKaechele/TitleScreenManager.git
   ```

### Option B — Clone into Assets

```bash
git clone https://github.com/RolandKaechele/TitleScreenManager.git Assets/TitleScreenManager
```

### Option C — npm / postinstall

```bash
cd Assets/TitleScreenManager
npm install
```

`postinstall.js` confirms installation. No additional data folders are required.


## Scene Setup

1. Create a **TitleScreen scene**.
2. Build a Canvas with a panel per menu section (Main, Load Game, Gallery, Options, Credits, Extras).  
   Add a `CanvasGroup` to each panel root.
3. Create a persistent GameObject and attach `TitleScreenManager`.
4. Assign each panel's `CanvasGroup` in the Inspector.
5. Wire your UI Buttons' `onClick` events to the corresponding public methods (e.g. `ShowGallery()`, `NewGame()`).


## Quick Start

### 1. Add TitleScreenManager to your scene

| Component | Purpose |
| --------- | ------- |
| `TitleScreenManager` | Main orchestrator (required) |

**Inspector fields overview:**

| Field | Default | Description |
| ----- | ------- | ----------- |
| `gameplayScene` | `"Gameplay"` | Scene loaded on New Game / Continue / Load |
| `panelMain` … `panelExtras` | — | `CanvasGroup` references for each panel |
| `continueButtonObject` | — | Root GameObject of the Continue button |
| `audioMixer` | — | `AudioMixer` for volume parameters |
| `masterVolumeParam` | `"MasterVolume"` | Exposed AudioMixer parameter name |
| `musicVolumeParam` | `"MusicVolume"` | Exposed AudioMixer parameter name |
| `sfxVolumeParam` | `"SFXVolume"` | Exposed AudioMixer parameter name |

### 2. Wire buttons

```csharp
// All public methods can be used directly in Button.onClick UnityEvents:
// ShowMainPanel()  ShowLoadGame()  ShowGallery()  ShowOptions()
// ShowCredits()    ShowExtras()
// NewGame()        Continue()      QuitGame()
// LoadSlot(int)    SaveOptions()   RevertOptions()
```

### 3. Respond to events

```csharp
var tsm = FindFirstObjectByType<TitleScreenManager.Runtime.TitleScreenManager>();

tsm.OnNewGame        += () => Debug.Log("Starting new game!");
tsm.OnContinue       += slot => Debug.Log($"Continuing from slot {slot}");
tsm.OnLoadSlot       += slot => Debug.Log($"Loading slot {slot}");
tsm.OnOptionsSaved   += () => Debug.Log("Options saved.");
```

### 4. Gallery

Gallery panel navigation is always available via `ShowGallery()`.  
Gallery **content** (entries, unlock state) is managed by **GalleryManager** — see the `TITLESCREEN_GM` section below.  
Without GalleryManager, the panel simply opens and your custom UI handles the content.

### 5. Options

```csharp
// Change live — each setter immediately applies the value
tsm.MasterVolume    = 0.8f;
tsm.MusicVolume     = 0.7f;
tsm.SfxVolume       = 0.9f;
tsm.GraphicsQuality = 2;       // maps to QualitySettings level
tsm.Fullscreen      = true;
tsm.LanguageIndex   = 1;       // 0 = Deutsch, 1 = English (TITLESCREEN_LOC)

// Persist to PlayerPrefs
tsm.SaveOptions();

// Discard changes
tsm.RevertOptions();
```


## SaveManager Integration (`TITLESCREEN_SM`)

Add `TITLESCREEN_SM` to **Edit → Project Settings → Player → Scripting Define Symbols**.

- The **Continue** button is shown/hidden based on whether any slot has a save file.
- `Continue()` loads the `ActiveSlot` from the SaveManager.

Requires [SaveManager](https://github.com/RolandKaechele/SaveManager) in the project.


## GalleryManager Integration (`TITLESCREEN_GM`)

Add `TITLESCREEN_GM` to **Edit → Project Settings → Player → Scripting Define Symbols**.

- The custom Inspector shows a live gallery overview sourced from the `GalleryManager` in scene.
- `ShowGallery()` warms the GalleryManager's unlock cache when the gallery panel is opened.
- Unlock / lock buttons are exposed in the Inspector for testing.

Requires [GalleryManager](https://github.com/RolandKaechele/GalleryManager) in the project.


## Runtime API

### `TitleScreenManager`

| Member | Description |
| ------ | ----------- |
| `ShowPanel(panel)` | Switch to a named panel, hiding all others |
| `ShowMainPanel()` | Navigate to the main button menu |
| `ShowLoadGame()` | Open the save-slot selection panel |
| `ShowGallery()` | Open the gallery panel (notifies GalleryManager if `TITLESCREEN_GM`) |
| `ShowOptions()` | Open the options / settings panel |
| `ShowCredits()` | Open the credits panel |
| `ShowExtras()` | Open the extras / bonus content panel |
| `NewGame()` | Fire `OnNewGame` then load `gameplayScene` |
| `Continue()` | Fire `OnContinue(slot)` then load `gameplayScene` |
| `LoadSlot(slot)` | Fire `OnLoadSlot(slot)` then load `gameplayScene` |
| `QuitGame()` | `Application.Quit()` (stops Play Mode in Editor) |
| `SaveOptions()` | Persist options to PlayerPrefs; fires `OnOptionsSaved` |
| `RevertOptions()` | Reload options from PlayerPrefs and re-apply |
| `ActivePanel` | Currently visible `TitlePanel` |
| `MasterVolume` | Live master volume [0, 1] |
| `MusicVolume` | Live music volume [0, 1] |
| `SfxVolume` | Live SFX volume [0, 1] |
| `GraphicsQuality` | Unity quality level index |
| `Fullscreen` | Current fullscreen state |
| `LanguageIndex` | Currently selected locale index |
| `OnNewGame` | `event Action` |
| `OnContinue` | `event Action<int>` — slot index |
| `OnLoadSlot` | `event Action<int>` — slot index |
| `OnOptionsSaved` | `event Action` |


## Optional Integrations

### SaveManager (`TITLESCREEN_SM`)

Requires `TITLESCREEN_SM` define and [SaveManager](https://github.com/RolandKaechele/SaveManager).

### GalleryManager (`TITLESCREEN_GM`)

Requires `TITLESCREEN_GM` define and [GalleryManager](https://github.com/RolandKaechele/GalleryManager).

### MapLoaderFramework (`TITLESCREEN_MLF`)

Requires `TITLESCREEN_MLF` define. Scene transitions use `FindFirstObjectByType<MapLoaderFramework.Runtime.MapLoaderManager>()?.LoadMap(scene)`, with a `SceneManager.LoadScene` fallback.

### EventManager (`TITLESCREEN_EM`)

Requires `TITLESCREEN_EM` define. The following named `GameEvent`s are fired:

| Event name | When |
| ---------- | ---- |
| `TitlePanelChanged` | Panel switches; value = panel name |
| `TitleNewGame` | New Game selected |
| `TitleContinue` | Continue selected; value = slot index |
| `TitleLoadSlot` | Load slot confirmed; value = slot index |
| `TitleOptionsSaved` | Options saved |
| `TitleQuit` | Quit selected |

### Unity Localization (`TITLESCREEN_LOC`)

Requires `TITLESCREEN_LOC` define and `com.unity.localization`.  
`LanguageIndex` maps directly to `LocalizationSettings.AvailableLocales.Locales[index]`.


### Odin Inspector (`ODIN_INSPECTOR`)

Requires `ODIN_INSPECTOR` define (standard Odin Inspector scripting define). Inherits from `SerializedMonoBehaviour` for full Inspector serialization; runtime-display fields are marked `[ReadOnly]`.


## Options — PlayerPrefs Keys

| Key | Default | Description |
| --- | ------- | ----------- |
| `opt_master_volume` | `0.8` | Master volume |
| `opt_music_volume` | `0.7` | Music volume |
| `opt_sfx_volume` | `0.9` | SFX volume |
| `opt_language` | `0` | Language index |
| `opt_graphics_quality` | `2` | Unity quality level |
| `opt_fullscreen` | `1` | Fullscreen (`1` = true) |


## Dependencies

| Dependency | Required | Notes |
| ---------- | -------- | ----- |
| Unity 2022.3+ | ✓ | |
| SaveManager | optional | Required when `TITLESCREEN_SM` is defined |
| GalleryManager | optional | Required when `TITLESCREEN_GM` is defined |
| MapLoaderFramework | optional | Required when `TITLESCREEN_MLF` is defined |
| EventManager | optional | Required when `TITLESCREEN_EM` is defined |
| com.unity.localization | optional | Required when `TITLESCREEN_LOC` is defined |
| Odin Inspector | optional | Required when `ODIN_INSPECTOR` is defined |


## Repository

[https://github.com/RolandKaechele/TitleScreenManager](https://github.com/RolandKaechele/TitleScreenManager)


## License

MIT — see [LICENSE](LICENSE).
