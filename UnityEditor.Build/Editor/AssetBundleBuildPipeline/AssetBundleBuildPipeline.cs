using System.Diagnostics;
using System.Text;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Utilities;
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
            return settings;
        }

        public static bool BuildAssetBundles(BuildSettings settings, BuildInput input, out BuildOutput output)
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            output = new BuildOutput();
            var compression = BuildCompression.DefaultUncompressed;

            // Rebuild sprite atlas cache for correct dependency calculation & writting
            Packer.RebuildAtlasCacheIfNeeded(settings.target, true, Packer.Execution.Normal);

            // Generate command set
            BuildCommandSet commands;
            var packer = new Unity5Packer();
            if (!packer.Convert(input, settings.target, out commands))
                return false;

            //DebugPrintCommandSet(ref commands);

            // Calculate dependencies
            BuildCommandSet depCommands;
            var dependencyCalculator = new Unity5DependencyCalculator();
            if (!dependencyCalculator.Convert(commands, out depCommands))
                return false;
            
            DebugPrintCommandSet(ref depCommands);

            // TODO: implement incremental building when LLAPI supports it

            // Write out resource files
            var resourceWriter = new ResourceWriter();
            if (!resourceWriter.Convert(depCommands, settings, out output))
                return false;

            // Archive and compress resource files
            uint[] crc;
            var archiveWriter = new ArchiveWriter();
            if (!archiveWriter.Convert(output, compression, settings.outputFolder, out crc))
                return false;

            // Generate Unity5 compatible manifest files
            string[] manifestfiles;
            var manifestWriter = new Unity5ManifestWriter();
            if (!manifestWriter.Convert(depCommands, output, crc, settings.outputFolder, out manifestfiles))
                return false;
            
            buildTimer.Stop();
            BuildLogger.Log("Build Asset Bundles complete in: {0:c}", buildTimer.Elapsed);

            return true;
        }

        private static void DebugPrintCommandSet(ref BuildCommandSet commandSet)
        {
            // TODO: this debug printing function is ugly as sin, fix it
            var msg = new StringBuilder();
            if (!commandSet.commands.IsNullOrEmpty())
            {
                foreach (var bundle in commandSet.commands)
                {
                    msg.AppendFormat("Bundle: '{0}'\n", bundle.assetBundleName);
                    if (!bundle.explicitAssets.IsNullOrEmpty())
                    {
                        msg.Append("\tExplicit Assets:\n");
                        foreach (var asset in bundle.explicitAssets)
                        {
                            // TODO: Create GUIDToAssetPath that takes GUID struct
                            var addressableName = string.IsNullOrEmpty(asset.address) ? AssetDatabase.GUIDToAssetPath(asset.asset.ToString()) : asset.address;
                            msg.AppendFormat("\t\tAsset: {0} - '{1}'\n", asset.asset, addressableName);
                            if (!asset.includedObjects.IsNullOrEmpty())
                            {
                                msg.Append("\t\t\tIncluded Objects:\n");
                                foreach (var obj in asset.includedObjects)
                                    msg.AppendFormat("\t\t\t\t{0}\n", obj);
                            }

                            if (!asset.referencedObjects.IsNullOrEmpty())
                            {
                                msg.Append("\t\t\tReferenced Objects:\n");
                                foreach (var obj in asset.referencedObjects)
                                    msg.AppendFormat("\t\t\t\t{0}\n", obj);
                            }
                        }
                    }

                    if (!bundle.assetBundleObjects.IsNullOrEmpty())
                    {
                        msg.Append("\tAsset Bundle Objects:\n");
                        foreach (var obj in bundle.assetBundleObjects)
                            msg.AppendFormat("\t\t{0}: {1}\n", obj.serializationIndex, obj.serializationObject);
                    }

                    if (!bundle.assetBundleDependencies.IsNullOrEmpty())
                    {
                        msg.Append("\tAsset Bundle Dependencies:\n");
                        foreach (var dependency in bundle.assetBundleDependencies)
                            msg.AppendFormat("\t\t{0}\n", dependency);
                    }
                    msg.Append("\n");
                }
            }
            BuildLogger.Log(msg);
        }
    }
}