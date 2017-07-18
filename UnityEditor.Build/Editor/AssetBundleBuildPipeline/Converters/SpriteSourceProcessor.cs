using System;
using System.Collections.Generic;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

using AssetInfoMap = System.Collections.Generic.Dictionary<UnityEditor.GUID, UnityEditor.Experimental.Build.AssetBundle.BuildCommandSet.AssetLoadInfo>;
using SpriteRefMap = System.Collections.Generic.Dictionary<UnityEditor.Experimental.Build.AssetBundle.ObjectIdentifier, int>;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class SpriteSourceProcessor : IDataConverter<AssetInfoMap, AssetInfoMap>
    {
        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(AssetInfoMap assetLoadInfo, SpriteRefMap spriteRefCount, bool useCache)
        {
            if (!useCache)
                return new Hash128();
            
            return HashingMethods.CalculateMD5Hash(Version, assetLoadInfo, spriteRefCount);
        }

        public bool Convert(AssetInfoMap assetLoadInfo, out AssetInfoMap output, bool useCache = true)
        {
            var spriteRefCount = new Dictionary<ObjectIdentifier, int>();
            foreach (var assetInfo in assetLoadInfo)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetInfo.Value.asset.ToString());
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType == TextureImporterType.Sprite && !string.IsNullOrEmpty(importer.spritePackingTag))
                    spriteRefCount[assetInfo.Value.includedObjects[0]] = 0;
            }

            Hash128 hash = CalculateInputHash(assetLoadInfo, spriteRefCount, useCache);
            if (useCache && BuildCache.TryLoadCachedResults(hash, out output))
                return true;

            // Mutating the input, this is the only converter that does this
            output = assetLoadInfo;

            foreach (var assetInfo in output)
            {
                if (!string.IsNullOrEmpty(assetInfo.Value.processedScene))
                    continue;

                foreach (var reference in assetInfo.Value.referencedObjects)
                {
                    int refCount = 0;
                    if (!spriteRefCount.TryGetValue(reference, out refCount))
                        continue;

                    // Note: Because pass by value
                    spriteRefCount[reference] = ++refCount;
                }
            }

            foreach (var source in spriteRefCount)
            {
                if (source.Value > 0)
                    continue;

                var assetInfo = output[source.Key.guid];
                assetInfo.includedObjects.Swap(0, assetInfo.includedObjects.Length - 1);
                Array.Resize(ref assetInfo.includedObjects, assetInfo.includedObjects.Length - 1);

                // Note: Because pass by value
                output[source.Key.guid] = assetInfo;
            }

            if (useCache && !BuildCache.SaveCachedResults(hash, output))
                BuildLogger.LogWarning("Unable to cache SpriteSourceProcessor results.");
            return true;
        }
    }
}
