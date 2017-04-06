using System.Text;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Sprites;

namespace UnityEditor.Build.AssetBundle
{
    public class AssetBundleBuildPipeline
    {
        public static BuildSettings GenerateBuildSettings()
        {
            var settings = new BuildSettings();
            settings.target = EditorUserBuildSettings.activeBuildTarget;
            settings.group = EditorUserBuildSettings.selectedBuildTargetGroup;
            settings.outputFolder = "AssetBundles/" + settings.target;
            // Example: Point this to the dll's of previous player build
            // TODO: wil improve this functionality as certain platforms don't leave their dll's for reuse after a player build
            //settings.scriptsFolder = "Build/Player_Data/Managed";
            settings.editorBundles = false;
            return settings;
        }

        [MenuItem("AssetBundles/Build Asset Bundles")]
        public static void BuildAssetBundles()
        {
            var input = BuildInterface.GenerateBuildInput();
            var settings = GenerateBuildSettings();
            var compression = BuildCompression.DefaultUncompressed;

            // Rebuild sprite atlas cache for correct dependency calculation & writting
            Packer.RebuildAtlasCacheIfNeeded(settings.target, true, Packer.Execution.Normal);

            // Generate command set
            BuildCommandSet commands;
            var packer = new Unity5Packer();
            if (!packer.Convert(input, settings.target, out commands))
                return;

            //DebugPrintCommandSet(ref commands);

            // Calculate dependencies
            var dependencyCalculator = new Unity5DependencyCalculator();
            if (!dependencyCalculator.Convert(commands, out commands))
                return;

            
            //DebugPrintCommandSet(ref commands);
            
            // TODO: implement incremental building when LLAPI supports it

            BuildOutput output;
            var resourceWriter = new ResourceWriter();
            if (!resourceWriter.Convert(commands, settings, out output))
                return;

            uint[] crc;
            var archiveWriter = new ArchiveWriter();
            if (!archiveWriter.Convert(output, compression, settings.outputFolder, out crc))
                return;
        }

        private static void DebugPrintCommandSet(ref BuildCommandSet commandSet)
        {
            // TODO: this is ugly, fix it
            var msg = new StringBuilder();
            if (commandSet.commands != null)
            {
                foreach (var bundle in commandSet.commands)
                {
                    msg.AppendFormat("Bundle: '{0}'\n", bundle.assetBundleName);
                    if (bundle.explicitAssets != null)
                    {
                        msg.Append("\tExplicit Assets:\n");
                        foreach (var asset in bundle.explicitAssets)
                        {
                            var addressableName = string.IsNullOrEmpty(asset.address) ? AssetDatabase.GUIDToAssetPath(asset.asset.ToString()) : asset.address;
                            msg.AppendFormat("\t\tAsset: {0} - '{1}'\n", asset.asset, addressableName);
                            if (asset.includedObjects != null)
                            {
                                msg.Append("\t\t\tIncluded Objects:\n");
                                foreach (var obj in asset.includedObjects)
                                    msg.AppendFormat("\t\t\t\t{0}\n", obj);
                            }
                            if (asset.referencedObjects != null)
                            {
                                msg.Append("\t\t\tReferenced Objects:\n");
                                foreach (var obj in asset.referencedObjects)
                                    msg.AppendFormat("\t\t\t\t{0}\n", obj);
                            }
                        }
                    }
                    if (bundle.assetBundleObjects != null)
                    {
                        msg.Append("\tAsset Bundle Objects:\n");
                        foreach (var obj in bundle.assetBundleObjects)
                            msg.AppendFormat("\t\t{0}: {1}\n", obj.serializationIndex, obj.serializationObject);
                    }
                    if (bundle.assetBundleDependencies != null)
                    {
                        msg.Append("\tAsset Bundle Dependencies:\n");
                        foreach (var dependency in bundle.assetBundleDependencies)
                            msg.AppendFormat("\t\t{0}\n", dependency);
                    }
                    msg.Append("\n");
                }
            }
            UnityEngine.Debug.Log(msg);
        }
    }
}