using UnityEditor;
using UnityEngine;

namespace Modules.Editor
{
    [CreateAssetMenu(fileName = "Build Settings", menuName = "Javascript on Unity/Build Settings", order = 0)]
    public class BuildSettings : ScriptableObject
    {
        public DefaultAsset rawScriptRoot;
        public DefaultAsset builtScriptRoot;
        public bool isDevBuild = true;

        public DefaultAsset generatedHelpersRoot;
        public MonoScript[] engines;
    }
}