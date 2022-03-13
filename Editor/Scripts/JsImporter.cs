using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace OOTL.JavascriptOnUnity.Editor.Scripts
{
    [ScriptedImporter(1, "js", AllowCaching = true)]
    public class JsImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                var subAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
                ctx.AddObjectToAsset("text", subAsset);
                ctx.SetMainObject(subAsset);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}