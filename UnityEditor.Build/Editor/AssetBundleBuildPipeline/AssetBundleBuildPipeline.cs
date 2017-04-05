using System.IO;
using System.Text;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Sprites;

namespace UnityEditor.Build.AssetBundle
{
    public class AssetBundleBuildPipeline
    {
        [MenuItem("AssetBundles/Build Asset Bundles")]
        static void BuildAssetBundlesMenuItem()
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
            
            DebugPrintCommandSet(ref commands);

            // Calculate dependencies
            var dependencyCalculator = new Unity5DependencyCalculator();
            if (!dependencyCalculator.Convert(commands, out commands))
                return;
            
            DebugPrintCommandSet(ref commands);

            // Ensure the output path is created
            // TODO: implement incremental building when LLAPI supports it
            Directory.CreateDirectory(settings.outputFolder);
            var output = BuildInterface.WriteResourceFiles(commands, settings);
            foreach (var bundle in output.results)
            {
                var filePath = Path.Combine(settings.outputFolder, bundle.assetBundleName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                BuildInterface.ArchiveAndCompress(bundle.resourceFiles, filePath, compression);
            }

            CacheAssetBundleBuildOutput(output, settings);
        }

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

        private static void DebugPrintCommandSet(ref BuildCommandSet commandSet)
        {
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

        public static void CacheAssetBundleBuildOutput(BuildOutput output, BuildSettings settings)
        {
            // TODO: Cache data about this build result for future patching, incremental build, etc
        }
    }
}