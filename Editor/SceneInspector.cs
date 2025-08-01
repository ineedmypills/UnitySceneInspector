using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ineedmypills.SceneInspector.Editor
{
    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        public static readonly List<Action> LeftToolbarGUI = new List<Action>();
        public static readonly List<Action> RightToolbarGUI = new List<Action>();

        static ToolbarExtender()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            var toolbars = Resources.FindObjectsOfTypeAll(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar"));
            var toolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

            if (toolbar == null) return;

#if UNITY_2021_1_OR_NEWER
            var root = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(toolbar) as VisualElement;
            if (root == null) return;

            var leftZone = root.Q("ToolbarZoneLeftAlign");
            if (leftZone != null && leftZone.Q("SceneInspectorLeftContainer") == null)
            {
                var leftContainer = new IMGUIContainer(DrawLeftZoneGUI)
                {
                    name = "SceneInspectorLeftContainer",
                    style = { flexGrow = 1 }
                };
                leftZone.Add(leftContainer);
            }

            var rightZone = root.Q("ToolbarZoneRightAlign");
            if (rightZone != null && rightZone.Q("SceneInspectorRightContainer") == null)
            {
                var rightContainer = new IMGUIContainer(DrawRightZoneGUI)
                {
                    name = "SceneInspectorRightContainer",
                    style = { flexGrow = 1 }
                };
                rightZone.Add(rightContainer);
            }

            EditorApplication.update -= OnUpdate;
#else
            InitializeOldToolbar(toolbar);
#endif
        }

        private static void DrawToolbar(IEnumerable<Action> elements)
        {
            using (new GUILayout.HorizontalScope())
            {
                foreach (var element in elements)
                {
                    element();
                }
            }
        }

#if UNITY_2021_1_OR_NEWER
        private static void DrawLeftZoneGUI()
        {
            var settings = SceneInspector.CurrentSettings;
            var mainButtonsOnLeft = settings.mainButtonsPosition == SceneInspector.ToolbarPosition.Left;
            DrawToolbar(mainButtonsOnLeft ? LeftToolbarGUI : RightToolbarGUI);
        }
        
        private static void DrawRightZoneGUI()
        {
            var settings = SceneInspector.CurrentSettings;
            var mainButtonsOnLeft = settings.mainButtonsPosition == SceneInspector.ToolbarPosition.Left;
            DrawToolbar(mainButtonsOnLeft ? RightToolbarGUI : LeftToolbarGUI);
        }
#endif

#if !UNITY_2021_1_OR_NEWER
        private const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Assembly s_assembly = typeof(UnityEditor.Editor).Assembly;
        private static readonly Type s_toolbarType = s_assembly.GetType("UnityEditor.Toolbar");
        private static readonly FieldInfo s_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler", FLAGS);
        private static ScriptableObject s_currentToolbar;

#if UNITY_2020_1_OR_NEWER
        private static readonly Type s_iWindowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");
        private static readonly PropertyInfo s_windowBackend = s_assembly.GetType("UnityEditor.GUIView").GetProperty("windowBackend", FLAGS);
        private static readonly PropertyInfo s_viewVisualTree = s_iWindowBackendType.GetProperty("visualTree", FLAGS);
#else
        private static readonly PropertyInfo s_viewVisualTree = s_assembly.GetType("UnityEditor.GUIView").GetProperty("visualTree", FLAGS);
#endif

        private static GUIStyle s_commandStyle = null;

        private static void InitializeOldToolbar(ScriptableObject toolbar)
        {
            s_currentToolbar = toolbar;
#if UNITY_2020_1_OR_NEWER
            var windowBackend = s_windowBackend.GetValue(s_currentToolbar);
            var visualTree = (VisualElement)s_viewVisualTree.GetValue(windowBackend, null);
#else
            var visualTree = (VisualElement)s_viewVisualTree.GetValue(s_currentToolbar, null);
#endif
            var container = visualTree[0] as IMGUIContainer;
            if (container == null) return;
            
            var handler = s_imguiContainerOnGui.GetValue(container) as Action;
            handler -= OnGUI;
            handler += OnGUI;
            s_imguiContainerOnGui.SetValue(container, handler);
            
            EditorApplication.update -= OnUpdate;
        }

        private static void OnGUI()
        {
            if (s_commandStyle == null)
            {
                s_commandStyle = new GUIStyle("Command");
            }

            var screenWidth = EditorGUIUtility.currentViewWidth;
            var settings = SceneInspector.CurrentSettings;

#if UNITY_2019_1_OR_NEWER
            const float playButtonsWidth = 140f;
#else
            const float playButtonsWidth = 100f;
#endif
            var playButtonsPosition = (screenWidth - playButtonsWidth) / 2;
            
            var mainButtonsWidth = SceneInspector.GetMainButtonsWidth();
            const float leftToolsWidth = 80f;
            const float rightCollabWidth = 300f;
            const float gap = 5f;

            if (settings.mainButtonsPosition == SceneInspector.ToolbarPosition.Right)
            {
                var shortcutsRect = new Rect(leftToolsWidth + gap, 2, screenWidth - (leftToolsWidth + gap) - rightCollabWidth - mainButtonsWidth - gap, 19);
                var mainButtonsRect = new Rect(screenWidth - rightCollabWidth - mainButtonsWidth, 2, mainButtonsWidth, 19);
                HandleCustomToolbar(RightToolbarGUI, shortcutsRect);
                HandleCustomToolbar(LeftToolbarGUI, mainButtonsRect);
            }
            else
            {
                var mainButtonsRect = new Rect(leftToolsWidth + gap, 2, mainButtonsWidth, 19);
                var shortcutsRect = new Rect(playButtonsPosition + playButtonsWidth + gap, 2, screenWidth - (playButtonsPosition + playButtonsWidth + gap) - rightCollabWidth, 19);
                HandleCustomToolbar(LeftToolbarGUI, mainButtonsRect);
                HandleCustomToolbar(RightToolbarGUI, shortcutsRect);
            }
        }

        private static void HandleCustomToolbar(IEnumerable<Action> toolbar, Rect rect)
        {
            if (rect.width <= 0) return;

            using (new GUILayout.AreaScope(rect))
            {
                DrawToolbar(toolbar);
            }
        }
#endif
    }

    [InitializeOnLoad]
    public class SceneInspector
    {
        public enum ToolbarPosition { Left, Right }

        [Serializable]
        public class Settings
        {
            public bool onlyIncludedScenes;
            public bool enableShortcuts;
            public bool showShortcutNames = true;
            public bool restoreAfterPlay = true;
            public List<string> shortcuts = new List<string>();
            public string lastOpenedScene;
            
            public bool showPlayButton = true;
            public bool showSceneSwitcher = true;
            public bool showAddSceneButton = true;
            public ToolbarPosition mainButtonsPosition = ToolbarPosition.Left;

            public static string Key => $"ineedmypills:{Application.productName}:scene-inspector-settings";
            public bool ShortcutsValid => enableShortcuts && shortcuts != null && shortcuts.Count > 0;

            public void Save()
            {
                EditorPrefs.SetString(Key, EditorJsonUtility.ToJson(this, true));
                SceneInspector.RepaintToolbar();
            }

            public void Load()
            {
                var json = EditorPrefs.GetString(Key, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    EditorJsonUtility.FromJsonOverwrite(json, this);
                }
            }
        }

        private static class Styles
        {
            private static GUIContent s_playButtonContent;
            private static GUIContent s_addSceneContent;
            private static GUIContent s_settingsContent;
            private static GUIContent s_changeSceneContent;
            private static GUIStyle s_toolbarButtonStyle;

            public static GUIStyle ToolbarButton => s_toolbarButtonStyle ??= new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageLeft
            };

            public static GUIContent PlaySceneContent => s_playButtonContent ??= new GUIContent
            {
                image = EditorGUIUtility.IconContent("Animation.Play").image,
                tooltip = "Enter play mode from first scene defined in build settings."
            };

            public static GUIContent ChangeSceneContent => s_changeSceneContent;

            public static GUIContent AddSceneContent => s_addSceneContent ??= new GUIContent
            {
                image = EditorGUIUtility.IconContent("Toolbar Plus").image,
                tooltip = "Open scene in additive mode"
            };

            public static GUIContent SettingsContent => s_settingsContent ??= new GUIContent
            {
                image = EditorGUIUtility.IconContent("d_Settings").image,
                tooltip = "Scene inspector settings"
            };
            
            public static readonly Color ActiveShortcutColor = Color.cyan;
            public static readonly Color PlayModeEnabledColor = Color.green;
            public static readonly Color PlayModeDisabledColor = Color.red;

            public static void UpdateChangeSceneContent()
            {
                s_changeSceneContent = new GUIContent
                {
                    text = SceneManager.GetActiveScene().name,
                    image = EditorGUIUtility.IconContent("d_SceneAsset Icon").image,
                    tooltip = "Change active scene"
                };
            }
        }

        private static Settings s_settings;
        internal static Settings CurrentSettings => s_settings ??= new Settings();

        private const float MainButtonsHorizontalPadding = 15f;

        internal static float GetMainButtonsWidth()
        {
            float width = 0;
            var buttonStyle = Styles.ToolbarButton;
            var settings = CurrentSettings;

            if (settings.showPlayButton) width += buttonStyle.CalcSize(Styles.PlaySceneContent).x;
            if (settings.showSceneSwitcher) width += EditorStyles.toolbarDropDown.CalcSize(Styles.ChangeSceneContent).x;
            if (settings.showAddSceneButton) width += buttonStyle.CalcSize(Styles.AddSceneContent).x;
            width += buttonStyle.CalcSize(Styles.SettingsContent).x;
            
            width += MainButtonsHorizontalPadding; 

            return width;
        }

        static SceneInspector()
        {
            CurrentSettings.Load();
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
            ToolbarExtender.RightToolbarGUI.Add(OnShortcutsGUI);
            EditorApplication.playModeStateChanged += OnModeChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            Styles.UpdateChangeSceneContent();
        }

        private static void OnActiveSceneChanged(Scene from, Scene to)
        {
            Styles.UpdateChangeSceneContent();
            RepaintToolbar();
        }
        
        internal static void RepaintToolbar()
        {
            var toolbars = Resources.FindObjectsOfTypeAll(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar"));
            if (toolbars.Length > 0)
            {
                UnityEditor.Editor.CreateEditor(toolbars[0]).Repaint();
            }
        }

        private static void OnModeChanged(PlayModeStateChange playModeState)
        {
            CurrentSettings.Load();

            if (!CurrentSettings.restoreAfterPlay || string.IsNullOrEmpty(CurrentSettings.lastOpenedScene)) return;

            if (playModeState == PlayModeStateChange.EnteredEditMode)
            {
                EditorSceneManager.OpenScene(CurrentSettings.lastOpenedScene);
                CurrentSettings.lastOpenedScene = string.Empty;
                CurrentSettings.Save();
            }
        }

        private static void OnToolbarGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    if (CurrentSettings.showPlayButton) CreatePlayButton();
                    if (CurrentSettings.showSceneSwitcher) CreateSceneChangeButton();
                }

                if (CurrentSettings.showAddSceneButton) CreateSceneAddButton();

                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    CreateSettingsButton();
                }
                GUILayout.FlexibleSpace();
            }
        }

        private static void OnShortcutsGUI()
        {
            if (EditorApplication.isPlaying || !CurrentSettings.ShortcutsValid) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < CurrentSettings.shortcuts.Count; ++i)
                {
                    var scenePath = CurrentSettings.shortcuts[i];
                    var isActiveScene = IsActiveScene(scenePath);
                    var sceneName = GetSceneNameFromPath(scenePath);

                    var originalContentColor = GUI.contentColor;
                    if (isActiveScene)
                    {
                        GUI.contentColor = Styles.ActiveShortcutColor;
                    }

                    using (new EditorGUI.DisabledScope(isActiveScene))
                    {
                        var content = new GUIContent
                        {
                            text = CurrentSettings.showShortcutNames ? sceneName : $"{i + 1}",
                            tooltip = sceneName
                        };
                        
                        if (GUILayout.Button(content, Styles.ToolbarButton))
                        {
                            SwitchScene(scenePath);
                        }
                    }
                
                    GUI.contentColor = originalContentColor;
                }
            }
        }

        private static void SwitchScene(object scenePathObj)
        {
            if (scenePathObj is string scenePath && !string.IsNullOrEmpty(scenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
        }

        private static void AddScene(object scenePathObj)
        {
            if (scenePathObj is string scenePath && !string.IsNullOrEmpty(scenePath))
            {
                if (EditorApplication.isPlaying)
                {
                    SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
                }
                else
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }
        }

        private static void CreatePlayButton()
        {
            var originalContentColor = GUI.contentColor;
            GUI.contentColor = EditorApplication.isPlaying ? Styles.PlayModeDisabledColor : Styles.PlayModeEnabledColor;

            using (new EditorGUI.DisabledScope(EditorBuildSettings.scenes.Length == 0))
            {
                if (GUILayout.Button(Styles.PlaySceneContent, Styles.ToolbarButton) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    CurrentSettings.lastOpenedScene = SceneManager.GetActiveScene().path;
                    CurrentSettings.Save();
                    EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
                    EditorApplication.isPlaying = true;
                }
            }

            GUI.contentColor = originalContentColor;
        }

        private static void CreateSceneChangeButton()
        {
            if (GUILayout.Button(Styles.ChangeSceneContent, EditorStyles.toolbarDropDown) && !EditorApplication.isPlaying)
            {
                var menu = new GenericMenu();
                FillScenesMenu(menu, SwitchScene);
                menu.ShowAsContext();
            }
        }

        private static void CreateSceneAddButton()
        {
            if (GUILayout.Button(Styles.AddSceneContent, Styles.ToolbarButton))
            {
                var menu = new GenericMenu();
                FillScenesMenu(menu, AddScene, false);
                menu.ShowAsContext();
            }
        }

        private static void FillScenesMenu(GenericMenu menu, GenericMenu.MenuFunction2 callback, bool markActiveSceneAsChecked = true)
        {
            var scenePaths = GetScenes();
            foreach (var path in scenePaths)
            {
                menu.AddItem(new GUIContent(GetSceneNameFromPath(path)), IsActiveScene(path) && markActiveSceneAsChecked, callback, path);
            }
        }

        private static string[] GetScenes()
        {
            if (CurrentSettings.onlyIncludedScenes && EditorBuildSettings.scenes.Length > 0)
            {
                return EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            }

            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets", "Packages" });
            if (sceneGuids == null || sceneGuids.Length == 0)
            {
                return Array.Empty<string>();
            }
            return sceneGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        }

        private static void CreateSettingsButton()
        {
            if (GUILayout.Button(Styles.SettingsContent, Styles.ToolbarButton))
            {
                var menu = new GenericMenu();

                AddSettingsMenuItems(menu);
                AddViewMenuItems(menu);
                AddShortcutsMenuItems(menu);
                
                menu.AddSeparator("");
                AddCreateSceneMenuItems(menu);

                menu.ShowAsContext();
            }
        }

        private static void AddSettingsMenuItems(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Settings/Only use build scenes"), CurrentSettings.onlyIncludedScenes, () =>
            {
                CurrentSettings.onlyIncludedScenes = !CurrentSettings.onlyIncludedScenes;
                CurrentSettings.Save();
            });

            menu.AddItem(new GUIContent("Settings/Enable shortcuts"), CurrentSettings.enableShortcuts, () =>
            {
                CurrentSettings.enableShortcuts = !CurrentSettings.enableShortcuts;
                CurrentSettings.Save();
            });

            if (CurrentSettings.enableShortcuts)
            {
                menu.AddItem(new GUIContent("Settings/Show shortcut names"), CurrentSettings.showShortcutNames, () =>
                {
                    CurrentSettings.showShortcutNames = !CurrentSettings.showShortcutNames;
                    CurrentSettings.Save();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Settings/Show shortcut names"));
            }

            menu.AddItem(new GUIContent("Settings/Restore scene on exit"), CurrentSettings.restoreAfterPlay, () =>
            {
                CurrentSettings.restoreAfterPlay = !CurrentSettings.restoreAfterPlay;
                CurrentSettings.Save();
            });
        }

        private static void AddViewMenuItems(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("View/Show Play Button"), CurrentSettings.showPlayButton, () =>
            {
                CurrentSettings.showPlayButton = !CurrentSettings.showPlayButton;
                CurrentSettings.Save();
            });
            menu.AddItem(new GUIContent("View/Show Scene Switcher"), CurrentSettings.showSceneSwitcher, () =>
            {
                CurrentSettings.showSceneSwitcher = !CurrentSettings.showSceneSwitcher;
                CurrentSettings.Save();
            });
            menu.AddItem(new GUIContent("View/Show Add Scene Button"), CurrentSettings.showAddSceneButton, () =>
            {
                CurrentSettings.showAddSceneButton = !CurrentSettings.showAddSceneButton;
                CurrentSettings.Save();
            });
            
            menu.AddSeparator("View/");

            menu.AddItem(new GUIContent("View/Position/Left"), CurrentSettings.mainButtonsPosition == ToolbarPosition.Left, () => SetToolbarPosition(ToolbarPosition.Left));
            menu.AddItem(new GUIContent("View/Position/Right"), CurrentSettings.mainButtonsPosition == ToolbarPosition.Right, () => SetToolbarPosition(ToolbarPosition.Right));
        }

        private static void AddShortcutsMenuItems(GenericMenu menu)
        {
            if (!CurrentSettings.enableShortcuts) return;
            
            FetchShortcutScenes(menu);
            menu.AddSeparator("Shortcuts/");
            menu.AddItem(new GUIContent("Shortcuts/Clear"), false, () =>
            {
                CurrentSettings.shortcuts.Clear();
                CurrentSettings.Save();
            });
        }

        private static void AddCreateSceneMenuItems(GenericMenu menu)
        {
            void AddNewScene(string label, NewSceneSetup setup, NewSceneMode mode)
            {
                menu.AddItem(new GUIContent($"Create Scene/{label}"), false, () => EditorSceneManager.NewScene(setup, mode));
            }

            AddNewScene("Empty", NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddNewScene("Empty (Additive)", NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            AddNewScene("Default", NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            AddNewScene("Default (Additive)", NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
        }

        private static void SetToolbarPosition(ToolbarPosition position)
        {
            CurrentSettings.mainButtonsPosition = position;
            CurrentSettings.Save();
        }

        private static void FetchShortcutScenes(GenericMenu menu)
        {
            CurrentSettings.shortcuts ??= new List<string>();

            var scenes = GetScenes();
            foreach (var path in scenes)
            {
                var sceneName = GetSceneNameFromPath(path);
                var isShortcut = CurrentSettings.shortcuts.Contains(path);

                menu.AddItem(new GUIContent("Shortcuts/" + sceneName), isShortcut, () =>
                {
                    if (isShortcut)
                    {
                        CurrentSettings.shortcuts.Remove(path);
                    }
                    else
                    {
                        CurrentSettings.shortcuts.Add(path);
                    }
                    CurrentSettings.Save();
                });
            }
        }

        private static string GetSceneNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "Invalid Scene Path";
            }
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        private static bool IsActiveScene(string scenePath) => scenePath == SceneManager.GetActiveScene().path;
    }
}
