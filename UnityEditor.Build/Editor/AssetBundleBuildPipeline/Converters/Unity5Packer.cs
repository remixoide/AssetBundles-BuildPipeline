using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class Unity5Packer : IDataConverter<BuildInput, BuildTarget, BuildCommandSet>
    {
        private static readonly SerializationInfoComparer kCompareer = new SerializationInfoComparer();

        public int GetInputHash(BuildInput input, BuildTarget target)
        {
            return input.GetHashCode() ^ target.GetHashCode();
        }

        public bool Convert(BuildInput input, BuildTarget target, out BuildCommandSet output)
        {
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
                    output.commands[o].explicitAssets[j].referencedObjects = BuildInterface.GetPlayerDependenciesForObjects(output.commands[i].explicitAssets[j].includedObjects, target);

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
                // Sorting is unneccessary - just makes it more human readable
                Array.Sort(output.commands[o].assetBundleObjects, kCompareer);
            }
            Array.Resize(ref output.commands, o + 1);

            return true;
        }

        private long CalculateSerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            long hash;
            using (var md4 = new MD4())
            {
                byte[] bytes;
                if (objectID.fileType == FileType.MetaAssetType || objectID.fileType == FileType.SerializedAssetType)
                {
                    // TODO: Variant info
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
                hash = BitConverter.ToInt64(md4.Hash, 0);
            }
            return hash;
        }
    }
}
