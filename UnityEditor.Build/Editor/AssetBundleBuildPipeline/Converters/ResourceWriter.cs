using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Cache;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ResourceWriter : IDataConverter<BuildCommandSet, BuildSettings, BuildOutput>
    {
        public uint Version { get { return 1; } }

        public Hash128 CalculateInputHash(BuildCommandSet commands, BuildSettings settings)
        {
            // TODO: Figure out if explicitAssets hash is not enough and we need use assetBundleObjects instead
            var assetHashes = new List<string>();
            if (!commands.commands.IsNullOrEmpty())
            {
                for (var i = 0; i < commands.commands.Length; i++)
                {
                    if (commands.commands[i].explicitAssets.IsNullOrEmpty())
                        continue;

                    for (var k = 0; k < commands.commands[i].explicitAssets.Length; k++)
                    {
                        // TODO: Create GUIDToAssetPath that takes GUID struct
                        var path = AssetDatabase.GUIDToAssetPath(commands.commands[i].explicitAssets[k].asset.ToString());
                        var hash = AssetDatabase.GetAssetDependencyHash(path);
                        // TODO: Figure out a way to not create a string for every hash.
                        assetHashes.Add(hash.ToString());
                    }
                }
            }

            return HashingMethods.CalculateMD5Hash(Version, commands, settings.target, settings.group, settings.editorBundles, assetHashes);
        }

        public bool Convert(BuildCommandSet commands, BuildSettings settings, out BuildOutput output, bool useCache = true)
        {
            // If enabled, try loading from cache
            var hash = CalculateInputHash(commands, settings);
            if (useCache && LoadFromCache(hash, settings.outputFolder, out output))
                return true;

            // Convert inputs
            foreach (var bundle in commands.commands)
            {
                // TODO: Reduce copying of value tyeps
                if (ValidateCommand(bundle))
                    continue;

                output = new BuildOutput();
                return false;
            }

            // TODO: Validate settings

            // TODO: Prepare settings.outputFolder
            Directory.CreateDirectory(settings.outputFolder);

            output = BuildInterface.WriteResourceFiles(commands, settings);
            
            // Cache results
            if (useCache)
                SaveToCache(hash, output, settings.outputFolder);
            // TODO: Change this return based on if WriteResourceFiles was successful or not - Need public BuildReporting
            return true;
        }

        private bool LoadFromCache(Hash128 hash, string outputFolder, out BuildOutput output)
        {
            string rootCachePath;
            string[] artifactPaths;
            
            if (BuildCache.TryLoadCachedResultsAndArtifacts(hash, out output, out artifactPaths, out rootCachePath))
            {
                // TODO: Prepare settings.outputFolder
                Directory.CreateDirectory(outputFolder);

                foreach (var artifact in artifactPaths)
                    File.Copy(artifact, artifact.Replace(rootCachePath, outputFolder), true);
                return true;
            }
            return false;
        }

        private void SaveToCache(Hash128 hash, BuildOutput output, string outputFolder)
        {
            var artifacts = new List<string>();
            for (var i = 0; i < output.results.Length; i++)
            {
                for (var j = 0; j < output.results[i].resourceFiles.Length; j++)
                    artifacts.Add(Path.GetFileName(output.results[i].resourceFiles[j].fileName));
            }
            BuildCache.SaveCachedResultsAndArtifacts(hash, output, artifacts.ToArray(), outputFolder);
        }

        private bool ValidateCommand(BuildCommandSet.Command bundle)
        {
            if (bundle.explicitAssets.IsNullOrEmpty())
                BuildLogger.LogWarning("Asset bundle '{0}' does not have any explicit assets defined.", bundle.assetBundleName);
            else
            {
                foreach (var asset in bundle.explicitAssets)
                {
                    if (string.IsNullOrEmpty(asset.address))
                        BuildLogger.LogWarning("Asset bundle '{0}' has an asset '{1}' with an empty addressable name.", bundle.assetBundleName, asset.asset);

                    if (asset.includedObjects.IsNullOrEmpty() && asset.includedObjects.IsNullOrEmpty())
                        BuildLogger.LogWarning("Asset bundle '{0}' has an asset '{1}' with no objects to load.", bundle.assetBundleName, asset.asset);
                }
            }

            if (bundle.assetBundleObjects.IsNullOrEmpty())
                BuildLogger.LogWarning("Asset bundle '{0}' does not have any serialized objects.", bundle.assetBundleName);
            else
            {
                var localIDs = new HashSet<long>();
                foreach (var serializedInfo in bundle.assetBundleObjects)
                {
                    if (serializedInfo.serializationIndex == 1)
                    {
                        BuildLogger.LogError("Unable to continue resource writing. Asset bundle '{0}' has a serialized object with index of '1'. This is a reserved index and can not be used.",
                            bundle.assetBundleName);
                        return false;
                    }

                    if (!string.IsNullOrEmpty(bundle.processedScene) && serializedInfo.serializationIndex == 2)
                    {
                        BuildLogger.LogError("Unable to continue resource writing. Asset bundle '{0}' has a serialized object with index of '1'. This is a reserved index and can not be used.",
                            bundle.assetBundleName);
                        return false;
                    }

                    if (!localIDs.Add(serializedInfo.serializationIndex))
                    {
                        BuildLogger.LogError("Unable to continue resource writing. Asset bundle '{0}' has multiple serialized objects with the same index '{1}'. Each serialized object must have a unique index.",
                            bundle.assetBundleName, serializedInfo.serializationIndex);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
