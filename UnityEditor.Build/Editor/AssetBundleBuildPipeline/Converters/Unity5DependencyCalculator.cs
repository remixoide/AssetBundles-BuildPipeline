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
        private const string kUnityDefaultResourcePath = "library/unity default resources";
        private const string kUnityAtlasCachePath = "library/atlascache";

        private static readonly SerializationInfoComparer kSerializationInfoComparer = new SerializationInfoComparer();
        private static readonly ObjectIdentifierComparer kObjectIdentifierComparer = new ObjectIdentifierComparer();

        public uint Version { get { return 1; } }

        private struct AtlasRef
        {
            public int bundle;
            public int asset;
            public int count;

            public AtlasRef(int bundleIndex, int assetIndex)
            {
                bundle = bundleIndex;
                asset = assetIndex;
                count = 0;
            }
        }

        public Hash128 CalculateInputHash(BuildCommandSet input)
        {
            return HashingMethods.CalculateMD5Hash(Version, input);
        }

        public bool Convert(BuildCommandSet input, out BuildCommandSet output, bool useCache = true)
        {
            // If enabled, try loading from cache
            var hash = CalculateInputHash(input);
            if (useCache && LoadFromCache(hash, out output))
                return true;
            
            // Convert inputs
            output = input;

            if (input.commands.IsNullOrEmpty())
                return false;

            // Generate asset lookup
            var assetToBundle = new Dictionary<GUID, string>();
            var spriteSourceRef = new Dictionary<ObjectIdentifier, AtlasRef>();
            for (var i = 0; i < output.commands.Length; i++)
            {
                if (string.IsNullOrEmpty(output.commands[i].assetBundleName))
                {
                    BuildLogger.LogError("Unable to continue dependency calculation. Asset bundle name is null or empty!");
                    return false;
                }

                if (output.commands[i].explicitAssets.IsNullOrEmpty())
                {
                    BuildLogger.LogError("Asset bundle '{0}' does not have any explicit assets defined.", output.commands[i].assetBundleName);
                    continue;
                }

                for (var j = 0; j < output.commands[i].explicitAssets.Length; j++)
                {
                    string bundleName;
                    if (assetToBundle.TryGetValue(output.commands[i].explicitAssets[j].asset, out bundleName))
                    {
                        if (bundleName == output.commands[i].assetBundleName)
                            continue;
                        BuildLogger.LogError("Unable to continue dependency calculation. Asset '{0}' added to multiple bundles: '{1}', ,'{2}'!", 
                            output.commands[i].explicitAssets[j].asset, bundleName, output.commands[i].assetBundleName);
                        return false;
                    }
                    assetToBundle.Add(output.commands[i].explicitAssets[j].asset, output.commands[i].assetBundleName);

                    if (IsAssetSprite(ref output.commands[i].explicitAssets[j]))
                    {
                        spriteSourceRef[output.commands[i].explicitAssets[j].includedObjects[0]] = new AtlasRef(i, j);
                    }
                }
            }

            var assetBundleDependencies = new HashSet<string>();
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

                    AtlasRef refCount;
                    if (spriteSourceRef.TryGetValue(output.commands[i].assetBundleObjects[j].serializationObject, out refCount) && refCount.bundle != i)
                    {
                        refCount.count++;
                        spriteSourceRef[output.commands[i].assetBundleObjects[j].serializationObject] = refCount;
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

                    assetBundleDependencies.Add(dependency);
                    output.commands[i].assetBundleObjects.Swap(j, --end);
                }
                Array.Resize(ref output.commands[i].assetBundleObjects, end);
                // Sorting is unnecessary - just makes it more human readable
                Array.Sort(output.commands[i].assetBundleObjects, kSerializationInfoComparer);
                output.commands[i].assetBundleDependencies = assetBundleDependencies.OrderBy(s => s).ToArray();
                assetBundleDependencies.Clear();
            }

            // Remove source textures if no references
            foreach (var refCount in spriteSourceRef)
            {
                if (refCount.Value.count != 0)
                    continue;

                int i = refCount.Value.bundle;
                int j = refCount.Value.asset;
                int end = output.commands[i].explicitAssets[j].includedObjects.Length;
                output.commands[i].explicitAssets[j].includedObjects.Swap(0, end - 1);
                Array.Resize(ref output.commands[i].explicitAssets[j].includedObjects, end - 1);
                Array.Sort(output.commands[i].explicitAssets[j].includedObjects, kObjectIdentifierComparer);

                end = output.commands[refCount.Value.bundle].assetBundleObjects.Length;
                for (j = 0; j < end; j++)
                {
                    if (output.commands[i].assetBundleObjects[j].serializationObject != refCount.Key)
                        continue;
                    
                    output.commands[i].assetBundleObjects.Swap(j, --end);
                }
                Array.Resize(ref output.commands[i].assetBundleObjects, end);
                Array.Sort(output.commands[i].assetBundleObjects, kSerializationInfoComparer);
            }
            
            // Cache results
            if (useCache)
                SaveToCache(hash, output);
            return true;
        }

        private bool LoadFromCache(Hash128 hash, out BuildCommandSet output)
        {
            if (BuildCache.TryLoadCachedResults(hash, out output))
                return true;
            return false;
        }

        private void SaveToCache(Hash128 hash, BuildCommandSet output)
        {
            BuildCache.SaveCachedResults(hash, output);
        }

        private bool IsAssetSprite(ref BuildCommandSet.AssetLoadInfo asset)
        {
            if (asset.referencedObjects.IsNullOrEmpty())
                return false;
            if (string.IsNullOrEmpty(asset.referencedObjects[0].filePath))
                return false;
            return asset.referencedObjects[0].filePath.StartsWith(kUnityAtlasCachePath);
        }
    }
}