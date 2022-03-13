using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
#if JSOU_ADDRESSABLE_SUPPORT
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace OOTL.JavascriptOnUnity.Editor.Scripts.Addressables
{
    public class AddressableSupport : EditorWindow
    {
        private const string Title = "Addressable Support";
        private const string PackageTitle = Common.PackageTitle;

        private const string RequiredDefineSymbol = "JSOU_ADDRESSABLE_SUPPORT";

        private static AddressableSupport _window;

        private static AddressableSupport Window
        {
            get
            {
                if (null == _window)
                {
                    _window = GetWindow<AddressableSupport>();
                }

                return _window;
            }
        }

#if JSOU_ADDRESSABLE_SUPPORT
        [SerializeField]
        private AddressableAssetGroup _targetAssetGroup;
#endif

        private Rect _rcInspectorArea;

        [MenuItem(PackageTitle + "/Open " + Title)]
        private static void ShowWindow()
        {
            var window = GetWindow<AddressableSupport>();
            window.titleContent = new GUIContent(Title);
            window.Show();
        }

#if JSOU_ADDRESSABLE_SUPPORT
        private void AddOrMoveSelectedToTarget(AddressableAssetGroup target)
        {
            var guids = Selection.assetGUIDs;
            var javascripts = new HashSet<(string Guid, string Filename)>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var extension = Path.GetExtension(path);
                if (!string.IsNullOrEmpty(extension) && extension != ".js")
                {
                    continue;
                }

                if (string.IsNullOrEmpty(extension))
                {
                    var absolutePath = Regex.Replace(
                        Path.Combine(Application.dataPath, path.Replace("Assets/", "")),
                        @"[\\/]", Path.DirectorySeparatorChar.ToString());
                    var paths = Directory
                        .GetFiles(absolutePath, @"*.js", SearchOption.AllDirectories)
                        .Where(absPath => !Path.GetFileName(absPath).StartsWith("."))
                        .Select(absPath =>
                        {
                            absPath = absPath.Replace(Path.DirectorySeparatorChar, '/');
                            return absPath.Replace(Application.dataPath, "Assets");
                        });

                    var textAssets = paths.Select(assetPath =>
                        (AssetDatabase.AssetPathToGUID(assetPath), Path.GetFileName(assetPath)));
                    foreach (var asset in textAssets)
                    {
                        javascripts.Add(asset);
                    }
                }
                else
                {
                    javascripts.Add((guid, Path.GetFileName(path)));
                }
            }

            AddOrMoveToTarget(javascripts, target);
        }

        private void AddOrMoveToTarget(IEnumerable<(string Guid, string Filename)> newAssets,
            AddressableAssetGroup target)
        {
            foreach (var (guid, filename) in newAssets)
            {
                var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, target);
                entry.SetAddress(Path.GetFileNameWithoutExtension(filename));
            }
        }

        private void DrawTargetAssetGroup()
        {
            var oldIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUILayout.LabelField("Add Javascripts to an Addressable Asset Group");
                ++EditorGUI.indentLevel;

                EditorGUILayout.Separator();

                _targetAssetGroup = EditorGUILayout.ObjectField(new GUIContent("Target"), _targetAssetGroup,
                    typeof(AddressableAssetGroup), false) as AddressableAssetGroup;

                var oldValue = GUI.enabled;
                GUI.enabled = _targetAssetGroup;

                EditorGUILayout.Separator();

                try
                {
                    if (GUILayout.Button("Add or Move selected to target"))
                    {
                        AddOrMoveSelectedToTarget(_targetAssetGroup);
                    }
                }
                finally
                {
                    GUI.enabled = oldValue;
                }
            }
            finally
            {
                EditorGUI.indentLevel = oldIndentLevel;
            }
        }
#endif

        private void DrawBase()
        {
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                out var symbolsAsArray);

            var symbols = new HashSet<string>(symbolsAsArray);
            if (symbols.Contains(RequiredDefineSymbol))
            {
                return;
            }

            var style = new GUIStyle("Button") { stretchHeight = true };
            if (GUILayout.Button("Activate Addressable Support", style))
            {
                symbols.Add(RequiredDefineSymbol);

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    symbols.ToArray());
            }
        }

        private void DrawInspector()
        {
            using var area = new GUILayout.AreaScope(_rcInspectorArea);

            DrawBase();

#if JSOU_ADDRESSABLE_SUPPORT
            DrawTargetAssetGroup();
#endif
        }

        private void OnGUI()
        {
            Restore();

            DrawInspector();
        }

        private void Restore()
        {
            ResetArea();
        }

        private void ResetArea()
        {
            var windowRight = Window.position.width - 2f;
            var windowBottom = Window.position.height - 2f;

            var area = Rect.MinMaxRect(2f, 2f, windowRight, windowBottom);

            _rcInspectorArea = Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, area.yMax);
        }
    }
}