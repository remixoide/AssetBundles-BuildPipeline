using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ResourceWriter : IDataConverter<BuildCommandSet, BuildSettings, BuildOutput>
    {
        public long CalculateInputHash(BuildCommandSet commandSet, BuildSettings settings)
        {
            // TODO: Figure out if explicitAssets hash is not enough and we need use assetBundleObjects instead
            var assetHashes = new List<string>();
            if (!commandSet.commands.IsNullOrEmpty())
            {
                for (var i = 0; i < commandSet.commands.Length; i++)
                {
                    if (commandSet.commands[i].explicitAssets.IsNullOrEmpty())
                        continue;

                    for (var k = 0; k < commandSet.commands[i].explicitAssets.Length; k++)
                    {
                        // TODO: Create GUIDToAssetPath that takes GUID struct
                        var path = AssetDatabase.GUIDToAssetPath(commandSet.commands[i].explicitAssets[k].asset.ToString());
                        var hash = AssetDatabase.GetAssetDependencyHash(path);
                        // TODO: Figure out a way to not create a string for every hash.
                        assetHashes.Add(hash.ToString());
                    }
                }
            }

            return HashingMethods.CalculateMD5Hash(commandSet, settings.target, settings.group);
        }

        public bool Convert(BuildCommandSet commandSet, BuildSettings settings, out BuildOutput output)
        {
            foreach (var bundle in commandSet.commands)
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

            output = BuildInterface.WriteResourceFiles(commandSet, settings);
            // TODO: Change this return based on if WriteResourceFiles was successful or not - Need public BuildReporting
            return true;
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
