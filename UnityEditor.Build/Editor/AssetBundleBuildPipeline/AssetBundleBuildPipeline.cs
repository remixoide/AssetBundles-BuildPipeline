using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
            var hash = packer.CalculateInputHash(input, settings.target);
            var cachedPath = GetPathForCachedResults(hash, "Unity5Packer", settings.outputFolder);
            if (!TryLoadCachedResults(cachedPath, out commands))
            {
                if (!packer.Convert(input, settings.target, out commands))
                    return;
                SaveCachedResults(cachedPath, commands);
            }

            //DebugPrintCommandSet(ref commands);

            // Calculate dependencies
            BuildCommandSet depCommands;
            var dependencyCalculator = new Unity5DependencyCalculator();
            hash = dependencyCalculator.CalculateInputHash(commands);
            cachedPath = GetPathForCachedResults(hash, "Unity5DependencyCalculator", settings.outputFolder);
            if (!TryLoadCachedResults(cachedPath, out depCommands))
            {
                if (!dependencyCalculator.Convert(commands, out depCommands))
                    return;
                SaveCachedResults(cachedPath, depCommands);
                // TODO: BuildCommandSet.Command.assetBundleDependencies serialization
            }
            
            //DebugPrintCommandSet(ref commands);

            // TODO: implement incremental building when LLAPI supports it

            // Write out resource files
            BuildOutput output;
            var resourceWriter = new ResourceWriter();
            hash = resourceWriter.CalculateInputHash(depCommands, settings);
            cachedPath = GetPathForCachedResults(hash, "ResourceWriter", settings.outputFolder);
            if (!TryLoadCachedResults(cachedPath, out output))
            {
                if (!resourceWriter.Convert(depCommands, settings, out output))
                    return;
                SaveCachedResults(cachedPath, output);
            }

            // Archive and compress resource files
            uint[] crc;
            var archiveWriter = new ArchiveWriter();
            hash = archiveWriter.CalculateInputHash(output, compression, settings.outputFolder);
            cachedPath = GetPathForCachedResults(hash, "ArchiveWriter", settings.outputFolder);
            if (!TryLoadCachedResults(cachedPath, out crc))
            {
                if (!archiveWriter.Convert(output, compression, settings.outputFolder, out crc))
                    return;
                SaveCachedResults(cachedPath, crc);
            }

            // Generate Unity5 compatible manifest files
            string[] manifestfiles;
            var manifestWriter = new Unity5ManifestWriter();
            hash = manifestWriter.CalculateInputHash(commands, output, crc, settings.outputFolder);
            cachedPath = GetPathForCachedResults(hash, "Unity5ManifestWriter", settings.outputFolder);
            if (!TryLoadCachedResults(cachedPath, out manifestfiles))
            {
                if (!manifestWriter.Convert(commands, output, crc, settings.outputFolder, out manifestfiles))
                    return;
                SaveCachedResults(cachedPath, manifestfiles);
            }
        }

        public static string GetPathForCachedResults(long hash, string type, string folderPath)
        {
            return string.Format("{0}/{1}_{2:x16}.blob", folderPath, type, hash);
        }

        public static bool TryLoadCachedResults<T>(string filePath, out T results)
        {
            if (!File.Exists(filePath))
            {
                results = default(T);
                return false;
            }

            try
            {
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    results = (T) formatter.Deserialize(stream);
            }
            catch (Exception)
            {
                results = default(T);
                return false;
            }
            return true;
        }

        public static bool SaveCachedResults<T>(string filePath, T results)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                    formatter.Serialize(stream, results);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static void DebugPrintCommandSet(ref BuildCommandSet commandSet)
        {
            // TODO: this debug printing function is ugly as sin, fix it
            var msg = new StringBuilder();
            if (commandSet.commands.IsNullOrEmpty())
            {
                foreach (var bundle in commandSet.commands)
                {
                    msg.AppendFormat("Bundle: '{0}'\n", bundle.assetBundleName);
                    if (bundle.explicitAssets.IsNullOrEmpty())
                    {
                        msg.Append("\tExplicit Assets:\n");
                        foreach (var asset in bundle.explicitAssets)
                        {
                            // TODO: Create GUIDToAssetPath that takes GUID struct
                            var addressableName = string.IsNullOrEmpty(asset.address) ? AssetDatabase.GUIDToAssetPath(asset.asset.ToString()) : asset.address;
                            msg.AppendFormat("\t\tAsset: {0} - '{1}'\n", asset.asset, addressableName);
                            if (asset.includedObjects.IsNullOrEmpty())
                            {
                                msg.Append("\t\t\tIncluded Objects:\n");
                                foreach (var obj in asset.includedObjects)
                                    msg.AppendFormat("\t\t\t\t{0}\n", obj);
                            }

                            if (asset.referencedObjects.IsNullOrEmpty())
                            {
                                msg.Append("\t\t\tReferenced Objects:\n");
                                foreach (var obj in asset.referencedObjects)
                                    msg.AppendFormat("\t\t\t\t{0}\n", obj);
                            }
                        }
                    }

                    if (bundle.assetBundleObjects.IsNullOrEmpty())
                    {
                        msg.Append("\tAsset Bundle Objects:\n");
                        foreach (var obj in bundle.assetBundleObjects)
                            msg.AppendFormat("\t\t{0}: {1}\n", obj.serializationIndex, obj.serializationObject);
                    }

                    if (bundle.assetBundleDependencies.IsNullOrEmpty())
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