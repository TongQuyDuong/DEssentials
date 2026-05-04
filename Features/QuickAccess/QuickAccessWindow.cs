using System;
using System.Collections.Generic;
using Dessentials.Serializables;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Dessentials.Features.QuickAccess
{
    /// <summary>
    /// Editor window providing fast access to scenes, configurable groups of folders, and pinned shortcuts.
    /// Drag-drop into a group:
    ///   • all folders, no Shift → added as roots (autoloaded into group)
    ///   • Shift held, or any non-folder → pinned (reference-only)
    /// Drops outside any group are rejected.
    /// </summary>
    public class QuickAccessWindow : EditorWindow
    {
        // ──────────────── Types ────────────────

        private enum ItemSource { Loaded, Pinned }

        // ──────────────── Static UI cache ────────────────

        private static readonly Color DangerColor    = new(0.95f, 0.35f, 0.35f, 1f);
        private static readonly Color PlayTintColor  = new(0.45f, 0.85f, 0.55f, 1f);
        private static readonly Color RootHoverColor = new(0.35f, 0.7f, 1f, 0.45f);    // root mode: soft blue
        private static readonly Color PinHoverColor  = new(1f, 0.78f, 0.35f, 0.5f);    // pin  mode: soft amber

        // Theme-aware separator (translucent line under section titles).
        private static Color SeparatorColor =>
            EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.10f) : new Color(0f, 0f, 0f, 0.15f);

        // GUIStyle init is deferred to first OnGUI because GUI.skin is not available before that.
        private static class Styles
        {
            public static GUIStyle SectionTitle;
            public static GUIStyle GroupTitle;
            public static GUIStyle AssetButton;     // left-aligned for readable names
            public static GUIStyle PlayButton;
            public static GUIStyle GroupBox;
            public static GUIStyle DropHint;

            public static void Ensure()
            {
                if (SectionTitle != null) return;

                SectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    margin = new RectOffset(2, 2, 6, 0),
                };

                GroupTitle = new GUIStyle(EditorStyles.foldout) { fontSize = 13, fontStyle = FontStyle.Bold };

                AssetButton = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(8, 4, 2, 2),
                };

                PlayButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };

                GroupBox = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(2, 2, 4, 4),
                };

                DropHint = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic,
                };
            }
        }

        private static class Content
        {
            // Stable text — eager init.
            public static readonly GUIContent AddGroup       = new("Add Group", "Append a new group");
            public static readonly GUIContent Edit           = new("Edit", "Toggle inline editor");
            public static readonly GUIContent MoveUp         = new("▲", "Move group up");
            public static readonly GUIContent MoveDown       = new("▼", "Move group down");
            public static readonly GUIContent Ping           = new("Ping", "Highlight in Project window");
            public static readonly GUIContent Remove         = new("Remove");
            public static readonly GUIContent PlayGame       = new("▶ Play Game", "Open the loading scene and enter Play mode");
            public static readonly GUIContent Favourite      = new("Favourite");
            public static readonly GUIContent BuildScenes    = new("Build Scenes");
            public static readonly GUIContent TitleField     = new("Title");
            public static readonly GUIContent LoadAssets     = new("Load Assets");
            public static readonly GUIContent LoadSubfolders = new("Load Subfolders");
            public static readonly GUIContent RootFolders    = new("Root Folders");
            public static readonly GUIContent PinnedSection  = new("📌 Pinned", "Items pinned directly to this group (folders here are reference-only).");
            public static readonly GUIContent LoadedSection  = new("From root folders");
            public static readonly GUIContent EmptyRoots     = new("Drop folders here to add as roots.");
            public static readonly GUIContent EmptyGroup     = new("Drop a folder to add as root, or hold Shift while dropping to pin.");

            // Icon-bearing content — lazy-init from EditorGUIUtility (icons require editor skin).
            public static GUIContent Refresh;
            public static GUIContent Trash;     // group/header X
            public static GUIContent TrashSmall; // root folder X (smaller scale)

            private static bool _iconsLoaded;

            public static void EnsureIcons()
            {
                if (_iconsLoaded) return;
                _iconsLoaded = true;

                Refresh    = WithIcon("Refresh",            "Reload all groups and rescan build scenes", "↻");
                Trash      = WithIcon("d_TreeEditor.Trash", "Remove",                                    "X");
                TrashSmall = WithIcon("d_TreeEditor.Trash", "Remove",                                    "×");
            }

            private static GUIContent WithIcon(string iconName, string tooltip, string textFallback)
            {
                Texture image = EditorGUIUtility.IconContent(iconName)?.image;
                return image != null ? new GUIContent(image, tooltip) : new GUIContent(textFallback, tooltip);
            }

            // Dynamic label "Scene: <name>" with cached invalidation by current value.
            private static GUIContent _loadingButton;
            private static string     _loadingButtonCachedFor;

            public static GUIContent LoadingButton(string sceneName)
            {
                if (_loadingButton == null)
                    _loadingButton = new GUIContent { tooltip = "Loading scene played by Play Game" };

                if (_loadingButtonCachedFor != sceneName)
                {
                    _loadingButton.text = string.IsNullOrEmpty(sceneName) ? "Scene: <none>" : "Scene: " + sceneName;
                    _loadingButtonCachedFor = sceneName;
                }
                return _loadingButton;
            }
        }

        private static class Layouts
        {
            public static readonly GUILayoutOption[] TbRefresh    = { GUILayout.Width(50) };
            public static readonly GUILayoutOption[] TbAddGroup   = { GUILayout.Width(100) };
            public static readonly GUILayoutOption[] TbScene      = { GUILayout.Width(180) };
            public static readonly GUILayoutOption[] Mini24       = { GUILayout.Width(24) };
            public static readonly GUILayoutOption[] Mini40       = { GUILayout.Width(40) };
            public static readonly GUILayoutOption[] PlayBtn      = { GUILayout.Height(32) };
            public static readonly GUILayoutOption[] Ping         = { GUILayout.MaxWidth(50) };
            public static readonly GUILayoutOption[] AssetName    = { GUILayout.MinWidth(150) };
            public static readonly GUILayoutOption[] FolderName   = { GUILayout.MinWidth(200) };
            public static readonly GUILayoutOption[] Remove       = { GUILayout.MinWidth(50), GUILayout.MaxWidth(60) };
        }

        // ──────────────── Build-scenes cache (shared across windows) ────────────────

        private static Object[] _buildScenes      = Array.Empty<Object>();
        private static string[] _buildSceneNames  = Array.Empty<string>();
        private static string[] _buildScenePaths  = Array.Empty<string>();
        private static bool     _buildScenesDirty = true;

        // ──────────────── Per-instance state ────────────────

        private QuickAccessConfig _config;
        private Vector2           _scroll;
        private readonly List<Rect> _groupRects = new();
        private int  _hoveredGroup = -1;
        private bool _hoverPinMode;
        private int  _pendingRemoveGroup = -1;

        // ──────────────── Lifecycle ────────────────

        [MenuItem("Tools/Dessentials/Quick Access")]
        private static void ShowWindow()
        {
            var window = GetWindow<QuickAccessWindow>();
            window.titleContent = new GUIContent("Quick Access");
            window.Show();
        }

        private void OnEnable()
        {
            EditorBuildSettings.sceneListChanged += MarkBuildScenesDirty;
            Refresh();
        }

        private void OnDisable()
        {
            EditorBuildSettings.sceneListChanged -= MarkBuildScenesDirty;
        }

        private void OnGUI()
        {
            Styles.Ensure();
            Content.EnsureIcons();
            _config = QuickAccessConfig.instance;
            EnsureBuildScenes();

            if (Event.current.type == EventType.Repaint)
                _groupRects.Clear();

            HandleScrollWheelBoost();

            DrawToolbar();

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawFavouriteSection();
            DrawBuildScenesSection();
            DrawGroupsSection();
            HandleDragAndDrop();
            GUILayout.EndScrollView();

            ApplyPendingRemovals();
        }

        // ──────────────── Build-scenes cache ────────────────

        private static void EnsureBuildScenes()
        {
            if (!_buildScenesDirty) return;

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int n = scenes.Length;
            if (_buildScenes.Length     != n) _buildScenes     = new Object[n];
            if (_buildSceneNames.Length != n) _buildSceneNames = new string[n];
            if (_buildScenePaths.Length != n) _buildScenePaths = new string[n];

            for (int i = 0; i < n; i++)
            {
                string path  = scenes[i].path;
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                _buildScenes[i]     = asset;
                _buildScenePaths[i] = path;
                _buildSceneNames[i] = asset != null ? asset.name : "<missing>";
            }

            _buildScenesDirty = false;
        }

        private static void MarkBuildScenesDirty() => _buildScenesDirty = true;

        // ──────────────── Toolbar ────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(Content.Refresh, EditorStyles.toolbarButton, Layouts.TbRefresh))
                Refresh();
            if (GUILayout.Button(Content.AddGroup, EditorStyles.toolbarButton, Layouts.TbAddGroup))
                _config.AddGroup("New Group", true, true);

            GUILayout.FlexibleSpace();

            GUIContent dropdownLabel = Content.LoadingButton(_config.loadingSceneName);
            if (EditorGUILayout.DropdownButton(dropdownLabel, FocusType.Keyboard, EditorStyles.toolbarDropDown, Layouts.TbScene))
                ShowLoadingSceneMenu();

            EditorGUILayout.EndHorizontal();
        }

        private void ShowLoadingSceneMenu()
        {
            var menu = new GenericMenu();
            if (_buildSceneNames.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes in Build Settings"));
                menu.ShowAsContext();
                return;
            }

            string current = _config.loadingSceneName;
            for (int i = 0; i < _buildSceneNames.Length; i++)
            {
                string name = _buildSceneNames[i]; // captured per-iteration by the closure below
                menu.AddItem(new GUIContent(name), name == current, () =>
                {
                    _config.loadingSceneName = name;
                    _config.Persist();
                });
            }
            menu.ShowAsContext();
        }

        private void Refresh()
        {
            QuickAccessConfig.instance.RebuildAllLoaded();
            MarkBuildScenesDirty();
            Repaint();
        }

        // ──────────────── Top-level sections ────────────────

        private void DrawFavouriteSection()
        {
            DrawSectionTitle(Content.Favourite);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_config.loadingSceneName)))
            {
                Color savedBg = GUI.backgroundColor;
                GUI.backgroundColor = PlayTintColor;
                if (GUILayout.Button(Content.PlayGame, Styles.PlayButton, Layouts.PlayBtn))
                    PlayLoadingScene();
                GUI.backgroundColor = savedBg;
            }

            GUILayout.Space(8);
        }

        private void DrawBuildScenesSection()
        {
            DrawSectionTitle(Content.BuildScenes);
            for (int i = 0; i < _buildScenes.Length; i++)
                DrawAssetRow(_buildScenes[i]);
            GUILayout.Space(8);
        }

        private void DrawGroupsSection()
        {
            if (_config.groups == null) return;
            for (int i = 0; i < _config.groups.Count; i++)
                DrawGroup(i, _config.groups[i]);
        }

        // ──────────────── Group rendering ────────────────

        private void DrawGroup(int index, QuickAccessGroup group)
        {
            bool hovered = _hoveredGroup == index;
            Color savedBg = default;

            if (hovered)
            {
                savedBg = GUI.backgroundColor;
                GUI.backgroundColor = _hoverPinMode ? PinHoverColor : RootHoverColor;
            }

            EditorGUILayout.BeginVertical(Styles.GroupBox);
            if (hovered) GUI.backgroundColor = savedBg;

            DrawGroupHeader(index, group);
            if (group.editExpanded)    DrawGroupEditor(index, group);
            if (group.foldoutExpanded) DrawGroupItems(index, group);

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
                _groupRects.Add(GUILayoutUtility.GetLastRect());
        }

        private void DrawGroupHeader(int index, QuickAccessGroup group)
        {
            EditorGUILayout.BeginHorizontal();

            string title = string.IsNullOrEmpty(group.title) ? "unnamed" : group.title;
            group.foldoutExpanded = EditorGUILayout.Foldout(group.foldoutExpanded, title, true, Styles.GroupTitle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Content.Edit, EditorStyles.miniButton, Layouts.Mini40))
                group.editExpanded = !group.editExpanded;

            int last = _config.groups.Count - 1;
            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button(Content.MoveUp,   EditorStyles.miniButton, Layouts.Mini24)) _config.MoveGroup(index, -1);
            using (new EditorGUI.DisabledScope(index >= last))
                if (GUILayout.Button(Content.MoveDown, EditorStyles.miniButton, Layouts.Mini24)) _config.MoveGroup(index, +1);

            if (DangerButton(Content.Trash, EditorStyles.miniButton, Layouts.Mini24))
                ConfirmRemoveGroup(index, group);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGroupEditor(int index, QuickAccessGroup group)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            float savedLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 110;

            EditorGUI.BeginChangeCheck();
            string title          = EditorGUILayout.TextField(Content.TitleField, group.title);
            bool   loadAssets     = EditorGUILayout.Toggle(Content.LoadAssets, group.loadAssets);
            bool   loadSubfolders = EditorGUILayout.Toggle(Content.LoadSubfolders, group.loadSubfolders);
            if (EditorGUI.EndChangeCheck())
            {
                group.title          = title;
                group.loadAssets     = loadAssets;
                group.loadSubfolders = loadSubfolders;
                group.RebuildLoaded();
                _config.Persist();
            }

            GUILayout.Space(4);
            EditorGUILayout.LabelField(Content.RootFolders, EditorStyles.boldLabel);
            DrawRootsEditor(index, group);

            EditorGUIUtility.labelWidth = savedLabelWidth;
            EditorGUILayout.EndVertical();
        }

        private void DrawRootsEditor(int index, QuickAccessGroup group)
        {
            List<FolderReference> roots = group.roots;
            if (roots == null || roots.Count == 0)
            {
                GUILayout.Label(Content.EmptyRoots, Styles.DropHint);
                return;
            }

            int removeAt = -1;
            for (int j = 0; j < roots.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();

                FolderReference fr = roots[j];
                // Single GUIDToAssetPath call — IsValid would call Path again under the hood.
                string path  = fr != null ? fr.Path : null;
                bool   valid = !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);

                using (new EditorGUI.DisabledScope(!valid))
                {
                    if (GUILayout.Button(valid ? path : "<invalid>", EditorStyles.objectField))
                    {
                        Object folder = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (folder != null) EditorGUIUtility.PingObject(folder);
                    }
                }

                if (DangerButton(Content.TrashSmall, Layouts.Mini24)) removeAt = j;

                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0) _config.RemoveRootFolderFromGroup(index, removeAt);
        }

        private void DrawGroupItems(int index, QuickAccessGroup group)
        {
            bool hasPinned = HasItems(group.pinnedAssets) || HasItems(group.pinnedFolders);
            bool hasLoaded = (group.loadAssets     && HasItems(group.loadedAssets))
                          || (group.loadSubfolders && HasItems(group.loadedSubfolders));

            if (hasPinned)
            {
                DrawSubsectionLabel(Content.PinnedSection);
                DrawAssetRows(group.pinnedAssets,  ItemSource.Pinned, index);
                DrawFolderRows(group.pinnedFolders, ItemSource.Pinned, index);
            }

            if (hasLoaded)
            {
                if (hasPinned) GUILayout.Space(4);
                DrawSubsectionLabel(Content.LoadedSection);
                if (group.loadAssets)     DrawAssetRows(group.loadedAssets,     ItemSource.Loaded, index);
                if (group.loadSubfolders) DrawFolderRows(group.loadedSubfolders, ItemSource.Loaded, index);
            }

            if (!hasPinned && !hasLoaded)
                GUILayout.Label(Content.EmptyGroup, Styles.DropHint);
        }

        private void ConfirmRemoveGroup(int index, QuickAccessGroup group)
        {
            if (EditorUtility.DisplayDialog("Remove group",
                    $"Remove group '{group.title}'? This cannot be undone.", "Remove", "Cancel"))
                _pendingRemoveGroup = index;
        }

        // ──────────────── Item rendering ────────────────

        private void DrawAssetRows(List<Object> list, ItemSource source, int groupIndex)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
                DrawAssetRow(list[i], source, groupIndex);
        }

        private void DrawFolderRows(List<Object> list, ItemSource source, int groupIndex)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
                DrawFolderRow(list[i], source, groupIndex);
        }

        private void DrawAssetRow(Object asset, ItemSource source = ItemSource.Loaded, int groupIndex = -1)
        {
            if (asset == null) return;

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button(Content.Ping, Layouts.Ping))
            {
                EditorGUIUtility.PingObject(asset);
                
                if (AssetDatabase.OpenAsset(asset))
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                }
            }
            
            if (GUILayout.Button(asset.name, Styles.AssetButton, Layouts.AssetName))
            {
                if (AssetDatabase.OpenAsset(asset))
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                }
            }
            
            if (DangerButton(Content.Remove, Layouts.Remove))                          RemoveAsset(asset, source, groupIndex);
            GUILayout.EndHorizontal();
        }

        private void DrawFolderRow(Object folder, ItemSource source = ItemSource.Loaded, int groupIndex = -1)
        {
            if (folder == null) return;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(folder.name, Styles.AssetButton, Layouts.FolderName)) EditorGUIUtility.PingObject(folder);
            if (DangerButton(Content.Remove, Layouts.Remove))                          RemoveFolder(folder, source, groupIndex);
            GUILayout.EndHorizontal();
        }

        private void RemoveAsset(Object asset, ItemSource source, int groupIndex)
        {
            if (source == ItemSource.Pinned) _config.RemovePinnedFromGroup(groupIndex, asset);
            else                             _config.RemoveLoadedAsset(asset);
        }

        private void RemoveFolder(Object folder, ItemSource source, int groupIndex)
        {
            if (source == ItemSource.Pinned) _config.RemovePinnedFromGroup(groupIndex, folder);
            else                             _config.RemoveLoadedFolder(folder);
        }

        // ──────────────── Drag-drop ────────────────

        private void HandleDragAndDrop()
        {
            Event evt = Event.current;
            EventType et = evt.type;

            if (et == EventType.DragExited)
            {
                ClearHover();
                return;
            }
            if (et != EventType.DragUpdated && et != EventType.DragPerform) return;

            Object[] dragged = DragAndDrop.objectReferences;
            if (dragged.Length == 0) return;

            int targetGroup = FindGroupAt(evt.mousePosition);
            if (targetGroup < 0)
            {
                ClearHover();
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.Use();
                return;
            }

            // Shift forces pin mode. Without Shift: all-folders → root, otherwise → pin (files cannot be roots).
            bool pinMode = evt.shift || !AreAllFolders(dragged);

            UpdateHover(targetGroup, pinMode);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (et == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (pinMode) _config.AddPinnedToGroup(targetGroup, dragged);
                else         _config.AddRootFoldersToGroup(targetGroup, dragged);
                ClearHover();
            }
            evt.Use();
        }

        private static bool AreAllFolders(Object[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                string p = AssetDatabase.GetAssetPath(items[i]);
                if (string.IsNullOrEmpty(p) || !AssetDatabase.IsValidFolder(p)) return false;
            }
            return true;
        }

        private int FindGroupAt(Vector2 pos)
        {
            for (int i = 0; i < _groupRects.Count; i++)
                if (_groupRects[i].Contains(pos)) return i;
            return -1;
        }

        private void UpdateHover(int group, bool pinMode)
        {
            if (_hoveredGroup == group && _hoverPinMode == pinMode) return;
            _hoveredGroup = group;
            _hoverPinMode = pinMode;
            Repaint();
        }

        private void ClearHover()
        {
            if (_hoveredGroup == -1 && !_hoverPinMode) return;
            _hoveredGroup = -1;
            _hoverPinMode = false;
            Repaint();
        }

        // ──────────────── Misc ────────────────

        private void HandleScrollWheelBoost()
        {
            if (Event.current.type != EventType.ScrollWheel) return;
            _scroll += Event.current.delta * 10f;
            Event.current.Use();
        }

        private void ApplyPendingRemovals()
        {
            if (_pendingRemoveGroup < 0) return;
            _config.RemoveGroup(_pendingRemoveGroup);
            _pendingRemoveGroup = -1;
            Repaint();
        }

        private void PlayLoadingScene()
        {
            string sceneName = _config.loadingSceneName;
            int idx = Array.IndexOf(_buildSceneNames, sceneName);
            if (idx < 0)
                throw new Exception($"Scene '{sceneName}' set in QuickAccessConfig is not in Build Settings.");

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(_buildScenePaths[idx]);
            EditorApplication.isPlaying = true;
        }

        // ──────────────── Layout helpers ────────────────

        private static void DrawSectionTitle(GUIContent label)
        {
            GUILayout.Space(4);
            GUILayout.Label(label, Styles.SectionTitle);
            DrawSeparator();
            GUILayout.Space(2);
        }

        private static void DrawSubsectionLabel(GUIContent label)
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
        }

        private static void DrawSeparator()
        {
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, SeparatorColor);
        }

        private static bool HasItems(List<Object> list) => list != null && list.Count > 0;

        private static bool DangerButton(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = DangerColor;
            bool clicked = GUILayout.Button(content, style, options);
            GUI.backgroundColor = bg;
            return clicked;
        }

        private static bool DangerButton(GUIContent content, GUILayoutOption[] options)
        {
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = DangerColor;
            bool clicked = GUILayout.Button(content, options);
            GUI.backgroundColor = bg;
            return clicked;
        }
    }
}
