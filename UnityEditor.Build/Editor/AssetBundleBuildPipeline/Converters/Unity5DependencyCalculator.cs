using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Cache;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class Unity5DependencyCalculator : IDataConverter<BuildCommandSet, BuildCommandSet>
    {
        // TODO: Need to do some more sprite handling work
        private const string kUnityDefaultResourcePath = "library/unity default resources";

        private static readonly SerializationInfoComparer kCompareer = new SerializationInfoComparer();

        public Hash128 CalculateInputHash(BuildCommandSet input)
        {
            return HashingMethods.CalculateMD5Hash(input);
        }

        public bool Convert(BuildCommandSet input, out BuildCommandSet output)
        {
            output = input;

            if (input.commands.IsNullOrEmpty())
                return false;

            // Generate asset lookup
            var assetToBundle = new Dictionary<GUID, string>();
            foreach (var bundle in output.commands)
            {
                if (string.IsNullOrEmpty(bundle.assetBundleName))
                {
                    BuildLogger.LogError("Unable to continue dependency calcualtion. Asset bundle name is null or empty!");
                    return false;
                }

                if (bundle.explicitAssets.IsNullOrEmpty())
                {
                    BuildLogger.LogError("Asset bundle '{0}' does not have any explicit assets defined.", bundle.assetBundleName);
                    continue;
                }

                foreach (var asset in bundle.explicitAssets)
                {
                    string bundleName;
                    if (assetToBundle.TryGetValue(asset.asset, out bundleName))
                    {
                        if (bundleName == bundle.assetBundleName)
                            continue;
                        BuildLogger.LogError("Unable to continue dependency calcualtion. Asset '{0}' added to multiple bundles: '{1}', ,'{2}'!", asset.asset, bundleName, bundle.assetBundleName);
                        return false;
                    }
                    assetToBundle.Add(asset.asset, bundle.assetBundleName);
                }
            }

            var dependencies = new HashSet<string>();
            for (var i = 0; i < output.commands.Length; i++)
            {
                if (output.commands[i].assetBundleObjects.IsNullOrEmpty())
                {
                    BuildLogger.LogWarning("Asset bundle '{0}' does not have any serialized objects.", output.commands[i].assetBundleName);
                    continue;
                }

                var j = 0;
                var end = output.commands[i].assetBundleObjects.Length;
                while (j < end)
                {
                    if (output.commands[i].assetBundleObjects[j].serializationObject.filePath == kUnityDefaultResourcePath)
                    {
                        output.commands[i].assetBundleObjects.Swap(j, --end);
                        continue;
                    }

                    string dependency;
                    if (!assetToBundle.TryGetValue(output.commands[i].assetBundleObjects[j].serializationObject.guid, out dependency))
                    {
                        j++;
                        continue;
                    }

                    if (dependency == output.commands[i].assetBundleName)
                    {
                        j++;
                        continue;
                    }

                    dependencies.Add(dependency);
                    output.commands[i].assetBundleObjects.Swap(j, --end);
                }
                Array.Resize(ref output.commands[i].assetBundleObjects, end);
                // Sorting is unneccessary - just makes it more human readable
                Array.Sort(output.commands[i].assetBundleObjects, kCompareer);
                output.commands[i].assetBundleDependencies = dependencies.OrderBy(s => s).ToArray();
                dependencies.Clear();
            }

            return true;
        }

        public bool LoadFromCacheOrConvert(BuildCommandSet input, out BuildCommandSet output)
        {
            var hash = CalculateInputHash(input);
            if (BuildCache.TryLoadCachedResults(hash, out output))
                return true;

            if (!Convert(input, out output))
                return false;

            BuildCache.SaveCachedResults(hash, output);
            return true;
        }
    }
}