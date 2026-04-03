using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace TitleScreenManager.Runtime
{

    // -------------------------------------------------------------------------
    // TitlePanel — panel states
    // -------------------------------------------------------------------------

    /// <summary>The currently active panel on the title screen.</summary>
    public enum TitlePanel { Main, LoadGame, Gallery, Options, Credits, Extras }

    // -------------------------------------------------------------------------
    // TitleScreenManager
    // -------------------------------------------------------------------------

    /// <summary>
    /// <b>TitleScreenManager</b> orchestrates the title screen: main menu navigation,
    /// new/continue/load game flow, options, credits, and extras.
    /// Gallery panel visibility is supported; gallery content management is
    /// delegated to <b>GalleryManager</b> (activated via <c>TITLESCREEN_GM</c>).
    ///
    /// <para><b>Setup:</b>
    /// <list type="number">
    ///   <item>Add this component to a persistent GameObject in the Title scene.</item>
    ///   <item>Assign a <see cref="CanvasGroup"/> for each panel in the Inspector.</item>
    ///   <item>Wire UI buttons to the public methods via UnityEvents or direct calls.</item>
    ///   <item>Assign the <see cref="gameplayScene"/> name (first gameplay scene).</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Options</b> are stored in <c>PlayerPrefs</c> and applied immediately.
    /// Audio volumes require an <see cref="AudioMixer"/> with exposed float parameters.</para>
    ///
    /// <para><b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>TITLESCREEN_SM</c>  — SaveManager: enables Continue button and load-slot flow.</item>
    ///   <item><c>TITLESCREEN_GM</c>  — GalleryManager: wires <see cref="ShowGallery"/> to notify GalleryManager.</item>
    ///   <item><c>TITLESCREEN_MLF</c> — MapLoaderFramework: scene loading via MapLoader instead of SceneManager.</item>
    ///   <item><c>TITLESCREEN_EM</c>  — EventManager: broadcasts menu transitions as named GameEvents.</item>
    ///   <item><c>TITLESCREEN_LOC</c> — Unity Localization: language switching applies to LocalizationSettings.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("TitleScreenManager/Title Screen Manager")]
    [DisallowMultipleComponent]
    public class TitleScreenManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Scenes")]
        [Tooltip("Scene loaded when starting a new game or continuing.")]
        [SerializeField] private string gameplayScene = "Gameplay";

        [Header("Panels")]
        [Tooltip("Canvas Group for the main button menu.")]
        [SerializeField] private CanvasGroup panelMain;

        [Tooltip("Canvas Group for the save-slot selection panel (load game).")]
        [SerializeField] private CanvasGroup panelLoadGame;

        [Tooltip("Canvas Group for the gallery viewer.")]
        [SerializeField] private CanvasGroup panelGallery;

        [Tooltip("Canvas Group for the options / settings panel.")]
        [SerializeField] private CanvasGroup panelOptions;

        [Tooltip("Canvas Group for the credits panel.")]
        [SerializeField] private CanvasGroup panelCredits;

        [Tooltip("Canvas Group for the extras / bonus panel.")]
        [SerializeField] private CanvasGroup panelExtras;

        [Header("Continue Button")]
        [Tooltip("Root GameObject of the Continue button. Hidden when no saves exist (requires TITLESCREEN_SM).")]
        [SerializeField] private GameObject continueButtonObject;

        [Header("Audio / Options")]
        [Tooltip("AudioMixer used to apply volume settings at runtime.")]
        [SerializeField] private AudioMixer audioMixer;

        [Tooltip("Exposed AudioMixer parameter name for master volume.")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";

        [Tooltip("Exposed AudioMixer parameter name for music volume.")]
        [SerializeField] private string musicVolumeParam  = "MusicVolume";

        [Tooltip("Exposed AudioMixer parameter name for SFX volume.")]
        [SerializeField] private string sfxVolumeParam    = "SFXVolume";

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when the player chooses New Game.</summary>
        public event Action OnNewGame;

        /// <summary>Fired when the player chooses Continue. Parameter: last active slot index.</summary>
        public event Action<int> OnContinue;

        /// <summary>Fired when a save slot is selected in the Load Game panel. Parameter: slot index.</summary>
        public event Action<int> OnLoadSlot;

        /// <summary>Fired after options are saved to PlayerPrefs.</summary>
        public event Action OnOptionsSaved;

        // -------------------------------------------------------------------------
        // Properties
        // -------------------------------------------------------------------------

        /// <summary>Currently visible panel.</summary>
        public TitlePanel ActivePanel { get; private set; } = TitlePanel.Main;

        // ── Options accessors (each setter immediately applies the value) ─────────

        /// <summary>Master volume in the range [0, 1].</summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); ApplyVolumes(); }
        }

        /// <summary>Music volume in the range [0, 1].</summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Mathf.Clamp01(value); ApplyVolumes(); }
        }

        /// <summary>SFX volume in the range [0, 1].</summary>
        public float SfxVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); ApplyVolumes(); }
        }

        /// <summary>Selected language index (maps to LocalizationSettings locales when TITLESCREEN_LOC is active).</summary>
        public int LanguageIndex
        {
            get => _languageIndex;
            set { _languageIndex = value; ApplyLanguage(); }
        }

        /// <summary>Unity quality level index (0 = Lowest … n = Highest). Applied via QualitySettings.</summary>
        public int GraphicsQuality
        {
            get => _graphicsQuality;
            set { _graphicsQuality = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1); ApplyQuality(); }
        }

        /// <summary>Whether the game runs in fullscreen mode.</summary>
        public bool Fullscreen
        {
            get => _fullscreen;
            set { _fullscreen = value; ApplyFullscreen(); }
        }

        // -------------------------------------------------------------------------
        // PlayerPrefs keys (options)
        // -------------------------------------------------------------------------

        private const string PrefMasterVolume    = "opt_master_volume";
        private const string PrefMusicVolume     = "opt_music_volume";
        private const string PrefSfxVolume       = "opt_sfx_volume";
        private const string PrefLanguage        = "opt_language";
        private const string PrefGraphicsQuality = "opt_graphics_quality";
        private const string PrefFullscreen      = "opt_fullscreen";

        // -------------------------------------------------------------------------
        // Options state
        // -------------------------------------------------------------------------

        private float _masterVolume;
        private float _musicVolume;
        private float _sfxVolume;
        private int   _languageIndex;
        private int   _graphicsQuality;
        private bool  _fullscreen;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            LoadOptionsFromPrefs();
            ApplyOptions();
        }

        private void Start()
        {
            UpdateContinueVisibility();
            ShowPanel(TitlePanel.Main);
        }

        // -------------------------------------------------------------------------
        // Panel navigation
        // -------------------------------------------------------------------------

        /// <summary>Switch to <paramref name="panel"/>, hiding all others.</summary>
        public void ShowPanel(TitlePanel panel)
        {
            ActivePanel = panel;
            SetPanelVisible(panelMain,     panel == TitlePanel.Main);
            SetPanelVisible(panelLoadGame, panel == TitlePanel.LoadGame);
            SetPanelVisible(panelGallery,  panel == TitlePanel.Gallery);
            SetPanelVisible(panelOptions,  panel == TitlePanel.Options);
            SetPanelVisible(panelCredits,  panel == TitlePanel.Credits);
            SetPanelVisible(panelExtras,   panel == TitlePanel.Extras);

#if TITLESCREEN_EM
            EventManager.Runtime.EventManager.Raise("TitlePanelChanged", panel.ToString());
#endif
        }

        // Convenience wrappers suitable for Button.onClick UnityEvents
        public void ShowMainPanel() => ShowPanel(TitlePanel.Main);
        public void ShowLoadGame()  => ShowPanel(TitlePanel.LoadGame);
        public void ShowOptions()   => ShowPanel(TitlePanel.Options);
        public void ShowCredits()   => ShowPanel(TitlePanel.Credits);
        public void ShowExtras()    => ShowPanel(TitlePanel.Extras);

        /// <summary>
        /// Navigate to the Gallery panel and notify GalleryManager (requires <c>TITLESCREEN_GM</c>).
        /// </summary>
        public void ShowGallery()
        {
            ShowPanel(TitlePanel.Gallery);
#if TITLESCREEN_GM
            // GalleryManager can react to panel open (e.g. refresh UI)
            var gm = FindObjectOfType<GalleryManager.Runtime.GalleryManager>();
            gm?.GetUnlockedEntries(); // touch to ensure cache is warm
#endif
        }

        private static void SetPanelVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha          = visible ? 1f : 0f;
            group.interactable   = visible;
            group.blocksRaycasts = visible;
        }

        // -------------------------------------------------------------------------
        // Main menu actions
        // -------------------------------------------------------------------------

        /// <summary>
        /// Start a new game from chapter 1.
        /// Fires <see cref="OnNewGame"/>, then loads <see cref="gameplayScene"/>.
        /// </summary>
        public void NewGame()
        {
            OnNewGame?.Invoke();
#if TITLESCREEN_EM
            EventManager.Runtime.EventManager.Raise("TitleNewGame");
#endif
            LoadGameplayScene();
        }

        /// <summary>
        /// Continue from the last active save slot (requires <c>TITLESCREEN_SM</c>).
        /// Fires <see cref="OnContinue"/>, then loads <see cref="gameplayScene"/>.
        /// </summary>
        public void Continue()
        {
            int slot = GetLastActiveSlot();
            OnContinue?.Invoke(slot);
#if TITLESCREEN_EM
            EventManager.Runtime.EventManager.Raise("TitleContinue", slot.ToString());
#endif
            LoadGameplayScene();
        }

        /// <summary>
        /// Confirm loading a specific save slot selected in the Load Game panel.
        /// Fires <see cref="OnLoadSlot"/>, then loads <see cref="gameplayScene"/>.
        /// </summary>
        public void LoadSlot(int slot)
        {
            OnLoadSlot?.Invoke(slot);
#if TITLESCREEN_EM
            EventManager.Runtime.EventManager.Raise("TitleLoadSlot", slot.ToString());
#endif
            LoadGameplayScene();
        }

        /// <summary>Quit the application (stops Play Mode in the Editor).</summary>
        public void QuitGame()
        {
#if TITLESCREEN_EM
            EventManager.Runtime.EventManager.Raise("TitleQuit");
#endif
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // -------------------------------------------------------------------------
        // Options
        // -------------------------------------------------------------------------

        /// <summary>
        /// Persist current option values to PlayerPrefs.
        /// Fires <see cref="OnOptionsSaved"/>.
        /// </summary>
        public void SaveOptions()
        {
            PlayerPrefs.SetFloat(PrefMasterVolume,    _masterVolume);
            PlayerPrefs.SetFloat(PrefMusicVolume,     _musicVolume);
            PlayerPrefs.SetFloat(PrefSfxVolume,       _sfxVolume);
            PlayerPrefs.SetInt  (PrefLanguage,        _languageIndex);
            PlayerPrefs.SetInt  (PrefGraphicsQuality, _graphicsQuality);
            PlayerPrefs.SetInt  (PrefFullscreen,      _fullscreen ? 1 : 0);
            PlayerPrefs.Save();

            OnOptionsSaved?.Invoke();
#if TITLESCREEN_EM
            EventManager.Runtime.EventManager.Raise("TitleOptionsSaved");
#endif
        }

        /// <summary>Discard unsaved changes and reload options from PlayerPrefs.</summary>
        public void RevertOptions()
        {
            LoadOptionsFromPrefs();
            ApplyOptions();
        }

        // -------------------------------------------------------------------------
        // Options internals
        // -------------------------------------------------------------------------

        private void LoadOptionsFromPrefs()
        {
            _masterVolume    = PlayerPrefs.GetFloat(PrefMasterVolume,    0.8f);
            _musicVolume     = PlayerPrefs.GetFloat(PrefMusicVolume,     0.7f);
            _sfxVolume       = PlayerPrefs.GetFloat(PrefSfxVolume,       0.9f);
            _languageIndex   = PlayerPrefs.GetInt  (PrefLanguage,        0);
            _graphicsQuality = PlayerPrefs.GetInt  (PrefGraphicsQuality, 2);
            _fullscreen      = PlayerPrefs.GetInt  (PrefFullscreen,      1) == 1;
        }

        private void ApplyOptions()
        {
            ApplyVolumes();
            ApplyQuality();
            ApplyFullscreen();
            ApplyLanguage();
        }

        private void ApplyVolumes()
        {
            if (audioMixer == null) return;
            audioMixer.SetFloat(masterVolumeParam, LinearToDecibel(_masterVolume));
            audioMixer.SetFloat(musicVolumeParam,  LinearToDecibel(_musicVolume));
            audioMixer.SetFloat(sfxVolumeParam,    LinearToDecibel(_sfxVolume));
        }

        private void ApplyQuality()
        {
            if (_graphicsQuality >= 0 && _graphicsQuality < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(_graphicsQuality, applyExpensiveChanges: true);
        }

        private void ApplyFullscreen() => Screen.fullScreen = _fullscreen;

        private void ApplyLanguage()
        {
#if TITLESCREEN_LOC
            var settings = UnityEngine.Localization.Settings.LocalizationSettings.Instance;
            if (settings == null) return;
            var locales = settings.GetAvailableLocales().Locales;
            if (_languageIndex >= 0 && _languageIndex < locales.Count)
                UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = locales[_languageIndex];
#endif
        }

        /// <summary>Converts a linear [0,1] volume value to decibels for AudioMixer.</summary>
        private static float LinearToDecibel(float linear) =>
            linear > 0.0001f ? 20f * Mathf.Log10(linear) : -80f;

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void UpdateContinueVisibility()
        {
            if (continueButtonObject == null) return;

#if TITLESCREEN_SM
            var sm = FindObjectOfType<SaveManager.Runtime.SaveManager>();
            bool hasSave = false;
            if (sm != null)
            {
                for (int i = 0; i < sm.MaxSlots; i++)
                {
                    if (sm.HasSave(i)) { hasSave = true; break; }
                }
            }
            continueButtonObject.SetActive(hasSave);
#else
            continueButtonObject.SetActive(false);
#endif
        }

        private int GetLastActiveSlot()
        {
#if TITLESCREEN_SM
            var sm = FindObjectOfType<SaveManager.Runtime.SaveManager>();
            return sm != null ? sm.ActiveSlot : 0;
#else
            return 0;
#endif
        }

        private void LoadGameplayScene()
        {
            if (string.IsNullOrEmpty(gameplayScene))
            {
                Debug.LogWarning("[TitleScreenManager] No gameplay scene specified.");
                return;
            }

#if TITLESCREEN_MLF
            MapLoader.Runtime.MapLoader.LoadScene(gameplayScene);
#else
            SceneManager.LoadScene(gameplayScene);
#endif
        }
    }
}
