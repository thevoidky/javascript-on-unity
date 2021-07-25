using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Modules.Editor
{
    public class JavascriptBuilder : EditorWindow
    {
        private const string Title = "Javascript Builder";
        private const string PackageTitle = "Javascript on Unity";

#if OOTL_DEV_LOCAL
        private const string RootPath = "Assets/Modules/javascript-on-unity/Editor";
#else
        private const string RootPath = "Packages/" + PackageTitle + "/Editor";
#endif

        private static JavascriptBuilder _window = null;

        private static JavascriptBuilder Window
        {
            get
            {
                if (null == _window)
                {
                    _window = GetWindow<JavascriptBuilder>();
                }

                return _window;
            }
        }

        private static string ProjectPath =>
            Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/Assets", StringComparison.Ordinal));

        private static string LocalAssetsPath => $"Assets/{PackageTitle}";
        private static string LocalAssetFullPath => $"{ProjectPath}/{LocalAssetsPath}";
        private static string SettingsAssetPath => $"{LocalAssetsPath}/Editor/Build Settings.asset";
        private static string SettingsFullPath => $"{LocalAssetFullPath}/Editor/Build Settings.asset";

        private static string Workspace => $"{ProjectPath}/{RootPath}";
        private static string BuilderPath => $"{Workspace}/builder.sh";

#if OOTL_DEV_LOCAL
        private static string NodeModulesPath => $"{Workspace}/node_modules";
        private static string InstallerPath => $"{Workspace}/installer.sh";
#endif

        private static string EntryPath => $"{LocalAssetsPath}/Editor/entry.json";
        private static string OutputPath => $"{LocalAssetsPath}/Editor/output.json";

        private static readonly Regex PathReplacer = new Regex(@"[/\\]", RegexOptions.Compiled);

        private static BuildSettings _buildSettings;

        private Rect _rcControlArea;
        private Rect _rcButtonArea;

        [MenuItem("Javascript on Unity/Open Javascript Builder")]
        private static void ShowWindow()
        {
            var window = GetWindow<JavascriptBuilder>();
            window.titleContent = new GUIContent(Title);
            window.Show();
        }

        private void OnGUI()
        {
            Restore();

            DrawControlPanel();
            DrawButtonPanel();
        }

        private void Restore()
        {
            ResetArea();

            if (null == _buildSettings)
            {
                if (!File.Exists(SettingsFullPath))
                {
                    Directory.CreateDirectory(LocalAssetsPath + "/Editor");

                    var asset = CreateInstance<BuildSettings>();
                    var assetName = AssetDatabase.GenerateUniqueAssetPath(SettingsAssetPath);
                    AssetDatabase.CreateAsset(asset, assetName);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                _buildSettings = AssetDatabase.LoadAssetAtPath<BuildSettings>(SettingsAssetPath);
            }
        }

        private void ResetArea()
        {
            var windowRight = Window.position.width - 4f;
            var windowBottom = Window.position.height - 4f;

            _rcControlArea = Rect.MinMaxRect(4f, 4f, windowRight, windowBottom - 40f);
            _rcButtonArea = Rect.MinMaxRect(4f, _rcControlArea.yMax, windowRight, windowBottom);
        }

        private void DrawControlPanel()
        {
            using (new GUILayout.AreaScope(_rcControlArea))
            {
                GUILayout.Space(8f);

                if (null != _buildSettings)
                {
                    var so = new SerializedObject(_buildSettings);

                    var rawScriptRoots = new ReorderableList(so, so.FindProperty("rawScriptRoots"));
                    EditorGUILayout.PropertyField(rawScriptRoots.serializedProperty, new GUIContent("Raw Script Roots"),
                        true);

                    var builtScriptRoot = so.FindProperty("builtScriptRoot");
                    EditorGUILayout.PropertyField(builtScriptRoot, new GUIContent("Built Script Root"));

                    var isDevBuild = so.FindProperty("isDevBuild");
                    EditorGUILayout.PropertyField(isDevBuild, new GUIContent("Dev Build"));

                    so.ApplyModifiedProperties();
                }
            }
        }

        private void DrawButtonPanel()
        {
            using (new GUILayout.AreaScope(_rcButtonArea))
            {
#if OOTL_DEV_LOCAL
                if (!Directory.Exists(NodeModulesPath))
                {
                    if (GUILayout.Button("Install npm modules", GUILayout.Height(_rcButtonArea.height)))
                    {
                        Install();
                    }
                }
                else
#endif
                {
                    if (GUILayout.Button("Build", GUILayout.Height(_rcButtonArea.height)))
                    {
                        Build();
                    }
                }
            }
        }

        private static void Install()
        {
#if OOTL_DEV_LOCAL
            if (Directory.Exists(NodeModulesPath))
            {
                return;
            }

            var match = Regex.Match(Workspace, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = PathReplacer
                .Replace(match.Success ? match.Groups[2].Value : Workspace, $"{Path.DirectorySeparatorChar}")
                .Replace(":", "");

            var parameters = $"\"{workspace}\" {drive}";

            var process = Process.Start(InstallerPath, parameters);
            process?.WaitForExit();

            AssetDatabase.Refresh();
#endif
        }

        private static void Build()
        {
            var match = Regex.Match(Workspace, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = match.Success ? match.Groups[2].Value : Workspace;

            static string AssetPathToAbsolutePath(string assetPath)
            {
                return PathReplacer.Replace(
                    Path.Combine(Application.dataPath, Regex.Replace(assetPath, @"^Assets[/\\]", "")),
                    $"{Path.DirectorySeparatorChar}");
            }

            void GenerateMetadata()
            {
                var rawScriptPaths = _buildSettings.rawScriptRoots
                    .SelectMany(defaultAsset =>
                        Directory.GetFiles(AssetPathToAbsolutePath(AssetDatabase.GetAssetPath(defaultAsset)), @"*.js",
                            SearchOption.AllDirectories));

                var builtScriptRoot = AssetDatabase.GetAssetPath(_buildSettings.builtScriptRoot);

                var output = new Dictionary<string, string>();
                output.Add("path", AssetPathToAbsolutePath(builtScriptRoot));

                var outputContent = JsonConvert.SerializeObject(output);
                File.WriteAllText(OutputPath, outputContent);

                var entry = new Dictionary<string, string>();
                foreach (var scriptPath in rawScriptPaths)
                {
                    var filename = Path.GetFileName(scriptPath);
                    entry.Add(Path.GetFileNameWithoutExtension(filename), scriptPath);
                }

                var entryContent = JsonConvert.SerializeObject(entry);
                File.WriteAllText(EntryPath, entryContent, Encoding.UTF8);
            }

            GenerateMetadata();

            workspace = PathReplacer.Replace(workspace, $"{Path.DirectorySeparatorChar}").Replace(":", "");
            var isDevBuild = _buildSettings.isDevBuild.ToString().ToLower();
            var parameters = $"\"{workspace}\" {drive} {isDevBuild}";

            Process.Start(BuilderPath, parameters);
        }
    }
}