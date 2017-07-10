using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
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
            settings.outputFolder = "AssetBundles/";// + settings.target;
            return settings;
        }
        public static ScriptCompilationSettings GenerateBuildPlayerSettings()
        {
            var settings = new ScriptCompilationSettings();
            settings.target = EditorUserBuildSettings.activeBuildTarget;
            settings.targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            settings.outputFolder = "AssetBundles/";// + settings.target;
            return settings;
        }

        [MenuItem("AssetBundles/Prepare Test Scene", priority = 1)]
        public static void PrepareTestScene()
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            var settings = GenerateBuildSettings();
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var stream = new FileStream(@"C:\Projects\AssetBundlesHLAPI\TypeDB", FileMode.Open, FileAccess.Read))
                settings.typeDB = (TypeDB)formatter.Deserialize(stream);

            var compression = BuildCompression.DefaultUncompressed;

            SceneLoadInfo[] sceneInfo = new SceneLoadInfo[2];
            var prepareScene = new PrepareScene();
            var success = prepareScene.Convert("Assets/Debug/TestScene1.unity", settings, out sceneInfo[0], false);
            success &= prepareScene.Convert("Assets/Debug/TestScene2.unity", settings, out sceneInfo[1], false);

            BuildCommandSet commands;
            var packer = new ScenePacker();
            success &= packer.Convert(sceneInfo, out commands, false);

            //DebugPrintCommandSet(ref commands);

            BuildOutput output;
            var resourceWriter = new ResourceWriter();
            success &= resourceWriter.Convert(commands, settings, out output, false);

            output.results[0].resourceFiles = output.results[0].resourceFiles.Concat(sceneInfo[0].resourceFiles).Concat(sceneInfo[1].resourceFiles).ToArray();

            uint[] crc;
            var archiveWriter = new ArchiveWriter();
            success &= archiveWriter.Convert(output, compression, settings.outputFolder, out crc, false);

            buildTimer.Stop();
            BuildLogger.Log("Prepare Test Scene {0} in: {1:c}", success ? "completed" : "failed", buildTimer.Elapsed);
        }


        [MenuItem("AssetBundles/Build Asset Bundles", priority = 0)]
        public static void BuildAssetBundles()
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            var success = BuildAssetBundles_Internal();
            
            buildTimer.Stop();
            BuildLogger.Log("Build Asset Bundles {0} in: {1:c}", success ? "completed" : "failed", buildTimer.Elapsed);
        }

        private static bool BuildAssetBundles_Internal()
        {
            var input = BuildInterface.GenerateBuildInput();
            var settings = GenerateBuildSettings();
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var stream = new FileStream(@"C:\Projects\AssetBundlesHLAPI\TypeDB", FileMode.Open, FileAccess.Read))
                settings.typeDB = (TypeDB)formatter.Deserialize(stream);

            var compression = BuildCompression.DefaultUncompressed;

            // Rebuild sprite atlas cache for correct dependency calculation & writing
            Packer.RebuildAtlasCacheIfNeeded(settings.target, true, Packer.Execution.Normal);

            // Generate command set
            BuildCommandSet commands;
            var packer = new Unity5Packer();
            if (!packer.Convert(input, settings.target, settings.typeDB, out commands))
                return false;

            //DebugPrintCommandSet(ref commands);

            // Calculate assetBundleDependencies
            BuildCommandSet depCommands;
            var dependencyCalculator = new Unity5DependencyCalculator();
            if (!dependencyCalculator.Convert(commands, out depCommands))
                return false;

            //DebugPrintCommandSet(ref depCommands);

            // TODO: implement incremental building when LLAPI supports it

            // Write out resource files
            BuildOutput output;
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
            return true;
        }

        private static void DebugPrintCommandSet(ref BuildCommandSet commandSet)
        {
            var stream = new StreamWriter("C:/Projects/AssetBundlesHLAPI/DebugPrint.log", false);
            // TODO: this debug printing function is ugly as sin, fix it
            //var msg = new StringBuilder();
            if (!commandSet.commands.IsNullOrEmpty())
            {
                foreach (var bundle in commandSet.commands)
                {
                    stream.Write("Bundle: '{0}'\n", bundle.assetBundleName);
                    if (!bundle.explicitAssets.IsNullOrEmpty())
                    {
                        stream.Write("\tExplicit Assets:\n");
                        foreach (var asset in bundle.explicitAssets)
                        {
                            // TODO: Create GUIDToAssetPath that takes GUID struct
                            var addressableName = string.IsNullOrEmpty(asset.address) ? AssetDatabase.GUIDToAssetPath(asset.asset.ToString()) : asset.address;
                            stream.Write("\t\tAsset: {0} - '{1}'\n", asset.asset, addressableName);
                            if (!asset.includedObjects.IsNullOrEmpty())
                            {
                                stream.Write("\t\t\tIncluded Objects:\n");
                                foreach (var obj in asset.includedObjects)
                                    stream.Write("\t\t\t\t{0}\n", obj);
                            }

                            if (!asset.referencedObjects.IsNullOrEmpty())
                            {
                                stream.Write("\t\t\tReferenced Objects:\n");
                                foreach (var obj in asset.referencedObjects)
                                    stream.Write("\t\t\t\t{0}\n", obj);
                            }
                        }
                    }

                    if (!bundle.assetBundleObjects.IsNullOrEmpty())
                    {
                        stream.Write("\tAsset Bundle Objects:\n");
                        foreach (var obj in bundle.assetBundleObjects)
                           stream.Write("\t\t{0}: {1}\n", obj.serializationIndex, obj.serializationObject);
                    }

                    if (!bundle.assetBundleDependencies.IsNullOrEmpty())
                    {
                        stream.Write("\tAsset Bundle Dependencies:\n");
                        foreach (var dependency in bundle.assetBundleDependencies)
                            stream.Write("\t\t{0}\n", dependency);
                    }
                    stream.Write("\n");
                }
            }
            //BuildLogger.Log(msg);
            stream.Close();
        }
    }
}