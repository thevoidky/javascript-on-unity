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
        private const string RootPath = "Modules/javascript-on-unity/Editor";

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

        private static string Workspace => $"{Application.dataPath}/{RootPath}";
        private static string BuilderPath => $"{Workspace}/builder.sh";
        private static string EntryPath => $"{Workspace}/entry.json";
        private static string OutputPath => $"{Workspace}/output.json";

        private Rect _rcControlArea;
        private Rect _rcButtonArea;

        private BuildSettings _buildSettings;

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
                _buildSettings =
                    AssetDatabase.LoadAssetAtPath<BuildSettings>($"Assets/{RootPath}/Build Settings.asset");
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

        private void DrawButtonPanel()
        {
            using (new GUILayout.AreaScope(_rcButtonArea))
            {
                if (GUILayout.Button("Build", GUILayout.Height(_rcButtonArea.height)))
                {
                    Build();
                }
            }
        }

        private void Build()
        {
            var match = Regex.Match(Workspace, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = match.Success ? match.Groups[2].Value : Workspace;

            static string AssetPathToAbsolutePath(string assetPath)
            {
                var replacer = new Regex(@"[/\\]", RegexOptions.Compiled);
                return replacer.Replace(
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

            workspace = workspace.Replace('\\', '/').Replace(":", "");
            var isDevBuild = _buildSettings.isDevBuild.ToString().ToLower();
            var parameters = $"\"{workspace}\" {drive} {isDevBuild}";

            Process.Start(BuilderPath, parameters);
        }
    }
}