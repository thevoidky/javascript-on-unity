using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Jint.Native;
using OOTL.JavascriptOnUnity.Runtime;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OOTL.JavascriptOnUnity.Editor
{
    public class JavascriptBuilder : EditorWindow
    {
        private const string Title = "Javascript Builder";
        private const string PackageTitle = "Javascript on Unity";

        private const string ClassHeader = "__class_";

#if OOTL_DEV_LOCAL
        private const string RootPath = "Assets/Modules/javascript-on-unity/Editor";
#else
        private const string PackageName = "com.ootl.jsou";
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

        private static readonly Regex Comment = new Regex(
            @"(^[\s]*(?:import |(?:(?:const|let|var).*require\()).*['""`](?:.+[/\\])*([.][^./\\]+?)['""`])",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex Uncomment = new Regex(
            @"^// ([\s]*(?:import |(?:(?:const|let|var).*require\()).*['""`](?:.+[/\\])*([.][^./\\]+?)['""`])",
            RegexOptions.Compiled | RegexOptions.Multiline);

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
            ReplacePath(Application.dataPath.Substring(0,
                Application.dataPath.LastIndexOf("/Assets", StringComparison.Ordinal)));

        private static string LocalAssetsPath => $"Assets/{PackageTitle}";
        private static string LocalAssetFullPath => $"{ProjectPath}/{LocalAssetsPath}";
        private static string SettingsAssetPath => $"{LocalAssetsPath}/Editor/Build Settings.asset";
        private static string SettingsFullPath => $"{LocalAssetFullPath}/Editor/Build Settings.asset";

#if OOTL_DEV_LOCAL
        private static string Workspace => $"{ProjectPath}/{RootPath}";
        private static string SourceInstallerPath => $"{Workspace}/installer.sh";
        private static string SourceBuilderPath => $"{Workspace}/builder.sh";
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
        private static string InstallerPath => $"{NodeModulesParentPath}/installer.sh";
        private static string BuilderPath => $"{NodeModulesParentPath}/builder.sh";
        private static string NodeModulesParentPath => RawScriptPath;
        private static string NodeModulesPath => ReplacePath($"{NodeModulesParentPath}/node_modules");

        private static string RawScriptPath =>
            !RawScriptRoot
                ? string.Empty
                : ReplacePath($"{ProjectPath}{Path.DirectorySeparatorChar}{AssetDatabase.GetAssetPath(RawScriptRoot)}");

        private static string GeneratedHelpersPath =>
            !GeneratedHelpersRoot
                ? string.Empty
                : ReplacePath(
                    $"{ProjectPath}{Path.DirectorySeparatorChar}{AssetDatabase.GetAssetPath(GeneratedHelpersRoot)}");

        private static string EntryPath => ReplacePath($"{NodeModulesParentPath}/entry.json");
        private static string OutputPath => ReplacePath($"{NodeModulesParentPath}/output.json");

        private static readonly Regex PathReplacer = new Regex(@"[/\\]", RegexOptions.Compiled);

        private static BuildSettings _buildSettings;

        private Vector2 _buildInspectorPosition,
            _buildButtonPosition,
            _generateInspectorPosition,
            _generateButtonPosition;

        private Rect _rcBuildInspectorArea, _rcGenerateInspectorArea;
        private Rect _rcBuildButtonArea, _rcGenerateButtonArea;

        public static DefaultAsset RawScriptRoot
        {
            get => _buildSettings.rawScriptsRoot;
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
            get => _buildSettings.builtScriptsRoot;
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

        public static DefaultAsset GeneratedHelpersRoot
        {
            get => _buildSettings.generatedHelpersRoot;
            set
            {
                var so = new SerializedObject(_buildSettings);
                var generatedHelpersRoot = so.FindProperty("generatedHelpersRoot");

                if (value == null)
                {
                    generatedHelpersRoot.objectReferenceValue = value;
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

                generatedHelpersRoot.objectReferenceValue = value;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        public static IEnumerable<MonoScript> Engines
        {
            get => _buildSettings.engines;
            set
            {
                var so = new SerializedObject(_buildSettings);

                var engines = new ReorderableList(so, so.FindProperty("engines"));
                engines.serializedProperty.ClearArray();

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
                    if (!File.Exists(fullPath))
                    {
                        Debug.LogError($"Input directory is not exist. ({asset.name})");
                        return;
                    }
                }

                value.Aggregate(0, (aggregated, script) =>
                {
                    if (!script ||
                        script.GetClass() == null ||
                        !(Activator.CreateInstance(script.GetClass()) is JavascriptEngine))
                    {
                        return aggregated;
                    }

                    engines.serializedProperty.InsertArrayElementAtIndex(aggregated);
                    var element = engines.serializedProperty.GetArrayElementAtIndex(aggregated);
                    element.objectReferenceValue = script;

                    return aggregated + 1;
                });

                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        private static string ReplacePath(string path) =>
            PathReplacer.Replace(path, Path.DirectorySeparatorChar.ToString());

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
            var installerPath = InstallerPath.Replace('\\', '/').Replace(" ", "\\ ");

            var process = Process.Start(installerPath, parameters);
            process?.WaitForExit();

            AssetDatabase.Refresh();
        }

        public static void Build()
        {
            static string AssetPathToAbsolutePath(string assetPath)
            {
                return ReplacePath(Path.Combine(Application.dataPath, Regex.Replace(assetPath, @"^Assets[/\\]", "")));
            }

            var rawScriptFullPaths = new List<string>();

            void GenerateMetadata()
            {
                var absolutePath = AssetPathToAbsolutePath(RawScriptPath);
                rawScriptFullPaths.AddRange(Directory.GetFiles(absolutePath, @"*.js", SearchOption.AllDirectories)
                    .Where(path => !path.StartsWith(NodeModulesPath) && !path.EndsWith("webpack.config.babel.js")));
                var rawScriptPaths =
                    rawScriptFullPaths
                        .Select(path => Regex.Replace(path, $@"{absolutePath.Replace("\\", "\\\\")}[/\\]*",
                            $".{Path.DirectorySeparatorChar}"));

                var builtScriptRoot = AssetDatabase.GetAssetPath(_buildSettings.builtScriptsRoot);

                var output = new Dictionary<string, string> { { "path", AssetPathToAbsolutePath(builtScriptRoot) } };

                var outputContent = JsonConvert.SerializeObject(output);
                File.WriteAllText(OutputPath, outputContent);

                var entry = rawScriptPaths.ToDictionary(scriptPath => Regex.Replace(scriptPath, @".js$", ""));

                var entryContent = JsonConvert.SerializeObject(entry);
                File.WriteAllText(EntryPath, entryContent, Encoding.UTF8);
            }

            void Comment()
            {
                foreach (var path in rawScriptFullPaths)
                {
                    var javascript = File.ReadAllText(path);
                    javascript = JavascriptBuilder.Comment.Replace(javascript, "// $1");
                    File.WriteAllText(path, javascript);
                }
            }

            void Uncomment()
            {
                foreach (var path in rawScriptFullPaths)
                {
                    var javascript = File.ReadAllText(path);
                    javascript = JavascriptBuilder.Uncomment.Replace(javascript, "$1");
                    File.WriteAllText(path, javascript);
                }
            }

            GenerateMetadata();
            Comment();

            var match = Regex.Match(NodeModulesParentPath, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = PathReplacer
                .Replace(match.Success ? match.Groups[2].Value : NodeModulesParentPath,
                    $"{Path.DirectorySeparatorChar}")
                .Replace(":", "");

            var isDevBuild = _buildSettings.isDevBuild.ToString().ToLower();
            var parameters = $"'{workspace}' {drive} {isDevBuild}";
            var builderPath = BuilderPath.Replace('\\', '/').Replace(" ", "\\ ");;

            var process = Process.Start(builderPath, parameters);
            if (process == null)
            {
                Debug.LogError($"Failed to start a process.\nPath: {BuilderPath}\nArguments: {parameters})");
                return;
            }

            // Wait for an hour
            process.WaitForExit(3600000);

            Uncomment();

            AssetDatabase.Refresh();
        }

        public static void Generate()
        {
            var types = Engines
                .Select(engine => engine.GetClass())
                .Where(type => type != null && type.IsSubclassOf(typeof(JavascriptEngine)));

            var boundTypesToImportPaths = new Dictionary<Type, string>();

            foreach (var type in types)
            {
                try
                {
                    var temporaryInstance = Activator.CreateInstance(type) as JavascriptEngine;
                    if (temporaryInstance == null)
                    {
                        continue;
                    }

                    var engineDirectory =
                        $"{GeneratedHelpersPath}{Path.DirectorySeparatorChar}{type.Namespace?.Replace('.', Path.DirectorySeparatorChar)}";
                    var engineFilename = $".{type.Name}.js";
                    var engineFullPath = $"{engineDirectory}{Path.DirectorySeparatorChar}{engineFilename}";

                    {
                        var typesToBind = temporaryInstance.TypesToBind;
                        if (null != typesToBind)
                        {
                            var tuples = typesToBind
                                .Where(t => t.IsClass && !boundTypesToImportPaths.ContainsKey(t))
                                .Select(t => (t, SerializeClass(t, typesToBind)));

                            foreach (var (t, javascript) in tuples)
                            {
                                var directory =
                                    $"{GeneratedHelpersPath}{Path.DirectorySeparatorChar}{t.Namespace?.Replace('.', Path.DirectorySeparatorChar)}";
                                var filename = $".{t.Name}.js";
                                var fullPath = $"{directory}{Path.DirectorySeparatorChar}{filename}";

                                var equalIndex = engineFullPath.Aggregate(0, (index, ch) =>
                                {
                                    if (index >= fullPath.Length || fullPath[index] != ch)
                                    {
                                        return index;
                                    }

                                    return index + 1;
                                });

                                var equalPathLength = engineFullPath.Substring(0, equalIndex)
                                    .LastIndexOf(Path.DirectorySeparatorChar) + 1;
                                var differentEnginePath = engineFullPath.Substring(equalPathLength,
                                    engineFullPath.Length - equalPathLength);
                                var differentClassPath =
                                    fullPath.Substring(equalPathLength, fullPath.Length - equalPathLength);

                                var depthCount = differentEnginePath.Aggregate(0,
                                    (count, ch) => count + ch == Path.DirectorySeparatorChar ? 1 : 0);

                                var relativePathBuilder = new StringBuilder("./");
                                for (var i = 0; i < depthCount; ++i)
                                {
                                    relativePathBuilder.Append("../");
                                }

                                relativePathBuilder.Append(differentClassPath);

                                boundTypesToImportPaths.Add(t, relativePathBuilder.ToString());

                                if (!File.Exists(fullPath) && !Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }

                                File.WriteAllText(fullPath, javascript, Encoding.UTF8);

                                Debug.Log($"Succeeded to create helper \"{fullPath}\"");
                            }
                        }
                    }

                    {
                        var typesToBind = temporaryInstance.TypesToBind;
                        var imports = null != typesToBind
                            ? string.Join("\r\n", typesToBind
                                .Where(boundTypesToImportPaths.ContainsKey)
                                .Select(boundType =>
                                    $"import {{{boundType.Name}}} from '{boundTypesToImportPaths[boundType]}';"))
                            : string.Empty;

                        var engineJs = $"{imports}\r\n\r\n{SerializeEngine(type)}";

                        if (!File.Exists(engineFullPath) && !Directory.Exists(engineDirectory))
                        {
                            Directory.CreateDirectory(engineDirectory);
                        }

                        File.WriteAllText(engineFullPath, engineJs, Encoding.UTF8);
                        AssetDatabase.Refresh();

                        Debug.Log($"Succeeded to create helper \"{engineFullPath}\"");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
            }
        }

        private static bool IsValidType(Type t) =>
            // boolean
            t == typeof(bool) ||
            // number
            t == typeof(int) || t == typeof(long) ||
            t == typeof(float) || t == typeof(double) || t == typeof(decimal) ||
            // string
            t == typeof(string) ||
            // ***void*** return type only
            t == typeof(void) ||
            // class
            (t.IsClass && !t.IsSubclassOf(typeof(MonoBehaviour)) && !t.IsSubclassOf(typeof(JavascriptEngine)));

        private static string TypeToPrefix(Type t) => $"{(t.IsValueType || t == typeof(string) ? t.Name.ToLower() : t.Name)}_";

        private static string SerializeClass(Type type, ICollection<Type> typesToBind)
        {
            if (!type.IsClass)
            {
                return string.Empty;
            }

            try
            {
                bool IsValidTypeWithBind(Type t) => IsValidType(t) || typesToBind.Contains(t);

                var members =
                    type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var javascript = new StringBuilder($"export class {type.Name} {{\r\n");

                var constructorInfos =
                    type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var constructors = constructorInfos
                    .Select(constructorInfo =>
                    {
                        var parameterInfos = constructorInfo.GetParameters();
                        var parameters = parameterInfos.Select(parameterInfo =>
                            $"{TypeToPrefix(parameterInfo.ParameterType)}{parameterInfo.Name}");

                        return $"constructor({string.Join(",", parameters)}) {{}}";
                    });

                foreach (var ctor in constructors)
                {
                    if (string.IsNullOrEmpty(ctor))
                    {
                        continue;
                    }

                    javascript.AppendLine(ctor);
                }

                var lines = members
                    .Where(member => member switch
                    {
                        PropertyInfo prop => prop.CanWrite && prop.CanRead && IsValidTypeWithBind(prop.PropertyType),
                        MethodInfo method => !method.IsSpecialName &&
                                             (method.GetParameters().Length <= 0 || method.GetParameters()
                                                 .All(parameterInfo =>
                                                     IsValidTypeWithBind(parameterInfo.ParameterType))) &&
                                             IsValidTypeWithBind(method.ReturnType),
                        _ => false
                    })
                    .Select(member =>
                    {
                        switch (member)
                        {
                            case PropertyInfo prop:
                                return
                                    $@"{prop.Name} = {(prop.PropertyType == typeof(string) ? "''" : Activator.CreateInstance(prop.PropertyType))};";

                            case MethodInfo method:
                            {
                                var methodBuilder = new StringBuilder($@"{method.Name}(");
                                var parameters = method.GetParameters().Select(parameterInfo =>
                                    $"{TypeToPrefix(parameterInfo.ParameterType)}{parameterInfo.Name}");
                                methodBuilder.Append(string.Join(",", parameters));

                                var returnValue = method.ReturnType == typeof(void)
                                    ? string.Empty
                                    : method.ReturnType.IsValueType
                                        ? Activator.CreateInstance(method.ReturnType).ToString()
                                        : Regex.IsMatch(method.Name, @"jsasync$", RegexOptions.IgnoreCase) &&
                                          method.ReturnType == typeof(JsValue)
                                            ? $"new Promise(null)"
                                            : $"new {method.ReturnType.Name}()";

                                methodBuilder.Append(
                                    $") {{{(string.IsNullOrEmpty(returnValue) ? returnValue : $" return {returnValue}; ")}}}");
                                return methodBuilder.ToString();
                            }

                            default:
                                return string.Empty;
                        }
                    });

                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    javascript.AppendLine(line);
                }

                javascript.AppendLine("}");

                return javascript.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        private static string SerializeEngine(Type type)
        {
            if (!type.IsClass)
            {
                return string.Empty;
            }

            try
            {
                if (!(Activator.CreateInstance(type) is JavascriptEngine temporaryInstance))
                {
                    return string.Empty;
                }

                var typesToBind = temporaryInstance.TypesToBind;
                bool IsValidTypeWithBind(Type t) => IsValidType(t) || typesToBind.Contains(t);

                var members =
                    type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var constructorInfos =
                    type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var javascript = new StringBuilder($"class {ClassHeader}{type.Name} {{\r\n");

                var constructors = constructorInfos
                    .Select(constructorInfo =>
                    {
                        var parameterInfos = constructorInfo.GetParameters();
                        var parameters = parameterInfos.Select(parameterInfo =>
                            $"{TypeToPrefix(parameterInfo.ParameterType)}{parameterInfo.Name}");

                        return $"constructor({string.Join(",", parameters)}) {{}}";
                    });

                foreach (var ctor in constructors)
                {
                    if (string.IsNullOrEmpty(ctor))
                    {
                        continue;
                    }

                    javascript.AppendLine(ctor);
                }

                var lines = members
                    .Where(member => member switch
                    {
                        PropertyInfo prop => prop.CanWrite && prop.CanRead && IsValidTypeWithBind(prop.PropertyType),
                        MethodInfo method => !method.IsSpecialName &&
                                             (method.GetParameters().Length <= 0 || method.GetParameters()
                                                 .All(parameterInfo =>
                                                     IsValidTypeWithBind(parameterInfo.ParameterType))) &&
                                             IsValidTypeWithBind(method.ReturnType),
                        _ => false
                    })
                    .Select(member =>
                    {
                        switch (member)
                        {
                            case PropertyInfo prop:
                                return
                                    $@"{prop.Name} = {(prop.PropertyType == typeof(string) ? "''" : prop.GetValue(temporaryInstance))};";

                            case MethodInfo method:
                            {
                                var methodBuilder = new StringBuilder($@"{method.Name}(");
                                var parameters = method.GetParameters().Select(parameterInfo =>
                                    $"{TypeToPrefix(parameterInfo.ParameterType)}{parameterInfo.Name}");
                                methodBuilder.Append(string.Join(",", parameters));

                                var returnValue = method.ReturnType == typeof(void)
                                    ? string.Empty
                                    : method.ReturnType.IsValueType
                                        ? Activator.CreateInstance(method.ReturnType).ToString()
                                        : Regex.IsMatch(method.Name, @"jsasync$", RegexOptions.IgnoreCase) &&
                                          method.ReturnType == typeof(JsValue)
                                            ? $"new Promise(null)"
                                            : $"new {method.ReturnType.Name}()";

                                methodBuilder.Append(
                                    $") {{{(string.IsNullOrEmpty(returnValue) ? returnValue : $" return {returnValue}; ")}}}");
                                return methodBuilder.ToString();
                            }

                            default:
                                return string.Empty;
                        }
                    });

                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    javascript.AppendLine(line);
                }

                javascript.AppendLine($"}}\r\n\r\nexport const window = new {ClassHeader}{type.Name}();");

                return javascript.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
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

            if (null == _buildSettings)
            {
                var oldColor = GUI.color;
                GUI.color = Color.red;
                EditorGUILayout.LabelField("\"Build Settings\" is not exist.");
                GUI.color = oldColor;
                return;
            }

            DrawBuildPanel();
            DrawGeneratePanel();
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
            var windowRight = Window.position.width - 2f;
            var windowBottom = Window.position.height - 2f;

            const float buildAreaRatio = 0.5f;
            const float generateAreaRatio = 1f - buildAreaRatio;

            var buildArea = Rect.MinMaxRect(2f, 2f, windowRight, windowBottom * buildAreaRatio - 2f);
            var generateArea = Rect.MinMaxRect(buildArea.xMin, buildArea.yMax + 2f, windowRight, windowBottom);

            _rcBuildInspectorArea =
                Rect.MinMaxRect(buildArea.xMin, buildArea.yMin, buildArea.xMax, buildArea.yMax - 60f);
            _rcBuildButtonArea =
                Rect.MinMaxRect(buildArea.xMin, _rcBuildInspectorArea.yMax, buildArea.xMax, buildArea.yMax);

            _rcGenerateInspectorArea = Rect.MinMaxRect(generateArea.xMin, generateArea.yMin, generateArea.xMax,
                generateArea.yMax - 40f);
            _rcGenerateButtonArea = Rect.MinMaxRect(generateArea.xMin, _rcGenerateInspectorArea.yMax, generateArea.xMax,
                generateArea.yMax);
        }

        private void DrawBuildPanel()
        {
            DrawBuildInspector();
            DrawBuildButtons();
        }

        private void DrawGeneratePanel()
        {
            DrawGenerateInspector();
            DrawGenerateButtons();
        }

        private void DrawBuildInspector()
        {
            var oldIndentLevel = EditorGUI.indentLevel;
            try
            {
                using var area = new GUILayout.AreaScope(_rcBuildInspectorArea);
                using var scrollView = new GUILayout.ScrollViewScope(_buildInspectorPosition);

                EditorGUILayout.LabelField("Build Options");
                ++EditorGUI.indentLevel;

                var so = new SerializedObject(_buildSettings);

                /*
                var rawScriptsRoots = new ReorderableList(so, so.FindProperty("rawScriptsRoots"));
                EditorGUILayout.PropertyField(rawScriptsRoots.serializedProperty, new GUIContent("Raw Scripts Roots"),
                    true);
                */

                var rawScriptsRoot = so.FindProperty("rawScriptsRoot");
                EditorGUILayout.PropertyField(rawScriptsRoot, new GUIContent("Raw Scripts Root"));

                var builtScriptsRoot = so.FindProperty("builtScriptsRoot");
                EditorGUILayout.PropertyField(builtScriptsRoot, new GUIContent("Built Scripts Root"));

                var isDevBuild = so.FindProperty("isDevBuild");
                EditorGUILayout.PropertyField(isDevBuild, new GUIContent("Dev Build"));

                so.ApplyModifiedProperties();

                _buildInspectorPosition = scrollView.scrollPosition;
            }
            finally
            {
                EditorGUI.indentLevel = oldIndentLevel;
            }
        }

        private void DrawGenerateInspector()
        {
            var oldIndentLevel = EditorGUI.indentLevel;
            try
            {
                using var area = new GUILayout.AreaScope(_rcGenerateInspectorArea);
                using var scrollView = new GUILayout.ScrollViewScope(_generateInspectorPosition);

                EditorGUILayout.LabelField("Generate Helpers Options");
                ++EditorGUI.indentLevel;

                var so = new SerializedObject(_buildSettings);

                var generatedHelpersRoot = so.FindProperty("generatedHelpersRoot");
                EditorGUILayout.PropertyField(generatedHelpersRoot, new GUIContent("Generated Root"));

                var engines = new ReorderableList(so, so.FindProperty("engines"));
                EditorGUILayout.PropertyField(engines.serializedProperty, new GUIContent("Engine codes"), true);

                so.ApplyModifiedProperties();

                _generateInspectorPosition = scrollView.scrollPosition;
            }
            finally
            {
                EditorGUI.indentLevel = oldIndentLevel;
            }
        }

        private void DrawBuildButtons()
        {
            var style = new GUIStyle("Button") { stretchHeight = true };

            var isInstallationComplete = Directory.Exists(NodeModulesPath) && File.Exists(InstallerPath) &&
                                         File.Exists(BuilderPath) &&
                                         File.Exists(EntryPath) && File.Exists(OutputPath);

            var height = _rcBuildButtonArea.height * (isInstallationComplete ? 0.5f : 1f);

            using (new GUILayout.AreaScope(_rcBuildButtonArea))
            {
                using (new GUILayout.VerticalScope())
                {
                    if (RawScriptRoot == null)
                    {
                        var oldEnabled = GUI.enabled;
                        GUI.enabled = RawScriptRoot != null;

                        GUILayout.Button("Set raw script root to install npm modules or build", style,
                            GUILayout.Height(height));

                        GUI.enabled = oldEnabled;
                    }
                    else if (GUILayout.Button($"{(isInstallationComplete ? "Force install" : "Install")} npm modules",
                        style, GUILayout.Height(height)))
                    {
                        Install();
                    }

                    if (isInstallationComplete)
                    {
                        if (GUILayout.Button("Build", style, GUILayout.Height(height)))
                        {
                            Build();
                        }
                    }
                }
            }
        }

        private void DrawGenerateButtons()
        {
            var oldEnabled = GUI.enabled;

            try
            {
                GUI.enabled = GeneratedHelpersRoot;
                using var area = new GUILayout.AreaScope(_rcGenerateButtonArea);

                if (GUILayout.Button(GUI.enabled ? "Generate" : "Set root path to generate",
                    GUILayout.Height(_rcGenerateButtonArea.height)))
                {
                    Generate();
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }
    }
}