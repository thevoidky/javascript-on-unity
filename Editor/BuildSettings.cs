using UnityEditor;
using UnityEngine;

namespace OOTL.JavascriptOnUnity.Editor
{
    [CreateAssetMenu(fileName = "Build Settings", menuName = "Javascript on Unity/Build Settings", order = 0)]
    public class BuildSettings : ScriptableObject
    {
        public bool isTypescriptMode = true;

        public DefaultAsset rawScriptsRoot;
        public DefaultAsset builtScriptsRoot;
        public bool isDevBuild = true;

        public DefaultAsset generatedHelpersRoot;
        public MonoScript[] engines;
    }
}