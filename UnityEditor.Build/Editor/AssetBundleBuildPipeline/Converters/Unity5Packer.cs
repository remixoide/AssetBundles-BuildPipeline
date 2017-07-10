using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build.Cache;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class Unity5Packer : IDataConverter<BuildInput, BuildTarget, TypeDB, BuildCommandSet>
    {
        public uint Version { get { return 1; } }

        private static readonly SerializationInfoComparer kCompareer = new SerializationInfoComparer();
        
        public Hash128 CalculateInputHash(BuildInput input, BuildTarget target)
        {
            var assetHashes = new List<string>();
            if (!input.definitions.IsNullOrEmpty())
            {
                for (var i = 0; i < input.definitions.Length; i++)
                {
                    if (input.definitions[i].explicitAssets.IsNullOrEmpty())
                        continue;

                    for (var k = 0; k < input.definitions[i].explicitAssets.Length; k++)
                    {
                        // TODO: Create GUIDToAssetPath that takes GUID struct
                        var path = AssetDatabase.GUIDToAssetPath(input.definitions[i].explicitAssets[k].asset.ToString());
                        var hash = AssetDatabase.GetAssetDependencyHash(path);
                        // TODO: Figure out a way to not create a string for every hash.
                        assetHashes.Add(hash.ToString());
                    }
                }
            }

            return HashingMethods.CalculateMD5Hash(Version, input, target, assetHashes);
        }

        public bool Convert(BuildInput input, BuildTarget target, TypeDB typeDB, out BuildCommandSet output, bool useCache = true)
        {
            // If enabled, try loading from cache
            var hash = CalculateInputHash(input, target);
            if (useCache && LoadFromCache(hash, out output))
                return true;
            
            // Convert inputs
            output = new BuildCommandSet();

            if (input.definitions.IsNullOrEmpty())
                return false;
            
            var o = -1;
            output.commands = new BuildCommandSet.Command[input.definitions.Length];
            for (var i = 0; i < input.definitions.Length; i++)
            {
                // If this definition has no assets, it's empty and we don't want to write anything out
                if (input.definitions[i].explicitAssets.IsNullOrEmpty())
                {
                    BuildLogger.LogError("Asset bundle '{0}' does not have any explicit assets defined.", input.definitions[i].assetBundleName);
                    continue;
                }
                o++;
                
                var allObjectIDs = new HashSet<ObjectIdentifier>();
                output.commands[o].assetBundleName = input.definitions[i].assetBundleName;
                output.commands[o].explicitAssets = new BuildCommandSet.AssetLoadInfo[input.definitions[i].explicitAssets.Length];
                for (var j = 0; j < input.definitions[i].explicitAssets.Length; j++)
                {
                    output.commands[o].explicitAssets[j].asset = input.definitions[i].explicitAssets[j].asset;
                    output.commands[o].explicitAssets[j].address = string.IsNullOrEmpty(input.definitions[i].explicitAssets[j].address) ? 
                        AssetDatabase.GUIDToAssetPath(input.definitions[i].explicitAssets[j].asset.ToString()) : input.definitions[i].explicitAssets[j].address;
                    output.commands[o].explicitAssets[j].includedObjects = BuildInterface.GetPlayerObjectIdentifiersInAsset(input.definitions[i].explicitAssets[j].asset, target);
                    output.commands[o].explicitAssets[j].referencedObjects = BuildInterface.GetPlayerDependenciesForObjects(output.commands[i].explicitAssets[j].includedObjects, target, typeDB);

                    allObjectIDs.UnionWith(output.commands[i].explicitAssets[j].includedObjects);
                    allObjectIDs.UnionWith(output.commands[i].explicitAssets[j].referencedObjects);
                }
                
                var k = 0;
                output.commands[o].assetBundleObjects = new BuildCommandSet.SerializationInfo[allObjectIDs.Count];
                foreach (var objectID in allObjectIDs)
                {
                    output.commands[o].assetBundleObjects[k].serializationObject = objectID;
                    output.commands[o].assetBundleObjects[k].serializationIndex = CalculateSerializationIndexFromObjectIdentifier(objectID);
                    k++;
                }
                // Sorting is unnecessary - just makes it more human readable
                Array.Sort(output.commands[o].assetBundleObjects, kCompareer);
            }
            Array.Resize(ref output.commands, o + 1);
            
            // Cache results
            if (useCache)
                SaveToCache(hash, output);
            return true;
        }

        private bool LoadFromCache(Hash128 hash, out BuildCommandSet output)
        {
            return BuildCache.TryLoadCachedResults(hash, out output);
        }

        private void SaveToCache(Hash128 hash, BuildCommandSet output)
        {
            BuildCache.SaveCachedResults(hash, output);
        }

        public static long CalculateSerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            byte[] bytes;
            var md4 = MD4.Create();
            if (objectID.fileType == FileType.MetaAssetType || objectID.fileType == FileType.SerializedAssetType)
            {
                // TODO: Variant info
                // NOTE: ToString() required as unity5 used the guid as a string to hash
                bytes = Encoding.ASCII.GetBytes(objectID.guid.ToString());
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
                bytes = BitConverter.GetBytes((int) objectID.fileType);
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            }
            // Or path
            else
            {
                bytes = Encoding.ASCII.GetBytes(objectID.filePath);
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            }

            bytes = BitConverter.GetBytes(objectID.localIdentifierInFile);
            md4.TransformFinalBlock(bytes, 0, bytes.Length);
            var hash = BitConverter.ToInt64(md4.Hash, 0);
            return hash;
        }
    }
}
