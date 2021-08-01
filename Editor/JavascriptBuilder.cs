// #undef OOTL_DEV_LOCAL


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
using Debug = UnityEngine.Debug;

namespace Modules.Editor
{
    public class JavascriptBuilder : EditorWindow
    {
        private const string Title = "Javascript Builder";
        private const string PackageTitle = "Javascript on Unity";
        private const string PackageName = "com.ootl.jsou";

#if OOTL_DEV_LOCAL
        private const string RootPath = "Assets/Modules/javascript-on-unity/Editor";
#else
        private static string RootFullPath
        {
            get
            {
                var directories = Directory.GetDirectories($"{ProjectPath}/Library/PackageCache", $@"{PackageName}@*",
                    SearchOption.TopDirectoryOnly);

                return directories.Length == 0 ? null : $"{directories.First()}/Editor";
            }
        }
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

#if OOTL_DEV_LOCAL
        private static string Workspace => $"{ProjectPath}/{RootPath}";
        private static string SourceInstallerPath => $"{Workspace}/installer.sh";
        private static string SourceBuilderPath => $"{Workspace}/builder.sh";
        private static string InstallerPath => $"{NodeModulesParentPath}/installer.sh";
        private static string BuilderPath => $"{NodeModulesParentPath}/builder.sh";
        private static IEnumerable<string> FilesToCopy => new[]
        {
            SourceInstallerPath,
            SourceBuilderPath,
            $"{Workspace}/package.json",
            $"{Workspace}/package-lock.json",
            $"{Workspace}/webpack.config.babel.js",
            $"{Workspace}/.babelrc",
        };
#else
        private static string SourceInstallerPath => $"{RootFullPath}/installer.sh";
        private static string SourceBuilderPath => $"{RootFullPath}/builder.sh";
        private static string InstallerPath => $"{NodeModulesParentPath}/installer.sh";
        private static string BuilderPath => $"{NodeModulesParentPath}/builder.sh";
        private static IEnumerable<string> FilesToCopy => new[]
        {
            SourceInstallerPath,
            SourceBuilderPath,
            $"{RootFullPath}/package.json",
            $"{RootFullPath}/package-lock.json",
            $"{RootFullPath}/webpack.config.babel.js",
            $"{RootFullPath}/.babelrc",
        };
#endif
        private static string NodeModulesParentPath => RawScriptPath;
        private static string NodeModulesPath => ReplacePath($"{NodeModulesParentPath}/node_modules");

        private static string RawScriptPath =>
            !RawScriptRoot
                ? string.Empty
                : ReplacePath($"{ProjectPath}/{AssetDatabase.GetAssetPath(RawScriptRoot)}");

        private static string EntryPath => ReplacePath($"{NodeModulesParentPath}/entry.json");
        private static string OutputPath => ReplacePath($"{NodeModulesParentPath}/output.json");

        private static readonly Regex PathReplacer = new Regex(@"[/\\]", RegexOptions.Compiled);

        private static string ReplacePath(string path) =>
            PathReplacer.Replace(path, Path.DirectorySeparatorChar.ToString());

        private static BuildSettings _buildSettings;

        private Rect _rcControlArea;
        private Rect _rcButtonArea;

        public static DefaultAsset RawScriptRoot
        {
            get => _buildSettings.rawScriptRoot;
            set
            {
                var so = new SerializedObject(_buildSettings);
                var builtScriptRoot = so.FindProperty("rawScriptRoot");

                if (value == null)
                {
                    builtScriptRoot.objectReferenceValue = value;
                    so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return;
                }

                var path = AssetDatabase.GetAssetPath(value);
                var fullPath = Path.Combine(ProjectPath, path);
                if (!Directory.Exists(fullPath))
                {
                    Debug.LogError($"Input directory is not exist. ({value.name})");
                    return;
                }

                builtScriptRoot.objectReferenceValue = value;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                /*
                var so = new SerializedObject(_buildSettings);

                var rawScriptRoots = new ReorderableList(so, so.FindProperty("rawScriptRoots"));
                rawScriptRoots.serializedProperty.ClearArray();

                if (value == null)
                {
                    so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return;
                }

                foreach (var asset in value)
                {
                    var path = AssetDatabase.GetAssetPath(asset);
                    var fullPath = Path.Combine(ProjectPath, path);
                    if (!Directory.Exists(fullPath))
                    {
                        Debug.LogError($"Input directory is not exist. ({asset.name})");
                        return;
                    }
                }

                for (var i = 0; i < value.Length; ++i)
                {
                    rawScriptRoots.serializedProperty.InsertArrayElementAtIndex(i);
                    var element = rawScriptRoots.serializedProperty.GetArrayElementAtIndex(i);
                    element.objectReferenceValue = value[i];
                }

                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                */
            }
        }

