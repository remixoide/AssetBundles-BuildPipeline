using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
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

        public static bool GenerateCommandSet(BuildSettings settings, BuildInput input, out BuildCommandSet output)
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            output = new BuildCommandSet();

            settings.outputFolder = Path.Combine(settings.outputFolder, "_resources");
            Directory.CreateDirectory(settings.outputFolder);

            // Rebuild sprite atlas cache for correct dependency calculation & writing
            Packer.RebuildAtlasCacheIfNeeded(settings.target, true, Packer.Execution.Normal);
            
            var commandList = new List<BuildCommandSet.Command>();
            
            var prepareScene = new PrepareScene();
            var scenePacker = new ScenePacker();
            
            // Generate command set for scenes
            for(var x = 0; x < input.definitions.Length; x++)
            {
                var def = input.definitions[x];
                if(def.explicitAssets.Length > 0)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(def.explicitAssets[0].asset.ToString());
                    if(AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset))
                    {
                        SceneLoadInfo sceneInfo;
                        if(!prepareScene.Convert(assetPath, settings, out sceneInfo, false))
                        {
                            UnityEngine.Debug.LogError("Scene build of " + assetPath + " failed in PrepareScene");
                            continue;
                        }
                        
                        BuildCommandSet sceneCommands;
                        if(!scenePacker.Convert(sceneInfo, out sceneCommands, false))
                        {
                            UnityEngine.Debug.LogError("Scene build of " + assetPath + " failed in ScenePacker");
                            continue;
                        }
                        
                        // Fix up asset bundle name
                        sceneCommands.commands[0].assetBundleName = def.assetBundleName;
                        
                        commandList.AddRange(sceneCommands.commands);
                        ArrayUtility.RemoveAt<BuildInput.Definition>(ref input.definitions, x--);
                    }
                }
            }
            
            // Generate command set for loose bundles
            BuildCommandSet assetCommands;
            var packer = new Unity5Packer();
            if (!packer.Convert(input, settings.target, out assetCommands))
                return false;
            
            // Combine scene and loose bundle commands
            if(assetCommands.commands.Length > 0)
                commandList.AddRange(assetCommands.commands);
            
            var allCommands = new BuildCommandSet();
            allCommands.commands = commandList.ToArray();
            
            // Calculate assetBundleDependencies
            var dependencyCalculator = new Unity5DependencyCalculator();
            if (!dependencyCalculator.Convert(allCommands, out output))
                return false;

            return true;
        }

        public static bool ExecuteCommandSet(BuildSettings settings, BuildCommandSet commands, out BuildOutput output)
        {
            output = new BuildOutput();
            output.results = new BuildOutput.Result[0];

            if(commands.commands.IsNullOrEmpty())
                return true;

            var compression = BuildCompression.DefaultUncompressed;

            var bundleOutputFolder = settings.outputFolder;
            settings.outputFolder = Path.Combine(settings.outputFolder, "_resources");
            Directory.CreateDirectory(settings.outputFolder);
            //DebugPrintCommandSet(ref depCommands);

            // TODO: implement incremental building when LLAPI supports it

            // Write out resource files
            var resourceWriter = new ResourceWriter();
            if (!resourceWriter.Convert(commands, settings, out output))
                return false;

            // Archive and compress resource files
            uint[] crc;
            var archiveWriter = new ArchiveWriter();
            if (!archiveWriter.Convert(output, compression, bundleOutputFolder, out crc))
                return false;

            // Generate Unity5 compatible manifest files
            string[] manifestfiles;
            var manifestWriter = new Unity5ManifestWriter();
            if (!manifestWriter.Convert(commands, output, crc, bundleOutputFolder, out manifestfiles))
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