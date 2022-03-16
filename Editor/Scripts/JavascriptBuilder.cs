#define OOTL_DEV_LOCAL

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

namespace OOTL.JavascriptOnUnity.Editor.Scripts
{
    public class JavascriptBuilder : EditorWindow
    {
        private const string Title = "Javascript Builder";
        private const string PackageTitle = Common.PackageTitle;

        private const string ClassHeader = "__class_";

#if OOTL_DEV_LOCAL
        private const string RootPath = "Assets/Modules/javascript-on-unity/Editor";
#else
        private const string PackageName = Common.PackageName;
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

        private static readonly Regex CommentImport = new Regex(
            @"((?:import |(?:(?:const|let|var).*require\()).*['""`](?:.+[\/\\])*([.][^.\/\\]+?)['""`])",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex CommentExportClass = new Regex(
            @"(export class .*\{(?:[\r\n]*.*)*\})",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex CommentExportEtc = new Regex(
            @"(export (?:const|let|var).*)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex UncommentImport = new Regex(
            @"\/\/ ([\s]*(?:import |(?:(?:const|let|var).*require\()).*(?:['""`](?:.+[\/\\])*([.][^.\/\\]+?)['""`])?)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex UncommentExportClass = new Regex(
            @"\/\*(export class .*\{(?:[\r\n]*?.*?)*?\})\*\/",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex UncommentExportEtc = new Regex(
            @"\/\/ (export (?:const|let|var).*)",
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
            $"{Workspace}/tsconfig.json",
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
            $"{RootFullPath}/tsconfig.json",
        };
#endif
        private static string InstallerPath => $"{RawScriptPath}/installer.sh";
        private static string TsConfigPath => $"{RawScriptPath}{Path.DirectorySeparatorChar}tsconfig.json";
        private static string BuilderPath => $"{RawScriptPath}{Path.DirectorySeparatorChar}builder.sh";
        private static string NodeModulesPath => ReplacePath($"{RawScriptPath}/node_modules");

        private static string RawScriptPath =>
            !RawScriptRoot
                ? string.Empty
                : ReplacePath($"{ProjectPath}{Path.DirectorySeparatorChar}{AssetDatabase.GetAssetPath(RawScriptRoot)}");

        private static string GeneratedHelpersPath =>
            !GeneratedHelpersRoot
                ? string.Empty
                : ReplacePath(
                    $"{ProjectPath}{Path.DirectorySeparatorChar}{AssetDatabase.GetAssetPath(GeneratedHelpersRoot)}");

        private static string EntryPath => ReplacePath($"{RawScriptPath}/entry.json");
        private static string OutputPath => ReplacePath($"{RawScriptPath}/output.json");

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

        private static void CopyAndInstall()
        {
            var match = Regex.Match(RawScriptPath, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = PathReplacer
                .Replace(match.Success ? match.Groups[2].Value : RawScriptPath,
                    $"{Path.DirectorySeparatorChar}")
                .Replace(":", "");

            foreach (var path in FilesToCopy)
            {
                var filename = Path.GetFileName(path);
                var combinedPath = Path.Combine(RawScriptPath, filename);
                if (File.Exists(combinedPath))
                {
                    File.Delete(combinedPath);
                }

                File.Copy(path, Path.Combine(RawScriptPath, filename));
            }

            workspace = workspace.Replace('\\', '/').Replace(" ", "\\ ");
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

            var rawScriptFullPaths = new List<string>();
            var rawScriptPaths = new Dictionary<string, string>();
            var isTypescript = _buildSettings.isTypescriptMode;

            void GenerateMetadata()
            {
                var absolutePath = AssetPathToAbsolutePath(RawScriptPath);
                var javascripts = Directory.GetFiles(absolutePath, @"*.js", SearchOption.AllDirectories)
                    .Where(path => !path.StartsWith(NodeModulesPath) && !path.EndsWith("webpack.config.babel.js"));

                foreach (var js in javascripts)
                {
                    var path = Regex.Replace(js, $@"{absolutePath.Replace("\\", "\\\\")}[/\\]*",
                        $".{Path.DirectorySeparatorChar}");
                    // var path = Regex.Replace(js, $@"{absolutePath.Replace("\\", "\\\\")}[/\\]*", $"./");
                    rawScriptFullPaths.Add(js);

                    var directory = Path.GetDirectoryName(path);
                    var filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                    var key = $"{directory}{Path.DirectorySeparatorChar}{filenameWithoutExtension}";
                    rawScriptPaths[key] = path;
                }

                if (isTypescript)
                {
                    var typescripts = Directory.GetFiles(absolutePath, @"*.ts", SearchOption.AllDirectories)
                        .Where(path => !path.StartsWith(NodeModulesPath));

                    foreach (var ts in typescripts)
                    {
                        var path = Regex.Replace(ts, $@"{absolutePath.Replace("\\", "\\\\")}[/\\]*",
                            $".{Path.DirectorySeparatorChar}");
                        // var path = Regex.Replace(ts, $@"{absolutePath.Replace("\\", "\\\\")}[/\\]*", $"./");
                        rawScriptFullPaths.Add(ts);

                        var directory = Path.GetDirectoryName(path);
                        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                        var key = $"{directory}{Path.DirectorySeparatorChar}{filenameWithoutExtension}";
                        rawScriptPaths[key] = path;
                    }
                }

                // var rawScriptPaths =
                //     rawScriptFullPaths
                //         .Select(path => Regex.Replace(path, $@"{absolutePath.Replace("\\", "\\\\")}[/\\]*",
                //             $".{Path.DirectorySeparatorChar}"))
                //         .ToArray();

                var builtScriptRoot = AssetDatabase.GetAssetPath(_buildSettings.builtScriptsRoot);

                var output = new Dictionary<string, string> { { "path", AssetPathToAbsolutePath(builtScriptRoot) } };

                var outputContent = JsonConvert.SerializeObject(output);
                File.WriteAllText(OutputPath, outputContent);

                // var entry = rawScriptPaths.ToDictionary(scriptPath => Regex.Replace(scriptPath, @".(js|ts)$", ""));
                var entry = rawScriptPaths;

                var entryContent = JsonConvert.SerializeObject(entry);
                File.WriteAllText(EntryPath, entryContent, Encoding.UTF8);
            }

            void Comment()
            {
                foreach (var path in rawScriptFullPaths)
                {
                    var script = File.ReadAllText(path);
                    script = CommentImport.Replace(script, "// $1");

                    if (Path.GetFileNameWithoutExtension(path)[0] == '.')
                    {
                        script = CommentExportClass.Replace(script, "/*$1*/");
                        script = CommentExportEtc.Replace(script, "// $1");
                    }

                    File.WriteAllText(path, script);
                }
            }

            void Uncomment()
            {
                foreach (var path in rawScriptFullPaths)
                {
                    var script = File.ReadAllText(path);
                    script = UncommentImport.Replace(script, "$1");
                    if (Path.GetFileNameWithoutExtension(path)[0] == '.')
                    {
                        script = UncommentExportClass.Replace(script, "$1");
                        script = UncommentExportEtc.Replace(script, "$1");
                    }

                    File.WriteAllText(path, script);
                }
            }

            GenerateMetadata();
            Comment();

            var match = Regex.Match(RawScriptPath, @"([a-zA-Z]):?[\\/](.*)");
            var drive = $"/{(match.Success ? match.Groups[1].Value : string.Empty)}";
            var workspace = PathReplacer
                .Replace(match.Success ? match.Groups[2].Value : RawScriptPath,
                    $"{Path.DirectorySeparatorChar}")
                .Replace(":", "");

            var isDevBuild = _buildSettings.isDevBuild;

            workspace = workspace.Replace('\\', '/').Replace(" ", "\\ ");
            var parameters = $"'{workspace}' {drive} {isDevBuild}";

            var process = Process.Start(BuilderPath, parameters);
            if (process == null)
            {
                Debug.LogError($"Failed to start a process.\nPath: {BuilderPath}\nArguments: {parameters})");
                return;
            }

            // Wait for an hour as limit
            process.WaitForExit(3600000);

            Uncomment();

            AssetDatabase.Refresh();
        }

        private static string GetScriptFullPath(Type t)
        {
            var isTypescript = _buildSettings.isTypescriptMode;

            var directory =
                $"{GeneratedHelpersPath}{Path.DirectorySeparatorChar}{t.Namespace?.Replace('.', Path.DirectorySeparatorChar)}";
            var filename = $".{t.Name}.{(isTypescript ? "ts" : "js")}";
            var fullPath = $"{directory}{Path.DirectorySeparatorChar}{filename}";

            return fullPath;
        }

        private static string GetRelativePath(string basePath, string targetPath)
        {
            var equalIndex = basePath.Aggregate(0, (index, ch) =>
            {
                if (index >= targetPath.Length || targetPath[index] != ch)
                {
                    return index;
                }

                return index + 1;
            });

            var equalPathLength = basePath.Substring(0, equalIndex)
                .LastIndexOf(Path.DirectorySeparatorChar) + 1;
            var differentEnginePath = basePath.Substring(equalPathLength,
                basePath.Length - equalPathLength);
            var differentClassPath =
                targetPath.Substring(equalPathLength, targetPath.Length - equalPathLength);

            var depthCount = differentEnginePath.Aggregate(0,
                (count, ch) => count + (ch == Path.DirectorySeparatorChar ? 1 : 0));

            var relativePathBuilder = new StringBuilder("./");
            for (var i = 0; i < depthCount; ++i)
            {
                relativePathBuilder.Append("../");
            }

            var additionalDirectory = Path.GetDirectoryName(differentClassPath)?.Replace('\\', '/');
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(differentClassPath);
            relativePathBuilder.Append($"{additionalDirectory}/{fileNameWithoutExtension}");

            return relativePathBuilder.ToString();
        }

        public static void Generate()
        {
            var types = Engines
                .Select(engine => engine.GetClass())
                .Where(type => type != null && type.IsSubclassOf(typeof(JavascriptEngine)))
                .ToArray();

            var boundTypesToImportPaths = new Dictionary<Type, string>();
            var isTypescript = _buildSettings.isTypescriptMode;

            foreach (var type in types)
            {
                try
                {
                    if (!(Activator.CreateInstance(type) is JavascriptEngine temporaryInstance))
                    {
                        continue;
                    }

                    var engineDirectory =
                        $"{GeneratedHelpersPath}{Path.DirectorySeparatorChar}{type.Namespace?.Replace('.', Path.DirectorySeparatorChar)}";
                    var engineFilename = $".{type.Name}.{(isTypescript ? "ts" : "js")}";
                    var engineFullPath = $"{engineDirectory}{Path.DirectorySeparatorChar}{engineFilename}";

                    {
                        var typesToBind = temporaryInstance.TypesToBind;
                        typesToBind.Add(typeof(JavascriptEngine));
                        if (null != typesToBind)
                        {
                            var tuples = typesToBind
                                .Where(t => t.IsClass && !boundTypesToImportPaths.ContainsKey(t))
                                .Select(t => (t, SerializeClass(t, typesToBind)));

                            foreach (var (t, script) in tuples)
                            {
                                var directory =
                                    $"{GeneratedHelpersPath}{Path.DirectorySeparatorChar}{t.Namespace?.Replace('.', Path.DirectorySeparatorChar)}";

                                var typeFullPath = GetScriptFullPath(t);
                                var relativePath = GetRelativePath(engineFullPath, typeFullPath);

                                boundTypesToImportPaths.Add(t, relativePath);

                                if (!File.Exists(typeFullPath) && !Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }

                                File.WriteAllText(typeFullPath, script, Encoding.UTF8);

                                Debug.Log($"Succeeded to create helper \"{typeFullPath}\"");
                            }
                        }
                    }

                    {
                        var typesToBind = temporaryInstance.TypesToBind;
                        typesToBind.Add(temporaryInstance.GetType());
                        typesToBind.Add(typeof(JavascriptEngine));
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

        private static string TypeToTypename(Type t)
        {
            // boolean
            if (t == typeof(bool))
            {
                return "boolean";
            }

            // number
            if (t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double) ||
                t == typeof(decimal))
            {
                return "number";
            }

            // string
            if (t == typeof(string))
            {
                return "string";
            }

            // ***void*** return type only
            if (t == typeof(void))
            {
                return "void";
            }

            // class
            if (t.IsClass && !t.IsSubclassOf(typeof(MonoBehaviour)) && !t.IsSubclassOf(typeof(JavascriptEngine)))
            {
                return t.Name;
            }

            return string.Empty;
        }

        private static string TypeToPrefix(Type t) =>
            $"{(t.IsValueType || t == typeof(string) ? t.Name.ToLower() : t.Name)}_";

        private static string SerializeClass(Type type, ICollection<Type> typesToBind)
        {
            if (!type.IsClass)
            {
                return string.Empty;
            }

            if (type == typeof(JavascriptEngine))
            {
                return "export class JavascriptEngine{}";
            }

            try
            {
                bool IsValidTypeWithBind(Type t) => IsValidType(t) || typesToBind.Contains(t);

                var isTypescript = _buildSettings.isTypescriptMode;
                var members =
                    type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var script = new StringBuilder();

                var constructorInfos =
                    type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                var isBoundClass = constructorInfos.Any(constructorInfo =>
                    constructorInfo.GetParameters().Any(parameterInfo =>
                        parameterInfo.ParameterType == typeof(JavascriptEngine)));

                if (isBoundClass)
                {
                    var jsEnginePath = GetScriptFullPath(typeof(JavascriptEngine));
                    var boundClassPath = GetScriptFullPath(type);

                    var relativePath = GetRelativePath(boundClassPath, jsEnginePath);
                    script.AppendLine($"import {{{nameof(JavascriptEngine)}}} from \"{relativePath}\"\r\n");
                }

                script.AppendLine($"export class {type.Name} {{");

                var constructors = constructorInfos
                    .Select(constructorInfo =>
                    {
                        var parameterInfos = constructorInfo.GetParameters();
                        var parameters = parameterInfos.Select(parameterInfo =>
                        {
                            var prefix = isTypescript
                                ? string.Empty
                                : TypeToPrefix(parameterInfo.ParameterType);
                            var suffix = isTypescript
                                ? $": {TypeToTypename(parameterInfo.ParameterType)}"
                                : string.Empty;

                            return $"{prefix}{parameterInfo.Name}{suffix}";
                        });

                        return $"constructor({string.Join(",", parameters)}) {{}}";
                    });

                foreach (var ctor in constructors)
                {
                    if (string.IsNullOrEmpty(ctor))
                    {
                        continue;
                    }

                    script.AppendLine(ctor);
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
                            {
                                var typename = isTypescript
                                    ? $": {TypeToTypename(prop.PropertyType)}"
                                    : string.Empty;

                                return
                                    $@"{prop.Name}{typename} = {(prop.PropertyType == typeof(string) ? "''" : Activator.CreateInstance(prop.PropertyType))};";
                            }
                            case MethodInfo method:
                            {
                                var methodBuilder = new StringBuilder($@"{method.Name}(");
                                var parameters = method.GetParameters()
                                    .Select(parameterInfo =>
                                    {
                                        var prefix = isTypescript
                                            ? string.Empty
                                            : TypeToPrefix(parameterInfo.ParameterType);
                                        var suffix = isTypescript
                                            ? $": {TypeToTypename(parameterInfo.ParameterType)}"
                                            : string.Empty;

                                        return $"{prefix}{parameterInfo.Name}{suffix}";
                                    });

                                methodBuilder.Append($"{string.Join(",", parameters)})");

                                var isPromise = Regex.IsMatch(method.Name, @"jsasync$", RegexOptions.IgnoreCase) &&
                                                method.ReturnType == typeof(JsValue);

                                if (isTypescript)
                                {
                                    var typename = isPromise ? "Promise<void>" : TypeToTypename(method.ReturnType);
                                    methodBuilder.Append($": {typename}");
                                }

                                var returnValue = method.ReturnType == typeof(void)
                                    ? string.Empty
                                    : method.ReturnType.IsValueType
                                        ? Activator.CreateInstance(method.ReturnType).ToString()
                                        : method.ReturnType == typeof(string)
                                            ? @""""""
                                            : Regex.IsMatch(method.Name, @"jsasync$", RegexOptions.IgnoreCase) &&
                                              method.ReturnType == typeof(JsValue)
                                                ? $"new Promise(null)"
                                                : $"new {method.ReturnType.Name}()";

                                if ( /*isTypescript &&*/ method.ReturnType == typeof(bool))
                                {
                                    returnValue = returnValue.ToLower();
                                }

                                methodBuilder.Append(
                                    $" {{{(string.IsNullOrEmpty(returnValue) ? returnValue : $" return {returnValue}; ")}}}");
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

                    script.AppendLine(line);
                }

                script.AppendLine("}");

                return script.ToString();
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

                var isTypescript = _buildSettings.isTypescriptMode;
                var typesToBind = temporaryInstance.TypesToBind;
                typesToBind.Add(temporaryInstance.GetType());
                typesToBind.Add(typeof(JavascriptEngine));
                bool IsValidTypeWithBind(Type t) => IsValidType(t) || typesToBind.Contains(t);

                var members =
                    type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var constructorInfos =
                    type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                var extend = $" extends {nameof(JavascriptEngine)}";
                var script = new StringBuilder($"class {ClassHeader}{type.Name}{extend} {{\r\n");

                var constructors = constructorInfos
                    .Select(constructorInfo =>
                    {
                        var parameterInfos = constructorInfo.GetParameters();
                        var parameters = parameterInfos.Select(parameterInfo =>
                        {
                            var prefix = isTypescript
                                ? string.Empty
                                : TypeToPrefix(parameterInfo.ParameterType);
                            var suffix = isTypescript
                                ? $": {TypeToTypename(parameterInfo.ParameterType)}"
                                : string.Empty;

                            return $"{prefix}{parameterInfo.Name}{suffix}";
                        });

                        return $"constructor({string.Join(",", parameters)}) {{super();}}";
                    });

                foreach (var ctor in constructors)
                {
                    if (string.IsNullOrEmpty(ctor))
                    {
                        continue;
                    }

                    script.AppendLine(ctor);
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
                            {
                                var typename = isTypescript
                                    ? $": {TypeToTypename(prop.PropertyType)}"
                                    : string.Empty;

                                var value = prop.GetValue(temporaryInstance)?.ToString();
                                if ( /*isTypescript && */prop.PropertyType == typeof(bool))
                                {
                                    value = value.ToLower();
                                }

                                return
                                    $@"{prop.Name}{typename} = {(prop.PropertyType == typeof(string) ? "''" : value)};";
                            }
                            case MethodInfo method:
                            {
                                var methodBuilder = new StringBuilder($@"{method.Name}(");
                                var parameters = method.GetParameters().Select(parameterInfo =>
                                {
                                    var prefix = isTypescript
                                        ? string.Empty
                                        : TypeToPrefix(parameterInfo.ParameterType);
                                    var suffix = isTypescript
                                        ? $": {TypeToTypename(parameterInfo.ParameterType)}"
                                        : string.Empty;

                                    return $"{prefix}{parameterInfo.Name}{suffix}";
                                });

                                methodBuilder.Append($"{string.Join(",", parameters)})");

                                var isPromise = Regex.IsMatch(method.Name, @"jsasync$", RegexOptions.IgnoreCase) &&
                                                method.ReturnType == typeof(JsValue);

                                if (isTypescript)
                                {
                                    var typename = isPromise ? "Promise<void>" : TypeToTypename(method.ReturnType);
                                    methodBuilder.Append($": {typename}");
                                }

                                var returnValue = method.ReturnType == typeof(void)
                                    ? string.Empty
                                    : method.ReturnType.IsValueType
                                        ? Activator.CreateInstance(method.ReturnType).ToString()
                                        : method.ReturnType == typeof(string)
                                            ? @""""""
                                            : Regex.IsMatch(method.Name, @"jsasync$", RegexOptions.IgnoreCase) &&
                                              method.ReturnType == typeof(JsValue)
                                                ? $"new Promise(null)"
                                                : $"new {method.ReturnType.Name}()";

                                if ( /*isTypescript && */method.ReturnType == typeof(bool))
                                {
                                    returnValue = returnValue.ToLower();
                                }

                                methodBuilder.Append(
                                    $" {{{(string.IsNullOrEmpty(returnValue) ? returnValue : $" return {returnValue}; ")}}}");
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

                    script.AppendLine(line);
                }

                script.AppendLine($"}}\r\n\r\nexport const {type.Name} = new {ClassHeader}{type.Name}();");

                return script.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        [MenuItem(PackageTitle + "/Open " + Title)]
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

            const float buildAreaRatio = 0.4f;
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
                EditorGUILayout.PropertyField(generatedHelpersRoot, new GUIContent("Root to generate"));

                var isTypescriptMode = so.FindProperty("isTypescriptMode");
                EditorGUILayout.PropertyField(isTypescriptMode,
                    new GUIContent("Typescript Mode",
                        "When activated, helpers generated as typescript instead javascript."));

                EditorGUILayout.Separator();

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

            var isInstallationComplete = File.Exists(TsConfigPath) &&
                                         File.Exists(BuilderPath);

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
                        if (!isInstallationComplete && File.Exists(TsConfigPath))
                        {
                            return;
                        }

                        CopyAndInstall();
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