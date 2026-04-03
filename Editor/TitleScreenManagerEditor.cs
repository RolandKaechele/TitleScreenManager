#if UNITY_EDITOR
using TitleScreenManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace TitleScreenManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="TitleScreenManager.Runtime.TitleScreenManager"/>.
    /// Validates required references, shows runtime panel controls, and lists gallery unlock state.
    /// </summary>
    [CustomEditor(typeof(TitleScreenManager.Runtime.TitleScreenManager))]
    public class TitleScreenManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            // ── Validation ──────────────────────────────────────────────────────

            var gameplayScene = serializedObject.FindProperty("gameplayScene");
            var panelMain     = serializedObject.FindProperty("panelMain");
            var audioMixer    = serializedObject.FindProperty("audioMixer");

            if (gameplayScene != null && string.IsNullOrEmpty(gameplayScene.stringValue))
                EditorGUILayout.HelpBox(
                    "Gameplay Scene is empty — New Game / Continue / Load will not load any scene.",
                    MessageType.Error);

            if (panelMain != null && panelMain.objectReferenceValue == null)
                EditorGUILayout.HelpBox(
                    "Main Panel is not assigned — panel switching will not work.",
                    MessageType.Warning);

            if (audioMixer != null && audioMixer.objectReferenceValue == null)
                EditorGUILayout.HelpBox(
                    "No Audio Mixer assigned — volume options will not be applied at runtime.",
                    MessageType.Info);

            // ── Runtime controls (Play Mode only) ───────────────────────────────

            if (!Application.isPlaying) return;

            var mgr = (TitleScreenManager.Runtime.TitleScreenManager)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Panel", mgr.ActivePanel.ToString(), EditorStyles.boldLabel);

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Main"))    mgr.ShowMainPanel();
            if (GUILayout.Button("Load"))    mgr.ShowLoadGame();
            if (GUILayout.Button("Gallery")) mgr.ShowGallery();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Options")) mgr.ShowOptions();
            if (GUILayout.Button("Credits")) mgr.ShowCredits();
            if (GUILayout.Button("Extras"))  mgr.ShowExtras();
            EditorGUILayout.EndHorizontal();

            // ── Options display ─────────────────────────────────────────────────

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Options (live)", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Master Volume",    $"{mgr.MasterVolume:P0}");
            EditorGUILayout.LabelField("Music Volume",     $"{mgr.MusicVolume:P0}");
            EditorGUILayout.LabelField("SFX Volume",       $"{mgr.SfxVolume:P0}");
            EditorGUILayout.LabelField("Graphics Quality", QualitySettings.names[mgr.GraphicsQuality]);
            EditorGUILayout.LabelField("Fullscreen",       mgr.Fullscreen.ToString());
            EditorGUILayout.LabelField("Language Index",   mgr.LanguageIndex.ToString());

            // ── Gallery overview (requires TITLESCREEN_GM) ──────────────────────

#if TITLESCREEN_GM
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Gallery (via GalleryManager)", EditorStyles.miniBoldLabel);

            var gm = FindFirstObjectByType<GalleryManager.Runtime.GalleryManager>();
            if (gm == null)
            {
                EditorGUILayout.HelpBox(
                    "TITLESCREEN_GM is active but no GalleryManager found in scene.",
                    MessageType.Warning);
            }
            else
            {
                var entries = gm.Entries;
                if (entries == null || entries.Length == 0)
                {
                    EditorGUILayout.LabelField("  (no entries defined in GalleryManager)");
                }
                else
                {
                    foreach (var e in entries)
                    {
                        if (e == null) continue;
                        bool unlocked = e.alwaysUnlocked || gm.IsUnlocked(e.id);
                        string label  = $"  [{e.type}]  {e.displayName ?? e.id}";
                        EditorGUILayout.LabelField(label, unlocked ? "✓ Unlocked" : "— Locked");
                    }
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlock All (Testing)"))  gm.UnlockAll();
                if (GUILayout.Button("Lock All (Testing)"))    gm.LockAll();
                EditorGUILayout.EndHorizontal();
            }
#else
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Gallery management is handled by GalleryManager.\n" +
                "Add TITLESCREEN_GM to Scripting Define Symbols to enable the integration.",
                MessageType.Info);
#endif
        }
    }
}
#endif