        public static DefaultAsset BuiltScriptRoot
        {
            get => _buildSettings.builtScriptRoot;
            set
            {
                var so = new SerializedObject(_buildSettings);
                var builtScriptRoot = so.FindProperty("builtScriptRoot");

                if (value == null)
                {
                    builtScriptRoot.objectReferenceValue = value;
                    so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return;
                }

                var path = AssetDatabase.GetAssetPath(value);
                var fullPath = Path.Combine(ProjectPath, path);
                if (!Directory.Exists(fullPath))
                {
                    Debug.LogError($"Input directory is not exist. ({value.name})");
                    return;
                }

                builtScriptRoot.objectReferenceValue = value;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        public static bool IsDevBuild
        {
            get => _buildSettings.isDevBuild;
            set
            {
                var so = new SerializedObject(_buildSettings);
                var isDevBuild = so.FindProperty("isDevBuild");

                isDevBuild.boolValue = value;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        private static void Install()
        {
            if (Directory.Exists(NodeModulesPath))
            {
                return;
            }

            var match = Regex.Match(NodeModulesParentPath, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = PathReplacer
                .Replace(match.Success ? match.Groups[2].Value : NodeModulesParentPath,
                    $"{Path.DirectorySeparatorChar}")
                .Replace(":", "");

            foreach (var path in FilesToCopy)
            {
                var filename = Path.GetFileName(path);
                var combinedPath = Path.Combine(NodeModulesParentPath, filename);
                if (File.Exists(combinedPath))
                {
                    File.Delete(combinedPath);
                }

                File.Copy(path, Path.Combine(NodeModulesParentPath, filename));
            }

            var parameters = $"\"{workspace}\" {drive}";

            var process = Process.Start(InstallerPath, parameters);
            process?.WaitForExit();

            AssetDatabase.Refresh();
        }

        public static void Build()
        {
            static string AssetPathToAbsolutePath(string assetPath)
            {
                return ReplacePath(Path.Combine(Application.dataPath, Regex.Replace(assetPath, @"^Assets[/\\]", "")));
            }

            void GenerateMetadata()
            {
                var absolutePath = AssetPathToAbsolutePath(RawScriptPath);
                var rawScriptPaths =
                    Directory.GetFiles(absolutePath, @"*.js", SearchOption.AllDirectories)
                        .Where(path => !path.StartsWith(NodeModulesPath) && !path.EndsWith("webpack.config.babel.js"))
                        .Select(path => Regex.Replace(path, $@"{absolutePath.Replace("\\", "\\\\")}[/\\]*",
                            $".{Path.DirectorySeparatorChar}"));

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

            var match = Regex.Match(NodeModulesParentPath, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = PathReplacer
                .Replace(match.Success ? match.Groups[2].Value : NodeModulesParentPath,
                    $"{Path.DirectorySeparatorChar}")
                .Replace(":", "");

            var isDevBuild = _buildSettings.isDevBuild.ToString().ToLower();
            var parameters = $"'{workspace}' {drive} {isDevBuild}";

            Process.Start(BuilderPath, parameters);
        }

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

                    /*
                    var rawScriptRoots = new ReorderableList(so, so.FindProperty("rawScriptRoots"));
                    EditorGUILayout.PropertyField(rawScriptRoots.serializedProperty, new GUIContent("Raw Script Roots"),
                        true);
                    */

                    var rawScriptRoot = so.FindProperty("rawScriptRoot");
                    EditorGUILayout.PropertyField(rawScriptRoot, new GUIContent("Raw Script Root"));

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
                if (!Directory.Exists(NodeModulesPath))
                {
                    if (GUILayout.Button("Install npm modules", GUILayout.Height(_rcButtonArea.height)))
                    {
                        Install();
                    }
                }
                else
                {
                    if (GUILayout.Button("Build", GUILayout.Height(_rcButtonArea.height)))
                    {
                        Build();
                    }
                }
            }
        }
    }
}