#if TITLESCREEN_DOTWEEN
using UnityEngine;
using DG.Tweening;

namespace TitleScreenManager.Runtime
{
    /// <summary>
    /// Optional bridge that replaces the instant panel visibility toggles in
    /// <see cref="TitleScreenManager"/> with DOTween-driven fade and slide transitions.
    /// Enable define <c>TITLESCREEN_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Wire your UI buttons to this bridge's <c>Show*WithTween()</c> wrapper methods instead of
    /// (or in addition to) the corresponding <see cref="TitleScreenManager"/> methods to get
    /// animated panel transitions.
    /// </para>
    /// </summary>
    [AddComponentMenu("TitleScreenManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenTitleScreenBridge : MonoBehaviour
    {
        [Header("Panel Canvas Groups")]
        [Tooltip("CanvasGroup for the main menu panel — same as TitleScreenManager.panelMain.")]
        [SerializeField] private CanvasGroup panelMain;

        [Tooltip("CanvasGroup for the load-game panel — same as TitleScreenManager.panelLoadGame.")]
        [SerializeField] private CanvasGroup panelLoadGame;

        [Tooltip("CanvasGroup for the gallery panel — same as TitleScreenManager.panelGallery.")]
        [SerializeField] private CanvasGroup panelGallery;

        [Tooltip("CanvasGroup for the options panel — same as TitleScreenManager.panelOptions.")]
        [SerializeField] private CanvasGroup panelOptions;

        [Tooltip("CanvasGroup for the credits panel — same as TitleScreenManager.panelCredits.")]
        [SerializeField] private CanvasGroup panelCredits;

        [Tooltip("CanvasGroup for the extras panel — same as TitleScreenManager.panelExtras.")]
        [SerializeField] private CanvasGroup panelExtras;

        [Header("Transition")]
        [Tooltip("Duration of each panel fade-in.")]
        [SerializeField] private float fadeInDuration = 0.25f;

        [Tooltip("Duration of each panel fade-out.")]
        [SerializeField] private float fadeOutDuration = 0.15f;

        [Tooltip("DOTween ease for panel fade-in.")]
        [SerializeField] private Ease fadeInEase = Ease.OutQuad;

        [Tooltip("DOTween ease for panel fade-out.")]
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;

        [Header("Anchor Slide")]
        [Tooltip("When true, the incoming panel slides in from an offset position.")]
        [SerializeField] private bool useSlide = true;

        [Tooltip("Pixel offset from which the incoming panel slides in.")]
        [SerializeField] private Vector2 slideOffset = new Vector2(0f, -30f);

        [Tooltip("DOTween ease for the slide animation.")]
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        // -------------------------------------------------------------------------

        private TitleScreenManager _tsm;

        private void Awake()
        {
            _tsm = GetComponent<TitleScreenManager>() ?? FindFirstObjectByType<TitleScreenManager>();
            if (_tsm == null) Debug.LogWarning("[TitleScreenManager/DotweenTitleScreenBridge] TitleScreenManager not found.");
        }

        // ── Public wrappers — wire UI buttons here instead of TitleScreenManager ──

        /// <summary>Transition to the main panel with DOTween.</summary>
        public void ShowMainWithTween()    => TransitionToPanel(TitlePanel.Main);

        /// <summary>Transition to the load-game panel with DOTween.</summary>
        public void ShowLoadGameWithTween() => TransitionToPanel(TitlePanel.LoadGame);

        /// <summary>Transition to the gallery panel with DOTween.</summary>
        public void ShowGalleryWithTween()  => TransitionToPanel(TitlePanel.Gallery);

        /// <summary>Transition to the options panel with DOTween.</summary>
        public void ShowOptionsWithTween()  => TransitionToPanel(TitlePanel.Options);

        /// <summary>Transition to the credits panel with DOTween.</summary>
        public void ShowCreditsWithTween()  => TransitionToPanel(TitlePanel.Credits);

        /// <summary>Transition to the extras panel with DOTween.</summary>
        public void ShowExtrasWithTween()   => TransitionToPanel(TitlePanel.Extras);

        // -------------------------------------------------------------------------

        private void TransitionToPanel(TitlePanel target)
        {
            // Get incoming / outgoing groups before calling ShowPanel.
            var incoming = GetGroup(target);
            var outgoing = _tsm != null ? GetGroup(_tsm.ActivePanel) : null;

            // Let TitleScreenManager handle logic (continues, events, etc.).
            _tsm?.ShowPanel(target);

            // Animate outgoing panel out.
            if (outgoing != null && outgoing != incoming)
            {
                DOTween.Kill(outgoing);
                outgoing.DOFade(0f, fadeOutDuration).SetEase(fadeOutEase);
            }

            // Animate incoming panel in.
            if (incoming != null)
            {
                incoming.alpha = 0f;
                DOTween.Kill(incoming);
                incoming.DOFade(1f, fadeInDuration).SetEase(fadeInEase);

                if (useSlide)
                {
                    var rt = incoming.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 dest = rt.anchoredPosition;
                        rt.anchoredPosition = dest + slideOffset;
                        DOTween.Kill(rt);
                        rt.DOAnchorPos(dest, fadeInDuration).SetEase(slideEase);
                    }
                }
            }
        }

        private CanvasGroup GetGroup(TitlePanel panel) => panel switch
        {
            TitlePanel.Main     => panelMain,
            TitlePanel.LoadGame => panelLoadGame,
            TitlePanel.Gallery  => panelGallery,
            TitlePanel.Options  => panelOptions,
            TitlePanel.Credits  => panelCredits,
            TitlePanel.Extras   => panelExtras,
            _                   => null
        };
    }
}
#else
namespace TitleScreenManager.Runtime
{
    /// <summary>No-op stub — enable define <c>TITLESCREEN_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("TitleScreenManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenTitleScreenBridge : UnityEngine.MonoBehaviour { }
}
#endif
