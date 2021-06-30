using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Modules.Editor
{
    public class JavascriptBuilder : EditorWindow
    {
        private const string Title = "Javascript Builder";

        [MenuItem("Javascript on Unity/Build Javascripts")]
        private static void Build()
        {
            var path = $"{Application.dataPath}/Modules/javascript-on-unity/Editor/test.sh";
            Process.Start(path);
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
        }
    }
}